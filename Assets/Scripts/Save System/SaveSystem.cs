using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

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
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0
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
            int[] playerStats = new int[]
            {
                Mathf.FloorToInt(World.Instance.defaultSpawnPosition.x),
                Mathf.FloorToInt(World.Instance.defaultSpawnPosition.y),
                Mathf.FloorToInt(World.Instance.defaultSpawnPosition.x),
                player.GetComponent<Health>().hpMax,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0
            };
            return playerStats;
        }
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
        FileStream stream = new FileStream(savePath + chunkName + ".chunk", FileMode.Create);

        formatter.Serialize(stream, chunk);
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
        string chunkName = position.x + "-" + position.y;

        string loadPath = World.Instance.appPath + "/saves/" + _planetNumber + "-" + _seed + "/chunks/" + chunkName + ".chunk";

        if (File.Exists(loadPath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(loadPath, FileMode.Open);

            ChunkData chunkData = formatter.Deserialize(stream) as ChunkData;
            stream.Close();
            return chunkData;
        }

        return null;   
    }
}