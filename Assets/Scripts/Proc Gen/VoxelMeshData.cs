using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Voxel Mesh Data", menuName = "ProcGen/Voxel MeshData")]
public class VoxelMeshData : ScriptableObject
{
    public string blockName;
    public FaceMeshData[] faces; // 6 faces using our established winding order from VoxelData.
}

[System.Serializable]
public class VertData
{
    public Vector3 position;
    public Vector2 uv;

    public VertData (Vector3 pos, Vector2 _uv)
    {
        position = pos;
        uv = _uv;
    }
}

[System.Serializable]
public class FaceMeshData
{
    // Because all of the verts in this face are facing the same direction, we can store a single normal value
    // for each face and use that for each vert in the face.

    public string direction; // Purely to make things easier to read in the inspector.
    public Vector3 normal;
    public VertData[] vertData;
    public int[] triangles;
}
