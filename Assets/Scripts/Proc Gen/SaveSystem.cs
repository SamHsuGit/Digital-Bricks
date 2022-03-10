using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

// NOTES
// https://minecraft.fandom.com/wiki/Chunk_format
// https://wiki.vg/NBT
// https://gaming.stackexchange.com/questions/263816/what-is-the-average-disk-storage-size-of-a-minecraft-chunk#:~:text=This%20format%20stores%20each%20block,chunk%20area%20per%20mcanvil%2Dfile.
//Each voxel has a voxelState byte (8 bits = 2^8 = 256 blockIDs)
//Each Chunk has 16x16x96 voxels = 24,576 bytes
//Therefore, each chunk file should be only 24 KB (actual is 361 KB for 96 chunkHeight, 961 KB for 256 chunkHeight, 15 times larger, perhaps due to Unity Serialization and no compression
//Would chunk data compression slow down performance?
//Condense memory usage using run length encoding? Is list of bytes more performant than byte array?
//World data = dictionary <Vector2Int, chunkData>
//ChunkData map = (3D Byte Array) Byte[16, 96, 16]
//VoxelState = (byte)(0 - 256) blockID

//Minecraft Optimizations:
//Minecraft uses Named Binary Tag Format to efficiently store binary data related to chunks in region files
//Minecraft Chunks were originally stored as individual ".dat" files with the chunk position encoded in Base36
//MCRegion began storing groups of 32×32 chunks in individual ".mcr" files with coordinates in Base10 to reduce disk usage by cutting down on the number of file handles Minecraft had open at once (because Minecraft is constantly reading/writing to world data as it saves in run-time and ran up against a hard limit of 1024 open handles)
//Minecraft Anvil Format changed the max build height to 256 and empty sections were no longer saved or loaded to disk
//max # Block IDs was increased to 4096 (was 256) by adding a 4-bit data layer (similar to how meta data is stored).
//Minecraft Block ordering was been changed from XZY to YZX in order to improve compression.

public static class SaveSystem
{
    public static void SaveWorld(WorldData world)
    {
        // Set our save location and make sure we have a saves folder ready to go.
        string savePath = World.Instance.appPath + "/saves/" + world.planetNumber + "-" + world.seed + "/";

        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        //Debug.Log("Saving " + world.worldName);

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath + world.planetNumber + "-" + world.seed + ".worldData", FileMode.Create);

        formatter.Serialize(stream, world);
        stream.Close();

        string[] savedPlayerNames = new string[World.Instance.players.Count];

        // for all players (except world player dummy), save player stats (splitscreen play saves only stats of last player who joined)
        for (int i = 1; i < World.Instance.players.Count; i++)
        {
            if (World.Instance.players[i].playerGameObject != null) // check if client's left before host saved if so, cannot save their data
            {
                GameObject player = World.Instance.players[i].playerGameObject;
                string playerSaveName = player.GetComponent<Controller>().playerName;
                int[] playerStats = GetPlayerStats(player, i); // save player stats

                formatter = new BinaryFormatter();

                stream = new FileStream(savePath + playerSaveName + ".stats", FileMode.Create);

                formatter.Serialize(stream, playerStats);
                stream.Close();

                savedPlayerNames[i] = player.GetComponent<Controller>().playerName;
            }
        }

        Thread thread = new Thread(() => SaveChunks(world));
        thread.Start();
    }

    public static int[] GetPlayerStats(GameObject player, int playerIndex) // uses same savepath as SaveWorld
    {
        int[] playerStats = new int[] // make playerstats int array
        {
            Mathf.FloorToInt(player.transform.position.x),
            Mathf.FloorToInt(player.transform.position.y + 1), // add 1 unit to ensure player is not inside ground
            Mathf.FloorToInt(player.transform.position.z),
            Mathf.FloorToInt(player.GetComponent<Health>().hp),
            0, // slot 1 blockID (CREATIVE SLOT)
            0, // slot 1 qty (CREATIVE SLOT)
            0, // slot 2 blockID
            0, // slot 2 qty
            0, // slot 3 blockID
            0, // slot 3 qty
            0, // slot 4 blockID
            0, // slot 4 qty
            0, // slot 5 blockID
            0, // slot 5 qty
            0, // slot 6 blockID
            0, // slot 6 qty
            0, // slot 7 blockID
            0, // slot 7 qty
            0, // slot 8 blockID
            0, // slot 8 qty
            0, // slot 9 blockID
            0, // slot 9 qty
        };
        if(playerIndex > 0)
        {
            for (int i = 4; i < 22; i += 2)
            {
                if (player.GetComponent<Controller>().toolbar.slots[(i - 4) / 2].itemSlot.HasItem)
                {
                    playerStats[i] = player.GetComponent<Controller>().toolbar.slots[(i - 4) / 2].itemSlot.stack.id;
                    playerStats[i + 1] = player.GetComponent<Controller>().toolbar.slots[(i - 4) / 2].itemSlot.stack.amount;
                }
                else
                {
                    playerStats[i] = 0;
                    playerStats[i + 1] = 0;
                }
            }
        }
        return playerStats;
    }

    public static int[] LoadPlayerStats(GameObject player, string playerName, WorldData world)
    {
        string loadPath = World.Instance.appPath + "/saves/" + world.planetNumber + "-" + world.seed + "/";

        if (File.Exists(loadPath + playerName + ".stats")) // IF PLAYER STATS FOUND
        {
            //Debug.Log(playerName + " playerStats found. Loading from save.");

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath + playerName + ".stats", FileMode.Open);

            int[] playerStats = formatter.Deserialize(stream) as int[];
            stream.Close();
            return playerStats;
        }
        else // SET PLAYER STATS TO DEFAULT VALUES
        {
            //Debug.Log(loadPath + playerName + ".playerStats" + " not found. Creating.");
            int[] playerStats = GetDefaultPlayerStats(player);
            return playerStats;
        }
    }

    public static int[] GetDefaultPlayerStats(GameObject player)
    {
        int hpMax = 10;
        if (Settings.Platform != 2)
            hpMax = player.GetComponent<Health>().hpMax;
        //Debug.Log(loadPath + playerName + ".playerStats" + " not found. Creating.");
        int[] stats = new int[]
        {
                Mathf.FloorToInt(World.Instance.defaultSpawnPosition.x),
                Mathf.FloorToInt(World.Instance.defaultSpawnPosition.y),
                Mathf.FloorToInt(World.Instance.defaultSpawnPosition.x),
                hpMax,
                0, // slot 1 blockID (CREATIVE SLOT)
                0, // slot 1 qty (CREATIVE SLOT)
                0, // slot 2 blockID
                0, // slot 2 qty
                0, // slot 3 blockID
                0, // slot 3 qty
                0, // slot 4 blockID
                0, // slot 4 qty
                0, // slot 5 blockID
                0, // slot 5 qty
                0, // slot 6 blockID
                0, // slot 6 qty
                0, // slot 7 blockID
                0, // slot 7 qty
                0, // slot 8 blockID
                0, // slot 8 qty
                0, // slot 9 blockID
                0, // slot 9 qty
        };
        return stats;
    }

    public static void SaveChunks(WorldData world)
    {
        List<ChunkData> chunks = new List<ChunkData>(world.modifiedChunks);
        world.modifiedChunks.Clear();

        int count = 0;
        foreach(ChunkData chunk in chunks)
        {
            SaveChunk(chunk, world.planetNumber, world.seed);
            //Debug.Log("saving " + chunk.position.x + " , " + chunk.position.y);
            count++;
        }
        //Debug.Log(count + " chunks saved.");
    }

    public static void SaveChunk(ChunkData chunk, int _planetNumber, int _seed)
    {
        string chunkName = chunk.position.x + "-" + chunk.position.y;

        // Set our save location and make sure we have a saves folder ready to go.
        string savePath = World.Instance.appPath + "/saves/" + _planetNumber + "-" + _seed + "/chunks/";

        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream;

        stream = new FileStream(savePath + chunkName + ".chunk", FileMode.Create);
        formatter.Serialize(stream, chunk);
        stream.Close();

        stream = new FileStream(savePath + chunkName + ".chunkString", FileMode.Create);
        formatter.Serialize(stream, chunk.EncodeChunk(chunk));
        stream.Close();
    }

    public static WorldData LoadWorld(int _planetNumber, int _seed) // loads world upon game start in world script
    {
        string loadPath = World.Instance.appPath + "/saves/" + _planetNumber + "-" + _seed + "/";

        if (File.Exists(loadPath + _planetNumber + "-" + _seed + ".worldData"))
        {
            //Debug.Log(worldName + " found. Loading from save.");

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath + _planetNumber + "-" + _seed + ".worldData", FileMode.Open);

            WorldData world = formatter.Deserialize(stream) as WorldData;
            stream.Close();
            return new WorldData(world);
        }
        else
        {
            //Debug.Log(worldName + " not found. Creating new world.");

            WorldData world = new WorldData(_planetNumber, _seed);
            SaveWorld(world);

            return world;
        }
    }

    public static ChunkData LoadChunk(int _planetNumber, int _seed, Vector2Int position) // loads chunks from file (SLOW)
    {
        ChunkData chunk = new ChunkData();

        string chunkName = position.x + "-" + position.y;
        string loadPath = World.Instance.appPath + "/saves/" + _planetNumber + "-" + _seed + "/chunks/" + chunkName + ".chunk";
        string loadPathString = World.Instance.appPath + "/saves/" + _planetNumber + "-" + _seed + "/chunks/" + chunkName + ".chunkString";

        if (File.Exists(loadPath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath, FileMode.Open);

            chunk = formatter.Deserialize(stream) as ChunkData;

            string voxels = string.Empty;
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++)
                {
                    for (int z = 0; z < VoxelData.ChunkWidth; z++)
                    {
                        voxels += chunk.stringBlockIDs[chunk.map[x, y, z].id];
                    }
                }
            }
            Debug.Log(chunk.position);
            Debug.Log(chunk.RunLengthEncode(voxels));

            stream.Close();
        }
        else
            chunk = null;

        if (File.Exists(loadPathString))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPathString, FileMode.Open);

            ChunkData chunkString = new ChunkData();
            string str = formatter.Deserialize(stream) as string;
            chunkString = chunkString.DecodeChunk(str);

            string voxelsString = string.Empty;

            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int y = 0; y < VoxelData.ChunkHeight; y++)
                {
                    for (int z = 0; z < VoxelData.ChunkWidth; z++)
                    {
                        voxelsString += chunkString.stringBlockIDs[chunkString.map[x, y, z].id];
                    }
                }
            }
            Debug.Log(chunkString.position);
            Debug.Log(chunkString.RunLengthEncode(voxelsString));

            stream.Close();
        }

        if (chunk != null)
            return chunk;
        else
            return null;   
    }
}