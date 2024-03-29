﻿using System;
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

                if (name.Substring(0, 2).Contains(@"\"))
                {
                    name = name.Substring(3, name.Length);
                }

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
            var files = Directory.GetFiles(Settings.BasePartsPath, "*.*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (!file.Contains(".meta"))
                {
                    string fileName = file.Replace(Settings.BasePartsPath, "").Split('.')[0];

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
                                _MainColors.Add(int.Parse(args[4]), materials[i]); // add the material to the dictionary
                                matched = true;
                            }
                        }
                        if (!matched)
                            ErrorMessage.Show("Error: Material not found: " + args[2]);
                    }
                }
            }
        }

        private void PrepareModels()
        {
            _ModelFileNames = new Dictionary<string, string>();
            //Debug.Log("SEARCHING FOR MODELS IN " + _ModelsPath);
            var files = Directory.GetFiles(Settings.ModelsPath, "*.*", SearchOption.AllDirectories); // MacOS cannot search all directories with Directory.GetFiles so put all ldraw part files into same directory
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
                        if (args.Length > 1 && args[1] == "FILE")
                        {

                            filename = GetFileName(args, 2);
                            if (isFirst)
                            {
                                _ModelFileNames.Add(Path.GetFileNameWithoutExtension(file), filename);
                                isFirst = false;
                            }

                            if (_Models.ContainsKey(filename))
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

        private const string ConfigPath = "Assets/Scripts/ldraw import/LDrawConfigRuntime.asset"; // currently not used in runtime as an object reference was used instead
        public const int DefaultMaterialCode = 16;
    }
}