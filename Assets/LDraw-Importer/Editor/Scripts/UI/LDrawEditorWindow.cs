using UnityEditor;
using UnityEngine;

namespace LDraw
{
    public class LDrawEditorWindow : EditorWindow
    {
        [MenuItem("Window/LDrawImporter/Open Importer")]
        public static void Create()
        {
            var window = GetWindow<LDrawEditorWindow>("LDrawImporter");
            window.position = new Rect(100, 100, 400, 400);
            window.Show();
        }

        private string[] _ModelNames;
        
        private string _CurrentPart;
        private int _CurrentIndex;
        private GeneratingType _CurrentType;

        public float scale = 0.025f;

        private void OnEnable()
        {
            _ModelNames = LDrawConfigEditor.Instance.ModelFileNames;
        }

        private void OnGUI()
        {
            GUILayout.Label("This is LDraw model importer for file format v1.0.2");
            if (GUILayout.Button("Update blueprints"))
            {
                LDrawConfigEditor.Instance.InitParts();
                _ModelNames = LDrawConfigEditor.Instance.ModelFileNames;
            }
            _CurrentType = (GeneratingType) EditorGUILayout.EnumPopup("Blueprint Type", _CurrentType);
            switch (_CurrentType)
            {
                    case GeneratingType.ByName:
                        _CurrentPart = EditorGUILayout.TextField("Name", _CurrentPart);
                        break;
                    case GeneratingType.Models:
                        _CurrentIndex = EditorGUILayout.Popup("Models", _CurrentIndex, _ModelNames);
                        break;
            }
      
            GenerateModelButton();
        }

        private void GenerateModelButton()
        {
            if (GUILayout.Button("Generate"))
            {
                _CurrentPart = _CurrentType == GeneratingType.ByName ? _CurrentPart 
                    : LDrawConfigEditor.Instance.GetModelByFileName(_ModelNames[_CurrentIndex]);
                // good test 949ac01
                var model = LDrawModelEditor.Create(_CurrentPart, LDrawConfigEditor.Instance.GetSerializedPart(_CurrentPart));
                var modelOb = model.CreateMeshGameObject(LDrawConfigEditor.Instance.ScaleMatrix);
                CombineMeshes(modelOb);
                modelOb.transform.LocalReflect(Vector3.up);   
                
                BoxCollider modelObbc = modelOb.GetComponent<BoxCollider>();
                Transform submodel = modelOb.transform.GetChild(0);
                float distMoveRight = -modelObbc.center.x;
                float distMoveUp = Mathf.Abs(modelObbc.size.y / 2 - Mathf.Abs(modelObbc.center.y));
                float distMoveForward = -modelObbc.center.z;
                submodel.transform.position += new Vector3(-distMoveRight, distMoveUp, distMoveForward); // ensures imported model is always centered above 0 plane
                modelObbc.center += new Vector3(distMoveRight, -distMoveUp, distMoveForward); // move box collider by same distance

                modelOb.transform.localScale = new Vector3(scale, scale, scale); // rescale imported object to match voxel size
                modelOb.isStatic = true;
            }
            else if (GUILayout.Button("Import All Meshes"))
            {
                string[] partMeshNames = LDrawConfigEditor.Instance._PartMeshNames;
                for (int i = 0; i < partMeshNames.Length; i++)
                {
                    _CurrentPart = partMeshNames[i];
                    //Debug.Log(_CurrentPart);
                    var model = LDrawModelEditor.Create(_CurrentPart, LDrawConfigEditor.Instance.GetSerializedPart(_CurrentPart));
                    model.SaveMesh();
                }
            }
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
                i++;
            }

            // apply to parent object
            go.AddComponent<MeshFilter>();
            go.transform.GetComponent<MeshFilter>().sharedMesh = new Mesh();
            go.transform.GetComponent<MeshFilter>().sharedMesh.CombineMeshes(combine);
            go.transform.gameObject.SetActive(true);
            go.transform.gameObject.AddComponent<MeshRenderer>().enabled = false; // only used to generate bounds for BoxCollider
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.size = new Vector3(Mathf.Abs(bc.size.x), Mathf.Abs(bc.size.y), Mathf.Abs(bc.size.z)); // avoids negative values for box collider scale
        }

        private enum GeneratingType
        {
            ByName,
            Models
        }
        private const string PathToModels = "Assets/ldraw/parts/"; // not currently used???
    }
}