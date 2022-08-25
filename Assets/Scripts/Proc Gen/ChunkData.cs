using System;
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

    public void Populate()
    {
        //Debug.Log("ChunkData.Populate");
        // currently populates all voxel data, but only needs to populate voxels which are adjacent to air
        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++)
                {
                    Vector3 voxelGlobalPos = new Vector3(x + position.x, y, z + position.y);

                    map[x, y, z] = new VoxelState(World.Instance.GetVoxel(voxelGlobalPos), this, new Vector3Int(x,y,z));
                }
            }
        }
    }

    public void ModifyVoxel(Vector3Int pos, byte _id, int direction)
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
        //voxel.orientation = direction;

        //// If the opacity values of the voxel have changed and the voxel above is in direct sunlight
        //// (or is above the world) recast light from that voxel downwards.
        //if (voxel.properties.opacity != oldOpacity &&
        //    (pos.y == VoxelData.ChunkHeight - 1 || map[pos.x, pos.y + 1, pos.z].light == 15))
        //{
        //    Lighting.CastNaturalLight(this, pos.x, pos.z, pos.y + 1);
        //}

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
            World.Instance.activeChunks.Add(chunk.coord);

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

    public string EncodeChunk(ChunkData chunk)
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
                    sb.Append(stringBlockIDs[chunk.map[x, y, z].id]);
                }
                sb.Append(",");
            }
        }
        str = sb.ToString();

        str = chunk.position.x.ToString() + "," + chunk.position.y.ToString() + "," + RunLengthEncode(str);

        return str;
    }

    public ChunkData DecodeChunk(string str)
    {
        string[] substrings = new string[] { };
        substrings = str.Split(',');

        for (int i = 2; i < substrings.Length - 1; i++)
        {
            substrings[i] = RunLengthDecode(substrings[i]);
        }

        int xChunkPos = int.Parse(substrings[0]);
        int zChunkPos = int.Parse(substrings[1]);

        ChunkData chunk = new ChunkData(xChunkPos, zChunkPos);

        for (int z = 0; z < VoxelData.ChunkWidth; z++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++)
                {
                    chunk.map[x, y, z] = GetVoxelStateFromString(substrings[2 + x + 16 * z], x, y, z)[y];
                }
            }
        }
        return chunk;
    }

    public VoxelState[] GetVoxelStateFromString(string str, int _x, int _y, int _z)
    {
        // get an array of voxelStates for all y positions for a given x and z coordinate in a chunk
        VoxelState[] yVoxelStates = new VoxelState[str.Length];
        for (int i = 0; i < str.Length; i++)
        {
            for (int j = 0; j < stringBlockIDs.Length; j++)
            {
                if (str[i].ToString().Contains(stringBlockIDs[j]))
                    yVoxelStates[i] = new VoxelState((byte)j, this,new Vector3Int(_x, _y, _z));
            }
        }
        return yVoxelStates;
    }

    public string RunLengthEncode(string str)
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
            ErrorMessage.Show("Exception in Run Length Encoding: " + e.Message);
            return null;
        }
    }

    public string RunLengthDecode(string str)
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
            ErrorMessage.Show("Exception in Run Length Decoding: " + e.Message);
            return null;
        }
    }
}