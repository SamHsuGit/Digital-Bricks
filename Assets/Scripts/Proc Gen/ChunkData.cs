using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

[System.Serializable]
public class ChunkData
{
    // ChunkData was created to load data separately before chunks are created to create an efficient load time.

    // Set a goal to reduce savedata to under 8 MB to share worlds over internet in a reasonable way.
    // enables chunk strings to be sent over the network to sync world files upon start

    // Each voxel has a voxelState byte (8 bits = 2^8 = 256 blockIDs)
    // Each Chunk has a 3D Byte Array called map = Byte[16, 96, 16] = 16x96x16 voxels = 24,576 bytes
    // Therefore, each chunk file should be only 24 KB (actual is 361 KB for 96 chunkHeight, 961 KB for 256 chunkHeight, 15 times larger, perhaps due to Unity Serialization?

    // Optimized Data storage (possibly slow random access) 3D byte array into 1D string with Run Length Encoding
    // Non Run Length Encoded bytes (theoretical) = 16x16x96 = 24,796 bytes (24.8 KB)
    // Non Run Length Encoded bytes (actual) = 361,000 bytes (361 KB)
    // Run Length Encoded Memory Usage = 5,000 bytes (5KB) (~20% compression)

    // Future Optimizations:
    // Only save the voxel states of modified voxels instead of an entire chunk

    // Minecraft Optimizations:
    // Minecraft uses Named Binary Tag Format to efficiently store binary data related to chunks in region files
    // Minecraft Chunks were originally stored as individual ".dat" files with the chunk position encoded in Base36

    // MCRegion began storing groups of 32×32 chunks in individual ".mcr" files
    // with coordinates in Base10 to reduce disk usage by cutting down on the number of file handles Minecraft had open at once
    // (because Minecraft is constantly reading/writing to world data as it saves in run-time and ran up against a hard limit of 1024 open handles)

    // Minecraft Anvil Format changed the max build height to 256 and empty sections were no longer saved or loaded to disk
    // max # Block IDs was increased to 4096 (was 256) by adding a 4-bit data layer (similar to how meta data is stored).
    // Minecraft Block ordering was been changed from XZY to YZX in order to improve compression.

    // Minecraft further divides world into regions = 32x32 chunks https://docs.safe.com/fme/html/FME_Desktop_Documentation/FME_ReadersWriters/minecraft/minecraft.htm
    // Minecraft compresses save data to reduce level.data to 2 kB?!


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
    public ChunkData(int x, int y) { this.x = x; this.y = y; }
    public ChunkData() { x = 0; y = 0; } // default constructor for deserialization

    [System.NonSerialized] public Chunk chunk;

    [HideInInspector]
    public VoxelState[,,] map = new VoxelState[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    public VoxelState state;

    public void Populate()
    {
        // Profiling Reveals this is the most resource intensive operation (takes a long time primarily due to the number of iterations 16x16x96)
        // GC alone takes up 12.9% of CPU time

        World.Instance.StartProfileMarker();
        //Debug.Log("ChunkData.Populate");
        // currently populates all voxel data, but only needs to populate voxels which are adjacent to air
        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++)
                {
                    Vector3Int voxelGlobalPos = new Vector3Int(x + position.x, y, z + position.y);

                    // reuse same state voxelState variable to reduce garbage collection
                    //state.id = World.Instance.GetVoxel(voxelGlobalPos);
                    //state.chunkData = this;
                    //state.neighbors = new VoxelNeighbors(state);
                    //state.position = new Vector3Int(x, y, z);
                    //map[x, y, z] = state;

                    map[x, y, z] = new VoxelState(World.Instance.GetVoxel(voxelGlobalPos), this, new Vector3Int(x, y, z) , 1); // by default all blocks face forwards (otherwise, implement code to determine block orientation procedurally)
                }
            }
        }
        World.Instance.StopProfileMarker();
    }

    public void ModifyVoxel(Vector3Int pos, byte _id, byte direction)
    {

        // If we've somehow tried to change a block for the same block, just return.
        if (map[pos.x, pos.y, pos.z].id == _id)
            return;

        // Cache voxels for easier code.
        VoxelState voxel = map[pos.x, pos.y, pos.z];
        BlockType newVoxel = World.Instance.blockTypes[_id];

        // Cache the old opacity value.
        //byte oldOpacity = voxel.properties.opacity;

        // Set voxel to new ID.
        voxel.id = _id;
        voxel.orientation = direction;

        if (voxel.properties.isActive && BlockBehavior.Active(voxel))
            voxel.chunkData.chunk.AddActiveVoxel(voxel);
        for (int i = 0; i < 6; i++)
        {
            if (voxel.neighbors[i] != null)
                if (voxel.neighbors[i].properties.isActive && BlockBehavior.Active(voxel.neighbors[i]))
                    voxel.neighbors[i].chunkData.chunk.AddActiveVoxel(voxel.neighbors[i]);
        }

        // Do not add this to list of chunks to be saved (do not want to save all chunks where surfaceObjects/structures/trees are created, only player edited chunks).
        //World.Instance.worldData.AddToModifiedChunkList(this);

        // If we have a chunk attached, add that for updating.
        if (chunk != null)
            World.Instance.AddChunkToUpdate(chunk);
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
        "A", // 26
        "B", // 27
        "C", // 28
        "D", // 29
        "E", // 30
        "F", // 31
        "G", // 32
        "H", // 33
        "I", // 34
        "J", // 35
        "K", // 36
        "L", // 37
        "M", // 38
        "N", // 39
        "O", // 40
        "P", // 41
        "Q", // 42
        "R", // 43
        "S", // 44
        "T", // 45
        "U", // 46
        "V", // 47
        "W", // 48
        "X", // 49
        "Y", // 50
        "Z", // 51
        "`", // 52
        "~", // 53
        "!", // 54
        "@", // 55
        "#", // 56
        "$", // 57
        "%", // 58
        "^", // 59
        "&", // 60
        "*", // 61
        "(", // 62
        ")", // 63
        "-", // 64
        "_", // 65
        "=", // 66
        "+", // 67
        "-", // 68
        "-", // 69
        "[", // 70
        "{", // 71
        "]", // 72
        "}", // 73
        "|", // 74
        ":", // 75
        "'", // 76
        "<", // 77
        ".", // 78
        ">", // 79
        "/", // 80
        "?", // 81
    };

    public byte[] EncodeByteArray(ChunkData chunkData)
    {
        byte[] voxelBytes = new byte[2 + (VoxelData.ChunkWidth * VoxelData.ChunkWidth * VoxelData.ChunkHeight) * 2];
        voxelBytes[0] = (byte)chunkData.position.x;
        voxelBytes[1] = (byte)chunkData.position.y;
        int i = 2;

        for (int z = 0; z < VoxelData.ChunkWidth; z++) // last does next z value
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++) // then does next x value
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++) // first runs from y = 0 to 96 at x = 0, z = 0
                {
                    voxelBytes[i] = chunkData.map[x, y, z].id;
                    voxelBytes[i + 1] = chunkData.map[x, y, z].orientation;
                    i = i + 2;
                }
            }
        }
        voxelBytes = Compressor.Compress(voxelBytes);
        return voxelBytes;
    }

    public ChunkData DecodeByteArray(byte[] voxelBytes)
    {
        voxelBytes = Compressor.Decompress(voxelBytes);
        int xChunkPos = (int)voxelBytes[0];
        int zChunkPos = (int)voxelBytes[1];

        ChunkData chunkData = new ChunkData(xChunkPos, zChunkPos);
        int i = 2;
        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++)
                {
                    chunkData.map[x, y, z] = new VoxelState(voxelBytes[i], chunkData, new Vector3Int(x,y,z), voxelBytes[i + 1]);
                    i = i + 2;
                }
            }
        }
        return chunkData;
    }


    // THE FOLLOWING FUNCTIONS NEEDED TO SEND DATA OVER NETWORK (MIRROR DOCUMENTATION: https://mirror-networking.gitbook.io/docs/guides/data-types)
    public string EncodeString(ChunkData chunkData)
    {
        // Encodes chunks into a list of voxelStates runs from bottom to top of chunk, then to next increments x position, then next z position
        StringBuilder sb = new StringBuilder(); // used as recommended per https://docs.unity3d.com/2020.3/Documentation/Manual/performance-garbage-collection-best-practices.html
        string str = string.Empty;
        for (int z = 0; z < VoxelData.ChunkWidth; z++) // last does next z value
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++) // then does next x value
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++) // first runs from y = 0 to 96 at x = 0, z = 0
                {
                    sb.Append(stringBlockIDs[chunkData.map[x, y, z].id]);
                }
                sb.Append(",");
            }
        }
        str = sb.ToString();

        str = chunkData.position.x.ToString() + "," + chunkData.position.y.ToString() + "," + RLEString(str);

        return str;
    }

    public ChunkData DecodeString(string str)
    {
        string[] substrings = new string[] { };
        substrings = str.Split(',');

        for (int i = 2; i < substrings.Length - 1; i++)
        {
            substrings[i] = RLDString(substrings[i]);
        }

        int xChunkPos = int.Parse(substrings[0]);
        int zChunkPos = int.Parse(substrings[1]);

        ChunkData chunkData = new ChunkData(xChunkPos, zChunkPos);

        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++)
                {
                    chunkData.map[x, y, z] = GetSliceFromString(substrings[2 + x + VoxelData.ChunkWidth * z], x, y, z)[y];
                }
            }
        }
        return chunkData;
    }

    public VoxelState[] GetSliceFromString(string str, int _x, int _y, int _z)
    {
        // get an array of voxelStates for all y positions for a given x and z coordinate in a chunk
        VoxelState[] yVoxelStates = new VoxelState[str.Length];
        for (int i = 0; i < str.Length; i++)
        {
            for (int j = 0; j < stringBlockIDs.Length; j++)
            {
                if (str[i].ToString().Contains(stringBlockIDs[j]))
                    yVoxelStates[i] = new VoxelState((byte)j, this, new Vector3Int(_x, _y, _z), 1); // default orient all blocks to face forwards (i.e. orientation = 1)
            }
        }
        return yVoxelStates;
    }

    public string RLEString(string str)
    {
        // example input str = "aaaaabbbbacaa" represents the voxel states in a vertical slice of a chunk (bottom to top)
        // example output returnValue = "5a4b1a1c2a" represents the voxel states in a vertical slice of a chunk (bottom to top)
        try
        {
            StringBuilder sb = new StringBuilder();
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
                if (!str[i].ToString().Contains(","))
                {
                    sb.Append(count);
                }
                sb.Append(str[i]);
            }
            return sb.ToString();
        }
        catch (Exception e)
        {
            Debug.Log("Exception in Run Length Encoding: " + e.Message);
            return null;
        }
    }

    public string RLDString(string str)
    {
        // example input returnValue = "5a4b1a1c2a" represents the voxel states in a vertical slice of a chunk (bottom to top)
        // example output str = "aaaaabbbbacaa" represents the voxel states in a vertical slice of a chunk (bottom to top)
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
            Debug.Log("Exception in Run Length Decoding: " + e.Message);
            return null;
        }
    }
}

class Compressor
{
    //GZIP (RLE) Compression from https://stackoverflow.com/questions/1932691/how-to-do-rle-run-length-encoding-in-c-sharp-on-a-byte-array
    public static byte[] Compress(byte[] buffer)
    {
        MemoryStream ms = new MemoryStream();
        GZipStream zip = new GZipStream(ms, CompressionMode.Compress, true);
        zip.Write(buffer, 0, buffer.Length);
        zip.Close();
        ms.Position = 0;

        byte[] compressed = new byte[ms.Length];
        ms.Read(compressed, 0, compressed.Length);

        byte[] gzBuffer = new byte[compressed.Length + 4];
        Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
        Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
        return gzBuffer;
    }
    public static byte[] Decompress(byte[] gzBuffer)
    {
        MemoryStream ms = new MemoryStream();
        int msgLength = BitConverter.ToInt32(gzBuffer, 0);
        ms.Write(gzBuffer, 4, gzBuffer.Length - 4);

        byte[] buffer = new byte[msgLength];

        ms.Position = 0;
        GZipStream zip = new GZipStream(ms, CompressionMode.Decompress);
        zip.Read(buffer, 0, buffer.Length);

        return buffer;
    }
}