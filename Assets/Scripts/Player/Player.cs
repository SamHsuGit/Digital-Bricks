using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public GameObject playerGameObject;
    public string name;
    public Vector3 spawnPosition;
    public int hp;
    //public int instances = 1;
    public List<ChunkCoord> chunksToAddVBO;
    public int[] playerStats;

    public Player(GameObject player, string playerName)
    {
        // player Constructor

        playerGameObject = player;
        name = playerName;

        //int[] playerStats;
        if (!Settings.WebGL && Settings.Platform != 2)
            playerStats = SaveSystem.LoadPlayerStats(player, name); // load current player stats from save file
        else
            playerStats = SaveSystem.GetDefaultPlayerStats(playerGameObject);

        // set player position to saved position (move up by 1 to prevent player from glitching thru bottom of world)
        spawnPosition = new Vector3(playerStats[0], playerStats[1], playerStats[2]);

        // Set player health
        if (Settings.Platform != 2)
        {
            if(player.GetComponent<Health>() != null)
            {
                Health health = player.GetComponent<Health>();
                int hpMax = health.hpMax;
                
                if (playerStats[3] > hpMax) // if saved health is more than max calculated hp (based on # pieces)
                    health.hp = hpMax; // health is equal to calculated
                else
                    hp = playerStats[3];// otherwise set hp and health.hp to saved value
                hp = health.hp; // regardless, player hp set equal to health.hp after setting value
            }
        }

        //instances = 1;
        chunksToAddVBO = new List<ChunkCoord>();
    }

    public Player()
    {
        // default constructor

        playerGameObject = null;
        name = "PlayerName";
        spawnPosition = Settings.DefaultSpawnPosition;
        hp = 1;
        //instances = 1;
        chunksToAddVBO = new List<ChunkCoord>();
        playerStats = SaveSystem.GetDefaultPlayerStats(playerGameObject);
    }
}
