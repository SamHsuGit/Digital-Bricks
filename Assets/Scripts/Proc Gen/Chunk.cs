using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    public GameObject chunkObject;
    MeshRenderer meshRenderer;
    MeshFilter meshFilter;
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

    List<VoxelState> activeVoxels = new List<VoxelState>();

    public Chunk(ChunkCoord coord)
    {
        this.coord = coord;

        chunkObject = new GameObject();
        chunkObject.isStatic = true;
        World.Instance.chunksDict.Add(this.coord, this);
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = World.Instance.blockMaterial;
        materials[1] = World.Instance.blockMaterialTransparent;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(this.coord.x * VoxelData.ChunkWidth, 0f, this.coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk " + this.coord.x + ", " + this.coord.z;
        chunkObject.tag = "Chunk";
        position = chunkObject.transform.position;

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int((int)position.x, (int)position.z));
        chunkData.chunk = this;

        // when chunk is first created loop through all voxels in chunk and if they can be active, add to list
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    VoxelState voxel = chunkData.map[x, y, z];
                    if (voxel.properties.isActive) // Only runs for certain behavior blocks marked as active (i.e. grass, furnaces)
                        AddActiveVoxel(voxel);
                }
            }
        }

        lock (World.Instance.ChunkUpdateThreadLock)
        {
            World.Instance.chunksToUpdate.Add(this);

            //if (coord.x == World.Instance.firstChunkCoord.x && coord.z == World.Instance.firstChunkCoord.z) //always show first chunk
            //    isActive = true;
            //else if (!World.Instance.activateNewChunks)
            //    isActive = false;
            //else
                isActive = true;
        }
    }

    public void TickUpdate()
    {
        Debug.Log(chunkObject.name + " currently has " + activeVoxels.Count + " active blocks.");
        for (int i = activeVoxels.Count - 1; i > -1; i--)
        {
            if (!BlockBehavior.Active(activeVoxels[i]))
                RemoveActiveVoxel(activeVoxels[i]);
            else
                BlockBehavior.Behave(activeVoxels[i]);
        }
    }

    public void UpdateChunk() // are all references of this function locked to be threadsafe since they modify the array of vertices, triangles, normals?
    {
        ClearMeshData();

        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    if (World.Instance.blockTypes[chunkData.map[x, y, z].id].isDrawn)
                        UpdateMeshData(new Vector3(x, y, z)); // draw chunk by adding mesh data
                }
            }
        }

        World.Instance.chunksToDraw.Enqueue(this);
    }

    public void AddActiveVoxel (VoxelState voxel)
    {
        if (!activeVoxels.Contains(voxel)) // Make sure voxel isn't already in list.
        {
            activeVoxels.Add(voxel);
        }
    }

    public void RemoveActiveVoxel(VoxelState voxel)
    {
        for (int i = 0; i < activeVoxels.Count; i++)
        {
            if(activeVoxels[i] == voxel)
            {
                activeVoxels.RemoveAt(i);
                return;
            }
        }
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
        if (!World.Instance.IsGlobalPosInWorld(pos))
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

        // Added in https://www.youtube.com/watch?v=DjQ6yFRuZ7Q but is broken, does not allow player to modify voxels so commented out, instead use old code below
        //chunkData.ModifyVoxel(new Vector3Int(xCheck, yCheck, zCheck), newID, 0); // write new block ID to chunkData 
        //UpdateSurroundingVoxels(xCheck, yCheck, zCheck);

        chunkData.map[xCheck, yCheck, zCheck].id = newID; // write new block ID to chunkData
        World.Instance.worldData.AddToModifiedChunkList(chunkData); // save data only contains list of modified voxels, otherwise, generates using the GetVoxel algorithm.
        
        lock (World.Instance.ChunkUpdateThreadLock)
        {
            World.Instance.chunksToUpdate.Insert(0, this);
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
                if(World.Instance.IsGlobalPosInsideBorder(currentVoxel + position))
                    World.Instance.GetChunkFromVector3(currentVoxel + position).UpdateChunk();
            }
        }
    }

    public VoxelState CheckVoxel(Vector3 pos)
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

        VoxelState voxel = chunkData.map[x, y, z];

        for (int p = 0; p < 6; p++)
        {
            VoxelState neighbor = chunkData.map[x, y, z].neighbors[p];

            if (neighbor != null && World.Instance.blockTypes[neighbor.id].isTransparent)
            {
                int faceVertCount = 0;
                for (int i = 0; i < voxel.properties.meshData.faces[p].vertData.Length; i++)
                {
                    vertices.Add(pos + voxel.properties.meshData.faces[p].vertData[i].position);
                    normals.Add(voxel.properties.meshData.faces[p].normal);
                    AddTexture(voxel.properties.GetTextureID(p), voxel.properties.meshData.faces[p].vertData[i].uv);
                    faceVertCount++;
                }

                if (!World.Instance.blockTypes[voxel.id].isTransparent)
                {
                    for (int i = 0; i < voxel.properties.meshData.faces[p].triangles.Length; i++)
                        triangles.Add(vertexIndex + voxel.properties.meshData.faces[p].triangles[i]);
                }
                else
                {
                    for (int i = 0; i < voxel.properties.meshData.faces[p].triangles.Length; i++)
                        transparentTriangles.Add(vertexIndex + voxel.properties.meshData.faces[p].triangles[i]);
                }

                vertexIndex += faceVertCount;
            }
        }
    }

    public void CreateMesh() // UNKNOWN ERRORS PERSIST IN THIS FUNCTION, NOT SURE WHY...
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

        if(World.Instance.chunkMeshColliders)
        {
            if (col == null)
                col = chunkObject.AddComponent<MeshCollider>();
            else
                col = chunkObject.GetComponent<MeshCollider>();
            col.sharedMesh = mesh;
            col.material = World.Instance.physicMaterial;
        }
    }

    void AddTexture(int textureID, Vector2 uv)
    {
        float y = textureID / VoxelData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * VoxelData.TextureAtlasSizeInBlocks);

        x *= VoxelData.NormalizedBlockTextureSize;
        y *= VoxelData.NormalizedBlockTextureSize;

        y = 1f - y - VoxelData.NormalizedBlockTextureSize;

        x += VoxelData.NormalizedBlockTextureSize * uv.x;
        y += VoxelData.NormalizedBlockTextureSize * uv.y;
        uvs.Add(new Vector2(x, y));
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

    public ChunkCoord(int x, int z)
    {
        this.x = x;
        this.z = z;
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