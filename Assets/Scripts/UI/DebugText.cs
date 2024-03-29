using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugText : MonoBehaviour
{
    public GameObject player;
    private GameManagerScript gameManager;
    private Controller controller;
    private GameObject playerCamera;
    private Lighting lighting;

    TextMeshProUGUI text;
    string oldDebugText;

    float frameRate;
    float timer;
    private float sphereCastRadius;
    private float grabDist;
    private string placedBrickName = "";

    int halfWorldSizeInVoxels;
    int halfWorldSizeInChunks;
    
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManagerScript>();
        lighting = gameManager.globalLighting.GetComponent<Lighting>();

        halfWorldSizeInVoxels = VoxelData.WorldSizeInChunks * VoxelData.ChunkWidth / 2;
        halfWorldSizeInChunks = VoxelData.WorldSizeInChunks / 2;
        controller = null;
    }

    void Update()
    {
        if (player == null)
            return;

        if (player != null && controller == null)
        {
            controller = player.GetComponent<Controller>();
            playerCamera = controller.playerCamera;
            sphereCastRadius = controller.sphereCastRadius;
            grabDist = controller.grabDist;
        }

        Vector3 playerPos = player.transform.position;

        RaycastHit hit;
        if (Physics.SphereCast(playerCamera.transform.position, sphereCastRadius, playerCamera.transform.forward, out hit, grabDist))
        {
            placedBrickName = hit.transform.gameObject.name;
        }

            if (World.Instance.worldLoaded && World.Instance.GetChunkFromVector3(playerPos) != null) // don't do this unless the world is loaded and player is in a chunk
        {
            byte blockID = 0;
            string blockName = "air";
            if (controller != null)
            {
                blockID = controller.blockID;
                blockName = World.Instance.blockTypes[(int)blockID].name;
            }

            string debugText = "Digital Bricks v" + Application.version;
            debugText += " planetSeed: " + SettingsStatic.LoadedSettings.planetSeed + " worldCoords: " + SettingsStatic.LoadedSettings.worldCoord;
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
            debugText += "measured time: " + World.Instance.debugTimer;
            debugText += "\n";
            debugText += "CPU: " + SystemInfo.processorType + " RAM: " + SystemInfo.systemMemorySize + " Mb  OS: " + SystemInfo.operatingSystem;
            debugText += "\n";
            debugText += "GPU: " + SystemInfo.graphicsDeviceName + " VRAM: " + SystemInfo.graphicsMemorySize + " Mb";
            debugText += "\n";
            debugText += "pos: (" + (Mathf.FloorToInt(playerPos.x) - halfWorldSizeInVoxels) + " , " + Mathf.FloorToInt(playerPos.y) + " , " + (Mathf.FloorToInt(playerPos.z) - halfWorldSizeInVoxels) + ")";
            debugText += "\n";
            debugText += "chunk: (" + (World.Instance.GetChunkFromVector3(playerPos).coord.x - halfWorldSizeInChunks) + " , " + (World.Instance.GetChunkFromVector3(playerPos).coord.z - halfWorldSizeInChunks) + ")";
            debugText += "\n";
            debugText += "time of day: " + lighting.timeOfDay;
            debugText += "\n";
            debugText += "c: " + World.Instance.continentalness + " e: " + World.Instance.erosion + " pv: " + World.Instance.peaksAndValleys;
            debugText += "\n";
            debugText += "w: " + World.Instance.weirdness;
            debugText += "\n";
            if(World.Instance.biome != null)
            {
                debugText += "t: " + World.Instance.temperature + " h: " + World.Instance.humidity + " Biome: " + World.Instance.biome.biomeName;
                debugText += "\n";
            }
            debugText += "f: " + World.Instance.fertility + " p: " + World.Instance.percolation + " SurfaceObType: " + World.Instance.surfaceObType;
            debugText += "\n";
            debugText += "blockID: " + blockName;
            debugText += "\n";
            debugText += "brickID: " + placedBrickName;

            string direction = "";
            switch (controller.orientation)
            {
                case 0:
                    direction = "East";
                    break;
                case 5:
                    direction = "South";
                    break;
                case 1:
                    direction = "West";
                    break;
                default:
                    direction = "North";
                    break;
            }

            debugText += "\n";
            debugText += "Direction Facing: " + direction;

            if(debugText != oldDebugText)
            {
                text.text = debugText;
                oldDebugText = debugText;
            }

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