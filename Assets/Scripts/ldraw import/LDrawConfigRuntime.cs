using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LDraw
{
    [CreateAssetMenu(fileName = "LDrawConfigRuntime", menuName = "Scriptables/LDrawConfigRuntime", order = 1)]
    public class LDrawConfigRuntime : ScriptableObject
    {
        [SerializeField] private string materialsPath;
        [SerializeField] private string meshesPath;
        [SerializeField] private float scale;
        [SerializeField] private Material defaultOpaqueMaterial;
        [SerializeField] private Material defaultTransparentMaterial;
        private Dictionary<string, string> parts;
        private Dictionary<string, string> models;
        
        private Dictionary<int, Material> mainColors;
        private Dictionary<string, Material> customColors;
        private Dictionary<string, string> modelFileNames;
        public Material[] materials;
        public Matrix4x4 ScaleMatrix
        {
            get { return Matrix4x4.Scale(new Vector3(scale, scale, scale)); }
        }

        public Material GetColoredMaterial(int code)
        {
            return mainColors[code];
        }
        public Material GetColoredMaterial(string colorString)
        {
            if (customColors.ContainsKey(colorString))
                return customColors[colorString];

            for (int i = 0; i < materials.Length; i++)
            {
                if (colorString == materials[i].name)
                    customColors.Add(colorString, materials[i]);
                else
                {
                    var mat = new Material(defaultOpaqueMaterial);
                    customColors.Add(colorString, mat);
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

            return customColors[colorString];
        }
        public string[] ModelFileNames
        {
            get { return modelFileNames.Keys.ToArray(); }
        }

        public string GetModelByFileName(string modelFileName)
        {
            return modelFileNames[modelFileName];
        }
        public string GetSerializedPart(string name) // returns the name of the part from the file
        {
            try
            {
                name = name.ToLower();

                if (name.Substring(0, 2).Contains(@"\"))
                {
                    name = name.Substring(3, name.Length);
                }
           
                var serialized = parts.ContainsKey(name) ? File.ReadAllText(parts[name]) : models[name]; 
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
            parts = new Dictionary<string, string>();
            var files = Directory.GetFiles(Settings.BasePartsPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (!file.Contains(".meta"))
                {
                    string fileName = file.Replace(Settings.BasePartsPath, "").Split('.')[0];
                   
                    if (fileName.Contains("\\"))
                       fileName = fileName.Split('\\')[1];
                    if (!parts.ContainsKey(fileName))
                        parts.Add(fileName, file);
                }
            }
        }

        private void ParseColors()
        {
            mainColors = new Dictionary<int, Material>();

            using (StreamReader reader = new StreamReader(Settings.ColorConfigPath))
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
                                mainColors.Add(int.Parse(args[4]), materials[i]); // add the material to the dictionary
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
            modelFileNames = new Dictionary<string, string>();
            //Debug.Log("SEARCHING FOR MODELS IN " + _ModelsPath);
            var files = Directory.GetFiles(Settings.ModelsPath, "*.*", SearchOption.AllDirectories); // MacOS cannot search all directories with Directory.GetFiles so put all ldraw part files into same directory
            models = new Dictionary<string, string>();
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
                                modelFileNames.Add(Path.GetFileNameWithoutExtension(file), filename);
                                isFirst = false;
                            }
                            
                            if(models.ContainsKey(filename))
                                filename = String.Empty;
                            else
                                models.Add(filename, String.Empty);
                        }

                        if (!string.IsNullOrEmpty(filename))
                        {
                            models[filename] += line + "\n";
                        }
                    } 
                }
                
            }
        }

        // moved GetMesh to LDrawModelRuntime
        //public Mesh GetMesh(string name)
        //{
        //    if (LDrawImportRuntime.Instance._Meshes.ContainsKey(name))
        //        return LDrawImportRuntime.Instance._Meshes[name];
        //    else
        //        return null;
        //    //var path = Path.Combine(_MeshesPath, name + ".asset");
        //    //return File.Exists(path) ? AssetDatabase.LoadAssetAtPath<Mesh>(path) : null;
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

            // Manually remove all chars before and including '\' since MacOS does not remove 's\' when Path.GetFileNameWithoutExtension(name) is used (Windows does remove this)
            if (name.Contains(@"\"))
            {
                int index = name.IndexOf(@"\");
                name = name.Remove(0, index + 1);
            }
            //Debug.Log(name);
            //Debug.Log(Path.GetFileNameWithoutExtension(name).ToLower());
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

        private void OnEnable()
        {
            InitParts();
        }

        public const int DefaultMaterialCode = 16;
    }
}