using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxelState
{
    public byte id; // 1 byte = 2^8 (256) unique block IDs

    [System.NonSerialized] public ChunkData chunkData;
    [System.NonSerialized] public VoxelNeighbors neighbors;
    [System.NonSerialized] public Vector3Int position;

    public VoxelState()
    {
        // default constructor
        id = 0;
    }

    public VoxelState(byte _id, ChunkData _chunkData, Vector3Int _position)
    {
        // constructor
        this.id = _id;
        chunkData = _chunkData;
        neighbors = new VoxelNeighbors(this);
        position = _position;
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
        get { return World.Instance.blocktypes[id]; }
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
            // If the requested neighbour is null, attempt to get it from WorldData.GetVoxel.
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
