﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace LDraw
{

    public class LDrawSubFileEditor : LDrawCommandsEditor
    {
        private string _Name;
        private string _Extension;
        private Matrix4x4 _Matrix;
        private LDrawModelEditor _Model;

        public void GetModelGameObject(Transform parent)
        {
            _Model.CreateMeshGameObject(_Matrix, GetMaterial(), parent); // calls function that calls this function (recursive)
        }

        public override void PrepareMeshData(List<int> triangles, List<Vector3> verts)
        {

        }

        public override void Deserialize(string serialized)
        {
            var args = serialized.Split(' ');
            float[] param = new float[12];

            _Name = LDrawConfigEditor.GetFileName(args, 14);
            _Extension = LDrawConfigEditor.GetExtension(args, 14);
            for (int i = 0; i < param.Length; i++)
            {
                int argNum = i + 2;
                if (!Single.TryParse(args[argNum], out param[i]))
                {
                    Debug.LogError(
                        String.Format(
                            "Something wrong with parameters in {0} sub-file reference command. ParamNum:{1}, Value:{2}",
                            _Name,
                            argNum,
                            args[argNum]));
                }
            }

            _Model = LDrawModelEditor.Create(_Name, LDrawConfigEditor.Instance.GetSerializedPart(_Name));

            _Matrix = new Matrix4x4(
                new Vector4(param[3], param[6], param[9], 0),
                new Vector4(param[4], param[7], param[10], 0),
                new Vector4(param[5], param[8], param[11], 0),
                new Vector4(param[0], param[1], param[2], 1)
            );
        }

        private Material GetMaterial()
        {
            if (_Extension == ".ldr") return null;
            if (_ColorCode > 0) return LDrawConfigEditor.Instance.GetColoredMaterial(_ColorCode);
            if (_Color != null) return LDrawConfigEditor.Instance.GetColoredMaterial(_Color);
            return LDrawConfigEditor.Instance.GetColoredMaterial(0);
        }

    }

}