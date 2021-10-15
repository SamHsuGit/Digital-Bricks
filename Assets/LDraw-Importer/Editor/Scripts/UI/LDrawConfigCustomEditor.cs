using System.Collections;
using System.Collections.Generic;
using LDraw;
using UnityEditor;
using UnityEngine;

namespace LDraw
{
    [CustomEditor(typeof(LDrawConfigEditor))]
    public class LDrawConfigCustomEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Update blueprints"))
            {
                var config = target as LDrawConfigEditor;
                config.InitParts();
            }
        }
    }

}

