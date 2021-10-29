using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LDraw;
using UnityEngine.SceneManagement;

public class LDrawImportRuntime : MonoBehaviour
{
    private string[] _ModelNames;
    private string _CurrentPart;
    private int _CurrentIndex;
    public LDrawConfigRuntime ldrawConfigRuntime;
    public GameObject modelOb;

    public GameObject baseOb;
    //public GameObject procGenOb;
    public GameObject vehicleOb;
    public PhysicMaterial physicMaterial;

    //public int procGenObSizeX;
    //public int procGenObSizeZ;
    //public int procGenObSizeY;
    public int baseObSizeX;
    public int baseObSizeZ;
    public int baseObSizeY;
    public float scale = 0.025f;

    public static LDrawImportRuntime Instance { get { return _instance; } }
    private static LDrawImportRuntime _instance;

    private void Awake()
    {
        LoadVBOImports();
    }

    public void LoadVBOImports()
    {
        if (_instance != null && _instance != this)
            Destroy(gameObject);
        // Else set this to the instance.
        else
            _instance = this;

        ldrawConfigRuntime.SetFileNames();
        _ModelNames = ldrawConfigRuntime.ModelFileNames;

        // imports models and hides upon world load to be instantiated later
        baseOb = ImportLDraw("base", new Vector3(0, 0, 0), true);
        //procGenOb = ImportLDraw("procGen", new Vector3(0, 0, 0), true); // spawn preloaded copy far below world so its not visible since cannot make prefab???
        vehicleOb = ImportLDraw("vehicle", new Vector3(0, 0, 0), false);
        vehicleOb.tag = "Vehicle";

        // Cache size of bounding box of procGenOb.ldr and base.ldr
        baseObSizeX = Mathf.CeilToInt(baseOb.GetComponent<BoxCollider>().size.x / 40) + 1;
        baseObSizeZ = Mathf.CeilToInt(baseOb.GetComponent<BoxCollider>().size.z / 40) + 1;
        baseObSizeY = Mathf.CeilToInt(baseOb.GetComponent<BoxCollider>().size.y / 40) + 1;
        //procGenObSizeX = Mathf.CeilToInt(procGenOb.GetComponent<BoxCollider>().size.x / 40) + 1;
        //procGenObSizeZ = Mathf.CeilToInt(procGenOb.GetComponent<BoxCollider>().size.z / 40) + 1;
        //procGenObSizeY = Mathf.CeilToInt(procGenOb.GetComponent<BoxCollider>().size.y / 40) + 1;
    }

    public GameObject ImportLDraw(string fileName, Vector3 pos, bool staticVBO)
    {
        ldrawConfigRuntime.InitParts();
        _ModelNames = ldrawConfigRuntime.ModelFileNames;

        if (_ModelNames.Length < 1)
        {
            ErrorMessage.Show("No '.ldr' files found in 'BrickFormers - A Fan-Made Game_Data/ldraw/models/'");
            return null;
        }

        bool found = false;
        for (int i = 0; i < _ModelNames.Length; i++)
        {
            if (_ModelNames[i] == fileName)
            {
                _CurrentIndex = i;
                found = true;
            }
        }

        if (!found)
        {
            ErrorMessage.Show("'" + fileName + ".ldr' not found in 'BrickFormers - A Fan-Made Game_Data/ldraw/models/'. " +
                "File must have only 1 submodel named '" + fileName + "-submodel.ldr'. Always use latest version of game and Stud.io.");
            return null;
        }

        _CurrentPart = ldrawConfigRuntime.GetModelByFileName(_ModelNames[_CurrentIndex]);
        // good test 949ac01
        var model = LDrawModelRuntime.Create(_CurrentPart, ldrawConfigRuntime.GetSerializedPart(_CurrentPart));
        modelOb = model.CreateMeshGameObject(ldrawConfigRuntime.ScaleMatrix);
        modelOb.layer = 9; // add the model component to mark this object as a model
        CombineMeshes(modelOb);
        modelOb.transform.LocalReflect(Vector3.up);
        modelOb.transform.position = pos; // position imported gameObject at origin
        modelOb.SetActive(staticVBO);

        ElevateMeshRendererChildren(modelOb);

        BoxCollider modelObbc = modelOb.GetComponent<BoxCollider>();
        Transform submodel = modelOb.transform.GetChild(0);
        float distMoveRight = -modelObbc.center.x;
        float distMoveUp = Mathf.Abs(modelObbc.size.y / 2 - Mathf.Abs(modelObbc.center.y));
        float distMoveForward = -modelObbc.center.z;
        submodel.transform.position += new Vector3(-distMoveRight, distMoveUp, distMoveForward); // ensures imported model is always centered above 0 plane
        modelObbc.center += new Vector3(distMoveRight, -distMoveUp, distMoveForward); // move box collider by same distance

        modelOb.transform.localScale = new Vector3(scale, scale, scale); // rescale imported object to match voxel size
        modelOb.isStatic = true;

        return modelOb;
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
        bc.material = physicMaterial;
    }

    public void ElevateMeshRendererChildren(GameObject _modelOb)
    {
        foreach (Transform child in _modelOb.transform)
        {
            if (child.gameObject.GetComponent<MeshRenderer>() != null && child.gameObject.activeSelf)
            {
                child.parent = _modelOb.transform;
                child.gameObject.layer = 10; // mark as LEGO PIECE
            }
            if (child.childCount > 0)
                ElevateMeshRendererChildren(child.gameObject);
        }
    }

    private const string PathToModels = "Assets/ldraw/parts/"; // not currently used???
}