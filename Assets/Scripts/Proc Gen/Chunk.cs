using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    public GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
    Mesh mesh;
    MeshCollider col;

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    List<Vector3> normals = new List<Vector3>();

    public Vector3 position;

    private bool _isInLoadDist;
    private bool _isInDrawDist = false;
    private bool _isInStructDrawDist;
    private bool _isInObDrawDist;
    private bool _isActive;

    public ChunkData chunkData;

    public Chunk(ChunkCoord _coord)
    {
        coord = _coord;

        chunkObject = new GameObject();
        chunkObject.isStatic = true;
        World.Instance.chunks.Add(coord, this);
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = World.Instance.blockMaterial;
        materials[1] = World.Instance.blockMaterialTransparent;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
        position = chunkObject.transform.position;

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int((int)position.x, (int)position.z), true);

        lock (World.Instance.ChunkDrawThreadLock)
        {
            World.Instance.chunksToDrawList.Add(this);

            //if (coord.x == World.Instance.firstChunkCoord.x && coord.z == World.Instance.firstChunkCoord.z) //always show first chunk
            //    isActive = true;
            //else if (!World.Instance.activateNewChunks)
            //    isActive = false;
            //else
                isActive = true;
        }
    }

    public void DrawChunk() // are all references of this function locked to be threadsafe since they modify the array of vertices, triangles, normals?
    {
        ClearMeshData();

        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (World.Instance.blocktypes[chunkData.map[x, y, z].id].isDrawn)
                        UpdateMeshData(new Vector3(x, y, z)); // draw chunk by adding mesh data
                }
            }
        }

        World.Instance.chunksToDrawQueue.Enqueue(this);
    }

    void ClearMeshData()
    {
        vertexIndex = 0;
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        normals.Clear();
    }

    public bool isActive
    {
        get
        {
            if (_isActive)
                return true;
            else
                return false;
        }
        set
        {
            meshRenderer.enabled = value;
            if(chunkObject.GetComponent<MeshCollider>() != null)
                chunkObject.GetComponent<MeshCollider>().enabled = value;
            _isActive = value;
        }
    }

    public bool isInLoadDist
    {
        get { return _isInLoadDist; }
        set { _isInLoadDist = value; }
    }

    public bool isInDrawDist
    {
        get { return _isInDrawDist; }
        set
        {
            _isInDrawDist = value;
            if (chunkObject != null)
                chunkObject.SetActive(value);
        }
    }

    public bool isInStructDrawDist
    {
        get { return _isInStructDrawDist; }
        set { _isInStructDrawDist = value; }
    }

    public bool isInObDrawDist
    {
        get { return _isInObDrawDist; }
        set { _isInObDrawDist = value; }
    }

    bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;
        else
            return true;
    }

    public void EditVoxel(Vector3 pos, byte newID)
    {
        if (!World.Instance.IsVoxelInWorld(pos))
            return;

        if (newID == 0)
        {
            World.Instance.RemoveObjectsFromVoxel(pos); // removes objects if voxel below is replaced with air
        }
        else
        {
            // add objects immediately whenever a new block is placed. Currently only objects are only replaced. Studs are replaced when update chunk is called.
            World.Instance.AddObjectsToVoxel(pos, newID);
        }

        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        chunkData.map[xCheck, yCheck, zCheck].id = newID; // write new block ID to chunkData
        World.Instance.worldData.AddToModifiedChunkList(chunkData); // save data only contains list of modified voxels, otherwise, generates using the GetVoxel algorithm.

        lock (World.Instance.ChunkDrawThreadLock)
        {
            World.Instance.chunksToDrawList.Insert(0, this);
            UpdateSurroundingVoxels(xCheck, yCheck, zCheck);
        }
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                World.Instance.GetChunkFromVector3(currentVoxel + position).DrawChunk();
            }
        }
    }

    VoxelState CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        if (!IsVoxelInChunk(x, y, z))
            return World.Instance.GetVoxelState(pos + position);

        return chunkData.map[x, y, z];
    }

    public VoxelState GetVoxelFromGlobalVector3(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(position.x);
        zCheck -= Mathf.FloorToInt(position.z);

        return chunkData.map[xCheck, yCheck, zCheck];
    }

    void UpdateMeshData(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        byte blockID = chunkData.map[x, y, z].id;

        for (int p = 0; p < 6; p++)
        {
            Vector3 neighborPos = pos + VoxelData.faceChecks[p];
            VoxelState neighborVoxelState;

            //if (!World.Instance.activateNewChunks)
            //    neighborVoxelState = new VoxelState(0);
            //else
                neighborVoxelState = CheckVoxel(neighborPos);

            if (neighborVoxelState != null && World.Instance.blocktypes[neighborVoxelState.id].isTransparent)
            {
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                for (int i = 0; i < 4; i++)
                    normals.Add(VoxelData.faceChecks[p]);

                AddTexture(World.Instance.blocktypes[blockID].GetTextureID(p));

                if (World.Instance.blocktypes[neighborVoxelState.id].isTransparent)
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }
                else
                {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                }

                vertexIndex += 4;
            }
        }
    }

    public void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray(); // code is not threadsafe, being modified by other threads... but vertices appeared locked for all modifications and still failed...
        
        mesh.subMeshCount = 2; // used for block and transBlock materials?
        
        if (triangles.Count % 3 != 0) // The number of supplied triangle indices must be a multiple of 3
            return;
        else
        {
            mesh.SetTriangles(triangles.ToArray(), 0);
            mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        }

        if (uvs.ToArray().Length != vertices.ToArray().Length) // the supplied array needs to be the same size as teh Mesh.vertices array
            return;
        else
            mesh.uv = uvs.ToArray();

        if (normals.Count == 0 || mesh == null)
            return;
        else
            mesh.normals = normals.ToArray();
        
        meshFilter.mesh = mesh;

        if (col == null)
            col = chunkObject.AddComponent<MeshCollider>();
        else
            col = chunkObject.GetComponent<MeshCollider>();
        col.sharedMesh = mesh;
        col.material = World.Instance.physicMaterial;
    }

    void AddTexture(int textureID)
    {
        float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + VoxelData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + VoxelData.NormalizedBlockTextureSize, y + VoxelData.NormalizedBlockTextureSize));
    }

}

public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord()
    {
        x = 0;
        z = 0;
    }

    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public ChunkCoord(Vector3 pos)
    {
        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck / VoxelData.ChunkWidth;
        z = zCheck / VoxelData.ChunkWidth;
    }

    public bool Equals(ChunkCoord other)
    {
        if (other == null)
            return false;
        else if (other.x == x && other.z == z)
            return true;
        else
            return false;
    }
}

[HideInInspector]
[System.Serializable]
public class VoxelState
{
    public byte id; // 1 byte = 2^8 (256) unique block IDs

    public VoxelState()
    {
        id = 0;
    }

    public VoxelState(byte _id)
    {
        id = _id;
    }
}