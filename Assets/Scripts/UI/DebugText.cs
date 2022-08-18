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
        if (player == null)
            return;

        Vector3 playerPos = player.transform.position;

        if (World.Instance.worldLoaded && World.Instance.GetChunkFromVector3(playerPos) != null) // don't do this unless the world is loaded and player is in a chunk
        {
            string debugText = frameRate + " fps";
            debugText += "\n";
            debugText += " CPU: " + SystemInfo.processorType + " RAM: " + SystemInfo.systemMemorySize + " OS: " + SystemInfo.operatingSystem;
            debugText += "\n";
            debugText += "GPU: " + SystemInfo.graphicsDeviceName + " VRAM: " + SystemInfo.graphicsMemorySize;
            debugText += "\n";
            debugText += "XYZ: " + (Mathf.FloorToInt(playerPos.x) - halfWorldSizeInVoxels) + " / " + Mathf.FloorToInt(playerPos.y) + " / " + (Mathf.FloorToInt(playerPos.z) - halfWorldSizeInVoxels);
            debugText += "\n";
            debugText += "Chunk: " + (World.Instance.GetChunkFromVector3(playerPos).coord.x - halfWorldSizeInChunks) + " / " + (World.Instance.GetChunkFromVector3(playerPos).coord.z - halfWorldSizeInChunks);
            debugText += "\n";
            debugText += "c: " + World.Instance.continentalness + " e: " + World.Instance.erosion + " pv: " + World.Instance.peaksAndValleys;
            debugText += "\n";
            debugText += "w: " + World.Instance.weirdness;
            debugText += "\n";
            debugText += "t: " + World.Instance.temperature + " h: " + World.Instance.humidity + " Biome: " + World.Instance.biome.biomeName;
            debugText += "\n";
            debugText += "f: " + World.Instance.fertility + " p: " + World.Instance.percolation + " SurfaceObType: " + World.Instance.surfaceObType;
            debugText += "\n";
            debugText += "VBO: " + World.Instance.placementVBO;

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