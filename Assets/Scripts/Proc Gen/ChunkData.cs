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

    // Optimize Data storage (possibly slow random access) 3D byte array into 1D string with Run Length Encoding?
    // map = 3D array of voxelstates/bytes, 1 byte = 8 bits (each voxelState = 1 byte which represents up to 2^8 (256) values)
    // so 16x16x8x96 = 196,608 bits (196.608 kB per chunk), save file is larger than this... not sure why
    // To share worlds over discord, need to reduce savedata to under 8 MB
    // enables chunk strings to be sent over the network to sync world files upon start
    // Make function Encode to encode the map values into a 1D string
    // aaaaabbbbacaa = 5a4b1a1c2a
    // order is predetermined to be chunk coords x,z, then voxel positions y 96-0 (top to bottom, first for chunk optimization), x 0-16 (left to right), z 0-16 (bottom to top)
    // a = 0, b = 1, c = 2, etc.
    // Minecraft further divides world into regions = 32x32 chunks https://docs.safe.com/fme/html/FME_Desktop_Documentation/FME_ReadersWriters/minecraft/minecraft.htm
    // Minecraft compresses save data to reduce level.data to 2 kB?!
    [HideInInspector]
    public VoxelState[,,] map = new VoxelState[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    public void Populate()
    {
        // currently populates all voxel data, but only needs to populate voxels which are adjacent to air
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
        VoxelState[,,] _map = chunk.map;
        string str;
        str = chunk.position.x.ToString();
        str += ",";
        str += chunk.position.y.ToString();
        str += ",";
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
            str += stringBlockIDs[y];
        str += ",";
        for (int x = 0; x < VoxelData.ChunkWidth; x++)
            str += stringBlockIDs[x];
        str += ",";
        for (int z = 0; z < VoxelData.ChunkWidth; z++)
            str += stringBlockIDs[z];
        str = StringTransform.RunLengthEncode(str);
        return str;
    }

    public ChunkData DecodeChunk(string str)
    {
        str = StringTransform.RunLengthDecode(str);

        string[] substrings = new string[] { };
        substrings = str.Split(',');

        int xChunkPos = int.Parse(substrings[0]);
        int zChunkPos = int.Parse(substrings[1]);
        ChunkData chunk = new ChunkData(xChunkPos, zChunkPos);

        VoxelState[] yVoxelStates = GetVoxelStatesFromString(substrings[2]);
        VoxelState[] xVoxelStates = GetVoxelStatesFromString(substrings[3]);
        VoxelState[] zVoxelStates = GetVoxelStatesFromString(substrings[4]);

        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    //map[x, y, z] = xVoxelStates[x], yVoxelStates[y], zVoxelStates[z]; // WIP
                }
            }
        }
        return chunk;
    }

    public VoxelState[] GetVoxelStatesFromString(string str)
    {
        VoxelState[] array = new VoxelState[] { };
        for(int i = 0; i < str.Length; i++)
        {
            for(int j = 0; j < stringBlockIDs.Length; j++)
            {
                if (str[i].ToString().Contains(stringBlockIDs[j]))
                    array[i] = new VoxelState(j);
            }
        }
        return array;
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
                returnValue += count;
                returnValue += str[i];
            }
            return returnValue;
        }
        catch(Exception e)
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
            int n = str.Length;
            for (int i = 0; i < n - 1; i += 2)
            {
                for (int j = 0; j < str[i]; j++)
                {
                    returnValue += str[i + 1];
                }
            }
            return returnValue;
        }
        catch(Exception e)
        {
            ErrorMessage.Show("Exception in Run Length Decoding: " + e.Message);
            return null;
        }
    }
}

public class StringTransform
{

    public const char EOF = '\u007F';
    public const char ESCAPE = '\\';

    public static string RunLengthEncode(string s)
    {
        try
        {
            string srle = string.Empty;
            int ccnt = 1; //char counter
            for (int i = 0; i < s.Length - 1; i++)
            {
                if (s[i] != s[i + 1] || i == s.Length - 2) //..a break in character repetition or the end of the string
                {
                    if (s[i] == s[i + 1] && i == s.Length - 2) //end of string condition
                        ccnt++;
                    srle += ccnt + ("1234567890".Contains(s[i].ToString()) ? "" + ESCAPE : "") + s[i]; //escape digits
                    if (s[i] != s[i + 1] && i == s.Length - 2) //end of string condition
                        srle += ("1234567890".Contains(s[i + 1].ToString()) ? "1" + ESCAPE : "") + s[i + 1];
                    ccnt = 1; //reset char repetition counter
                }
                else
                {
                    ccnt++;
                }

            }
            return srle;
        }
        catch (Exception e)
        {
            ErrorMessage.Show("Exception in Run Length Encoding: " + e.Message);
            return null;
        }
    }
    public static string RunLengthDecode(string s)
    {
        try
        {
            string dsrle = string.Empty
                    , ccnt = string.Empty; //char counter
            for (int i = 0; i < s.Length; i++)
            {
                if ("1234567890".Contains(s[i].ToString())) //extract repetition counter
                {
                    ccnt += s[i];
                }
                else
                {
                    if (s[i] == ESCAPE)
                    {
                        i++;
                    }
                    dsrle += new String(s[i], int.Parse(ccnt));
                    ccnt = "";
                }

            }
            return dsrle;
        }
        catch (Exception e)
        {
            ErrorMessage.Show("Exception in Run Length Decoding: " + e.Message);
            return null;
        }
    }
}