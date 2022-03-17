using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

public static class SaveSystem
{
    public static void SaveWorld(WorldData worldData, World world, bool savePlayerData)
    {
        // Set our save location and make sure we have a saves folder ready to go.
        string savePath = Settings.AppSaveDataPath + "/saves/" + worldData.planetNumber + "-" + worldData.seed + "/";

        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        //Debug.Log("Saving " + world.worldName);

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(savePath + worldData.planetNumber + "-" + worldData.seed + ".worldData", FileMode.Create);

        formatter.Serialize(stream, worldData);
        stream.Close();

        if (savePlayerData)
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
                //Debug.Log("Saved " + savedPlayerNames[i] + " stats");
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
            // overwrite zero place holders with values from player toolbar (includes creative slot)
            for (int i = 4; i < 22; i += 2)
            {
                if (playerGameObject.GetComponent<Controller>().toolbar.slots[(i - 4) / 2].itemSlot.HasItem)
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
        string loadPath = Settings.AppSaveDataPath + "/saves/" + SettingsStatic.LoadedSettings.planetNumber + "-" + SettingsStatic.LoadedSettings.seed + "/";

        //Debug.Log(loadPath + playerName + ".stats");
        //Debug.Log(File.Exists(loadPath + playerName + ".stats"));
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
            SaveChunk(chunk, worldData.planetNumber, worldData.seed);
            //Debug.Log("saving " + chunk.position.x + " , " + chunk.position.y);
            count++;
        }
        //Debug.Log(count + " chunks saved.");
    }

    public static void SaveChunk(ChunkData chunk, int planetNumber, int seed)
    {
        string chunkName = chunk.position.x + "-" + chunk.position.y;

        // Set our save location and make sure we have a saves folder ready to go.
        string savePath = Settings.AppSaveDataPath + "/saves/" + planetNumber + "-" + seed + "/chunks/";

        if (!Directory.Exists(savePath))
            Directory.CreateDirectory(savePath);

        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream;

        stream = new FileStream(savePath + chunkName + ".chunk", FileMode.Create); // overwrites any existing files by default
        formatter.Serialize(stream, chunk.EncodeChunk(chunk));
        stream.Close();
    }

    public static WorldData LoadWorld(int planetNumber, int seed)
    {
        // loads world upon game start in world script

        string loadPath = Settings.AppSaveDataPath + "/saves/" + planetNumber + "-" + seed + "/";

        if (File.Exists(loadPath + planetNumber + "-" + seed + ".worldData"))
        {

            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath + planetNumber + "-" + seed + ".worldData", FileMode.Open);

            WorldData world = formatter.Deserialize(stream) as WorldData;
            stream.Close();
            return new WorldData(world);
        }
        else
        {

            WorldData worldData = new WorldData(planetNumber, seed);
            SaveWorld(worldData, World.Instance, false);

            return worldData;
        }
    }

    public static ChunkData LoadChunk(int planetNumber, int seed, Vector2Int position)
    {
        // loads chunks from file (SLOW)

        ChunkData chunk = new ChunkData();

        string chunkName = position.x + "-" + position.y;
        string loadPath = Settings.AppSaveDataPath + "/saves/" + planetNumber + "-" + seed + "/chunks/" + chunkName + ".chunk";

        if (File.Exists(loadPath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath, FileMode.Open);

            string str = formatter.Deserialize(stream) as string;
            chunk = chunk.DecodeChunk(str);

            stream.Close();
        }
        else
            chunk = null;

        if (chunk != null)
            return chunk;
        else
            return null;
    }

    public static List<string> LoadChunkFromFile(int planetNumber, int seed)
    {
        List<string> strArray = new List<string>();

        string path = Settings.AppSaveDataPath + "/saves/" + planetNumber + "-" + seed + "/chunks/";
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