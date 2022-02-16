using System.Collections;
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

    public Player(GameObject player, string _name) // player Constructor
    {
        playerGameObject = player;
        name = _name;

        int[] playerStats;
        if (!Settings.IsMobilePlatform)
            playerStats = SaveSystem.LoadPlayerStats(player, name, World.Instance.worldData); // load current player stats from save file
        else
            playerStats = SaveSystem.GetDefaultPlayerStats(playerGameObject);

        spawnPosition = new Vector3(playerStats[0], playerStats[1], playerStats[2]); // get player spawn position (move up by 1 to prevent player from glitching thru world???)

        // Set player health
        if (!Settings.IsMobilePlatform)
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

    public Player() // default constructor
    {
        playerGameObject = null;
        name = "undefinedPlayerName";
        spawnPosition = World.Instance.defaultSpawnPosition;
        hp = 1;
        //instances = 1;
        chunksToAddVBO = new List<ChunkCoord>();
    }
}
