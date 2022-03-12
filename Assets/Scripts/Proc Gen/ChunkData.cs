using System;
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

    [HideInInspector]
    public VoxelState[,,] map = new VoxelState[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    public void Populate()
    {
        // currently populates all voxel data, but only needs to populate voxels which are adjacent to air
        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++)
                {
                    map[x, y, z] = new VoxelState(World.Instance.GetVoxel(new Vector3(x + position.x, y, z + position.y)));
                }
            }
        }
    }

    public string[] stringBlockIDs = new string[]
    {
        "a", // 0
        "b", // 1
        "c", // 2
        "d", // 3
        "e", // 4
        "f", // 5
        "g", // 6
        "h", // 7
        "i", // 8
        "j", // 9
        "k", // 10
        "l", // 11
        "m", // 12
        "n", // 13
        "o", // 14
        "p", // 15
        "q", // 16
        "r", // 17
        "s", // 18
        "t", // 19
        "u", // 20
        "v", // 21
        "w", // 22
        "x", // 23
        "y", // 24
        "z", // 25
        "`", // 26
        "~", // 27
        "!", // 28
        "@", // 29
        "#", // 30
        "$", // 31
        "%", // 32
        "^", // 33
        "&", // 34
        "*", // 35
        "(", // 36
        ")", // 37
        "-", // 38
        "_", // 39
        "=", // 40
        "+", // 41
        "-", // 42
        "-", // 43
        "[", // 44
        "{", // 45
        "]", // 46
        "}", // 47
        "|", // 48
        ";", // 49
        ":", // 50
        "'", // 51
        "<", // 52
        ".", // 53
        ">", // 54
        "/", // 55
        "?", // 56
    };

    public string EncodeChunk(ChunkData chunk)
    {
        // Optimized Data storage (possibly slow random access) 3D byte array into 1D string with Run Length Encoding
        // map = 3D array of voxelstates/bytes, 1 byte = 8 bits (each voxelState = 1 byte which represents up to 2^8 (256) values)
        // bytes go from 16x16x96 = 24,796 to 5,000 a roughly 20% compression?
        // Compressing chunks from 3D byte arrays into 1D Run Length Encoded strings reduced each chunk file size from 361 KB to 5 KB
        // so 16x16x8x96 = 196,608 bits (196.608 kB per chunk), save file was 361 KB... not sure why
        // To share worlds over discord, need to reduce savedata to under 8 MB
        // enables chunk strings to be sent over the network to sync world files upon start
        // Make function Encode to encode the map values into a 1D string
        // aaaaabbbbacaa = 5a4b1a1c2a
        // order is predetermined to be chunk coords x,z, then voxel positions y 0-96 (bottom to top, first for chunk optimization), x 0-16 (left to right), z 0-16 (bottom to top)
        // a = 0, b = 1, c = 2, etc.
        // Minecraft further divides world into regions = 32x32 chunks https://docs.safe.com/fme/html/FME_Desktop_Documentation/FME_ReadersWriters/minecraft/minecraft.htm
        // Minecraft compresses save data to reduce level.data to 2 kB?!

        string str = string.Empty;
        for (int z = 0; z < VoxelData.ChunkWidth; z++) // last does next z value
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++) // then does next x value
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++) // first runs from y = 0 to 96 at x = 0, z = 0
                {
                    //if(_map[x, y, z] != 0) // do not store empty voxel states but mark end of vertical slice with 0?
                    str += stringBlockIDs[chunk.map[x, y, z].id];
                }
                str += ",";
            }
        }
        //Debug.Log(str);
        // example output: "dtadddddddddddddddddddaaadddddeaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa..."
        // with vertical slices of chunks smashed next to each other (stores 0's)

        str = chunk.position.x.ToString() + "," + chunk.position.y.ToString() + "," + RunLengthEncode(str);
        //Debug.Log(str);
        // example output: "1,2,1d1t1a19d3a5d1e65a,1d1t20d3a5d1e65a..."

        return str;
    }

    public ChunkData DecodeChunk(string str)
    {
        //Debug.Log(str);
        string[] substrings = new string[] { };
        substrings = str.Split(',');
        //Debug.Log(substrings[0]);
        //Debug.Log(substrings[1]);

        for (int i = 2; i < substrings.Length - 1; i++)
        {
            substrings[i] = RunLengthDecode(substrings[i]);
            //Debug.Log(substrings[i]);
            // example output: "dtadddddddddddddddddddaaadddddeaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa..."
        }

        int xChunkPos = int.Parse(substrings[0]);
        int zChunkPos = int.Parse(substrings[1]);

        //Debug.Log(xChunkPos);
        //Debug.Log(zChunkPos);
        ChunkData chunk = new ChunkData(xChunkPos, zChunkPos);

        string voxelString = string.Empty;
        //Debug.Log(substrings[0]);
        //Debug.Log(substrings[1]);
        Debug.Log("looking for " + RunLengthEncode(substrings[50]));
        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++)
                {
                    chunk.map[x, y, z] = GetVoxelStateFromString(substrings[2 + x + 16 * z])[y];
                    voxelString += stringBlockIDs[GetVoxelStateFromString(substrings[2 + x  + 16 * z])[y].id]; // need to revise this formula
                }
                Debug.Log("stubstring[" + x + z + "] = " + RunLengthEncode(voxelString));
                voxelString = string.Empty;
            }
        }
        return chunk;
    }

    public VoxelState[] GetVoxelStateFromString(string str)
    {
        // get an array of voxelStates for all y positions for a given x and z coordinate in a chunk
        VoxelState[] yVoxelStates = new VoxelState[str.Length];
        for (int i = 0; i < str.Length; i++)
        {
            for (int j = 0; j < stringBlockIDs.Length; j++)
            {
                if (str[i].ToString().Contains(stringBlockIDs[j]))
                    yVoxelStates[i] = new VoxelState((byte)j);
            }
        }
        return yVoxelStates;
    }

    public string RunLengthEncode(string str)
    {
        // example input str = "aaaaabbbbacaa"
        // example output returnValue = "5a4b1a1c2a"
        try
        {
            string returnValue = string.Empty;
            int n = str.Length;
            for (int i = 0; i < n; i++)
            {
                int count = 1;
                while (i < n - 1 && str[i] == str[i + 1])
                {
                    count++;
                    i++;
                }
                if(!str[i].ToString().Contains(","))
                    returnValue += count;
                returnValue += str[i];
            }
            return returnValue;
        }
        catch (Exception e)
        {
            ErrorMessage.Show("Exception in Run Length Encoding: " + e.Message);
            return null;
        }
    }

    public string RunLengthDecode(string str)
    {
        // example input returnValue = "5a4b1a1c2a"
        // example output str = "aaaaabbbbacaa"
        try
        {
            string returnValue = string.Empty;
            string charCount = string.Empty;
            for (int i = 0; i < str.Length; i++)
            {
                if ("1234567890".Contains(str[i].ToString())) //extract repetition counter
                    charCount += str[i];
                else
                {
                    returnValue += new string(str[i], int.Parse(charCount));
                    charCount = "";
                }
            }
            return returnValue;
        }
        catch (Exception e)
        {
            ErrorMessage.Show("Exception in Run Length Decoding: " + e.Message);
            return null;
        }
    }
}