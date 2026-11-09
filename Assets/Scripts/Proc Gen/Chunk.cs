using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using System.Diagnostics;
using System.Threading;

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
    List<int> waterTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();
    List<Color> colors = new List<Color>();
    List<Vector3> normals = new List<Vector3>();

    public Vector3 position;

    public VoxelState[,,] voxelMap = new VoxelState[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    private bool _isInLoadDist;
    private bool _isInStructDrawDist;
    private bool _isInObDrawDist;

    public Queue<VoxelMod> modifications = new Queue<VoxelMod>();
    private bool _isActive;
    private bool isVoxelMapPopulated = false;
    private bool threadLocked = false;

    public ChunkData chunkData;

    List<VoxelState> activeVoxels = new List<VoxelState>();

    public Chunk(ChunkCoord _coord)
    {
        coord = _coord;

        chunkObject = new GameObject();
        chunkObject.isStatic = true;
        World.Instance.chunksDict.Add(coord, this);
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = World.Instance.blockMaterial;
        materials[1] = World.Instance.blockMaterialTransparent;
        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(World.Instance.transform);
        chunkObject.transform.position = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
        chunkObject.tag = "Chunk";
        position = chunkObject.transform.position;

        chunkData = World.Instance.worldData.RequestChunk(new Vector2Int((int)position.x, (int)position.z), true); // requests the chunkData when adding a new chunk (either loads from memory or calculates new data)
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

        World.Instance.AddChunkToUpdate(this);

        if(!Settings.WebGL && SettingsStatic.LoadedSettings.chunkLoadAnim)
        {
            chunkObject.AddComponent<ChunkLoadAnimation>();
            World.Instance.chunkLoadSound.Play();
        }

        PopulateVoxelMap();
    }

    public void TickUpdate()
    {
        UnityEngine.Debug.Log(chunkObject.name + " currently has " + activeVoxels.Count + " active blocks.");
        for (int i = activeVoxels.Count - 1; i > -1; i--)
        {
            if (!BlockBehavior.Active(activeVoxels[i]))
                RemoveActiveVoxel(activeVoxels[i]);
            else
                BlockBehavior.Behave(activeVoxels[i]);
        }
    }

    void PopulateVoxelMap () {
		
        int posX = Mathf.FloorToInt(position.x);
        int posY = Mathf.FloorToInt(position.y);
        int posZ = Mathf.FloorToInt(position.z);
        Vector3Int positionInt = new Vector3Int(posX, posY, posZ);

		for (int y = 0; y < VoxelData.ChunkHeight; y++) {
			for (int x = 0; x < VoxelData.ChunkWidth; x++) {
				for (int z = 0; z < VoxelData.ChunkWidth; z++) {

                    //voxelMap[x,y,z] = chunkData.map[x,y,z];
                    // what chunkData is used to initialize?
                    voxelMap[x, y, z] = new VoxelState(World.Instance.GetVoxel(new Vector3Int(x, y, z) + positionInt), chunkData, new Vector3Int(x, y, z), 0);
    
				}
			}
		}

        _updateChunk();
        isVoxelMapPopulated = true;

	}

    public void UpdateChunk() // are all references of this function locked to be threadsafe since they modify the array of vertices, triangles, normals?
    {
        // threadLocked = true;

        if (World.Instance.multithreading) {
            Thread myThread = new Thread(new ThreadStart(_updateChunk));
            myThread.Start();
        } else
            _updateChunk();

        // while (modifications.Count > 0) {

        //     VoxelMod v = modifications.Dequeue();
        //     Vector3 pos = v.position -= position;
        //     chunkData.map[(int)pos.x, (int)pos.y, (int)pos.z].id = v.id;

        // }

        // ClearMeshData();

        // for (int y = 0; y < VoxelData.ChunkHeight; y++)
        // {
        //     for (int x = 0; x < VoxelData.ChunkWidth; x++)
        //     {
        //         for (int z = 0; z < VoxelData.ChunkWidth; z++)
        //         {
        //             if (World.Instance.blockTypes[chunkData.map[x, y, z].id].blockProperties.isDrawn)
        //                 UpdateMeshData(new Vector3Int(x, y, z)); // draw chunk by adding mesh data
        //         }
        //     }
        // }

        //World.Instance.chunksToDraw.Enqueue(this);

        // lock (World.Instance.chunksToDraw) {
        //     World.Instance.chunksToDraw.Enqueue(this);
        // }

        // threadLocked = false;
    }

    private void _updateChunk () {

        threadLocked = true;

        while (modifications.Count > 0) {

            VoxelMod v = modifications.Dequeue();
            Vector3 pos = v.position -= position;
            chunkData.map[(int)pos.x, (int)pos.y, (int)pos.z].id = v.id;

        }

        ClearMeshData();

        CalculateLight();

		for (int y = 0; y < VoxelData.ChunkHeight; y++) {
			for (int x = 0; x < VoxelData.ChunkWidth; x++) {
				for (int z = 0; z < VoxelData.ChunkWidth; z++) {

                    if (World.Instance.blockTypes[chunkData.map[x,y,z].id].blockProperties.isSolid)
					    UpdateMeshData (new Vector3Int(x, y, z));

				}
			}
		}

        lock (World.Instance.chunksToDraw) {
            World.Instance.chunksToDraw.Enqueue(this);
        }

        threadLocked = false;

	}

    void CalculateLight () {

        Queue<Vector3Int> litVoxels = new Queue<Vector3Int>();

        for (int x = 0; x < VoxelData.ChunkWidth; x++) {
            for (int z = 0; z < VoxelData.ChunkWidth; z++) {

                float lightRay = 1f;

                for (int y = VoxelData.ChunkHeight - 1; y >= 0; y--) {

                    VoxelState thisVoxel = chunkData.map[x, y, z];

                    if (thisVoxel.id > 0 && World.Instance.blockTypes[thisVoxel.id].blockProperties.opacity < lightRay)
                        lightRay = World.Instance.blockTypes[thisVoxel.id].blockProperties.opacity;

                    //thisVoxel.globalLightPercent = (byte)Mathf.FloorToInt(lightRay * 16);
                    // if(World.Instance.blockTypes[thisVoxel.id].baseLightLevel > 0)
                    //     thisVoxel.globalLightPercent = 100f;
                    // else
                        thisVoxel.globalLightPercent = lightRay;

                    chunkData.map[x, y, z] = thisVoxel;

                    if (lightRay > VoxelData.lightFalloff)
                        litVoxels.Enqueue(new Vector3Int(x, y, z));

                }
            }
        }

        while (litVoxels.Count > 0) {

            Vector3Int v = litVoxels.Dequeue();

            for (int p = 0; p < 6; p++) {

                Vector3 currentVoxel = v + VoxelData.faceChecks[p];
                Vector3Int neighbor = new Vector3Int((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z);

                if (IsVoxelInChunk(neighbor.x, neighbor.y, neighbor.z)) {
                    // if the neighbor voxel light value is less than the original voxel minus fall off
                    if (chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent < chunkData.map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff) {
                        // set neighbor light value to this voxel minus fall off
                        chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent = chunkData.map[v.x, v.y, v.z].globalLightPercent - VoxelData.lightFalloff;
                        // if the neighbor light value is light enough could illuminate other voxels, then do this code again for neighbor
                        if (chunkData.map[neighbor.x, neighbor.y, neighbor.z].globalLightPercent > VoxelData.lightFalloff)
                            litVoxels.Enqueue(neighbor);

                    }

                }

            }

        }

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
        colors.Clear();
        normals.Clear();
    }

    public bool isActive // used for object pooling to unload inactive chunks from memory to save RAM usage
    {
        get { return _isActive; }
        set
        {
            _isActive = value;

            if (chunkObject != null)
                chunkObject.SetActive(value); // turn off gameObject (however, doesn't release from memory...)
        }
    }

    public bool isEditable {

        get {

            if (!isVoxelMapPopulated || threadLocked)
                return false;
            else
                return true;

        }

    }

    bool IsVoxelInChunk(int x, int y, int z)
    {
        if (x < 0 || x > VoxelData.ChunkWidth - 1 || y < 0 || y > VoxelData.ChunkHeight - 1 || z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;
        else
            return true;
    }

    public bool isInLoadDist // used for object pooling? Need to unload inactive chunks from memory to save RAM usage?
    {
        get { return _isInLoadDist; }
        set { _isInLoadDist = value; }
    }

    public bool isInStructDrawDist
    {
        get { return _isInStructDrawDist; }
        set { _isInStructDrawDist = value; }
    }

    public bool isInVBODrawDist
    {
        get { return _isInObDrawDist; }
        set { _isInObDrawDist = value; }
    }

    public void EditVoxel(Vector3 pos, byte newID, Controller _player, ChunkData _chunkData)
    {
        if (!World.Instance.IsGlobalPosInWorld(pos))
            return;

        if (newID == 0)
            World.Instance.RemoveVBOFromVoxel(pos); // removes VBO if voxel below is replaced with air
        else
        {
            // add VBO immediately whenever a new block is placed. Currently only objects are only replaced. Studs are replaced when update chunk is called.
            World.Instance.AddVBOToVoxel(pos, newID);
        }

        int xCheck = Mathf.FloorToInt(pos.x);
        int yCheck = Mathf.FloorToInt(pos.y);
        int zCheck = Mathf.FloorToInt(pos.z);

        xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
        zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

        // Added in https://www.youtube.com/watch?v=DjQ6yFRuZ7Q but is broken, does not allow player to modify voxels so commented out, instead use old code below
        chunkData.ModifyVoxel(new Vector3Int(xCheck, yCheck, zCheck), newID, _player.orientation); // write new block ID to chunkData 
        UpdateSurroundingVoxels(xCheck, yCheck, zCheck);

        //chunkData.map[xCheck, yCheck, zCheck].id = newID; // write new block ID to chunkData (needed if using cross chunk lighting as this doesn't get set in calculate lighting)
        //World.Instance.worldData.AddToModifiedChunkList(_chunkData); // commented out because chunks explored by player get modified for structures and marked to saved to file.
    }

    void UpdateSurroundingVoxels(int x, int y, int z)
    {
        Vector3 thisVoxel = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentVoxel = thisVoxel + VoxelData.faceChecks[p];

            if (!chunkData.IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
            {
                World.Instance.AddChunkToUpdate(World.Instance.GetChunkFromVector3(currentVoxel + position), true);
            }
        }
    }

    void UpdateMeshData(Vector3Int pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        VoxelState voxel = chunkData.map[x, y, z];

        float rot = 0f;
        switch (voxel.orientation)
        {
            case 0:
                rot = 180f;
                break;
            case 5:
                rot = 270f;
                break;
            case 1:
                rot = 0f;
                break;
            default:
                rot = 90f;
                break;
        }

        VoxelMeshData meshData;
        if(SettingsStatic.LoadedSettings.useStuds && voxel.hasStuds)
            meshData = voxel.properties.blockProperties.studsMeshData; // use studs mesh data
        else
            meshData = voxel.properties.blockProperties.standardMeshData; // use regular mesh data

        for (int p = 0; p < 6; p++)
        {
            int translatedP = p;

            if(voxel.orientation != 1)
            {
                if (voxel.orientation == 0)
                {
                    if (p == 0) translatedP = 1;
                    else if (p == 1) translatedP = 0;
                    else if (p == 4) translatedP = 5;
                    else if (p == 5) translatedP = 4;
                }
                else if (voxel.orientation == 5)
                {
                    if (p == 0) translatedP = 5;
                    else if (p == 1) translatedP = 4;
                    else if (p == 4) translatedP = 0;
                    else if (p == 5) translatedP = 1;
                }
                else if (voxel.orientation == 4)
                {
                    if (p == 0) translatedP = 4;
                    else if (p == 1) translatedP = 5;
                    else if (p == 4) translatedP = 1;
                    else if (p == 5) translatedP = 0;
                }
            }

            bool isInFirstChunkBorder = false; // initialize
            if(World.Instance.singleChunk) // only for singleChunk render mode
            {
                int worldSizeInChunksHalf = Mathf.FloorToInt(VoxelData.WorldSizeInChunks / 2); // cache for later

                if(coord.x == worldSizeInChunksHalf && coord.z == worldSizeInChunksHalf) // only first chunk
                {
                    if(pos.x == 0 || pos.x == 15|| pos.z == 0 || pos.z == 15) // only outer faces
                        isInFirstChunkBorder = true; // flag to render this voxels faces
                }
            }

            //Vector3Int neighborPos = pos + VoxelData.faceChecks[translatedP];
            //VoxelState neighbor = CheckVoxel(neighborPos);
            //VoxelState neighbor = chunkData.map[neighborPos.x, neighborPos.y, neighborPos.z];
            
            //  if(chunkData.map[x, y, z].neighbors[p] != null)
            //      neighbor = chunkData.map[x, y, z].neighbors[p];
            //VoxelState neighbor = chunkData.map[x, y, z].neighbors[p]; // DOES NOT WORK. Chunks loaded from file do not have neighbors which causes chunk to not render
            
            VoxelState neighbor = chunkData.map[x, y, z].neighbors[translatedP];

            // do not render if this voxel is water AND neighbor above is also water
            if (neighbor != null && (World.Instance.blockTypes[neighbor.id].blockProperties.renderNeighborFaces || isInFirstChunkBorder || pos.y == 0 || pos.y == VoxelData.ChunkHeight))// && !(World.Instance.blockTypes[voxel.id].isWater && World.Instance.blockTypes[chunkData.map[x, y + 1, z].id].isWater))
            {
                //float lightLevel = neighbor.lightAsFloat;
                int faceVertCount = 0;
                
                for (int i = 0; i < meshData.faces[p].vertData.Length; i++)
                {
                    VertData vertData = meshData.faces[p].GetVertData(i);
                    vertices.Add(pos + vertData.GetRotatedPosition(new Vector3(0, rot, 0)));
                    normals.Add(VoxelData.faceChecks[p]);

                    float lightLevel = neighbor.globalLightPercent;

                    colors.Add(new Color(0, 0, 0, lightLevel));

                    if(p == 2 && !voxel.hasStuds)
                        AddTexture(voxel.properties.GetTextureID(6), vertData.uv); // smooth top texture
                    else
                        AddTexture(voxel.properties.GetTextureID(p), vertData.uv); // studs texture
                    
                    faceVertCount++;
                }
                
                // ADD TRIANGLES
                if (!voxel.properties.blockProperties.renderNeighborFaces) // basic blocks not transparent
                {
                    for (int i = 0; i < meshData.faces[p].triangles.Length; i++)
                        triangles.Add(vertexIndex + meshData.faces[p].triangles[i]);
                }
                else
                {
                    // Creates rendering issues when displaying oceans, sometimes water blocks are missing, likely overloading an array, for now set water voxel to isTransparent = 0
                    if (voxel.properties.blockProperties.isWater) // water blocks
                    {
                        for (int i = 0; i < meshData.faces[p].triangles.Length; i++)
                            transparentTriangles.Add(vertexIndex + meshData.faces[p].triangles[i]); // need to apply the transparent material for water
                    }
                    else
                    {
                        for (int i = 0; i < meshData.faces[p].triangles.Length; i++)
                            transparentTriangles.Add(vertexIndex + meshData.faces[p].triangles[i]);
                    }
                }

                vertexIndex += faceVertCount;
            }
        }
    }

    VoxelState CheckVoxel (Vector3 pos) {

		int x = Mathf.FloorToInt (pos.x);
		int y = Mathf.FloorToInt (pos.y);
		int z = Mathf.FloorToInt (pos.z);

        if (!IsVoxelInChunk(x, y, z))
            return GetVoxelFromGlobalVector3(pos + position);

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

    public void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.subMeshCount = 3; // used for block and transBlock materials?
        mesh.SetTriangles(triangles.ToArray(), 0);
        mesh.SetTriangles(transparentTriangles.ToArray(), 1);
        mesh.SetTriangles(waterTriangles.ToArray(), 2);
        mesh.uv = uvs.ToArray();
        mesh.colors = colors.ToArray();
        mesh.normals = normals.ToArray();

        meshFilter.mesh = mesh;

        if (Settings.WebGL || SettingsStatic.LoadedSettings.drawChunkMeshes) // allows rigidbodies to collide with chunks
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