using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugText : MonoBehaviour
{
    public GameObject player;

    TextMeshProUGUI text;

    float frameRate;
    float timer;

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;
    
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();

        halfWorldSizeInVoxels = VoxelData.WorldSizeInVoxels / 2;
        halfWorldSizeInChunks = SettingsStatic.LoadedSettings.worldSizeinChunks / 2;

    }

    void Update()
    {
        Vector3 playerPos = player.transform.position;

        if (World.Instance.worldLoaded && World.Instance.GetChunkFromVector3(playerPos) != null) // don't do this unless the world is loaded and player is in a chunk
        {
            string debugText = frameRate + " fps";
            debugText += "\n";
            debugText += "XYZ: " + (Mathf.FloorToInt(playerPos.x) - halfWorldSizeInVoxels) + " / " + Mathf.FloorToInt(playerPos.y) + " / " + (Mathf.FloorToInt(playerPos.z) - halfWorldSizeInVoxels);
            debugText += "\n";
            debugText += "Chunk: " + (World.Instance.GetChunkFromVector3(playerPos).coord.x - halfWorldSizeInChunks) + " / " + (World.Instance.GetChunkFromVector3(playerPos).coord.z - halfWorldSizeInChunks);
            debugText += "\n";
            //debugText += "Biome: " + World.Instance.biome.biomeName; // disabled, can see this in inspector
            //debugText += "\n";
            //debugText += "c: " + World.Instance.continentalness; // disabled, can see this in inspector
            //debugText += "\n";
            //debugText += "e: " + World.Instance.erosion; // disabled, can see this in inspector
            //debugText += "\n";
            //debugText += "w: " + World.Instance.weirdness; // disabled, can see this in inspector
            //debugText += "\n";
            debugText += "F3 (Xbox: Y) = Show Controls";

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