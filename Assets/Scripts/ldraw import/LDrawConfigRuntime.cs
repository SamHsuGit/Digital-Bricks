using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace LDraw
{
    [CreateAssetMenu(fileName = "LDrawConfigRuntime", menuName = "Scriptables/LDrawConfigRuntime", order = 1)]
    public class LDrawConfigRuntime : ScriptableObject
    {
        [SerializeField] public string _BasePartsPath;
        [SerializeField] public string _ModelsPath;
        [SerializeField] public string _ColorConfigPath;
        [SerializeField] private string _MaterialsPath;
        [SerializeField] private string _MeshesPath;
        [SerializeField] private float _Scale;
        [SerializeField] private Material _DefaultOpaqueMaterial;
        [SerializeField] private Material _DefaultTransparentMaterial;
        private Dictionary<string, string> _Parts;
        private Dictionary<string, string> _Models;
        
        private Dictionary<int, Material> _MainColors;
        private Dictionary<string, Material> _CustomColors;
        private Dictionary<string, string> _ModelFileNames;
        public Material[] materials;
        public Matrix4x4 ScaleMatrix
        {
            get { return Matrix4x4.Scale(new Vector3(_Scale, _Scale, _Scale)); }
        }

        public Material GetColoredMaterial(int code)
        {
            return _MainColors[code];
        }
        public Material GetColoredMaterial(string colorString)
        {
            if (_CustomColors.ContainsKey(colorString))
                return _CustomColors[colorString];

            for (int i = 0; i < materials.Length; i++)
            {
                if (colorString == materials[i].name)
                    _CustomColors.Add(colorString, materials[i]);
                else
                {
                    var mat = new Material(_DefaultOpaqueMaterial);
                    _CustomColors.Add(colorString, mat);
                }
            }

            //var path = _MaterialsPath + colorString + ".mat";
            //if (File.Exists(path))
            //{
            //    _CustomColors.Add(colorString, AssetDatabase.LoadAssetAtPath<Material>(path));
            //}
            //else
            //{
            //    var mat = new Material(_DefaultOpaqueMaterial);
                 
            //    mat.name = colorString;
            //    Color color;
            //    if (ColorUtility.TryParseHtmlString(colorString, out color))
            //        mat.color = color;
                            
            //    AssetDatabase.CreateAsset(mat, path);
            //    AssetDatabase.SaveAssets();
            //    _CustomColors.Add(colorString, mat);
            //}

            return _CustomColors[colorString];
        }
        public string[] ModelFileNames
        {
            get { return _ModelFileNames.Keys.ToArray(); }
        }

        public string GetModelByFileName(string modelFileName)
        {
            return _ModelFileNames[modelFileName];
        }
        public string GetSerializedPart(string name) // returns the name of the part from the file
        {
            try
            {
                name = name.ToLower();
           
                var serialized = _Parts.ContainsKey(name) ? File.ReadAllText(_Parts[name]) : _Models[name]; 
                return serialized;
            }
            catch
            {
                ErrorMessage.Show("ldraw part " + name + " cannot be found. Parts with 'bl' prefix are BrickLink submodels which cannot be read and must be exploded before importing.");
                throw;
            }
        }

        public void InitParts() // makes a list of parts in the ldraw file
        { 
            PrepareModels();
            ParseColors();
            _Parts = new Dictionary<string, string>();
            var files = Directory.GetFiles(_BasePartsPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (!file.Contains(".meta"))
                {
                    string fileName = file.Replace(_BasePartsPath, "").Split('.')[0];
                   
                    if (fileName.Contains("\\"))
                       fileName = fileName.Split('\\')[1];
                    if (!_Parts.ContainsKey(fileName))
                        _Parts.Add(fileName, file);
                }
            }
        }

        private void ParseColors()
        {
            _MainColors = new Dictionary<int, Material>();

            using (StreamReader reader = new StreamReader(_ColorConfigPath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
                    line = regex.Replace(line, " ").Trim();
                    var args = line.Split(' ');

                    bool matched = false;
                    if (args.Length > 1 && args[1] == "!COLOUR")
                    {
                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (args[2] == materials[i].name) // if the material name matches a material in the materials array
                            {
                                _MainColors.Add(int.Parse(args[4]), materials[i]); // add the material to the dictionary
                                matched = true;
                            }
                        }
                        if(!matched)
                            ErrorMessage.Show("Error: Material not found: " + args[2]);
                    }
                }
            }
        }

        private void PrepareModels()
        {
            _ModelFileNames = new Dictionary<string, string>();
            //Debug.Log("SEARCHING FOR MODELS IN " + _ModelsPath);
            var files = Directory.GetFiles(_ModelsPath, "*.*", SearchOption.AllDirectories);
            _Models = new Dictionary<string, string>();
            foreach (var file in files)
            {
                using (StreamReader reader = new StreamReader(file))
                {
                    string line;
                    string filename = String.Empty;

                    bool isFirst = true;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
                        line = regex.Replace(line, " ").Trim();
                        var args = line.Split(' ');
                        if (args.Length  > 1 && args[1] == "FILE")
                        {
                           
                            filename = GetFileName(args, 2);
                            if (isFirst)
                            {
                                _ModelFileNames.Add(Path.GetFileNameWithoutExtension(file), filename);
                                isFirst = false;
                            }
                            
                            if(_Models.ContainsKey(filename))
                                filename = String.Empty;
                            else
                                _Models.Add(filename, String.Empty);
                        }

                        if (!string.IsNullOrEmpty(filename))
                        {
                            _Models[filename] += line + "\n";
                        }
                    } 
                }
                
            }
        }

        //public Mesh GetMesh(string name)
        //{
        //    var path = Path.Combine(_MeshesPath, name + ".asset");
        //    return File.Exists(path) ? AssetDatabase.LoadAssetAtPath<Mesh>(path) : null;
        //}
        //public void SaveMesh(Mesh mesh)
        //{
        //    var path = _MeshesPath;
        //    if (!Directory.Exists(path))
        //    {
        //        Directory.CreateDirectory(path);
        //    }
        //    path = Path.Combine(path, mesh.name + ".asset");
        //    AssetDatabase.CreateAsset(mesh, path);
        //    AssetDatabase.SaveAssets();
        //}
        public static string GetFileName(string[] args, int filenamePos)
        {
            string name = string.Empty;
            for (int i = filenamePos; i < args.Length; i++)
            {
                name += args[i] + ' ';
            }

            return Path.GetFileNameWithoutExtension(name).ToLower();
        }
        public static string GetExtension(string[] args, int filenamePos)
        {
            string name = string.Empty;
            for (int i = filenamePos; i < args.Length; i++)
            {
                name += args[i] + ' ';
            }
         
            return Path.GetExtension(name).Trim();
        }
        private static LDrawConfigRuntime _Instance;

        //public static LDrawConfig1 Instance
        //{
        //    get
        //    {
        //        if (_Instance == null)
        //        {
        //            _Instance = AssetDatabase.LoadAssetAtPath<LDrawConfig1>(ConfigPath);
        //        }

        //        return _Instance;
        //    }
        //}

        public void SetFileNames()
        {
            //if (Application.isEditor)
            //{
            //    _BasePartsPath = "D:/BrickFormers/ldraw/parts/";
            //    _ModelsPath = "D:/BrickFormers/ldraw/models/";
            //    _ColorConfigPath = "D:/BrickFormers/ldraw/LDConfig.ldr";
            //}
            //else
            if(SystemInfo.operatingSystem.Substring(0,3) == "Mac")
            {
                _BasePartsPath = ".app/Contents/Data/Resources/StreamingAssets/ldraw/parts/";
                _ModelsPath = ".app/Contents/Data/Resources/StreamingAssets/ldraw/models/";
                _ColorConfigPath = ".app/Contents/Data/Resources/StreamingAssets/ldraw/LDConfig.ldr";
            }
            {
                _BasePartsPath = Application.streamingAssetsPath + "/ldraw/parts/";
                _ModelsPath = Application.streamingAssetsPath + "/ldraw/models/";
                _ColorConfigPath = Application.streamingAssetsPath + "/ldraw/LDConfig.ldr";
            }
        }

        private void OnEnable()
        {
            SetFileNames();
            InitParts();
        }

        private const string ConfigPath = "Assets/Scripts/ldraw import/LDrawConfigRuntime.asset"; // currently not used in runtime as an object reference was used instead
        public const int DefaultMaterialCode = 16;
    }
}