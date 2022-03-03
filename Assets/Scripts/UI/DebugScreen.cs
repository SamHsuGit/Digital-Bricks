using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    public GameObject player;

    Text text;

    float frameRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;
    
    void Start()
    {
        text = GetComponent<Text>();

        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;

    }

    void Update()
    {
        Vector3 playerPos = player.transform.position;
        Vector2 playerPosXZ = new Vector2(playerPos.x, playerPos.z);
        string biomeName;
        if (World.Instance.worldData.isAlive)
            biomeName = World.Instance.biomes[World.Instance.GetBiome(World.Instance.GetTemperature(playerPosXZ), World.Instance.GetRainfall(playerPosXZ))].biomeName;
        else
            biomeName = World.Instance.biomes[World.Instance.biomes.Length - 1].biomeName;

        if (World.Instance.worldLoaded && World.Instance.GetChunkFromVector3(playerPos) != null) // don't do this unless the world is loaded and player is in a chunk
        {
            string debugText = frameRate + " fps";
            debugText += "\n";
            debugText += "XYZ: " + (Mathf.FloorToInt(playerPos.x) - halfWorldSizeInVoxels) + " / " + Mathf.FloorToInt(playerPos.y) + " / " + (Mathf.FloorToInt(playerPos.z) - halfWorldSizeInVoxels);
            debugText += "\n";
            debugText += "Chunk: " + (World.Instance.GetChunkFromVector3(playerPos).coord.x - halfWorldSizeInChunks) + " / " + (World.Instance.GetChunkFromVector3(playerPos).coord.z - halfWorldSizeInChunks);
            debugText += "\n";
            debugText += "Biome: " + biomeName;
            debugText += "\n";
            debugText += "Y = Show Controls";

            text.text = debugText;

            if (timer > 1f)
            {

                frameRate = (int)(1f / Time.unscaledDeltaTime);
                timer = 0;

            }
            else
                timer += Time.deltaTime;
        }
    }
}