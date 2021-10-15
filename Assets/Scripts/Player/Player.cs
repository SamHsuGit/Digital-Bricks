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

        int[] playerStats = SaveSystem.LoadPlayerStats(player, name, World.Instance.worldData); // load current player stats from save file

        spawnPosition = new Vector3(playerStats[0], playerStats[1], playerStats[2]); // get player spawn position (move up by 1 to prevent player from glitching thru world???)

        // Set player health
        if (playerStats[3] > player.GetComponent<Health>().hpMax)
            player.GetComponent<Health>().hp = player.GetComponent<Health>().hpMax;
        else
            player.GetComponent<Health>().hp = playerStats[3];

        // Set player inventory
        for (int i = 4; i < 22; i += 2)
        {
            if (playerStats[i] != 0)
            {
                ItemStack stack = new ItemStack((byte)playerStats[i], playerStats[i + 1]);
                player.GetComponent<Controller>().toolbar.slots[(i - 4) / 2].itemSlot.InsertStack(stack);
            }
        }

        //instances = 1;
        chunksToAddVBO = new List<ChunkCoord>();
    }

    public Player() // default constructor
    {
        playerGameObject = null;
        name = "undefinedPlayerName";
        spawnPosition = World.Instance.spawnPosition;
        hp = 1;
        //instances = 1;
        chunksToAddVBO = new List<ChunkCoord>();
    }
}
