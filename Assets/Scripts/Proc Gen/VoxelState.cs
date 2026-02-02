using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

[System.Serializable]
public class VoxelState
{
    public byte id; // 1 byte = 2^8 (256) unique block IDs
    public byte orientation; // 1 byte used to store block state into such as orientation
    public bool hasStuds = false;
    public float randFloat;
    public static float studsThreshold = 0.90f;
    [System.NonSerialized] public ChunkData chunkData;
    [System.NonSerialized] public VoxelNeighbors neighbors;
    [System.NonSerialized] public Vector3Int position;

    public VoxelState(byte _id, ChunkData _chunkData, Vector3Int _position, byte _orientation)
    {
        // constructor
        id = _id;
        orientation = _orientation;
        chunkData = _chunkData;
        neighbors = new VoxelNeighbors(this);
        position = _position;

        if(SettingsStatic.LoadedSettings.useStuds)
        {
            randFloat = StaticRandom.RandFloat();
            if(randFloat >= studsThreshold) // controls if studs are visible on top of bricks
                hasStuds = true;
            else
                hasStuds = false;
        }
    }

    public Vector3Int globalPosition
    {
        get
        {
            return new Vector3Int(position.x + chunkData.position.x, position.y, position.z + chunkData.position.y);
        }
    }

    public BlockType properties
    {
        get { return World.Instance.blockTypes[id]; }
    }
}

public class VoxelNeighbors
{

    public readonly VoxelState parent;
    public VoxelNeighbors(VoxelState _parent) { parent = _parent; }

    private VoxelState[] _neighbors = new VoxelState[6];

    public int Length { get { return _neighbors.Length; } }

    public VoxelState this[int index]
    {
        get
        {
            // If the requested neighbour is null, attempt to get it from WorldData.GetVoxel which tries to get saved data but generates new data if does not exist.
            if (_neighbors[index] == null)
            {
                _neighbors[index] = World.Instance.worldData.GetVoxel(parent.globalPosition + VoxelData.faceChecks[index]);
                ReturnNeighbour(index);
            }

            // Return whatever we have. If it's null at this point, it means that neighbour doesn't exist yet.
            return _neighbors[index];
        }
        set
        {
            _neighbors[index] = value;
            ReturnNeighbour(index);
        }
    }

    void ReturnNeighbour(int index)
    {
        // Can't set our neighbour's neighbour if the neighbour is null.
        if (_neighbors[index] == null)
            return;

        // If the opposite neighbour of our voxel is null, set it to this voxel.
        // The opposite neighbour will perform the same check but that check will return true
        // because this neighbour is already set, so we won't run into an endless loop, freezing Unity.
        if (_neighbors[index].neighbors[VoxelData.revFaceCheckIndex[index]] != parent)
            _neighbors[index].neighbors[VoxelData.revFaceCheckIndex[index]] = parent;
    }

}

public static class StaticRandom
{
    static int seed = System.Environment.TickCount;

    static readonly ThreadLocal<System.Random> random =
        new ThreadLocal<System.Random>(() => new System.Random(Interlocked.Increment(ref seed)));

    public static float RandFloat()
    {
        return (float)random.Value.NextDouble();
    }
}
