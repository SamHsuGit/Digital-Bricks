using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxelState
{
    public byte id; // 1 byte = 2^8 (256) unique block IDs
    [System.NonSerialized] private byte _light;
    public float globalLightPercent = 0;
    public byte orientation; // 1 byte used to store block state into such as orientation
    [System.NonSerialized] public bool hasStuds = false;
    [System.NonSerialized] public float randFloat;
    [System.NonSerialized] public static float studsThreshold = 0.90f;
    [System.NonSerialized] public ChunkData chunkData;
    [System.NonSerialized] public VoxelNeighbors neighbors;
    [System.NonSerialized] public Vector3Int position;

    public byte light {

        get { return _light; }
        set {

            if (value != _light) {

                // Cache the old light and castlight values before updating them.
                byte oldLightValue = _light;
                byte oldCastValue = castLight;

                // Set light value to new value.
                _light = value;

                // If our new light value is darker than the old one, check our neighbouring voxels.
                if (_light < oldLightValue) {

                    List<int> neigboursToDarken = new List<int>();

                    // Loop through each neighbour.
                    for (int p = 0; p < 6; p++) {

                        // Make sure we have a neigbour here before trying to do anything with it.
                        if (neighbors[p] != null) {
                            // If a neigbour is less than or equal to our old light value, that means
                            // this voxel might have been lighting it up. We want to set it's light value
                            // to zero and then it will run its own neighbour checks, but we don't want to
                            // do that until we've finished here, so add it to our list and we'll do it
                            // after.
                            if (neighbors[p].light <= oldCastValue)
                                neigboursToDarken.Add(p);
                            // If the neighbour is brighter than our old value, then that voxel
                            // is being lit from somewhere else. We then tell that voxel to propogate and, if it
                            // is lighter than this voxel, light will be propogated to here.
                            else {
                                neighbors[p].PropogateLight();
                            }


                        }
                    }

                    // Loop through our neighbors for darkening and set their light to zero. They will then
                    // perform their own neighbour checks and tell any brighter voxels (including this one)
                    // to propogate.
                    foreach (int i in neigboursToDarken) {

                        neighbors[i].light = 0;

                    }

                    // If this voxel is part of an active chunk, add that chunk for updating.
                    if (chunkData.chunk != null)
                        World.Instance.AddChunkToUpdate(chunkData.chunk);

                } else if (_light > 1)
                    PropogateLight();

            }

        }

    }

    public VoxelState(byte _id, ChunkData _chunkData, Vector3Int _position, byte _orientation)
    {
        // constructor
        id = _id;
        orientation = _orientation;
        chunkData = _chunkData; // need to set before light
        neighbors = new VoxelNeighbors(this);
        position = _position;
        globalLightPercent = 0f;
        light = 0;

        if(SettingsStatic.LoadedSettings.useStuds)
        {
            randFloat = StaticRandom.GetRandom();
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

    public float lightAsFloat {

        get { return (float)light * VoxelData.unitOfLight; }

    }

    public byte castLight {

        get {

            // Get the amount of light this voxel is spreading. Bytes rap around so we
            // need to do this with an int so we can make sure it doesn't get below 0.
            int lightLevel = _light - properties.opacity - 1;
            if (lightLevel < 0) lightLevel = 0;
            return (byte)lightLevel;

        }

    }

    public void PropogateLight () {

        // If we somehow added a null voxel or one that isn't bright enough to propogate, return.
        if (light < 2)
            return;

        // Loop through each neighbour of this voxel.
        for (int p = 0; p < 6; p++) {

            // We can only propogate to voxels that exist so check there is one first.
            if (neighbors[p] != null) {

                // We can ONLY propogate light in one direction (lighter to darker). If
                // we work in both direction, we will get recurssive loops.
                // So any neighbors who are not darker than this voxel's lightCast value,
                // we leave alone.
                if (neighbors[p].light < castLight)
                    neighbors[p].light = castLight;

            }

            if (chunkData.chunk != null)
                World.Instance.AddChunkToUpdate(chunkData.chunk);

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
