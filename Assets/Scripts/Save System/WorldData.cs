using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[HideInInspector]
[System.Serializable]
public class WorldData
{
    public int planetNumber;
    public int seed;
    public int distToStar;
    public int system;
    public int galaxy;
    public byte blockIDsubsurface;
    public byte blockIDcore;
    public byte blockIDForest;
    public byte blockIDGrasslands;
    public byte blockIDDesert;
    public byte blockIDDeadForest;
    public byte blockIDHugeTree;
    public byte blockIDMountain;
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

    public WorldData (int _planetNumber, int _seed)
    {
        planetNumber = _planetNumber;
        seed = _seed;
    }

    public WorldData() // default constructor for deserialization
    {
        planetNumber = 3;
        seed = 1234;
    }

    public WorldData(WorldData wD)
    {
        planetNumber = wD.planetNumber;
        seed = wD.seed;
    }

    public ChunkData RequestChunk (Vector2Int coord, bool create)
    {
        ChunkData c;

        lock (World.Instance.ChunkListThreadLock)
        {
            if (chunks.ContainsKey(coord)) // if chunk already exists, return it instead of loading from file
            {
                c = chunks[coord];
            }
            else if (!create) // If it doesn't exist and we haven't asked it to be created, return null.
            {
                return null;
            }
            else // If it doesn't exist and we asked it to be created, create the chunk then return it (slow to load from file).
            {
                LoadChunkFromFile(coord); // (THIS IS SLOW/EXPENSIVE)
                c = chunks[coord];
            }
        }

        return c;
    }

    public void LoadChunkFromFile(Vector2Int coord)
    {
        if (chunks.ContainsKey(coord))
            return;

        ChunkData chunk = SaveSystem.LoadChunk(SettingsStatic.LoadedSettings.planetNumber, SettingsStatic.LoadedSettings.seed, coord); // (THIS IS SLOW/EXPENSIVE)
        if (chunk != null)
        {
            chunks.Add(coord, chunk);
            return;
        }

        chunks.Add(coord, new ChunkData(coord));
        chunks[coord].Populate();
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
        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

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
        ChunkData chunk = RequestChunk(new Vector2Int(x, z), true);

        // Then create a Vector3Int with the position of our voxel *within* the chunk.
        Vector3Int voxel = new Vector3Int((int)(pos.x - x), (int)pos.y, (int)(pos.z - z));

        // Then set the voxel in our chunk.
        return chunk.map[voxel.x, voxel.y, voxel.z];
    }
}