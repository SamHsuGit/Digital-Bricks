using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System;

public static class SaveSystem
{
    // bool to toggle how to encode saved data: as semi readable string (4 kb) or binary (0.5 kb)
    // binary encoding imposes limit on how many chunks can be saved (world size in chunks limited to 255) but yields far better data compression
    // chose to use binary compression and limit world size to 200 x 200 chunks
    static readonly bool stringSaveData = false;

    public static void SaveWorldDataToFile(WorldData worldData, World world)
    {
        // Set our save location and make sure we have a saves folder ready to go.
        string savePath = Settings.AppSaveDataPath + "/saves/" + worldData.planetSeed + "-" + worldData.worldCoord + "/";

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath + worldData.planetSeed + "-" + worldData.worldCoord + ".worldData", FileMode.Create);

        if (SettingsStatic.LoadedSettings.developerMode)
        {
            worldData.creativeMode = false; // if the world is saved in creative mode, the world is marked as non-survival forever after
        }

        formatter.Serialize(stream, worldData);
        stream.Close();

        SavePlayerStats(world, savePath);

        Thread thread = new Thread(() => SaveChunks(worldData));
        thread.Start();
    }

    public static void SavePlayerStats(World world, string savePath)
    {
        string[] savedPlayerNames = new string[world.players.Count];

        // for all players, save player stats (splitscreen play saves only stats of last player who joined)
        for (int i = 0; i < world.players.Count; i++)
        {
            // Do not save data if client's left before host saved or if player is worldPlayer
            if (world.players[i].playerGameObject != null && world.players[i].playerGameObject != world.worldPlayer.gameObject)
            {
                GameObject playerOb = world.players[i].playerGameObject;
                string playerSaveName = playerOb.GetComponent<Controller>().playerName;
                int[] playerStats = GetPlayerStats(playerOb); // save player stats

                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(savePath + playerSaveName + ".stats", FileMode.Create);
                formatter.Serialize(stream, playerStats);
                stream.Close();

                savedPlayerNames[i] = playerOb.GetComponent<Controller>().playerName;
            }
        }
    }

    public static int[] GetPlayerStats(GameObject playerGameObject)
    {
        // uses same savepath as SaveWorld

        int[] playerStats = new int[] // make playerstats int array
        {
            Mathf.FloorToInt(playerGameObject.transform.position.x),
            Mathf.FloorToInt(playerGameObject.transform.position.y + 1), // add 1 unit to ensure player is not inside ground
            Mathf.FloorToInt(playerGameObject.transform.position.z),
            Mathf.FloorToInt(playerGameObject.GetComponent<Health>().hp),
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
        if(playerGameObject != World.Instance.worldPlayer.gameObject) // do not save player stats for world player
        {
            // overwrite zero place holders with values from player toolbar if not in creative mode
            for (int i = 4; i < 22; i += 2)
            {
                if (!SettingsStatic.LoadedSettings.developerMode && playerGameObject.GetComponent<Controller>().toolbar.slots[(i - 4) / 2].itemSlot.HasItem)
                {
                    playerStats[i] = playerGameObject.GetComponent<Controller>().toolbar.slots[(i - 4) / 2].itemSlot.stack.id;
                    playerStats[i + 1] = playerGameObject.GetComponent<Controller>().toolbar.slots[(i - 4) / 2].itemSlot.stack.amount;
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

    public static int[] LoadPlayerStats(GameObject player, string playerName)
    {
        string loadPath = Settings.AppSaveDataPath + "/saves/" + SettingsStatic.LoadedSettings.planetSeed + "-" + SettingsStatic.LoadedSettings.worldCoord + "/";

        if (File.Exists(loadPath + playerName + ".stats")) // IF PLAYER STATS FOUND
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath + playerName + ".stats", FileMode.Open);

            int[] playerStats = formatter.Deserialize(stream) as int[];
            stream.Close();
            return playerStats;
        }
        else // SET PLAYER STATS TO DEFAULT VALUES
        {
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
                Mathf.FloorToInt(Settings.DefaultSpawnPosition.x),
                Mathf.FloorToInt(Settings.DefaultSpawnPosition.y),
                Mathf.FloorToInt(Settings.DefaultSpawnPosition.x),
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

    public static void SaveChunks(WorldData worldData)
    {
        List<ChunkData> chunks = new List<ChunkData>(worldData.modifiedChunks);
        worldData.modifiedChunks.Clear();

        int count = 0;
        foreach(ChunkData chunk in chunks)
        {
            SaveChunkToFile(chunk, worldData.planetSeed, worldData.worldCoord);
            count++;
        }
    }

    public static void SaveChunkToFile(ChunkData chunkData, int _planetSeed, int _worldCoord)
    {
        string chunkName = chunkData.position.x + "-" + chunkData.position.y;

        // Set our save location and make sure we have a saves folder ready to go.
        string savePath = Settings.AppSaveDataPath + "/saves/" + _planetSeed + "-" + _worldCoord + "/chunks/";

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        if (!stringSaveData)
        {
            // saving byte array as binary file is faster than reading strings from text files
            // https://answers.unity.com/questions/1259263/fastest-way-to-read-in-data.html
            FileStream stream;
            stream = new FileStream(savePath + chunkName + ".chunk", FileMode.Create); // overwrites any existing files by default
            BinaryWriter w = new BinaryWriter(stream, System.Text.Encoding.UTF8);
            w.Write(chunkData.EncodeByteArray(chunkData));
            stream.Close();
        }
        else
        {
            // uses strings for a human readable format (slower). Kept for debugging
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream;
            stream = new FileStream(savePath + chunkName + ".chunk", FileMode.Create); // overwrites any existing files by default
            formatter.Serialize(stream, chunkData.EncodeString(chunkData));
            stream.Close();
        }
    }

    public static WorldData LoadWorld(int _planetSeed, int _worldCoord)
    {
        // loads world upon game start in world script
        string loadPath = Settings.AppSaveDataPath + "/saves/" + _planetSeed + "-" + _worldCoord + "/";

        if (File.Exists(loadPath + _planetSeed + "-" + _worldCoord + ".worldData"))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath + _planetSeed + "-" + _worldCoord + ".worldData", FileMode.Open);

            WorldData worldData = formatter.Deserialize(stream) as WorldData;

            stream.Close();
            return new WorldData(worldData);
        }
        else
        {
            WorldData worldData = new WorldData(_planetSeed, _worldCoord);
            if (SettingsStatic.LoadedSettings.developerMode)
                worldData.creativeMode = true; // new worlds set value of creative mode from saved value
            else
                worldData.creativeMode = false;
            SettingsStatic.LoadedSettings.timeOfDay = 6.0f; // reset time of day to morning for new worlds
            FileSystemExtension.SaveSettings();

            return worldData;
        }
    }

    public static ChunkData LoadChunkFromFile(int _planetSeed, int _worldCoord, Vector2Int position)
    {
        // loads chunks from file
        ChunkData chunkData = new ChunkData();

        string chunkName = position.x + "-" + position.y;

        // IMPORTANT: use SettingsStatic.LoadedSettings.worldSizeInChunks
        // worldSizeInChunks = 0, renders correctly but doesn't use saved data
        string loadPath = Settings.AppSaveDataPath + "/saves/" + _planetSeed + "-" + _worldCoord + "/chunks/" + chunkName + ".chunk";

        if (File.Exists(loadPath))
        {
            if (!stringSaveData)
            {
                // saving byte array as binary file is faster than reading strings from text files
                // https://answers.unity.com/questions/1259263/fastest-way-to-read-in-data.html
                FileStream stream;
                stream = new FileStream(loadPath, FileMode.Open, FileAccess.Read);
                BinaryReader r = new BinaryReader(stream, System.Text.Encoding.UTF8);
                int count = 2 + (VoxelData.ChunkWidth * VoxelData.ChunkWidth * VoxelData.ChunkHeight);
                byte[] byteArray = r.ReadBytes(count);
                chunkData = chunkData.DecodeByteArray(byteArray);
                stream.Close();
            }
            else
            {
                // uses strings for a human readable format (slower). Kept for debugging
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(loadPath, FileMode.Open);
                string str = formatter.Deserialize(stream) as string;
                chunkData = chunkData.DecodeString(str);
                stream.Close();
            }
        }
        else
        {
            chunkData = null;
        }

        if (chunkData != null)
            return chunkData;
        else
            return null;
    }

    public static List<string> LoadChunkListFromFile(int _planetSeed, int _worldCoord)
    {
        List<string> strArray = new List<string>();

        string path = Settings.AppSaveDataPath + "/saves/" + _planetSeed + "-" + _worldCoord + "/chunks/";
        if(Directory.Exists(path))
        {
            foreach (string file in Directory.GetFiles(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(file, FileMode.Open);
                strArray.Add(formatter.Deserialize(stream) as string);
                stream.Close();
            }
        }
        return strArray;
    }
}