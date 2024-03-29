﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace LDraw
{
    public class LDrawModelEditor
    {
        /// FileFormatVersion 1.0.2;

        #region factory

        public static LDrawModelEditor Create(string name, string path)
        {
            if (_models.ContainsKey(name)) return _models[name];
            var model = new LDrawModelEditor();
            model.Init(name, path);

            return model;
        }

        #endregion

        #region fields and properties

        private string _Name;
        private List<LDrawCommandsEditor> _Commands;
        private List<string> _SubModels;
        private static Dictionary<string, LDrawModelEditor> _models = new Dictionary<string, LDrawModelEditor>();

        public string Name
        {
            get { return _Name; }
        }
        #endregion

        #region service methods

        private void Init(string name, string serialized)
        {
            _Name = name;
            //Debug.Log(_Name);
            _Commands = new List<LDrawCommandsEditor>();
            using (StringReader reader = new StringReader(serialized))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Regex regex = new Regex("[ ]{2,}", RegexOptions.None);
                    line = regex.Replace(line, " ").Trim();
                    //Debug.Log(line);
                    if (!String.IsNullOrEmpty(line))
                    {
                        var command = LDrawCommandsEditor.DeserializeCommand(line, this);
                        if (command != null)
                            _Commands.Add(command);
                    }
                }
            }

            if (!_models.ContainsKey(name))
            {
                _models.Add(name, this);
            }
        }

        public GameObject CreateMeshGameObject(Matrix4x4 trs, Material mat = null, Transform parent = null)
        {
            if (_Commands.Count == 0) return null;
            GameObject go = new GameObject(_Name);

            var triangles = new List<int>();
            var verts = new List<Vector3>();

            for (int i = 0; i < _Commands.Count; i++)
            {
                var sfCommand = _Commands[i] as LDrawSubFileEditor;
                if (sfCommand == null)
                {
                    _Commands[i].PrepareMeshData(triangles, verts);
                }
                else
                {
                    //if (parent != null)
                    //    Debug.Log(parent.gameObject.name + " " + go.name);
                    if (parent != null && parent.gameObject.name == go.name)
                    {
                        //Debug.Log(parent.gameObject.name + " = " + go.name); // Error handling to prevent stack overflow if parent and model have same name (except instead of error message, keeps going)...
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

        public void SaveMesh()
        {
            var triangles = new List<int>();
            var verts = new List<Vector3>();

            for (int i = 0; i < _Commands.Count; i++)
            {
                var sfCommand = _Commands[i] as LDrawSubFileEditor;
                if (sfCommand == null)
                {
                    _Commands[i].PrepareMeshData(triangles, verts);
                }
            }

            if (verts.Count > 0)
            {
                Mesh mesh = PrepareMesh(verts, triangles);
            }
        }

        public void CombineMeshes(GameObject _go)
        {
            // Combine the submeshes into one optimized mesh
            MeshFilter[] meshFilters = _go.GetComponentsInChildren<MeshFilter>();
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
            _go.transform.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            _go.transform.GetComponent<MeshFilter>().sharedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // supports up to 4 billion vertices
            //if (_go.transform.GetComponent<MeshFilter>().sharedMesh.vertexCount > 65535)
            //    ErrorMessage.Show("Error: Ldraw model too large to combine meshes.");
            _go.transform.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine, true);
            _go.transform.gameObject.SetActive(true);
            BoxCollider bc = _go.AddComponent<BoxCollider>();
            bc.size = new Vector3(Mathf.Abs(bc.size.x), Mathf.Abs(bc.size.y), Mathf.Abs(bc.size.z)); // avoids negative values for box collider scale
            bc.center = new Vector3(bc.center.x, bc.center.y, bc.center.z); // recenter box collider
        }

        private Mesh PrepareMesh(List<Vector3> verts, List<int> triangles)
        {

            Mesh mesh = LDrawConfigEditor.Instance.GetMesh(_Name);
            if (mesh != null) return mesh;

            mesh = new Mesh();

            mesh.name = _Name;
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
            LDrawConfigEditor.Instance.SaveMesh(mesh);
            return mesh;
        }

        #endregion

        private LDrawModelEditor()
        {

        }
    }
}