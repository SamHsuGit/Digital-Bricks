using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ChunkData
{
    // The global position of the chunk. ie, (16, 16) NOT (1, 1). We want to be able to
    // access it as a Vector2Int, but Vector2Int's are not serialized so we won't be able
    // to save them. So we'll store them as ints.
    int x;
    int y;
    public Vector2Int position
    {
        get
        {
            return new Vector2Int(x, y); // chunk location stored as a 2D Vector with ints as components. Int locations can be from -2147483648 to 2147483648. Each int takes 16 bits
        }
        set
        {
            x = value.x;
            y = value.y;
        }
    }

    public ChunkData(Vector2Int pos) { position = pos; }
    public ChunkData(int _x, int _y) { x = _x; y = _y; }
    public ChunkData() { x = 0; y = 0; } // default constructor for deserialization

    // Data storage optimization
    // map = 3D array of bytes, 1 byte = 8 bits, so 16x16x8x96 = 196,608 bits (196.608 kB per chunk), save file is larger than this... not sure why
    // To share worlds over discord, need to reduce savedata to under 8 MB: use RLE compression to reduce 3D chunk array into 3 unsigned integers?
    // Minecraft further divides world into regions = 32x32 chunks https://docs.safe.com/fme/html/FME_Desktop_Documentation/FME_ReadersWriters/minecraft/minecraft.htm
    // Minecraft compresses save data to reduce level.data to 2 kB?!
    [HideInInspector]
    public VoxelState[,,] map = new VoxelState[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    public void Populate()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    map[x, y, z] = new VoxelState(World.Instance.GetVoxel(new Vector3(x + position.x, y, z + position.y)));
                }
            }
        }
    }
}