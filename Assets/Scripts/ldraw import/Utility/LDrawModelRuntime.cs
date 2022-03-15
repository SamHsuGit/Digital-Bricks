using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LDraw
{
    public class LDrawModelRuntime
    {
        /// LDraw FileFormatVersion 1.0.2;

        #region factory

        public static LDrawModelRuntime Create(string name, string pathOrCommandString, bool isPath)
        {
            if (models.ContainsKey(name)) return models[name];
            var model = new LDrawModelRuntime();
            model.Init(name, pathOrCommandString, isPath);
          
            return model;
        }

        #endregion

        #region fields and properties

        private string name;
        private List<LDrawCommandRuntime> commands;
        private List<string> subModels;
        private static Dictionary<string, LDrawModelRuntime> models = new Dictionary<string, LDrawModelRuntime>();
        
        public string Name
        {
            get { return name; }
        }
        #endregion

        #region service methods

        private void Init(string name, string pathOrCommandString, bool isPath) // uses serialized ldraw commands to store commands into a new List called _Commands, adds matching models to the list of models
        {
            if (isPath)
                GetCommandsFromPath(name, pathOrCommandString);
            else
                GetCommandsFromString(pathOrCommandString);

            AddToModels(name);
        }

        public List<LDrawCommandRuntime> GetCommandsFromPath(string fileName, string path) // gets commands from a local path
        {
            name = fileName;
            //Debug.Log(_Name);
            commands = new List<LDrawCommandRuntime>();
            using (StringReader reader = new StringReader(path))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex regex = new Regex("[ ]{2,}", RegexOptions.None); // Sometime runs into stack overflow fatal error from multiple pieces with same name???
                    line = regex.Replace(line, " ").Trim();
                    //Debug.Log(line);
                    if (!String.IsNullOrEmpty(line))
                    {
                        var command = LDrawCommandRuntime.DeserializeCommand(line, this);
                        if (command != null)
                            commands.Add(command);
                    }
                }
            }

            return commands;
        }

        public List<LDrawCommandRuntime> GetCommandsFromString(string commandString) // gets commands from serialized string of commands
        {
            commands = new List<LDrawCommandRuntime>();
            using (StringReader reader = new StringReader(commandString))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex regex = new Regex("[ ]{2,}", RegexOptions.None); // Sometime runs into stack overflow fatal error from multiple pieces with same name???
                    line = regex.Replace(line, " ").Trim();
                    //Debug.Log(line);
                    if (!String.IsNullOrEmpty(line))
                    {
                        var command = LDrawCommandRuntime.DeserializeCommand(line, this);
                        if (command != null)
                            commands.Add(command);
                    }
                }
            }

            return commands;
        }

        public void AddToModels(string name)
        {
            if (!models.ContainsKey(name))
            {
                models.Add(name, this);
            }
        }

        public GameObject CreateMeshGameObject(Matrix4x4 trs, Material mat = null, Transform parent = null)
        {
            if (commands.Count == 0) return null;
            GameObject go = new GameObject(name);

            var triangles = new List<int>();
            var verts = new List<Vector3>();

            for (int i = 0; i < commands.Count; i++)
            {
                var sfCommand = commands[i] as LDrawSubFileRuntime;
                if (sfCommand == null)
                {
                    commands[i].PrepareMeshData(triangles, verts);
                }
                else
                {
                    //if (parent != null)
                    //    Debug.Log(parent.gameObject.name + " " + go.name);
                    if (parent != null && parent.gameObject.name == go.name)
                    {
                        //ErrorMessage.Show(parent.gameObject.name + " = " + go.name); // Error handling to prevent stack overflow if parent and model have same name (except instead of error message, keeps going)...
                    }
                    else
                        sfCommand.GetModelGameObject(go.transform); // calls function that calls this function (recursive), can sometimes create stack overflow fatal crash?
                }
            }

            if (mat != null)
            {
                var childMrs = go.transform.GetComponentsInChildren<MeshRenderer>();
                foreach (var meshRenderer in childMrs)
                {
                    meshRenderer.material = mat;
                }
            }

            if (verts.Count > 0)
            {
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = PrepareMesh(verts, triangles);
                var mr = go.AddComponent<MeshRenderer>();
                CombineMeshes(go); // Combine Meshes AFTER PrepareMesh (was hitting vertex limit?)
                if (mat != null)
                {
                    mr.sharedMaterial = mat;
                }
            }

            go.transform.ApplyLocalTRS(trs);
            go.transform.SetParent(parent);
            //go.isStatic = true; // cannot use static batching on individual parts since this hides the piece gameObject when rendered for some reason
            return go;
        }

        public void CombineMeshes(GameObject go)
        {
            // Combine the submeshes into one optimized mesh
            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
            CombineInstance[] combine = new CombineInstance[meshFilters.Length];
            int i = 0;
            while (i < meshFilters.Length)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
                meshFilters[i].gameObject.SetActive(false); // disable old submeshes
                i++;
            }

            // apply to parent object
            go.transform.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            go.transform.GetComponent<MeshFilter>().sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // supports up to 4 billion vertices
            //if (_go.transform.GetComponent<MeshFilter>().sharedMesh.vertexCount > 65535)
            //    ErrorMessage.Show("Error: Ldraw model too large to combine meshes.");
            go.transform.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine, true);
            go.transform.gameObject.SetActive(true);

            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.size = new Vector3(Mathf.Abs(bc.size.x), Mathf.Abs(bc.size.y), Mathf.Abs(bc.size.z)); // avoids negative values for box collider scale
            bc.center = new Vector3(bc.center.x, bc.center.y, bc.center.z); // recenter box collider
            bc.material = LDrawImportRuntime.Instance.physicMaterial;
        }

        public Mesh GetMesh(string name)
        {
            if (LDrawImportRuntime.Instance._Meshes.ContainsKey(name))
                return LDrawImportRuntime.Instance._Meshes[name];
            else
                return null;
        }

        private Mesh PrepareMesh(List<Vector3> verts, List<int> triangles)
        {
            Mesh mesh;

            // Tries to get mesh from previously saved meshes compiled into game code. If cannot find, then creates new mesh based on LDraw partfiles in StreamingAssets
            // This is only marginally faster since it takes alot of time to load, index, search the dictionary of loaded meshes so load time is about the same as creating new meshes every time...
            // Found compiling the meshes is actually slower on some machines, so chose to comment this out for now until better load/search method developed.
            //mesh = GetMesh(_Name);
            //if (mesh != null) return mesh;
          
            mesh = new Mesh();
      
            mesh.name = name;
            var frontVertsCount = verts.Count;
            //backface
            verts.AddRange(verts);
            int[] tris = new int[triangles.Count];
            triangles.CopyTo(tris);
            for (int i = 0; i < tris.Length; i += 3)
            {
                int temp = tris[i];
                tris[i] = tris[i + 1];
                tris[i + 1] = temp;
            }

            for (int i = 0; i < tris.Length; i++)
            {
                tris[i] = tris[i] + frontVertsCount;
            }
            triangles.AddRange(tris);
            //end backface
            
            mesh.SetVertices(verts);
            mesh.SetTriangles(triangles, 0);
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            //LDrawConfigRuntime.Instance.SaveMesh(mesh);
            return mesh;
        }
  
        #endregion

        private LDrawModelRuntime()
        {
            // default constructor
        }
    }
}