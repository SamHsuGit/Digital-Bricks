using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Diagnostics;
using Unity.Profiling;

public class World : MonoBehaviour
{
    // PUBLIC VARIABLES
    [Header("Shown For Debug")]
    public int playerCount = 0;
    public WorldData worldData;
    public bool multithreading = true;

    [Header("Public Referenced By Others")]
    // Procedural World Generation Values
    [HideInInspector] public int planetNumber;
    [HideInInspector] public int seed;
    [HideInInspector] public int worldSizeInChunks;
    [HideInInspector] public bool isEarth;
    
    // Cached Perlin Noise Map Values (10 2D perlin noise, 1 3D perlin noise, minecraft 1.18 uses 3 2D (continentalness, erosion, weirdness) and 3 3D (temp/humid/caves))
    [HideInInspector] public float continentalness = 0; // continentalness, defines distance from ocean
    [HideInInspector] public float erosion = 0; // erosion, defines how mountainous the terrain is
    [HideInInspector] public float peaksAndValleys = 0; // peaks and valleys
    [HideInInspector] public float weirdness = 0; // weirdness
    [HideInInspector] public float temperature = 0; // temperature, defines biome
    [HideInInspector] public float humidity = 0; // humidity, defines biome + cloud threshold
    [HideInInspector] public bool isAir = false; // used for 3D Perlin Noise pass

    // eventually derive these values from original perlin noise samples
    private int terrainHeight = 32; // defines height of terrain in voxels (eventually derive from depth + peaks and valleys = terrainHeight like minecraft?)
    private float terrainHeightPercentChunk; // defines height of terrain as percentage of total chunkHeight

    [HideInInspector] public float fertility = 0; // defines surfaceOb size, eventually derive from weirdness?
    [HideInInspector] public float percolation = 0; // defines surfaceOb size, eventually derive from weirdness?

    [HideInInspector] public int surfaceObType = 0; // based on percolation and fertility

    // Other Values
    [HideInInspector] public bool worldLoaded = false;
    [HideInInspector] public string preWorldLoadTime;
    [HideInInspector] public string worldLoadTime;
    [HideInInspector] public string debugTimer;
    [HideInInspector] public string chunkDrawTime;
    [HideInInspector] public string baseObString;
    [HideInInspector] public List<Player> players = new List<Player>();
    [HideInInspector] public List<GameObject> baseObPieces = new List<GameObject>();
    [HideInInspector] public Biome biome;
    [HideInInspector] public GameObject baseOb;
    [HideInInspector] public object ChunkUpdateThreadLock = new object();
    [HideInInspector] public object ChunkLoadThreadLock = new object();
    [HideInInspector] public object ChunkListThreadLock = new object();
    [HideInInspector] public Material blockMaterial;

    [Header("Public References")]
    public GameObject mainCameraGameObject;
    public Lighting globalLighting;
    public GameObject loadingText;
    public GameObject loadingBackground;
    public CustomNetworkManager customNetworkManager;
    public Planet[] planets;
    public Biome[] biomes;
    public GameObject worldPlayer;
    public Material blockMaterialLit;
    public Material blockMaterialUnlit;
    public Material blockMaterialTransparent;
    public PhysicsMaterial physicMaterial;
    public BlockType[] blockTypes;
    public GameObject[] voxelPrefabs;
    public AudioSource chunkLoadSound;

    // public chunk update lists, dictionaries, queues used by Chunk script
    [HideInInspector] public Dictionary<ChunkCoord, Chunk> chunksDict = new Dictionary<ChunkCoord, Chunk>();
    [HideInInspector] public List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    [HideInInspector] public List<Chunk> chunksToUpdate = new List<Chunk>();
    [HideInInspector] public Queue<Chunk> chunksToDraw = new Queue<Chunk>();
    [HideInInspector] public static World Instance { get { return _instance; } }

    // PRIVATE VARIABLES
    private int blockIDprocGen = 1; // leftover, was 11, now set as barrier
    private int blockIDbase = 12;
    //private int cloudHeight;
    private bool applyingModifications;
    private int loadDistance;
    private bool undrawVBO = false;
    private bool undrawVoxels = true;

    private bool useBiomes = true;
    private bool drawLodes = true;
    private bool drawSurfaceObjects = true;
    private bool drawVBO = true;

    private int viewDistance;
    private int undrawDistance;
    private int VBORenderDistanceInChunks; // acts as a radius like drawDistance

    private Chunk[,] chunks;
    private List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>();
    private List<ChunkCoord> activeChunksVBOList = new List<ChunkCoord>();
    private List<ChunkCoord> activeChunksVBOListCopy = new List<ChunkCoord>();

    private List<ChunkCoord> playerChunkCoords = new List<ChunkCoord>();
    private List<ChunkCoord> playerChunkCoordsCopy = new List<ChunkCoord>();
    private List<ChunkCoord> playerLastChunkCoords = new List<ChunkCoord>();
    private List<ChunkCoord> playerLastChunkCoordsCopy = new List<ChunkCoord>();

    private Dictionary<Player, GameObject> playerGameObjects = new Dictionary<Player, GameObject>();
    private List<Player> playersCopy = new List<Player>();
    public Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();
    private Dictionary<Vector3, GameObject> objectDictionary = new Dictionary<Vector3, GameObject>();

    private Thread ChunkRedrawThread;
    private Camera mainCamera;
    private Stopwatch worldLoadStopWatch;
    private Stopwatch chunkDrawStopWatch;
    private Stopwatch debugStopWatch;
    private Vector3 defaultSpawnPosition;

    // use spline points to define terrain shape like in https://www.youtube.com/watch?v=CSa5O6knuwI&t=1198s
    private Vector2[] continentalnessSplinePoints;
    private Vector2[] erosionSplinePoints;
    private Vector2[] peaksAndValleysSplinePoints;
    
    // hard coded values
    private const float seaLevelPercentChunk = 0.34f;
    //private const int LOD0threshold = 1;

    private static readonly ProfilerMarker s_PreparePerfMarker = new ProfilerMarker("MySystem.Prepare");
    private static readonly ProfilerMarker s_SimulatePerfMarker = new ProfilerMarker(ProfilerCategory.Ai, "MySystem.Simulate");

    private static World _instance;

    private void Awake()
    {
        if(Settings.WebGL) // settings for WebGL compatibility
        {
            useBiomes = true;
            drawLodes = true;
            drawSurfaceObjects = true;
            drawVBO = false;
            viewDistance = 3;
            undrawDistance = viewDistance * 4;
            multithreading = false;
        }
        else //get from settings file
        {
            viewDistance = SettingsStatic.LoadedSettings.viewDistance;
            undrawDistance = SettingsStatic.LoadedSettings.viewDistance * 4;
            multithreading = true;
        }
        worldSizeInChunks = VoxelData.WorldSizeInChunks;
        debugTimer = "notMeasured";

        if(Settings.WebGL)
            blockMaterial = blockMaterialLit;
        else if (SettingsStatic.LoadedSettings.graphicsQuality == 0)
            blockMaterial = blockMaterialUnlit;
        else
            blockMaterial = blockMaterialLit;

        worldLoadStopWatch = new Stopwatch();
        chunkDrawStopWatch = new Stopwatch();
        debugStopWatch = new Stopwatch();
        worldLoadStopWatch.Start();
        chunks = new Chunk[worldSizeInChunks, worldSizeInChunks]; // set size of array from saved value
        defaultSpawnPosition = Settings.DefaultSpawnPosition;
        mainCamera = mainCameraGameObject.GetComponent<Camera>();
        UnityEngine.Random.InitState(seed);

        // initialized, try to reset when players join
        SetUndrawVoxels();

        playerCount = 0;

        // lowest acceptable drawDistance is 1
        if (!Settings.WebGL && SettingsStatic.LoadedSettings.viewDistance < 1)
            SettingsStatic.LoadedSettings.viewDistance = 1;

        VBORenderDistanceInChunks = SettingsStatic.LoadedSettings.viewDistance - 1; // keep studs render distance lower than viewDistance to avoid errors.

        // If the instance value is not null and not *this*, we've somehow ended up with more than one World component.
        // Since another one has already been assigned, delete this one.
        if (_instance != null && _instance != this)
            Destroy(gameObject);
        // Else set this to the instance.
        else
            _instance = this;

        //cloudHeight = VoxelData.ChunkHeight - 15;
        float mainlandElevationPercent = 0.10f;
        float plateauElevationPercent = 0.50f;
        float step = 0.05f;
        
        // define spline points to define terrain shape like in https://www.youtube.com/watch?v=CSa5O6knuwI&t=1198s
        // guide: keep at least 0.02 between shelves to create stark contrast, jumping height too fast stretches terrain
        continentalnessSplinePoints = new Vector2[] // low continentalness = ocean
        {
            // rules: leave 0.2 between shelves, do not increase more than 0.3 between shelves
            new Vector2(0.00f, seaLevelPercentChunk - 0.10f), // deep ocean
            new Vector2(0.10f, seaLevelPercentChunk - 0.10f), // deep ocean only 10% to avoid interfering with caves
            new Vector2(0.11f, seaLevelPercentChunk - 0.04f), // ocean
            new Vector2(0.49f, seaLevelPercentChunk - 0.04f), // ocean is 40% to create continents
            new Vector2(0.50f, seaLevelPercentChunk), // sea level
            new Vector2(0.51f, seaLevelPercentChunk), // beach 2% and has the chance to get eroded into cliff
            new Vector2(0.58f, seaLevelPercentChunk),
            new Vector2(0.586f, seaLevelPercentChunk + step),
            new Vector2(0.589f, seaLevelPercentChunk + mainlandElevationPercent), // mainland 30%
            new Vector2(0.80f, seaLevelPercentChunk + mainlandElevationPercent + step),
            new Vector2(0.82f, seaLevelPercentChunk + plateauElevationPercent), // plateau 20%
            new Vector2(1.00f, seaLevelPercentChunk + plateauElevationPercent + step),
        };

        // badlands, eroded badlands -  controlled by erosion
        // river - controlled by erosion
        erosionSplinePoints = new Vector2[] // low erosion = high terrain height
        {
            new Vector2(0.00f, 0.61f),
            new Vector2(0.10f, 0.60f),
            new Vector2(0.30f, 0.50f),
            new Vector2(0.35f, 0.55f),
            new Vector2(0.40f, 0.11f),
            new Vector2(0.70f, 0.10f),
            new Vector2(0.72f, 0.30f),
            new Vector2(0.80f, 0.30f),
            new Vector2(0.82f, 0.10f),
            new Vector2(0.90f, 0.05f),
            new Vector2(1.00f, 0.04f),
        };

        // jagged peaks - controlled by peaks and valleys
        peaksAndValleysSplinePoints = new Vector2[] // high peaks and valley = high peaks
        {
            new Vector2(0.05f, 0.34f),
            new Vector2(0.10f, 0.35f),
            new Vector2(0.35f, 0.36f),
            new Vector2(0.50f, 0.37f),
            new Vector2(0.68f, 0.54f),
            new Vector2(0.90f, 0.69f),
            new Vector2(1.00f, 0.70f),
        };
    }

    private void Start()
    {
        worldLoaded = false;
        if (planetNumber == 3) // cache result for use in GetVoxel
            isEarth = true;
        else
            isEarth = false;

        if(!Settings.WebGL)
        {
            worldData = SaveSystem.LoadWorld(planetNumber, seed); // sets the worldData to the value determined by planetNumber and seed which are both set in the GameManger Script
        }
        WorldDataOverrides(planetNumber);

        if (Settings.Platform == 2 || Settings.WebGL)
            blockTypes[blockIDbase].voxelBoundObject = null;
        else
        {
            LoadSpawnedPiecesOnlinePlay();

            blockTypes[blockIDbase].voxelBoundObject = baseOb;

            if (Settings.OnlinePlay)
            {
                customNetworkManager.spawnPrefabs.Add(LDrawImportRuntime.Instance.baseOb);
                customNetworkManager.spawnPrefabs.Add(LDrawImportRuntime.Instance.projectileOb);
            }
        }

        LoadWorld();

        worldPlayer.transform.position = defaultSpawnPosition;

        JoinPlayer(worldPlayer); // needed to load world before player joins?

        if (multithreading)
        {
            ChunkRedrawThread = new Thread(new ThreadStart(ThreadedUpdate));
            ChunkRedrawThread.Start();
        }

        globalLighting.gameObject.SetActive(true);

        if (Settings.OnlinePlay)
        {
            loadingText.SetActive(false);
            loadingBackground.GetComponent<CanvasGroup>().alpha = 0;
        }

        if(Settings.Platform != 2)
            mainCamera.enabled = false;

        Settings.WorldLoaded = true;

        worldLoadStopWatch.Stop();
        TimeSpan ts = worldLoadStopWatch.Elapsed;
        worldLoadTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
    }

    public void LoadSpawnedPiecesOnlinePlay()
    {
        if (Settings.OnlinePlay && baseObString != null)
        {
            // load pieces
            //LoadSpawnedPieces(baseObString);

            if(SettingsStatic.LoadedSettings.loadLdrawBaseFile)
            {
                baseOb = LDrawImportRuntime.Instance.ImportLDrawOnline("baseNew", baseObString, LDrawImportRuntime.Instance.importPosition, true); // needs to have unique name (not base) for multiplayer
                LDrawImportRuntime.Instance.baseOb = baseOb;
                baseOb.transform.position = LDrawImportRuntime.Instance.importPosition;
            }
        }
    }

    public int GetGalaxy(int planetNumber)
    {
        int galaxy = Mathf.CeilToInt(planetNumber / 64.0f);
        return galaxy;
    }

    public int GetSystem(int planetNumber)
    {
        int solarSystem = Mathf.CeilToInt(planetNumber / 8.0f);
        return solarSystem;
    }

    public int GetDistToStar(int planetNumber)
    {
        int solarSystem = Mathf.CeilToInt(planetNumber / 8.0f);
        int distToStar = (int)(planetNumber - 8.0f * (solarSystem - 1));
        return distToStar;
    }

    public int GetSeedFromSpaceCoords (int galaxy, int solarSystem, int distToStar)
    {
        int planetNumber = (int)((galaxy - 1) * 64.0f + (solarSystem - 1) * 8.0f + distToStar);
        return planetNumber;
    }

    public void WorldDataOverrides(int planetNumber)
    {
        //override worldData with planet data for specific planets in our solar system, otherwise randomize the blockIDs/colors
        int minRandBlockID = 2;
        int maxRandBlockID = 10;
        int minRandBlockIDFlora = 5;
        int numberOfHardcodedPlanets = 18;

        worldData.system = GetSystem(planetNumber);
        worldData.distToStar = GetDistToStar(planetNumber);
        worldData.galaxy = GetGalaxy(planetNumber);
        int distToStar = worldData.distToStar;

        if (planetNumber <= numberOfHardcodedPlanets) // 8 planets + 9 solid colored planets
        {
            Planet planet = planets[planetNumber];

            worldData.blockIDsubsurface = planet.blockIDsubsurface;
            worldData.blockIDcore = planet.blockIDcore;
            worldData.blockIDwater = planet.blockIDwater;
            worldData.blockIDBiome00 = planet.blockIDBiome00;
            worldData.blockIDBiome01 = planet.blockIDBiome01;
            worldData.blockIDBiome02 = planet.blockIDBiome02;
            worldData.blockIDBiome03 = planet.blockIDBiome03;
            worldData.blockIDBiome04 = planet.blockIDBiome04;
            worldData.blockIDBiome05 = planet.blockIDBiome05;
            worldData.blockIDBiome06 = planet.blockIDBiome06;
            worldData.blockIDBiome07 = planet.blockIDBiome07;
            worldData.blockIDBiome08 = planet.blockIDBiome08;
            worldData.blockIDBiome09 = planet.blockIDBiome09;
            worldData.blockIDBiome10 = planet.blockIDBiome10;
            worldData.blockIDBiome11 = planet.blockIDBiome11;
            worldData.hasAtmosphere = planet.hasAtmosphere;
            worldData.isAlive = planet.isAlive; // controls if the world is hospitable to flora
            worldData.biomes = planet.biomes; // controls which biomes the world has
            worldData.blockIDTreeLeavesWinter = planet.blockIDTreeLeavesWinter;
            worldData.blockIDTreeLeavesSpring = planet.blockIDTreeLeavesSpring;
            worldData.blockIDTreeLeavesSummer = planet.blockIDTreeLeavesSummer;
            worldData.blockIDTreeLeavesFall1 = planet.blockIDTreeLeavesFall1;
            worldData.blockIDTreeLeavesFall2 = planet.blockIDTreeLeavesFall2;
            worldData.blockIDTreeTrunk = planet.blockIDTreeTrunk;
            worldData.blockIDCacti = planet.blockIDCacti;
            worldData.blockIDMushroomLargeCap = planet.blockIDMushroomLargeCap;
            worldData.blockIDMushroomLargeStem = planet.blockIDMushroomLargeStem;
            worldData.blockIDMonolith = planet.blockIDMonolith;
            worldData.blockIDEvergreenLeaves = planet.blockIDEvergreenLeaves;
            worldData.blockIDEvergreenTrunk = planet.blockIDEvergreenTrunk;
            worldData.blockIDHoneyComb = planet.blockIDHoneyComb;
            worldData.blockIDHugeTreeLeaves = planet.blockIDHugeTreeLeaves;
            worldData.blockIDHugeTreeTrunk = planet.blockIDHugeTreeTrunk;
            worldData.blockIDColumn = planet.blockIDColumn;
        }
        if (planetNumber > numberOfHardcodedPlanets) // random colored planets based on proximity to star
        {
            // if (distToStar >= 0 && distToStar <= 3) // hot, close to star
            // {
            //     minRandBlockID = 2;
            //     maxRandBlockID = 4;
            // }
            // else if (distToStar >= 4 && distToStar <= 6) // temperate, medium distance from star
            // {
            //     minRandBlockID = 5;
            //     maxRandBlockID = 6;
            // }
            // else if (distToStar >= 7 && distToStar <= 8) // cold, far from star
            // {
            //     minRandBlockID = 7;
            //     maxRandBlockID = 11;
            // }
            //randomize blockIDs based on seed values
            // Default ProcGen values based on seed
            worldData.blockIDsubsurface = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDcore = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDwater = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome00 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome01 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome02 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome03 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome04 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome05 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome06 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome07 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome08 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome09 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome10 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome11 = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            if (worldData.distToStar < 2 || worldData.distToStar > 5) // if distToStar is too close or far (too hot/cold)
            {
                worldData.hasAtmosphere = false; // world has no atmosphere
                worldData.isAlive = false; // world inhospitable to flora
            }
            else
            {
                worldData.hasAtmosphere = true; // world has atmosphere
                worldData.isAlive = true; // world is hospitable to flora
            }
            worldData.biomes = new int[] {0, 1, 2, 3, 4, 5, 6}; // controls which biomes the world has
            worldData.blockIDTreeLeavesWinter = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDTreeLeavesSpring = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDTreeLeavesSummer = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDTreeLeavesFall1 = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDTreeLeavesFall2 = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDTreeTrunk = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDCacti = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDMushroomLargeCap = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDMushroomLargeStem = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDMonolith = (byte)UnityEngine.Random.Range(minRandBlockID, 24);
            worldData.blockIDEvergreenLeaves = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDEvergreenTrunk = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDHoneyComb = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDHugeTreeLeaves = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDHugeTreeTrunk = (byte)UnityEngine.Random.Range(minRandBlockIDFlora, maxRandBlockID);
            worldData.blockIDColumn = (byte)UnityEngine.Random.Range(minRandBlockID, maxRandBlockID);
        }

        biomes[0].surfaceBlock = worldData.blockIDBiome00;
        biomes[1].surfaceBlock = worldData.blockIDBiome01;
        biomes[2].surfaceBlock = worldData.blockIDBiome02;
        biomes[3].surfaceBlock = worldData.blockIDBiome03;
        biomes[4].surfaceBlock = worldData.blockIDBiome04;
        biomes[5].surfaceBlock = worldData.blockIDBiome05;
        biomes[6].surfaceBlock = worldData.blockIDBiome06;
        biomes[7].surfaceBlock = worldData.blockIDBiome07;
        biomes[8].surfaceBlock = worldData.blockIDBiome08;
        biomes[9].surfaceBlock = worldData.blockIDBiome09;
        biomes[10].surfaceBlock = worldData.blockIDBiome10;
        biomes[11].surfaceBlock = worldData.blockIDBiome11;
    }

    public void LoadWorld()
    {
        // loadDistance must always be greater than viewDistance, the larger the multiplier, the less frequent load times
        if (!Settings.WebGL && worldSizeInChunks < 100)
            loadDistance = SettingsStatic.LoadedSettings.viewDistance;
        else if (!Settings.WebGL)
            loadDistance = Mathf.CeilToInt(SettingsStatic.LoadedSettings.viewDistance * 1.333f); // optimal loadDistance provides enough world to hide edges with low load time (7 sec)
        else
            loadDistance = Mathf.CeilToInt(viewDistance * 1.333f);

        for (int x = (worldSizeInChunks / 2) - loadDistance; x < (worldSizeInChunks / 2) + loadDistance; x++)
        {
            for (int z = (worldSizeInChunks / 2) - loadDistance; z < (worldSizeInChunks / 2) + loadDistance; z++)
                worldData.LoadChunkFromFile(new Vector2Int(x, z));
        }
    }

    public void JoinPlayer(GameObject playerGameObject)
    {
        Player player;

        if (playerGameObject == worldPlayer)
        {
            player = new Player(playerGameObject, "WorldPlayer"); // world player is needed to generate the world before the player is added
            players.Add(player);
        }
        else if(Settings.WebGL)
        {
            player = playerGameObject.GetComponent<Controller>().player;
            players.Add(player);
        }
        else if (Settings.Platform != 2) // if platform != mobile
        {
            player = playerGameObject.GetComponent<Controller>().player;
            players.Add(player);
        }
        else // must be mobile (VR)
        {
            player = new Player(playerGameObject, "VR Player");
            players.Add(player);
        }

        playerGameObjects.Add(player, player.playerGameObject);

        // Set player position from save file
        if (Settings.Platform != 2 && IsGlobalPosInsideBorder(player.spawnPosition)) // if the player position is in world border move to default spawn position
        {
            CharacterController charController = playerGameObject.GetComponent<CharacterController>();
            bool playerCharControllerActive = charController.enabled; // save active state of player character controller to reset to old value after teleport
            charController.enabled = false; // disable character controller since this prevents teleporting to saved locations
            playerGameObject.transform.position = player.spawnPosition; // teleport player to saved location
            charController.enabled = playerCharControllerActive; // reset character controller to previous state we saved earlier
        }
        else // if player pos is not in world
            playerGameObject.transform.position = defaultSpawnPosition; // spawn at world spawn point

        ChunkCoord coord = GetChunkCoordFromVector3(playerGameObject.transform.position);
        playerChunkCoords.Add(coord);
        playerLastChunkCoords.Add(playerChunkCoords[playerCount]);

        int firstLoadDrawDistance;

        if (playerCount < 1 && playerGameObject.transform.position == defaultSpawnPosition) // for world player
            firstLoadDrawDistance = loadDistance; // SettingsStatic.LoadedSettings.drawDistance; // first load distance is just large enough to render world for world player
        else
            firstLoadDrawDistance = loadDistance; // max value is 3 to ensure older PCs can still perform this

        if (firstLoadDrawDistance < loadDistance) // checks to ensure that firstLoadDrawDistance is at least as large as loadDistance
            firstLoadDrawDistance = loadDistance;


        FirstCheckViewDistance(firstLoadDrawDistance); // draw the default spawn chunk

        playerCount++;

        SetUndrawVoxels();
    }

    public void SetUndrawVoxels()
    {
        undrawVoxels = true;
        if ((!Settings.OnlinePlay && playerCount > 2)) // cannot undraw voxels in local splitscreen with more than 1 player regardless of graphics settings
            undrawVoxels = false;
        else if (!Settings.WebGL && SettingsStatic.LoadedSettings.graphicsQuality == 3) // if local splitscreen (singleplayer) or online and graphics settings are set to ultra
            undrawVoxels = false;
        undrawVBO = undrawVoxels; // set same as undrawVoxels
    }

    private void Update()
    {
        if (!Settings.WebGL && !worldLoaded) // don't continue with main loop if world has not been loaded.
            return;

        if(drawVBO)
            activeChunksVBOListCopy = new List<ChunkCoord>(activeChunksVBOList);
        if (drawVBO)
            activeChunksVBOList.Clear();

        // create copies of the lists to use so the original lists can be modified during the update loop (was causing errors)
        playersCopy = players;
        playerChunkCoordsCopy = playerChunkCoords; // playerChunkCoords is a placeholder so new players can join while playerChunkCoordsCopy is used in update loop
        playerLastChunkCoordsCopy = playerLastChunkCoords;

        for (int i = 0; i < playersCopy.Count; i++) // for all players (need to include worldplayer here since we do not know if the world player was added first or not, later check if worldplayer)
        {
            // if the player disconnected, remove their gameobject from the dictionary and go to the next dictionary value
            if (playersCopy[i] == null)
            {
                playersCopy.RemoveAt(i);
                playerChunkCoordsCopy.RemoveAt(i);
                playerLastChunkCoordsCopy.RemoveAt(i);
                continue;
            }

            // if the player is not the worldPlayer (checks for null players if the client disconnects before host). Also ensures that the chunk coords and players have same number of indices
            if (playersCopy[i].playerGameObject != worldPlayer && playersCopy[i].playerGameObject != null && playersCopy.Count == playerChunkCoordsCopy.Count)
            {
                playerChunkCoordsCopy[i] = GetChunkCoordFromVector3(playerGameObjects[playersCopy[i]].transform.position); // get the current chunkCoords for given player camera

                // Only update the chunks if the player has moved from the chunk they were previously on.
                if (!playerChunkCoordsCopy[i].Equals(playerLastChunkCoordsCopy[i]))
                {

                    CheckViewDistance(playerChunkCoordsCopy[i], i); // re-draw chunks
                    if (drawVBO)
                        CheckVBODrawDist(playerChunkCoordsCopy[i], i); // re-draw studs
                }
            }
        }

        if (!multithreading)
        {
            if (!applyingModifications)
                    ApplyModifications();

                if (chunksToUpdate.Count > 0)
                    UpdateChunks();
        }

        if (chunksToDraw.Count > 0)
        {
            {
                chunkDrawStopWatch.Start();

                chunksToDraw.Dequeue().CreateMesh();

                chunkDrawStopWatch.Stop();
                TimeSpan ts = chunkDrawStopWatch.Elapsed;
                chunkDrawTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
            }
        }

        if (drawVBO)
        {
            foreach (Player p in players)
            {
                foreach (ChunkCoord c in p.chunksToAddVBO)
                {
                    if (!activeChunksVBOList.Contains(c))
                        activeChunksVBOList.Add(c); // complile master list of chunks to draw objects
                }
            }
        }

        if (drawVBO)
        {
            foreach (ChunkCoord c in activeChunksVBOList)
            {
                AddVBOToChunk(c); // add voxel bound objects in chunksToDrawVBOList
            }
        }

        if (drawVBO)
        {
            foreach (ChunkCoord c in activeChunksVBOList)
            {
                if (activeChunksVBOListCopy.Contains(c))
                    activeChunksVBOListCopy.Remove(c);
            }
        }

        if (drawVBO && undrawVBO)
        {
            foreach (ChunkCoord c in activeChunksVBOListCopy)
            {
                RemoveVBOFromChunk(c); // remove voxel bound objects in previousChunksToDrawVBOList
            }
        }

        using (s_SimulatePerfMarker.Auto())
        {

        }
    }

    public void AddChunkToUpdate(Chunk chunk)
    {
        AddChunkToUpdate(chunk, false);
    }

    public void AddChunkToUpdate(Chunk chunk, bool insert)
    {
        // Lock list to ensure only one thing is using the list at a time.
        lock (ChunkUpdateThreadLock)
        {
            // Make sure update list doesn't already contain chunk.
            if (!chunksToUpdate.Contains(chunk))
            {
                // If insert is true, chunk gets inserted at the top of the list.
                if (insert)
                    chunksToUpdate.Insert(0, chunk);
                else
                    chunksToUpdate.Add(chunk);
            }
        }
    }

    void CheckVBODrawDist(ChunkCoord playerChunkCoord, int player)
    {
        if (drawVBO)
        {
            players[player].chunksToAddVBO.Clear();

            // Loop through all chunks currently within view distance of the player.
            for (int x = playerChunkCoord.x - VBORenderDistanceInChunks; x < playerChunkCoord.x + VBORenderDistanceInChunks; x++)
            {
                for (int z = playerChunkCoord.z - VBORenderDistanceInChunks; z < playerChunkCoord.z + VBORenderDistanceInChunks; z++)
                {
                    ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                    // If the current chunk is in the world...
                    if (IsChunkInWorld(thisChunkCoord))
                    {
                        if (!players[player].chunksToAddVBO.Contains(thisChunkCoord)) // if the list doesn't already contain this value
                            players[player].chunksToAddVBO.Add(thisChunkCoord); // mark chunk to draw objects
                    }
                }
            }
        }
    }

    void CheckViewDistance(ChunkCoord playerChunkCoord, int player)
    {
        playerLastChunkCoords[player] = playerChunkCoord;

        // if toggled, undraw chunks to save memory
        if (undrawVoxels)
        {
            previouslyActiveChunks = new List<ChunkCoord>(activeChunks);
            activeChunks.Clear();
        }

        // Loop through all chunks currently within view distance of the player.
        for (int x = playerChunkCoord.x - viewDistance; x < playerChunkCoord.x + viewDistance; x++)
        {
            for (int z = playerChunkCoord.z - viewDistance; z < playerChunkCoord.z + viewDistance; z++)
            {
                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                // If the current chunk is in the world...
                if (IsChunkInWorld(thisChunkCoord))
                {
                    // Check if its in view distance, if not, mark it to be re-drawn.
                    if (chunks[x, z] == null) // if the chunks array is empty at thisChunkCoord
                        chunks[x, z] = new Chunk(thisChunkCoord); // adds this chunk to the array at this position - CREATING NEW CHUNKS TAKES UP 90% OF CPU TIME WHEN PROFILING WHEN RUNNING THE POPULATE > GET VOXEL FUNCTION (Profiling get voxel runs out of memory)

                    chunks[x, z].isActive = true;
                    activeChunks.Add(thisChunkCoord); // marks chunk to be re-drawn by thread
                }

                // if this chunk coord is in the previous list, remove it so it doesn't get undrawn
                for (int i = 0; i < previouslyActiveChunks.Count; i++)
                {
                    if (previouslyActiveChunks[i].Equals(thisChunkCoord))
                        previouslyActiveChunks.RemoveAt(i);
                }
            }
        }

        // Any chunks left in the previousActiveChunks list are no longer in the player's view distance, so loop through and disable them (i.e. mark to un-draw them).
        foreach (ChunkCoord c in previouslyActiveChunks)
            chunks[c.x, c.z].isActive = false; // marks chunks to be un-drawn

        // for all loaded chunks in the world (regardless of previouslyActiveChunks list)
        foreach (Chunk c in chunks)
        {
            if (c != null)
            {
                // if the current active chunk is within the undraw distance
                if (c.coord.x < playerChunkCoord.x + undrawDistance && c.coord.x > playerChunkCoord.x - undrawDistance && c.coord.z < playerChunkCoord.z + undrawDistance && c.coord.z > playerChunkCoord.z - undrawDistance)
                    chunks[c.coord.x, c.coord.z].isActive = true;
                else
                    chunks[c.coord.x, c.coord.z].isActive = false;
            }
        }
    }

    void ThreadedUpdate() // the loop where the chunk draw occurs, this operation is threaded.
    {
        while (true)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToUpdate.Count > 0)
            {
                UpdateChunks();
            }
        }
    }

    // called during the threaded chunk draw
    void UpdateChunks()
    {
        lock (ChunkUpdateThreadLock)
        {
            chunksToUpdate[0].UpdateChunk(); // draw previous chunks

            if (!activeChunks.Contains(chunksToUpdate[0].coord)) // if the activeChunks does not contain the chunksToUpdate
                activeChunks.Add(chunksToUpdate[0].coord); // add it to activeChunks at end of list
            chunksToUpdate.RemoveAt(0); // remove previously drawn chunk from start of list
        }
    }

    void FirstCheckViewDistance(int firstDrawDistance) // load first chunks independent of player join
    {
        ChunkCoord _spawnChunkCoord = GetChunkCoordFromVector3(Settings.DefaultSpawnPosition);

        //playerLastChunkCoords[playerIndex] = playerChunkCoord;

        for (int x = _spawnChunkCoord.x - firstDrawDistance; x < _spawnChunkCoord.x + firstDrawDistance; x++)
        {
            for (int z = _spawnChunkCoord.z - firstDrawDistance; z < _spawnChunkCoord.z + firstDrawDistance; z++)
            {
                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                // If the current chunk is in the world...
                if (IsChunkInWorld(thisChunkCoord))
                {
                    // Check if its in view distance, if not, mark it to be re-drawn.
                    if (chunks[x, z] == null) // if the chunks array is empty at thisChunkCoord
                    {
                        chunks[x, z] = new Chunk(thisChunkCoord); // adds this chunk to the array at this position
                    }
                    activeChunks.Add(thisChunkCoord); // sends chunks to thread to be re-drawn
                }
            }
        }

        // ChunkCoord _spawnChunkCoord = GetChunkCoordFromVector3(Settings.DefaultSpawnPosition);
        // ChunkCoord thisChunkCoord = new ChunkCoord(_spawnChunkCoord.x, _spawnChunkCoord.z);

        // // If the current chunk is in the world...
        // if (IsChunkInWorld(thisChunkCoord))
        // {
        //     // Check if its in view distance, if not, mark it to be re-drawn.
        //     if (chunks[thisChunkCoord.x, thisChunkCoord.z] == null) // if the chunks array is empty at thisChunkCoord
        //     {
        //         chunks[thisChunkCoord.x, thisChunkCoord.z] = new Chunk(thisChunkCoord); // adds this chunk to the array at this position
        //     }
        //     activeChunks.Add(thisChunkCoord); // sends chunks to thread to be re-drawn
        // }

        for(int i = 0; i < chunksToUpdate.Count; i++)
            chunksToUpdate[i].UpdateChunk(); // draw previous chunks during first world draw

        worldLoaded = true;
    }

    public byte GetVoxel(Vector3Int globalPos)
    {

        //// For performance testing
        //return 0;

        // The main algorithm used in the procedural world generation
        // used to determine voxelID at each position in a chunk if not previously calculated.
        // Runs whenever voxel ids need to be calculated (only modified voxels are saved to the serialized file).
        // optimized to try to determine blockID in the fastest way possible
        // try to calculate the most commone block type first to minimize the # of checked conditions we run for each global position.

        // TERRAIN GEN ALGORITHM FOLLOWS THIS GENERAL ORDER:
        // BIOME PASS - needed for surface blocks
        // TERRAIN HEIGHT CALC - needed for Surface blocks/3D Noise
        // SURFACE BLOCKS
        // 3D NOISE PASS (Air, Block, Water)
        // CAVES
        // LODES
        // STRUCTURES

        int yGlobalPos = Mathf.FloorToInt(globalPos.y);
        Vector2 xzCoords = new Vector2(Mathf.FloorToInt(globalPos.x), Mathf.FloorToInt(globalPos.z));
        int chunkXCoord = Mathf.FloorToInt((globalPos.x - defaultSpawnPosition.x)/VoxelData.ChunkWidth * -0.375f); //scale factor controls biome sizes
        byte voxelValue = 0;

        /* IMMUTABLE PASS */
        // If outside world, return air.
        if (!IsGlobalPosInWorld(globalPos))
            return 0;

        //// for small worlds, return air outside world border to enable edges to render all faces and not block camera movement
        //if (!IsGlobalPosInsideBorder(globalPos))
        //    return 0;

        // reserve space for imported base file
        if (SettingsStatic.LoadedSettings.loadLdrawBaseFile && !Settings.WebGL && CheckMakeBase(globalPos))
            return 0;

        // bottom of world layer is core block (e.g. water/lava)
        if(yGlobalPos == 0)
            return 2; // bedrock
        else if (yGlobalPos == 1)
            return worldData.blockIDcore; // planet core block (e.g. lava)

        /* BIOME SELECTION PASS */
        // Calculates biome (determines surface and subsurface blocktypes)
        temperature = Noise.Get2DPerlin(xzCoords, 6666, 0.06f); // determines cloud threshold and biome
        humidity = Noise.Get2DPerlin(xzCoords, 2222, 0.07f); // determines cloud threshold and biome

        if (!Settings.WebGL && SettingsStatic.LoadedSettings.biomeOverride != 12)
            biome = biomes[SettingsStatic.LoadedSettings.biomeOverride];
        else if (!useBiomes)
            biome = biomes[0];
        else if (!worldData.isAlive)
            biome = biomes[11];
        else
            biome = biomes[GetBiome(chunkXCoord)];

        /* TERRAIN HEIGHT CALC */
        // USE 2D PERLIN NOISE AND SPLINE POINTS TO CALCULATE TERRAINHEIGHT
        terrainHeight = CalcTerrainHeight(xzCoords);

        /* 3D NOISE BASE TERRAIN GENERATION */
        // Calculate air blocks based on 3D Noise
        isAir = GetIsAir(globalPos);
        if (isAir)
            voxelValue = 0; // air
        else if (yGlobalPos < terrainHeight)
            voxelValue = worldData.blockIDsubsurface; // stone
        if (yGlobalPos == terrainHeight)
            voxelValue = biome.surfaceBlock; // dirt
        if (voxelValue == 0 && yGlobalPos <= seaLevelPercentChunk * VoxelData.ChunkHeight) // Generate water below sealevel
            voxelValue = worldData.blockIDwater; // water

        // /* OLD BASE TERRAIN GENERATION */
        // // add block types determined by biome calculation
        // if (yGlobalPos == terrainHeight)// && terrainHeightPercentChunk >= seaLevelPercentChunk) // if surface block above sea level
        //     voxelValue = biome.surfaceBlock;
        // else if (yGlobalPos < terrainHeight) // must be subsurface block
        //     voxelValue = worldData.blockIDsubsurface;

        //return voxelValue; // for testing without LODES or SURFACE OBJECTS

        /* CAVE PASS */
        //3D noise used for caves
        if(yGlobalPos < terrainHeight - 10)
        {
            if(Noise.Get3DPerlin(globalPos, 30, 0.001f, 0.8f)) // large cave
                return 0;
            else if (Noise.Get3DPerlin(globalPos, 40, 0.01f, 0.6f)) // medium cave
                return 0;
            else if (Noise.Get3DPerlin(globalPos, 30, 0.06f, 0.5f)) // small cave
                return 0;
        }

        //WEIRD TERRAIN GEN FOR ALL BUT WORLD 3
        if (worldData.planetSeed != 3)
        {
            // INSERT WEIRD WORLD GEN ALGORITHM HERE

            // if (yGlobalPos > 1)
            //     {
            //         isAir = GetIsAir(globalPos);
            //         if (isAir)
            //             return 0;
            //     }
        }

        /* LODE PASS */
        // noise used to determine if to use cheese, spaghetti, or noodle caves
        //add ores and underground caves
        // if object is below terrain, do not bother running code for surface objects
        if (drawLodes && yGlobalPos < terrainHeight - 5) // -5 offset ensures caves do not bleed into top of terrain
        {
            foreach (Lode lode in biome.lodes)
            {
                if (yGlobalPos > lode.minHeight && yGlobalPos < lode.maxHeight) // if position is within allowable lode range
                    if (Noise.Get3DPerlin(globalPos, lode.noiseOffset, lode.scale, lode.threshold)) // look into spawning more ores if air block to sides
                        voxelValue = lode.blockID;
            }
            return voxelValue;
        }

        /* SURFACE OBJECTS PASS */
        // Need to order small then overwrite with large so that smaller items do not overwrite larger items
        // Uses perlin noise (pseudo random) to generate same pattern of objects every time even if values are not saved to file (only modified chunks saved to file?)
        // Larger items take priority and so are ordered last
        // add structures like monoliths and flora like trees and plants and mushrooms
        // uses the tallest object height to limit the altitude at which objects can spawn
        int tallestStructureHeight = 25;
        if (drawSurfaceObjects && (yGlobalPos == terrainHeight && yGlobalPos < (VoxelData.ChunkHeight - tallestStructureHeight) && terrainHeightPercentChunk > seaLevelPercentChunk && worldData.isAlive) || biome == biomes[11]) // only place flora on worlds marked isAlive or if biome is monolith
        {
            // fertility adds random values to determine which surface object to generate and what height it will be
            fertility = Noise.Get2DPerlin(xzCoords, 1111, .9f);
            //fertility = StaticRandom.GetRandom(); // world must be deterministic for multiplayer, chose perlin noise instead
            // percolation = Noise.Get2DPerlin(xzCoords, 2222, .9f);
            surfaceObType = GetSurfaceObType(0, fertility);
            switch (surfaceObType) // ensures only one surface object is sampled (not sampled on top of each other)
            {
                case 0:
                    break;
                case 1:
                    for (int i = 0; i < biome.smallStructures.Length; i++) // for all smallStructures (rare game element for looting structure)
                    {
                        // if (Noise.Get2DPerlin(xzCoords, 0, biome.smallStructures[i].floraZoneScale) > biome.smallStructures[i].floraZoneThreshold)
                        // {
                            //if(StaticRandom.GetRandom() > 0.999f)
                            if (Noise.Get2DPerlin(xzCoords, biome.smallFlora[i].placementOffset, biome.smallStructures[i].floraPlacementScale) > biome.smallStructures[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.smallStructures[i].floraIndex, globalPos, biome.smallStructures[i].minHeight, +
                                    biome.smallStructures[i].maxHeight, biome.smallStructures[i].minRadius, biome.smallStructures[i].maxRadius, fertility, isEarth));
                            }
                        // }
                    }
                    break;
                // case 2:
                //     for (int i = 0; i < biome.mediumStructures.Length; i++) // for all mediumStructures
                //     {
                //         // if (Noise.Get2DPerlin(xzCoords, 0, biome.mediumStructures[i].floraZoneScale) > biome.mediumStructures[i].floraZoneThreshold)
                //         // {
                //             if (Noise.Get2DPerlin(xzCoords, biome.smallFlora[i].placementOffset, biome.mediumStructures[i].floraPlacementScale) > biome.mediumStructures[i].floraPlacementThreshold)
                //             {
                //                 modifications.Enqueue(Structure.GenerateSurfaceOb(biome.mediumStructures[i].floraIndex, globalPos, biome.mediumStructures[i].minHeight, +
                //                     biome.mediumStructures[i].maxHeight, biome.mediumStructures[i].minRadius, biome.mediumStructures[i].maxRadius, fertility, isEarth));
                //             }
                //         // }
                //     }
                //     break;
                // case 3:
                //     for (int i = 0; i < biome.largeStructures.Length; i++) // for all largeStructures
                //     {
                //         // if (Noise.Get2DPerlin(xzCoords, 0, biome.largeStructures[i].floraZoneScale) > biome.largeStructures[i].floraZoneThreshold)
                //         // {
                //             if (Noise.Get2DPerlin(xzCoords, biome.smallFlora[i].placementOffset, biome.largeStructures[i].floraPlacementScale) > biome.largeStructures[i].floraPlacementThreshold)
                //             {
                //                 modifications.Enqueue(Structure.GenerateSurfaceOb(biome.largeStructures[i].floraIndex, globalPos, biome.largeStructures[i].minHeight, +
                //                     biome.largeStructures[i].maxHeight, biome.largeStructures[i].minRadius, biome.largeStructures[i].maxRadius, fertility, isEarth));
                //             }
                //         // }
                //     }
                //     break;
                case 4:
                    for (int i = 0; i < biome.smallFlora.Length; i++) // for all smallFlora
                    {
                        // if (Noise.Get2DPerlin(xzCoords, 0, biome.smallFlora[i].floraZoneScale) > biome.smallFlora[i].floraZoneThreshold)
                        // {
                            //if(StaticRandom.GetRandom() > 0.98f)
                            if (Noise.Get2DPerlin(xzCoords, biome.smallFlora[i].placementOffset, biome.smallFlora[i].floraPlacementScale) > biome.smallFlora[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.smallFlora[i].floraIndex, globalPos, biome.smallFlora[i].minHeight, +
                                    biome.smallFlora[i].maxHeight, biome.smallFlora[i].minRadius, biome.smallFlora[i].maxRadius, fertility, isEarth));
                            }
                        // }
                    }
                    break;
                // case 5:
                //     for (int i = 0; i < biome.mediumFlora.Length; i++) // for all mediumFlora
                //     {
                //         // if (Noise.Get2DPerlin(xzCoords, 0, biome.mediumFlora[i].floraZoneScale) > biome.mediumFlora[i].floraZoneThreshold)
                //         // {
                //             if (Noise.Get2DPerlin(xzCoords, biome.smallFlora[i].placementOffset, biome.mediumFlora[i].floraPlacementScale) > biome.mediumFlora[i].floraPlacementThreshold)
                //             {
                //                 modifications.Enqueue(Structure.GenerateSurfaceOb(biome.mediumFlora[i].floraIndex, globalPos, biome.mediumFlora[i].minHeight, +
                //                     biome.mediumFlora[i].maxHeight, biome.mediumFlora[i].minRadius, biome.mediumFlora[i].maxRadius, fertility, isEarth));
                //             }
                //         // }
                //     }
                //     break;
                case 6:
                    for (int i = 0; i < biome.largeFlora.Length; i++) // for all largeFlora
                    {
                        // if (Noise.Get2DPerlin(xzCoords, 0, biome.largeFlora[i].floraZoneScale) > biome.largeFlora[i].floraZoneThreshold)
                        // {
                            //if(StaticRandom.GetRandom() > 0.98f)
                            if (Noise.Get2DPerlin(xzCoords, biome.smallFlora[i].placementOffset, biome.largeFlora[i].floraPlacementScale) > biome.largeFlora[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.largeFlora[i].floraIndex, globalPos, biome.largeFlora[i].minHeight, +
                                    biome.largeFlora[i].maxHeight, biome.largeFlora[i].minRadius, biome.largeFlora[i].maxRadius, fertility, isEarth));
                            }
                        // }
                    }
                    break;
                case 7:
                    for (int i = 0; i < biome.XLFlora.Length; i++) // for all XLFlora (mushrooms, critical game element for health, therefore comes last in stack)
                    {
                        // if (Noise.Get2DPerlin(xzCoords, 0, biome.XLFlora[i].floraZoneScale) > biome.XLFlora[i].floraZoneThreshold)
                        // {
                            //if(StaticRandom.GetRandom() > 0.98f)
                            if (Noise.Get2DPerlin(xzCoords, biome.smallFlora[i].placementOffset, biome.XLFlora[i].floraPlacementScale) > biome.XLFlora[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.XLFlora[i].floraIndex, globalPos, biome.XLFlora[i].minHeight, +
                                    biome.XLFlora[i].maxHeight, biome.XLFlora[i].minRadius, biome.XLFlora[i].maxRadius, fertility, isEarth));
                            }
                        // }
                    }
                    break;
            }
        }
        return voxelValue;
    }

    public bool CheckMakeBase(Vector3Int globalPos)
    {
        int baseTerrainHeight = terrainHeight;

        if (Settings.Platform != 2 && globalPos.y == baseTerrainHeight && globalPos.x == Mathf.FloorToInt(worldSizeInChunks * VoxelData.ChunkWidth / 2 + VoxelData.ChunkWidth / 2) && globalPos.z == Mathf.FloorToInt(worldSizeInChunks * VoxelData.ChunkWidth / 2 + VoxelData.ChunkWidth / 2))
        {
            modifications.Enqueue(Structure.GenerateSurfaceOb(0, globalPos, 0, 0, 0, 0, 0, isEarth)); // make base at center of first chunk at terrain height
            return true;
        }
        else
            return false;
    }

    public int CalcTerrainHeight(Vector2 xzCoords)
    {
        float continentalnessFactor;
        float erosionFactor;
        float peaksAndValleysFactor;

        // get values for continentalness, erosion, and wierdness from 3 Perlin Noise maps
        continentalness = Noise.Get2DPerlin(xzCoords, 0, 0.08f); // how far from coast

        // use spline points to determine terrainHeight for each component
        // from example https://www.youtube.com/watch?v=CSa5O6knuwI&t=1360s continentalness spline point feeds into erosion and peaks and valleys
        continentalnessFactor = GetValueFromSplinePoints(continentalness, continentalnessSplinePoints);

        if(continentalness < 0.5f)
        {
            erosionFactor = 0;
            peaksAndValleysFactor = 0;
        }
        else
        {
            erosion = Noise.Get2DPerlin(xzCoords, 1, 0.1f); // how flat or mountainous
            peaksAndValleys = Noise.Get2DPerlin(xzCoords, 2, 0.5f); // determines biome variants

            erosionFactor = GetValueFromSplinePoints(erosion, erosionSplinePoints); // higher continentalness leads to less erosion
            peaksAndValleysFactor = GetValueFromSplinePoints(peaksAndValleys, peaksAndValleysSplinePoints); // less erosion leads to higher peaksandvalleys
        }

        // larger values expose weird 3D noise terrain
        weirdness = Noise.Get2DPerlin(xzCoords, 321, 0.01f);
        //weirdness = peaksAndValleysFactor; // weird only where there are large peaks to obfuscate the perlin hills

        // for testing to individually visualize the spline points
        //terrainHeightPercentChunk = continentalnessFactor;
        //terrainHeightPercentChunk = erosionFactor;
        //terrainHeightPercentChunk = peaksAndValleysFactor;
        
        float reductionFactor = 0.5f; // need to update values in spline table

        terrainHeightPercentChunk = Mathf.Clamp(Mathf.Abs(continentalnessFactor - erosionFactor * reductionFactor + peaksAndValleysFactor * reductionFactor), 0, 0.99f);
        int _terrainHeightVoxels;
        // multiplies by number of voxels to get height in voxels
        int maxHeight = VoxelData.ChunkHeight - 1;
        _terrainHeightVoxels = Mathf.Clamp(Mathf.FloorToInt(maxHeight * terrainHeightPercentChunk - 0), 0, maxHeight);
        return _terrainHeightVoxels;
    }

    public bool GetIsAir(Vector3 globalPos)
    {
        // Broken, eventually turn this into a single function for terrainHeight using 3D Perlin Noise and (3) other 2D Perlin Noise maps to determine height and squashing?
        // based on https://youtu.be/CSa5O6knuwI
        // chance of block is a function of height:
        // chance of block (density) gradually decreased to 0 at top
        // chance of block (density) gradually increased to 1 at bottom
        // squashing factor hides the 3D noise by reducing the amount the density was changed
        // terrainHeight controls the base height of the terrain
        
        //float terrainHeight = VoxelData.ChunkHeight / 2; // for testing purposes
        // use to find good values for limits, eventually drive these values by adding 0.1f times the driving factors

        // high density is block, low density is air
        float density = 1f; // initialize

        // float x1 = 0; // bottom of chunk
        // float y1 = 1f; // density at bottom of chunk (must be larger than y2 to have less density/air at surface)
        // float x2 = terrainHeight; // position at terrainHeight
        // float y2 = 0; // density at terrainHeight (must be smaller than y1 to have less density/air at surface)

        // default values
        float x1 = 0; // bottom of chunk
        float y1 = 0.6f; // density at bottom of chunk (must be larger than y2 to have less density/air at surface)
        float x2 = terrainHeight; // position at terrainHeight
        weirdness = Mathf.Clamp(weirdness, 0, y1); // weirdness cannot be higher than y1 to keep y2 positive
        float y2 = 1.0f - weirdness; // density at terrainHeight (must be smaller than y1 to have less density/air at surface)
        
        // float x1 = terrainHeight; // terrainHeight
        // float y1 = 0.5f; // density at terrainHeight
        // float x2 = 0; // bottom of chunk
        // float y2 = 1f; // density unchanged at terrainHeight
        // if(globalPos.y > terrainHeight) // above surface
        // {
        //     x2 = VoxelData.ChunkHeight; // top of chunk
        //     weirdness = Mathf.Clamp(weirdness, 0, y1); // weirdness cannot be higher than y1
        //     y2 = y1 - weirdness; // density decreased above terrain height
        // } 
        // else if (globalPos.y < terrainHeight) // below surface
        // {
        //     x2 = 0; // bottom of chunk
        //     weirdness = Mathf.Clamp(weirdness, 0, y1); // weirdness cannot be higher than y1
        //     y2 = y1 + weirdness; // density increased below terrain height
        // }

        // input elevation, output density (cannot be negative)
        density = GetValueBetweenPoints(new Vector2(x1, y1), new Vector2(x2, y2), globalPos.y);
        density = Mathf.Clamp(Mathf.Abs(density), 0, 1); // must be positive value between 0 and 1

        // input 3d position, output true = air or false = block, high factor/threshold means more likely to be a block
        return Noise.Get3DPerlin(globalPos, 0, 0.1f, density);
                //.Get3DPerlin(globalPos, offset, scale, threshold)
    }

    public float GetValueBetweenPoints(Vector2 first, Vector2 last, float x)
    {
        // for a given input x, return the corresponding value from the linear function built from 2 given points

        // solve for slope using the two given points
        // m = y2-y1/x2-x1
        float m = (first.y - last.y) / (first.x - last.x);

        // solve for y-intercept using one of the given points
        // b = y - mx
        float b = first.y - m * first.x;

        // use slope intercept form to return a y-value for given x value
        float y = m * x + b;
        return y;
    }

    public float GetValueFromSplinePoints(float value, Vector2[] splinePoints)
    {
        float _returnValue = 0f;

        // figure out which spline points the continentalness is between
        for (int i = 0; i < splinePoints.Length - 1; i++)
        {
            if (value > splinePoints[i].x && value < splinePoints[i + 1].x)
                _returnValue = GetValueBetweenPoints(splinePoints[i], splinePoints[i + 1], value);
        }

        return _returnValue;
    }

    public int GetBiome(int chunkXCoord)
    {
        // based on https://minecraft.fandom.com/wiki/Biome
        // From https://minecraft.fandom.com/wiki/Anvil_file_format
        // Minecraft Biomes are saved per X,Z column, rather than being calculated on the fly, which means they can be altered by tools
        // This is useful for map makers. It also prevents bugs where features don't match the biome after changing the terrain algorithm. (Also known as "Biome Shifting").
        // Each Minecraft chunk has a 16×16 byte array with biome IDs called "Biomes".
        // If this array is missing it is filled when the game starts, as well any - 1 values in the array.
        // The converter source provided for developers doesn't include any biome sources, however.

        // hard coded biomes based on lattitude
        // if (chunkXCoord >= 5)
        //     return 3; // Tundra
        // else if (chunkXCoord == 4)
        //     return 6; // Tiaga
        // else if (chunkXCoord == 3)
        //     return 7; // Forest
        // else if (chunkXCoord == 2)
        //     return 8; // Fall Forest
        // else if (chunkXCoord == 1)
        //     return 5; // Woods
        // else if (chunkXCoord == 0)
        //     return 2; // Grassland
        // else if (chunkXCoord == -1)
        //     return 9; // Rain Forest
        // else if (chunkXCoord == -2)
        //     return 10; // Swamp
        // else if (chunkXCoord == -3)
        //     return 4; // Savanna
        // else if (chunkXCoord == -4)
        //     return 1; // Mesa
        // else if(chunkXCoord <= -5)
        //     return 0; // Desert
        // else
        //     return 3; // Tundra

        // // OLD ALGORITHM: Based on Minecraft, used temp and humidity from cloud calc...
        // // too often generates snowy biomes per minecraft youtube video

        if (humidity > 0 && humidity < 0.25f) // (dry)
        {
            if (temperature > 0.75f && temperature < 1.0f) // (freezing)
                return 3; // Tundra
            else if (temperature > 0.5f && temperature < 0.75f) // (cold)
                return 6; // Taiga
            else if (temperature > 0.25f && temperature < 0.5f) // (warm)
                return 0; // Desert
            else // assumes value is between 0f and 0.25f (hot)
                return 4; // Savanna
        }
        else if (humidity > 0.25f && humidity < 0.5f) // (temperate)
        {
            if (temperature > 0.75f && temperature < 1.0f) // (freezing)
                return 5; // Woods
            else if (temperature > 0.5f && temperature < 0.75f) // (cold)
                return 8; // Fall Forest
            else if (temperature > 0.25f && temperature < 0.5f) // (warm)
                return 2; // Grassland
            else // assumes value is between 0f and 0.25f (hot)
                return 1; // Mesa
        }
        else if (humidity > 0.5f && humidity < 0.75f) // (damp)
        {
            if (temperature > 0.75f && temperature < 1.0f) // (freezing)
                return 7; // Forest
            else if (temperature > 0.5f && temperature < 0.75f) // (cold)
                return 8; // Fall Forest
            else if (temperature > 0.25f && temperature < 0.5f) // (warm)
                return 9; // Rainforest
            else // assumes value is between 0f and 0.25f (hot)
                return 10; // Swamp
        }
        else // assumes value is between 0.75f and 1f (wet)
        {
            if (temperature > 0.75f && temperature < 1.0f) // (freezing)
                return 6; // Taiga
            else if (temperature > 0.5f && temperature < 0.75f) // (cold)
                return 7; // Forest
            else if (temperature > 0.25f && temperature < 0.5f) // (warm)
                return 9; // Rainforest
            else // assumes value is between 0f and 0.25f (hot)
                return 10; // Swamp
        }
    }

    public int GetSurfaceObType(float percolation, float fertility)
    {
        // based on https://minecraft.fandom.com/wiki/Biome
        // From https://minecraft.fandom.com/wiki/Anvil_file_format
        // Minecraft Biomes are saved per X,Z column, rather than being calculated on the fly, which means they can be altered by tools
        // This is useful for map makers. It also prevents bugs where features don't match the biome after changing the terrain algorithm. (Also known as "Biome Shifting").
        // Each Minecraft chunk has a 16×16 byte array with biome IDs called "Biomes".
        // If this array is missing it is filled when the game starts, as well any - 1 values in the array.
        // The converter source provided for developers doesn't include any biome sources, however.
        
        // modified to only return grass or flowers based on fertility alone, percolation not used
        if (fertility > 0 && fertility < 0.25f) // (barren)
        {
            return 4; // grass or flowers
            // if (percolation > 0.75f && percolation < 1.0f) // (gravel)
            //     return 1;
            // else if (percolation > 0.5f && percolation < 0.75f) // (sand)
            //     return 4;
            // else if (percolation > 0.25f && percolation < 0.5f) // (silt)
            //     return 6;
            // else // assumes value is between 0f and 0.25f (clay)
            //     return 7;
        }
        else if (fertility > 0.25f && fertility < 0.5f) // (sparse)
        {
            return 4; // grass or flowers
            // if (percolation > 0.75f && percolation < 1.0f) // (gravel)
            //     return 1;
            // else if (percolation > 0.5f && percolation < 0.75f) // (sand)
            //     return 4;
            // else if (percolation > 0.25f && percolation < 0.5f) // (silt)
            //     return 6;
            // else // assumes value is between 0f and 0.25f (clay)
            //     return 7;
        }
        else if (fertility > 0.5f && fertility < 0.75f) // (dense)
        {
            return 6; // trees or cacti
            // if (percolation > 0.75f && percolation < 1.0f) // (gravel)
            //     return 1;
            // else if (percolation > 0.5f && percolation < 0.75f) // (sand)
            //     return 4;
            // else if (percolation > 0.25f && percolation < 0.5f) // (silt)
            //     return 6;
            // else // assumes value is between 0f and 0.25f (clay)
            //     return 7;
        }
        else // assumes value is between 0.75f and 1f (fertile)
        {
            return 6; // trees or cacti
            // if (percolation > 0.75f && percolation < 1.0f) // (gravel)
            //     return 1;
            // else if (percolation > 0.5f && percolation < 0.75f) // (sand)
            //     return 4;
            // else if (percolation > 0.25f && percolation < 0.5f) // (silt)
            //     return 6;
            // else // assumes value is between 0f and 0.25f (clay)
            //     return 7;
        }
    }

    public void AddVBOToChunk(ChunkCoord chunkCoord)
    {
        // for all voxels in chunk
        for (int y = 0; y < VoxelData.ChunkHeight - 1; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    Vector3 globalPosition = new Vector3(chunkCoord.x * VoxelData.ChunkWidth + x, y, chunkCoord.z * VoxelData.ChunkWidth + z);
                    Vector3 globalPositionAbove = new Vector3(chunkCoord.x * VoxelData.ChunkWidth + x, y + 1, chunkCoord.z * VoxelData.ChunkWidth + z);

                    byte blockID = chunks[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id;

                    // if voxel has an object defined
                    if (blockTypes[blockID].voxelBoundObject != null)
                    {
                        // if objects don't already exist
                        if (!objectDictionary.TryGetValue(globalPosition, out _))
                        {
                            //Add VBO to voxel
                            Vector3 VBOPosition = globalPosition;
                            Quaternion VBOorientation = Quaternion.identity;
                            if (blockID == blockIDprocGen || blockID == blockIDbase)
                            {
                                VBOPosition = new Vector3(globalPosition.x + 0.5f, globalPosition.y, globalPosition.z + 0.5f); // make center of the VBO center of the voxel (voxel origin is corner)
                                VBOorientation.eulerAngles = new Vector3(180, 0, 0); // if VBOImport then flip right side up
                            }
                            GameObject VBO;
                            if (blockID == blockIDprocGen && Settings.Platform != 2 && blockTypes[blockID].voxelBoundObject != null)
                            {
                                baseOb = blockTypes[blockID].voxelBoundObject;
                                if (Settings.OnlinePlay)
                                {
                                    if (baseOb.GetComponent<NetworkIdentity>() == null)
                                        baseOb.AddComponent<NetworkIdentity>();
                                }
                                baseOb = Instantiate(blockTypes[blockID].voxelBoundObject, VBOPosition, VBOorientation);
                                baseOb.GetComponent<BoxCollider>().enabled = false; // disable large VBO Box collider used to add placeholder voxels for world procGen
                                AddToBaseChildren(baseOb);
                                VBO = baseOb;
                            }
                            else
                                VBO = Instantiate(blockTypes[blockID].voxelBoundObject, VBOPosition, VBOorientation);
                            objectDictionary.Add(globalPosition, VBO);
                        }
                    }
                }
            }
        }
    }

    public void AddToBaseChildren(GameObject go)
    {
        BoxCollider[] childObs = go.GetComponentsInChildren<BoxCollider>();
        for(int i = 0; i < childObs.Length; i++)
        {
            if(childObs[i].gameObject.layer == 10) // if layer is LegoPiece
            {
                GameObject ob = childObs[i].gameObject;

                ob.transform.parent = null; // unparent as separate objects from base parent object
                if (Settings.OnlinePlay)
                {
                    if (ob.GetComponent<NetworkIdentity>() == null)
                        ob.AddComponent<NetworkIdentity>();
                    customNetworkManager.GetComponent<CustomNetworkManager>().spawnPrefabs.Add(ob); // if not already registered, register child gameObject
                }

                ob.tag = "BaseObPiece";
                baseObPieces.Add(ob);

                ob.GetComponent<BoxCollider>().material = physicMaterial;
            }

        }
    }

    public void AddVBOToVoxel(Vector3 pos, byte id)
    {
        // if voxel has an object defined, then add object to voxel
        if (blockTypes[id].voxelBoundObject != null)
            objectDictionary.Add(pos, Instantiate(blockTypes[id].voxelBoundObject, pos, Quaternion.identity));
    }

    public void RemoveVBOFromChunk(ChunkCoord chunkCoord)
    {
        // for all voxels in chunk
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    Vector3 globalPosition = new Vector3(chunkCoord.x * VoxelData.ChunkWidth + x, y, chunkCoord.z * VoxelData.ChunkWidth + z);
                    Vector3 globalPositionAbove = new Vector3(chunkCoord.x * VoxelData.ChunkWidth + x, y + 1, chunkCoord.z * VoxelData.ChunkWidth + z);

                    // Destroy any gameObject associated with the global position or global position above
                    byte blockID = chunks[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id;

                    if (objectDictionary.TryGetValue(globalPosition, out _) && blockID != blockIDprocGen && blockID != blockIDbase) // voxelBoundObjects but not base or procGen.ldr
                    {
                        Destroy(objectDictionary[globalPosition]);
                        objectDictionary.Remove(globalPosition);
                        //Debug.Log("Removed Object at " + globalPosition.x + ", " + globalPosition.z);
                    }
                    else
                    {
                        //Debug.Log("No entry found in dictionary for: " + globalPositionAbove.x + "," + globalPositionAbove.y + "," + globalPositionAbove.z);
                    }
                }
            }
        }
    }

    public void RemoveVBOFromVoxel(Vector3 pos)
    {
        // for voxel specified
        Vector3 positionAbove = new Vector3(pos.x, pos.y + 1, pos.z);

        // Destroy each gameObject created
        if (objectDictionary.TryGetValue(pos, out _)) // else if voxelBoundObject is stored within voxel coord (all non-stud objects)
        {
            Destroy(objectDictionary[pos]);
            objectDictionary.Remove(pos);
        }
        else
        {
            //Debug.Log("No entry found in dictionary for: " + positionAbove.x + "," + positionAbove.y + "," + positionAbove.z);
        }
    }

    private void OnDisable()
    {
        if (gameObject.activeSelf)
        {
            if (multithreading)
            {
                ChunkRedrawThread.Abort();
            }
        }
    }

    private void ApplyModifications()
    {
        applyingModifications = true;

        while (modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0)
            {
                VoxelMod v = queue.Dequeue();

                worldData.SetVoxel(v.position, v.id, 1); // block orientation set to 1 by default, can determine orientation if generating directional blocks as modifications
            }
        }
        applyingModifications = false;
    }

    public ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return chunks[x, z];
    }

    public bool CheckForVoxel(Vector3 pos)
    {
        VoxelState voxel = worldData.GetVoxel(pos); // gets the voxel state from saved worldData

        if (voxel == null)
            return false;

        if (voxel.id == blockIDprocGen || voxel.id == blockIDbase)
            return true; // VBO placeholder to prevent player from replacing with a voxel

        if (blockTypes[voxel.id].isSolid) // gives error if the player starts outside of the world
            return true;
        else
            return false;
    }

    public VoxelState GetVoxelState (Vector3 pos)
    {
        return worldData.GetVoxel(pos);
    }

    public bool IsChunkInWorld(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < worldSizeInChunks - 1 && coord.z > 0 && coord.z < worldSizeInChunks - 1)
            return true;
        else
            return false;
    }

    public bool IsGlobalPosInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < worldSizeInChunks * VoxelData.ChunkWidth && pos.y >= 0 && pos.y < VoxelData.ChunkHeight - 1 && pos.z >= 0 && pos.z < worldSizeInChunks * VoxelData.ChunkWidth)
            return true;
        else
            return false;
    }

    public bool IsGlobalPosInsideBorder(Vector3 pos)
    {
        // IMPORTANT: has to use SettingsStatic.LoadedSettings.worldSizeInChunks for world size instead of private local variable to put char at correct position (script timing issues?)
        ChunkCoord _newChunkCoord = GetChunkCoordFromVector3(pos);
        if (_newChunkCoord.x > 0 && _newChunkCoord.x < VoxelData.WorldSizeInChunks - 1 && _newChunkCoord.z > 0 && _newChunkCoord.z < VoxelData.WorldSizeInChunks - 1)
            return true;
        else
            return false;
    }

    public void StartDebugTimer()
    {
        debugStopWatch.Reset();
        debugStopWatch.Restart();
    }

    public void StopDebugTimer()
    {
        debugStopWatch.Stop();
        TimeSpan ts = debugStopWatch.Elapsed;
        debugTimer = String.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
    }

    public void StartProfileMarker()
    {
        s_PreparePerfMarker.Begin();
    }

    public void StopProfileMarker()
    {
        s_PreparePerfMarker.End();
    }
}

public class VoxelMod
{
    public Vector3 position;
    public byte id;

    public VoxelMod()
    {
        position = new Vector3();
        id = 0;
    }

    public VoxelMod(Vector3 position, byte id)
    {
        this.position = position;
        this.id = id;
    }
}