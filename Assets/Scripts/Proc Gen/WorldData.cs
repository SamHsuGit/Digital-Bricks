using System.Collections.Generic;
using UnityEngine;

[HideInInspector]
[System.Serializable]
public class WorldData
{
    public int planetNumber;
    public int seed;
    public int sizeInChunks;
    public int distToStar;
    public int system;
    public int galaxy;
    public byte blockIDsubsurface;
    public byte blockIDcore;
    public byte blockIDBiome00;
    public byte blockIDBiome01;
    public byte blockIDBiome02;
    public byte blockIDBiome03;
    public byte blockIDBiome04;
    public byte blockIDBiome05;
    public byte blockIDBiome06;
    public byte blockIDBiome07;
    public byte blockIDBiome08;
    public byte blockIDBiome09;
    public byte blockIDBiome10;
    public byte blockIDBiome11;
    public bool hasAtmosphere;
    public bool isAlive; // controls if the world is hospitable to flora
    public int[] biomes;
    public byte blockIDTreeLeavesWinter;
    public byte blockIDTreeLeavesSpring;
    public byte blockIDTreeLeavesSummer;
    public byte blockIDTreeLeavesFall1;
    public byte blockIDTreeLeavesFall2;
    public byte blockIDTreeTrunk;
    public byte blockIDCacti;
    public byte blockIDMushroomLargeCap;
    public byte blockIDMushroomLargeStem;
    public byte blockIDMonolith;
    public byte blockIDEvergreenLeaves;
    public byte blockIDEvergreenTrunk;
    public byte blockIDHoneyComb;
    public byte blockIDHugeTreeLeaves;
    public byte blockIDHugeTreeTrunk;
    public byte blockIDColumn;

    [System.NonSerialized]
    public Dictionary<Vector2Int, ChunkData> chunks = new Dictionary<Vector2Int, ChunkData>();

    [System.NonSerialized]
    public List<ChunkData> modifiedChunks = new List<ChunkData>();

    public void AddToModifiedChunkList (ChunkData chunk)
    {
        if (!modifiedChunks.Contains(chunk))
            modifiedChunks.Add(chunk);
    }

    public WorldData (int planetNumber, int seed, int sizeInChunks)
    {
        this.planetNumber = planetNumber;
        this.seed = seed;
        this.sizeInChunks = sizeInChunks;
    }

    public WorldData()
    {
        // default constructor for deserialization

        planetNumber = 3;
        seed = 1;
    }

    public WorldData(WorldData wD)
    {
        planetNumber = wD.planetNumber;
        seed = wD.seed;
    }

    public ChunkData RequestChunk (Vector2Int coord)
    {
        ChunkData c;

        lock (World.Instance.ChunkListThreadLock)
        {
            if (chunks.ContainsKey(coord)) // if chunk already exists, return it instead of loading from file
            {
                c = chunks[coord];
            }
            else // If it doesn't exist, create the chunk then return it.
            {
                LoadChunkFromFile(coord);
                c = chunks[coord];
            }
        }

        return c;
    }

    public void LoadChunkFromFile(Vector2Int coord)
    {
        // assumes chunks.ContainsKey(coord) = false

        // attempt to load the chunk from memory (checks if file exists)
        ChunkData chunk = SaveSystem.LoadChunk(SettingsStatic.LoadedSettings.planetNumber, SettingsStatic.LoadedSettings.seed, SettingsStatic.LoadedSettings.worldSizeinChunks, coord); // can be slow if loading lots of chunks from memory?
        if (chunk != null)
        {
            chunks.Add(coord, chunk);
            return;
        }
        else
        {
            // generate new chunk data using the GetVoxel procGen algorithm
            chunks.Add(coord, new ChunkData(coord));
            chunks[coord].Populate();
        }
    }

    bool IsVoxelInWorld(Vector3 pos)
    {

        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        else
            return false;

    }

    public void SetVoxel (Vector3 pos, byte value)
    {
        // If the voxel is outside of the world we don't need to do anything with it.
        if (!IsVoxelInWorld(pos))
            return;

        // Find out the ChunkCoord value of our voxel's chunk.
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        // Then reverse that to get the position of the chunk.
        x *= VoxelData.ChunkWidth;
        z *= VoxelData.ChunkWidth;

        // Check if the chunk exists. If not, create it.
        ChunkData chunk = RequestChunk(new Vector2Int(x, z));

        // Then create a Vector3Int with the position of our voxel *within* the chunk.
        Vector3Int voxel = new Vector3Int((int)(pos.x -x),(int)pos.y, (int)(pos.z - z));

        // Then set the voxel in our chunk.
        chunk.map[voxel.x, voxel.y, voxel.z].id = value;
        //AddToModifiedChunkList(chunk); // DO NOT NEED TO SAVE ALL CHUNKS WITH STRUCTURES, GAME CAN GENERATE UPON START
    }

    public VoxelState GetVoxel(Vector3 pos)
    {
        // If the voxel is outside of the world we don't need to do anything with it.
        if (!IsVoxelInWorld(pos))
            return null;

        // Find out the ChunkCoord value of our voxel's chunk.
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);

        // Then reverse that to get the position of the chunk.
        x *= VoxelData.ChunkWidth;
        z *= VoxelData.ChunkWidth;

        // Check if the chunk exists. If not, create it.
        ChunkData chunk = RequestChunk(new Vector2Int(x, z));

        // Then create a Vector3Int with the position of our voxel *within* the chunk.
        Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));

        // Then set the voxel in our chunk.
        return new VoxelState(chunk.map[voxel.x, voxel.y, voxel.z].id);
    }
}