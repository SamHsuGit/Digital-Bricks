using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugText : MonoBehaviour
{
    public GameObject player;
    Controller controller;

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
        controller = null;
    }

    void Update()
    {
        if (player == null)
            return;

        if (player != null && controller == null)
            controller = player.GetComponent<Controller>();

        Vector3 playerPos = player.transform.position;

        if (World.Instance.worldLoaded && World.Instance.GetChunkFromVector3(playerPos) != null) // don't do this unless the world is loaded and player is in a chunk
        {
            byte blockID = 0;
            string blockName = "air";
            if (controller != null)
            {
                blockID = controller.blockID;
                blockName = World.Instance.blockTypes[(int)blockID].name;
            }

            string debugText = "LDPlay v" + Application.version;
            debugText += " planetSeed: " + SettingsStatic.LoadedSettings.planetSeed + " worldCoords: " + SettingsStatic.LoadedSettings.worldCoord + " season: " + World.Instance.season;
            debugText += "\n";
            debugText += frameRate + " fps";
            debugText += "\n";
            debugText += "Active Chunks: " + World.Instance.activeChunks.Count;
            debugText += "\n";
            debugText += "Chunks To Update: " + World.Instance.chunksToUpdate.Count;
            debugText += "\n";
            debugText += "Chunks To Draw: " + World.Instance.chunksToDraw.Count;
            debugText += "\n";
            debugText += "pre world load time: " + World.Instance.preWorldLoadTime;
            debugText += "\n";
            debugText += "world load time: " + World.Instance.worldLoadTime;
            debugText += "\n";
            debugText += "chunk draw time: " + World.Instance.chunkDrawTime;
            debugText += "\n";
            debugText += "CPU: " + SystemInfo.processorType + " RAM: " + SystemInfo.systemMemorySize + " Mb  OS: " + SystemInfo.operatingSystem;
            debugText += "\n";
            debugText += "GPU: " + SystemInfo.graphicsDeviceName + " VRAM: " + SystemInfo.graphicsMemorySize + " Mb";
            debugText += "\n";
            debugText += "pos: (" + (Mathf.FloorToInt(playerPos.x) - halfWorldSizeInVoxels) + " , " + Mathf.FloorToInt(playerPos.y) + " , " + (Mathf.FloorToInt(playerPos.z) - halfWorldSizeInVoxels) + ")";
            debugText += "\n";
            debugText += "chunk: (" + (World.Instance.GetChunkFromVector3(playerPos).coord.x - halfWorldSizeInChunks) + " , " + (World.Instance.GetChunkFromVector3(playerPos).coord.z - halfWorldSizeInChunks) + ")";
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
            debugText += "\n";
            debugText += "blockID: " + blockName;

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