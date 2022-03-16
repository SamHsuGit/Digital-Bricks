using Mirror;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class World : MonoBehaviour
{
    public static World Instance { get { return _instance; } }
    public bool worldLoaded = false;
    public int tick;
    public bool saving = false;
    public bool undrawVoxelBoundObjects = false;
    public bool undrawVoxels = false; // does not redraw voxels if set to true...
    public bool VBOs = true;
    public bool chunkMeshColliders = true;
    public int planetNumber;
    public int seed;
    public GameObject baseOb;
    public bool isEarth;

    public Vector2[] continentalnessSplinePoints;
    public Vector2[] erosionSplinePoints;
    public Vector2[] peaksAndValleysSplinePoints;

    // Cached Perlin Noise Map Values
    public float terrainHeightPercentChunk; // defines height of terrain as percentage of total chunkHeight
    public int terrainHeightVoxels = 0; // defines height of terrain in voxels
    public bool isAir = false; // used for 3D Perlin Noise pass
    public float continentalness = 0; // continentalness, defines distance from ocean
    public float erosion = 0; // erosion, defines how mountainous the terrain is
    public float peaksAndValleys = 0; // peaks and valleys
    public float weirdness = 0; // weirdness
    public float temperature = 0; // temperature, defines biome
    public float humidity = 0; // humidity, defines biome + cloud density
    public float fertility = 0; // defines surfaceOb size
    public float percolation = 0; // defines surfaceOb size
    public float placementVBO = 0; // defines placement of Voxel Bound Objects (i.e. studs, grass, flowers)
    public float seaLevelThreshold = 0.34f;
    public int cloudHeight;

    public int surfaceObType = 0;
    public Biome biome;
    public GameObject mainCameraGameObject;
    public Lighting globalLighting;
    public GameObject loadingText;
    public GameObject loadingBackground;
    
    public CustomNetworkManager customNetworkManager;

    [Header("World Generation Values")]
    public Vector3 defaultSpawnPosition;
    public int season;
    public Planet[] planets;
    public Biome[] biomes;
    public int heightOffset = 20;
    public GameObject worldPlayer;
    
    public Material blockMaterial;
    public Material blockMaterialTransparent;
    public PhysicMaterial physicMaterial;
    public BlockType[] blocktypes;
    public GameObject[] voxelPrefabs;
    public AudioSource chunkLoadSound;

    // public variables
    public List<Player> players = new List<Player>();
    public int playerCount = 0;

    // chunk draw lists and arrays
    public ChunkCoord firstChunkCoord;
    public bool firstChunkLoaded;
    public Dictionary<ChunkCoord, Chunk> chunks = new Dictionary<ChunkCoord, Chunk>();
    public Chunk[,] chunksToDrawArray = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    public List<ChunkCoord> chunkCoordsToDrawList = new List<ChunkCoord>();
    public Queue<Chunk> chunksToDrawQueue = new Queue<Chunk>();
    public List<Chunk> chunksToDrawList = new List<Chunk>();
    public List<ChunkCoord> previousChunksToDrawList = new List<ChunkCoord>();
    public List<ChunkCoord> chunksToDrawObjectsList = new List<ChunkCoord>();
    public List<ChunkCoord> copyOfChunksToDrawObjectsList = new List<ChunkCoord>();
    public List<GameObject> baseObPieces = new List<GameObject>();

    public object ChunkDrawThreadLock = new object();
    public object ChunkLoadThreadLock = new object();
    public object ChunkListThreadLock = new object();
    public Dictionary<Vector3, GameObject> studDictionary = new Dictionary<Vector3, GameObject>();
    public Dictionary<Vector3, GameObject> objectDictionary = new Dictionary<Vector3, GameObject>();
    public WorldData worldData;

    private static World _instance;
    private static bool multithreading = true;
    private int loadDistance;
    private int LOD0threshold;
    private int studRenderDistanceInChunks; // acts as a radius like drawDistance

    List<ChunkCoord> playerChunkCoords = new List<ChunkCoord>();
    List<ChunkCoord> playerLastChunkCoords = new List<ChunkCoord>();
    Dictionary<Player, GameObject> playerGameObjects = new Dictionary<Player, GameObject>();
    List<Player> playersCopy = new List<Player>();
    List<ChunkCoord> playerChunkCoordsCopy = new List<ChunkCoord>();
    List<ChunkCoord> playerLastChunkCoordsCopy = new List<ChunkCoord>();

    bool applyingModifications = false;
    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();
    Thread ChunkRedrawThread;
    Camera mainCamera;

    private void Awake()
    {
        defaultSpawnPosition = Settings.DefaultSpawnPosition;
        mainCamera = mainCameraGameObject.GetComponent<Camera>();
        season = Mathf.CeilToInt(System.DateTime.Now.Month / 3f);
        Random.InitState(seed);

        // found that performance is good enough to never undraw voxels or voxelBoundObjects (regardless of graphics settings) which can cause other issues
        undrawVoxelBoundObjects = false;
        undrawVoxels = false; // does not redraw voxels if set to true...

        playerCount = 0;

        // lowest acceptable drawDistance is 1
        if (SettingsStatic.LoadedSettings.viewDistance < 1)
            SettingsStatic.LoadedSettings.viewDistance = 1;

        studRenderDistanceInChunks = 1; // keep studs render distance lower than viewDistance to avoid errors.
        //Debug.Log("viewDist = " + SettingsStatic.LoadedSettings.viewDistance);
        //Debug.Log("stud render dist = " + studRenderDistanceInChunks);

        // If the instance value is not null and not *this*, we've somehow ended up with more than one World component.
        // Since another one has already been assigned, delete this one.
        if (_instance != null && _instance != this)
            Destroy(gameObject);
        // Else set this to the instance.
        else
            _instance = this;

        firstChunkCoord = new ChunkCoord(VoxelData.WorldSizeInChunks / 2, VoxelData.WorldSizeInChunks / 2);

        cloudHeight = VoxelData.ChunkHeight - 15;

        continentalnessSplinePoints = new Vector2[] // low continentalness = ocean
        {
            new Vector2(0.00f, 0.00f),
            new Vector2(seaLevelThreshold, 0.01f),
            new Vector2(0.38f, 0.35f),
            new Vector2(0.51f, 0.36f),
            new Vector2(0.52f, 0.70f),
            new Vector2(0.73f, 0.76f),
            new Vector2(1.00f, 1.00f),
        };

        erosionSplinePoints = new Vector2[] // low erosion = peaks
        {
            new Vector2(0.00f, 1.00f),
            new Vector2(0.10f, 0.70f),
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

        peaksAndValleysSplinePoints = new Vector2[] // high peaks and valley = high peaks
        {
            new Vector2(0.05f, 0.00f),
            new Vector2(0.10f, 0.10f),
            new Vector2(0.35f, 0.31f),
            new Vector2(0.50f, 0.32f),
            new Vector2(0.68f, 0.54f),
            new Vector2(0.90f, 0.80f),
            new Vector2(1.00f, 1.00f),
        };
    }

    public void JoinPlayer(GameObject playerGameObject)
    {
        Player player;

        if (playerGameObject == worldPlayer)
        {
            player = new Player(playerGameObject, "WorldPlayer", this); // world player is needed to generate the world before the player is added
            players.Add(player);
            Debug.Log("Added WorldPlayer");
        }
        else if (Settings.Platform != 2)
        {
            player = playerGameObject.GetComponent<Controller>().player;
            players.Add(player);
            Debug.Log("Added Player");
        }
        else
        {
            player = new Player(playerGameObject, "VR Player", this);
            players.Add(player);
        }

        playerGameObjects.Add(player, player.playerGameObject);

        // Set player position from save file
        if (Settings.Platform != 2 && IsVoxelInWorld(player.spawnPosition)) // if the player position is in world
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
            firstLoadDrawDistance = loadDistance; // max value is 3 to ensure older PCs can still handle the CPU Load

        if (firstLoadDrawDistance < loadDistance) // checks to ensure that firstLoadDrawDistance is at least as large as loadDistance
            firstLoadDrawDistance = loadDistance;

        //if(playerGameObject != worldPlayer) // doesn't make a difference in load times
            FirstCheckDrawDistance(GetChunkCoordFromVector3(playerGameObject.transform.position), playerCount, firstLoadDrawDistance); // help draw the world faster on startup for first player

        playerCount++;
        //Debug.Log("Player Joined");
        //Debug.Log("playerCount = " + playerCount);
    }

    private void Start()
    {
        worldLoaded = false;
        if (planetNumber == 3) // cache result for use in GetVoxel
            isEarth = true;
        else
            isEarth = false;
        worldData = SaveSystem.LoadWorld(planetNumber, seed); // sets the worldData to the value determined by planetNumber and seed which are both set in the GameManger Script
        WorldDataOverrides(planetNumber);

        if (Settings.Platform == 2)
            blocktypes[25].voxelBoundObject = null;
        else
        {
            blocktypes[25].voxelBoundObject = baseOb; // sets the base voxel bound object to the value set in the GameManager Script

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
            ChunkRedrawThread = new Thread(new ThreadStart(ThreadedChunkDraw));
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
        int maxRandBlockID = 24;

        worldData.system = GetSystem(planetNumber);
        worldData.distToStar = GetDistToStar(planetNumber);
        worldData.galaxy = GetGalaxy(planetNumber);
        int distToStar = worldData.distToStar;
        //Debug.Log("Seed:" + GetSeedFromSpaceCoords(worldData.galaxy, worldData.system, worldData.distToStar));
        //Debug.Log("Universe Coords (galaxy, system, planet)" + worldData.galaxy + "-" + worldData.system + "-" + distToStar);

        if (planetNumber < 32) // 8 planets + solid colored planets
        {
            Planet planet = planets[planetNumber];

            worldData.blockIDsubsurface = planet.blockIDsubsurface;
            worldData.blockIDcore = planet.blockIDcore;
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
        if (planetNumber >= 32) // random colored planets based on proximity to star
        {
            if (distToStar >= 0 && distToStar <= 3) // hot, close to star
            {
                minRandBlockID = 3;
                maxRandBlockID = 8;
            }
            else if (distToStar >= 4 && distToStar <= 6) // temperate, medium distance from star
            {
                minRandBlockID = 9;
                maxRandBlockID = 15;
            }
            else if (distToStar >= 7 && distToStar <= 8) // cold, far from star
            {
                minRandBlockID = 16;
                maxRandBlockID = 24;
            }
            //randomize blockIDs based on seed values
            // Default ProcGen values based on seed
            worldData.blockIDsubsurface = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDcore = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome00 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome01 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome02 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome03 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome04 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome05 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome06 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome07 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome08 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome09 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome10 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDBiome11 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
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
            worldData.blockIDTreeLeavesWinter = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDTreeLeavesSpring = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDTreeLeavesSummer = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDTreeLeavesFall1 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDTreeLeavesFall2 = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDTreeTrunk = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDCacti = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDMushroomLargeCap = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDMushroomLargeStem = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDMonolith = (byte)Random.Range(minRandBlockID, 24);
            worldData.blockIDEvergreenLeaves = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDEvergreenTrunk = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDHoneyComb = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDHugeTreeLeaves = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDHugeTreeTrunk = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDColumn = (byte)Random.Range(minRandBlockID, maxRandBlockID);
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
        loadDistance = Mathf.CeilToInt(SettingsStatic.LoadedSettings.viewDistance * 1.333f); //Mathf.CeilToInt(SettingsStatic.LoadedSettings.drawDistance * 1.99f); // cannot be larger than firstLoadDist (optimum value is 4, any larger yields > 30 sec exist world load time)
        LOD0threshold = 1; // Mathf.CeilToInt(SettingsStatic.LoadedSettings.drawDistance * 0.333f);

        for (int x = (VoxelData.WorldSizeInChunks / 2) - loadDistance; x < (VoxelData.WorldSizeInChunks / 2) + loadDistance; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - loadDistance; z < (VoxelData.WorldSizeInChunks / 2) + loadDistance; z++)
                worldData.RequestChunk(new Vector2Int(x, z));
        }
    }

    private void FixedUpdate()
    {
        // disabled autosave feature
        //if (Settings.OnlinePlay)
        //{
        //    saving = false;

        //    // AutoSave feature every hour
        //    if (tick > 50 * 60 * 60 * 24) // 1 sec = 50 ticks, 60 sec = 1 min, 60 min = 1 hr, 24 hr = 1 day (save every day for server maintenance)
        //    {
        //        tick = 0;
        //        saving = true;
        //        Debug.Log("Saved Game: " + System.DateTime.Now);
        //        SaveSystem.SaveWorld(Instance.worldData);
        //    }
        //    else
        //        tick++;
        //}
    }

    private void Update()
    {
        if (!worldLoaded) // don't continue with main loop if world has not been loaded.
            return;

        // if set to not undraw voxels, do not undraw chunks (DISABLED, was causing chunks to 'randomly' be turned off)
        //if(!undrawVoxels)
        //{
        //    previousChunksToDrawList = new List<ChunkCoord>(chunkCoordsToDrawList);
        //    chunkCoordsToDrawList.Clear();
        //}

        copyOfChunksToDrawObjectsList = new List<ChunkCoord>(chunksToDrawObjectsList);
        chunksToDrawObjectsList.Clear();

        // create copies of the lists to use so the original lists can be modified during the update loop (was causing errors)
        playersCopy = players;
        playerChunkCoordsCopy = playerChunkCoords; // playerChunkCoords is a placeholder so new players can join while playerChunkCoordsCopy is used in update loop
        playerLastChunkCoordsCopy = playerLastChunkCoords;
        for (int i = 0; i < playersCopy.Count; i++) // for all players (need to include worldplayer here since we do not know if the world player was added first or not, later check if worldplayer)
        {
            // if the player disconnected, remove their gameobject from the dictionary and go to the next dictionary value
            if (playersCopy[i] == null)//|| playerChunkCoords.Count > 1 && player.Key == worldPlayer)
            {
                playersCopy.RemoveAt(i);
                playerChunkCoordsCopy.RemoveAt(i);
                playerLastChunkCoordsCopy.RemoveAt(i);
                //Debug.Log("Player Quit");
                continue;
            }

            // WIP need to debug why playersCopy.Count != playerChunkCoordsCopy.Count
            //Debug.Log("player " + i + " = " + playersCopy[i].name);
            //Debug.Log(playersCopy.Count);
            //Debug.Log(playerChunkCoordsCopy.Count);
            // if the player is not the worldPlayer (checks for null players if the client disconnects before host). Also ensures that the chunk coords and players have same number of indices
            if (playersCopy[i].playerGameObject != worldPlayer && playersCopy[i].playerGameObject != null && playersCopy.Count == playerChunkCoordsCopy.Count)
            {
                playerChunkCoordsCopy[i] = GetChunkCoordFromVector3(playerGameObjects[playersCopy[i]].transform.position); // get the current chunkCoords for given player camera
                
                //Debug.Log("playerChunkCoordsCopy = " + playerChunkCoordsCopy[playerCount - 1].x + ", " + playerChunkCoordsCopy[playerCount - 1].z);
                //Debug.Log("playerLastChunkCoordsCopy = " + playerLastChunkCoordsCopy[playerCount - 1].x + " , " + playerLastChunkCoordsCopy[playerCount - 1].z);
                // Only update the chunks if the player has moved from the chunk they were previously on.
                if (!playerChunkCoordsCopy[i].Equals(playerLastChunkCoordsCopy[i]))
                {
                    CheckDrawDistance(playerChunkCoordsCopy[i], i); // re-draw chunks
                    CheckVBODrawDist(playerChunkCoordsCopy[i], i); // re-draw studs
                }
            }

            if (chunksToDrawQueue.Count > 0)
            {
                lock (chunksToDrawQueue)
                {
                    chunksToDrawQueue.Dequeue().CreateMesh();
                }
            }

            if (!multithreading)
            {
                if (!applyingModifications)
                    ApplyModifications();

                if (chunksToDrawList.Count > 0)
                {
                    DrawChunks();
                }
            }
        }

        //lock (ChunkDrawThreadLock) // chunks deactivate correctly, but do not get redrawn correctly...
        //{
        //    if (!activateNewChunks) // deactivate all non-active chunks as some still had mesh colliders enabled...
        //    {
        //        foreach (KeyValuePair<ChunkCoord, Chunk> chunk in chunks)
        //        {
        //            if (chunk.Value.isActive)
        //                chunk.Value.isActive = true;
        //            else
        //                chunk.Value.isActive = false;
        //        }
        //    }
        //}

        //if (!activateNewChunks && !firstChunkLoaded)
        //{
        //    AddObjectsToChunk(firstChunkCoord);
        //    firstChunkLoaded = true; // only load the firstChunkVBO once and only check while in 'tutorial' small world mode
        //}

        foreach (Player p in players)
        {
            foreach (ChunkCoord c in p.chunksToAddVBO)
            {
                if (!chunksToDrawObjectsList.Contains(c))
                    chunksToDrawObjectsList.Add(c); // complile master list of chunks to draw objects
            }
        }

        foreach (ChunkCoord c in chunksToDrawObjectsList)
        {
            //if (!activateNewChunks && chunks.ContainsKey(c) && chunks[c].isActive)
            //    AddObjectsToChunk(c); // add voxel bound objects in chunksToDrawObjectsList
            //else if (activateNewChunks)
            if(VBOs)
                AddObjectsToChunk(c); // add voxel bound objects in chunksToDrawObjectsList
        }

        foreach (ChunkCoord c in chunksToDrawObjectsList)
        {
            if (copyOfChunksToDrawObjectsList.Contains(c))
                copyOfChunksToDrawObjectsList.Remove(c);
        }

        if (undrawVoxelBoundObjects)
        {
            // create a new copy of master list
            // clear master list
            // for each player
            // clear player list
            // create player list of chunks to draw objects
            // Add all player list values into master list (avoid duplicates)
            // create objects in master list
            // destroy objects in copy of master list
            foreach (ChunkCoord c in copyOfChunksToDrawObjectsList)
            {
                //if(activateNewChunks) // only undraw if out of tutorial mode
                if (VBOs)
                    RemoveObjectsFromChunk(c); // remove voxel bound objects in previousChunksToDrawObjectsList
            }
        }
    }

    void CheckVBODrawDist(ChunkCoord playerChunkCoord, int playerIndex)
    {
        players[playerIndex].chunksToAddVBO.Clear();

        // Loop through all chunks currently within view distance of the player.
        for (int x = playerChunkCoord.x - studRenderDistanceInChunks; x < playerChunkCoord.x + studRenderDistanceInChunks; x++)
        {
            for (int z = playerChunkCoord.z - studRenderDistanceInChunks; z < playerChunkCoord.z + studRenderDistanceInChunks; z++)
            {
                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                // If the current chunk is in the world...
                if (IsChunkInWorld(thisChunkCoord))
                {
                    if (!players[playerIndex].chunksToAddVBO.Contains(thisChunkCoord)) // if the list doesn't already contain this value
                        players[playerIndex].chunksToAddVBO.Add(thisChunkCoord); // mark chunk to draw objects
                }
            }
        }
    }

    void CheckDrawDistance(ChunkCoord playerChunkCoord, int playerIndex)
    {
        playerLastChunkCoords[playerIndex] = playerChunkCoord;

        // if set to undrawVoxels, undraw chunks to save memory (Disabled until we can find a way to remove far away chunks selectively)
        if (undrawVoxels)
        {
            previousChunksToDrawList = new List<ChunkCoord>(chunkCoordsToDrawList);
            chunkCoordsToDrawList.Clear();
        }

        // Loop through all chunks currently within view distance of the player.
        for (int x = playerChunkCoord.x - SettingsStatic.LoadedSettings.viewDistance; x < playerChunkCoord.x + SettingsStatic.LoadedSettings.viewDistance; x++)
        {
            for (int z = playerChunkCoord.z - SettingsStatic.LoadedSettings.viewDistance; z < playerChunkCoord.z + SettingsStatic.LoadedSettings.viewDistance; z++)
            {
                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                // If the current chunk is in the world...
                if (IsChunkInWorld(thisChunkCoord))
                {
                    // Check if its in view distance, if not, mark it to be re-drawn.
                    if (chunksToDrawArray[x, z] == null) // if the chunksToDrawArray is empty at thisChunkCoord
                        chunksToDrawArray[x, z] = new Chunk(thisChunkCoord); // adds this chunk to the array at this position
                    chunksToDrawArray[x, z].isInDrawDist = true;
                    chunkCoordsToDrawList.Add(thisChunkCoord); // marks chunk to be re-drawn by thread

                    if(chunksToDrawList.Contains(chunksToDrawArray[x, z]))
                    {
                        chunksToDrawList[chunksToDrawList.IndexOf(chunksToDrawArray[x, z])].isInStructDrawDist = false; // mark as outside LOD0
                        if (x > playerChunkCoord.x - LOD0threshold && x < playerChunkCoord.x + LOD0threshold && z > playerChunkCoord.z - LOD0threshold && z < playerChunkCoord.z + LOD0threshold)
                            chunksToDrawList[chunksToDrawList.IndexOf(chunksToDrawArray[x, z])].isInStructDrawDist = true; // mark as inside LOD0
                    }
                }

                // if this chunk coord is in the previous list, remove it so it doesn't get undrawn
                for (int i = 0; i < previousChunksToDrawList.Count; i++)
                {
                    if (previousChunksToDrawList[i].Equals(thisChunkCoord))
                    {
                        previousChunksToDrawList.RemoveAt(i);
                    }
                }
            }
        }

        // Any chunks left in the previousActiveChunks list are no longer in the player's view distance, so loop through and mark to un-draw them.
        foreach (ChunkCoord c in previousChunksToDrawList)
        {
            if (chunksToDrawArray[c.x, c.z] != null)
            {
                chunksToDrawArray[c.x, c.z].isInDrawDist = false; // marks chunks to be un-drawn
            }
        }
    }

    void ThreadedChunkDraw() // the loop where the chunk draw occurs, this operation is threaded.
    {
        while (true)
        {
            if (!applyingModifications)
                ApplyModifications();

            if (chunksToDrawList.Count > 0)
                DrawChunks();
        }
    }

    // called during the threaded chunk draw
    void DrawChunks()
    {
        lock (ChunkDrawThreadLock)
        {
            chunksToDrawList[0].DrawChunk(); // draw previous chunks

            if (!chunkCoordsToDrawList.Contains(chunksToDrawList[0].coord)) // if the chunkCoordsToDrawList does not contain the chunkToDrawList
                chunkCoordsToDrawList.Add(chunksToDrawList[0].coord); // add it to chunkCoordsToDrawList at end of list
            chunksToDrawList.RemoveAt(0); // remove previously drawn chunk from start of list
        }
    }

    void FirstCheckDrawDistance(ChunkCoord playerChunkCoord, int playerIndex, int firstDrawDistance) // used to load a larger portion of the world upon scene start for first player
    {
        playerLastChunkCoords[playerIndex] = playerChunkCoord;

        for (int x = playerChunkCoord.x - firstDrawDistance; x < playerChunkCoord.x + firstDrawDistance; x++)
        {
            for (int z = playerChunkCoord.z - firstDrawDistance; z < playerChunkCoord.z + firstDrawDistance; z++)
            {
                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                // If the current chunk is in the world...
                if (IsChunkInWorld(thisChunkCoord))
                {
                    // Check if its in view distance, if not, mark it to be re-drawn.
                    if (chunksToDrawArray[x, z] == null) // if the chunksToDrawArray is empty at thisChunkCoord
                    {
                        chunksToDrawArray[x, z] = new Chunk(thisChunkCoord); // adds this chunk to the array at this position
                    }
                    chunkCoordsToDrawList.Add(thisChunkCoord); // sends chunks to thread to be re-drawn
                }
            }
        }

        for(int i = 0; i < chunksToDrawList.Count; i++)
            chunksToDrawList[i].DrawChunk(); // draw previous chunks during first world draw

        worldLoaded = true;
    }

    public byte GetVoxel(Vector3 globalPos)
    {
        // The main algorithm used in the procedural world generation
        // used to determine voxelID at each position in a chunk. Runs whenever voxel ids need to be calculated (only modified voxels are saved to the serialized file).
        // optimized to try to determine blockID in the fastest way possible

        int yGlobalPos = Mathf.FloorToInt(globalPos.y);
        int xGlobalPos = Mathf.FloorToInt(globalPos.x);
        int zGlobalPos = Mathf.FloorToInt(globalPos.z);
        Vector2 xzCoords = new Vector2(xGlobalPos, zGlobalPos);

        //// Cannot know what chunks are surrounded without first calculating the neighboring voxels...
        //// ignore chunks which do not touch transparent blocks
        //VoxelState[] neighborVoxelStates = new VoxelState[] { };
        //bool visible = false;
        //for (int p = 0; p < 6; p++)
        //{
        //    Vector3 neighborPos = globalPos + VoxelData.faceChecks[p];
        //    neighborVoxelStates[p] = GetChunkFromVector3(globalPos).CheckVoxel(neighborPos);
        //}
        //for(int i = 0; i < neighborVoxelStates.Length; i++) // if any face is visible, flag it
        //{
        //    if (neighborVoxelStates[i] != null && blocktypes[neighborVoxelStates[i].id].isTransparent)
        //        visible = true;
        //}
        //if (!visible) // if none of faces are visible, return 0 (air)
        //    return 0;

        /* IMMUTABLE PASS */
        // If outside world, return air.
        if (!IsVoxelInWorld(globalPos))
            return 0;

        // planet 0, seed 0 is a blank canvas for building around the imported ldraw file
        if (worldData.planetNumber == 0 && worldData.seed == 0)
        {
            terrainHeightVoxels = 1;
            if (yGlobalPos == 1 && !CheckMakeBase(globalPos))
                return 4;
            else if (yGlobalPos > 1)
                return 0;
        }

        // bottom of world layer is core block (e.g. water/lava)
        if (yGlobalPos == 1)
            return worldData.blockIDcore; // planet core block (e.g. lava)

        // If between certain height range, return clouds.
        if (yGlobalPos > cloudHeight && yGlobalPos < cloudHeight + 5)
        {
            // smaller clouds create illusion of more of them loaded (cloud density threshold determined by noise to generate large areas of thicker cloud cover)
            if (Noise.Get2DPerlin(new Vector2(xGlobalPos, zGlobalPos), 52, 0.1f) > 0.2f) // determines if cloud cover is dense or not
            {
                if (Noise.Get3DPerlin(globalPos, 1234, 0.2f, 0.6f)) // light cloud cover
                    return 4; // blocktype = cloud
                else
                    return 0; // blocktype = air
            }
            else
            {
                if (Noise.Get3DPerlin(globalPos, 1234, 0.2f, 0.4f)) // dense cloud cover
                    return 4; // blocktype = cloud
                else
                    return 0; // blocktype = air
            }
        }

        /* BASIC TERRAIN PASS */
        byte voxelValue = 0;
        GetTerrainHeight(xzCoords);
        if (yGlobalPos > terrainHeightVoxels) // guarantees all blocks above terrainHeight are 0
            return 0;
        if (weirdness > 0.5f)
        {
            isAir = GetIsAir(globalPos);
            if (isAir)
            {
                if (!CheckMakeBase(globalPos))
                    return 0;
            }
        }
        CheckMakeBase(globalPos);

        /* BIOME SELECTION PASS */
        humidity = Noise.Get2DPerlin(xzCoords, 2222, 0.07f); // determines cloud density and biome (call only once, expensive)
        temperature = Noise.Get2DPerlin(xzCoords, 6666, 0.06f); // determines cloud density and biome (call only once, expensive)
        if (!worldData.isAlive)
            biome = biomes[11];
        else
            biome = biomes[GetBiome(temperature, humidity)];

        /* TERRAIN PASS */
        if (yGlobalPos == terrainHeightVoxels && terrainHeightPercentChunk < seaLevelThreshold) // if surface block below sea level
            voxelValue = worldData.blockIDcore;
        else if (yGlobalPos == terrainHeightVoxels && terrainHeightPercentChunk >= seaLevelThreshold) // if surface block above sea level
            voxelValue = biome.surfaceBlock;
        else // must be subsurface block
            voxelValue = worldData.blockIDsubsurface;

        /* LODE PASS */
        if (yGlobalPos < terrainHeightVoxels - 5)
        {
            foreach (Lode lode in biome.lodes)
            {
                {
                    if (yGlobalPos > lode.minHeight) // make upper limit chunkHeight instead of lode.maxHeight since chunkHeight is variable
                        if (Noise.Get3DPerlin(globalPos, lode.noiseOffset, lode.scale, lode.threshold))
                            voxelValue = lode.blockID;
                }
            }
            return voxelValue; // if object is below terrain, do not bother running code for surface objects
        }

        /* SURFACE OBJECTS PASS */
        if ((yGlobalPos == terrainHeightVoxels && yGlobalPos < cloudHeight && terrainHeightPercentChunk > seaLevelThreshold && worldData.isAlive) || biome == biomes[11]) // only place flora on worlds marked isAlive or if biome is monolith
        {
            fertility = Noise.Get2DPerlin(xzCoords, 1111, .9f); // ideally only call once (expensive)
            percolation = Noise.Get2DPerlin(xzCoords, 2315, .9f); // ideally only call once (expensive)
            surfaceObType = GetSurfaceObType(percolation, fertility);
            placementVBO = Noise.Get2DPerlin(xzCoords, 321, 10f); // ideally only call once (expensive)

            switch (surfaceObType)
            {
                case 1:
                    for (int i = 0; i < biome.smallStructures.Length; i++) // for all smallStructures
                    {
                        if (Noise.Get2DPerlin(xzCoords, 0, biome.smallStructures[i].floraZoneScale) > biome.smallStructures[i].floraZoneThreshold)
                        {
                            if (Noise.Get2DPerlin(xzCoords, 0, biome.smallStructures[i].floraPlacementScale) > biome.smallStructures[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.smallStructures[i].floraIndex, globalPos, biome.smallStructures[i].minHeight, +
                                    biome.smallStructures[i].maxHeight, biome.smallStructures[i].minRadius, biome.smallStructures[i].maxRadius, fertility, isEarth));
                            }
                        }
                    }
                    break;
                case 2:
                    for (int i = 0; i < biome.mediumStructures.Length; i++) // for all mediumStructures
                    {
                        if (Noise.Get2DPerlin(xzCoords, 0, biome.mediumStructures[i].floraZoneScale) > biome.mediumStructures[i].floraZoneThreshold)
                        {
                            if (Noise.Get2DPerlin(xzCoords, 0, biome.mediumStructures[i].floraPlacementScale) > biome.mediumStructures[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.mediumStructures[i].floraIndex, globalPos, biome.mediumStructures[i].minHeight, +
                                    biome.mediumStructures[i].maxHeight, biome.mediumStructures[i].minRadius, biome.mediumStructures[i].maxRadius, fertility, isEarth));
                            }
                        }
                    }
                    break;
                case 3:
                    for (int i = 0; i < biome.largeStructures.Length; i++) // for all largeStructures
                    {
                        if (Noise.Get2DPerlin(xzCoords, 0, biome.largeStructures[i].floraZoneScale) > biome.largeStructures[i].floraZoneThreshold)
                        {
                            if (Noise.Get2DPerlin(xzCoords, 0, biome.largeStructures[i].floraPlacementScale) > biome.largeStructures[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.largeStructures[i].floraIndex, globalPos, biome.largeStructures[i].minHeight, +
                                    biome.largeStructures[i].maxHeight, biome.largeStructures[i].minRadius, biome.largeStructures[i].maxRadius, fertility, isEarth));
                            }
                        }
                    }
                    break;
                case 4:
                    for (int i = 0; i < biome.smallFlora.Length; i++) // for all smallFlora
                    {
                        if (Noise.Get2DPerlin(xzCoords, 0, biome.smallFlora[i].floraZoneScale) > biome.smallFlora[i].floraZoneThreshold)
                        {
                            if (Noise.Get2DPerlin(xzCoords, 0, biome.smallFlora[i].floraPlacementScale) > biome.smallFlora[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.smallFlora[i].floraIndex, globalPos, biome.smallFlora[i].minHeight, +
                                    biome.smallFlora[i].maxHeight, biome.smallFlora[i].minRadius, biome.smallFlora[i].maxRadius, fertility, isEarth));
                            }
                        }
                    }
                    break;
                case 5:
                    for (int i = 0; i < biome.mediumFlora.Length; i++) // for all mediummFlora
                    {
                        if (Noise.Get2DPerlin(xzCoords, 0, biome.mediumFlora[i].floraZoneScale) > biome.mediumFlora[i].floraZoneThreshold)
                        {
                            if (Noise.Get2DPerlin(xzCoords, 0, biome.mediumFlora[i].floraPlacementScale) > biome.mediumFlora[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.mediumFlora[i].floraIndex, globalPos, biome.mediumFlora[i].minHeight, +
                                    biome.mediumFlora[i].maxHeight, biome.mediumFlora[i].minRadius, biome.mediumFlora[i].maxRadius, fertility, isEarth));
                            }
                        }
                    }
                    break;
                case 6:
                    for (int i = 0; i < biome.largeFlora.Length; i++) // for all largeFlora
                    {
                        if (Noise.Get2DPerlin(xzCoords, 0, biome.largeFlora[i].floraZoneScale) > biome.largeFlora[i].floraZoneThreshold)
                        {
                            if (Noise.Get2DPerlin(xzCoords, 0, biome.largeFlora[i].floraPlacementScale) > biome.largeFlora[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.largeFlora[i].floraIndex, globalPos, biome.largeFlora[i].minHeight, +
                                    biome.largeFlora[i].maxHeight, biome.largeFlora[i].minRadius, biome.largeFlora[i].maxRadius, fertility, isEarth));
                            }
                        }
                    }
                    break;
                case 7:
                    for (int i = 0; i < biome.XLFlora.Length; i++) // for all XLFlora
                    {
                        if (Noise.Get2DPerlin(xzCoords, 0, biome.XLFlora[i].floraZoneScale) > biome.XLFlora[i].floraZoneThreshold)
                        {
                            if (Noise.Get2DPerlin(xzCoords, 0, biome.XLFlora[i].floraPlacementScale) > biome.XLFlora[i].floraPlacementThreshold)
                            {
                                modifications.Enqueue(Structure.GenerateSurfaceOb(biome.XLFlora[i].floraIndex, globalPos, biome.XLFlora[i].minHeight, +
                                    biome.XLFlora[i].maxHeight, biome.XLFlora[i].minRadius, biome.XLFlora[i].maxRadius, fertility, isEarth));
                            }
                        }
                    }
                    break;
            }
        }
        return voxelValue;
    }

    public bool CheckMakeBase(Vector3 globalPos)
    {
        if (Settings.Platform != 2 && globalPos.y == terrainHeightVoxels && globalPos.x == Mathf.FloorToInt(VoxelData.WorldSizeInVoxels / 2 + VoxelData.ChunkWidth / 2) && globalPos.z == Mathf.FloorToInt(VoxelData.WorldSizeInVoxels / 2 + VoxelData.ChunkWidth / 2))
        {
            modifications.Enqueue(Structure.GenerateSurfaceOb(0, globalPos, 0, 0, 0, 0, 0, isEarth)); // make base at center of first chunk at terrain height
            return true;
        }
        else
            return false;
            
    }

    public void GetTerrainHeight(Vector2 xzCoords)
    {
        // get values for continentalness, erosion, and wierdness from 3 Perlin Noise maps
        continentalness = Noise.Get2DPerlin(xzCoords, 0, 0.08f);
        erosion = Noise.Get2DPerlin(xzCoords, 1, 0.1f);
        peaksAndValleys = Noise.Get2DPerlin(xzCoords, 2, 0.5f);
        weirdness = Noise.Get2DPerlin(xzCoords, 321, 0.08f);

        // use spline points to determine terrainHeight for each component
        float continentalnessFactor = GetValueFromSplinePoints(continentalness, continentalnessSplinePoints);
        float erosionFactor = GetValueFromSplinePoints(erosion, erosionSplinePoints);
        float peaksAndValleysFactor = GetValueFromSplinePoints(peaksAndValleys, peaksAndValleysSplinePoints);

        terrainHeightPercentChunk = continentalness * continentalnessFactor + erosion * erosionFactor + peaksAndValleys * peaksAndValleysFactor;
        terrainHeightVoxels = Mathf.Clamp(Mathf.FloorToInt(cloudHeight * terrainHeightPercentChunk - 0),0, cloudHeight); // multiplies by number of voxels to get height in voxels
    }

    public bool GetIsAir(Vector3 globalPos)
    {
        // WIP, meant to be a single function for terrainHeight using 3D Perlin Noise and (3) other 2D Perlin Noise maps to determine height and squashing
        // based on https://youtu.be/CSa5O6knuwI

        //GetTerrainHeight(new Vector2(globalPos.x, globalPos.z));

        // testing
        //float terrainHeight = 0.5f; // WIP terrain height seems to update based changing this value
        //float squashingFactor = 0.1f; // WIP squashing factor does not seem to flatten the terrain

        //terrainHeight = terrainHeightPercentChunk;
        //squashingFactor = weirdness * weirdnessFactor;

        //float upperLimit = Mathf.Clamp(terrainHeight + squashingFactor, terrainHeight, 1);
        //float lowerLimit = Mathf.Clamp(terrainHeight - squashingFactor, 0, terrainHeight);

        //int terrainHeightVoxels = Mathf.FloorToInt(terrainHeight * cloudHeight);
        //int upperLimitVoxels = Mathf.FloorToInt(upperLimit * cloudHeight);
        //int lowerLimitVoxels = Mathf.FloorToInt(lowerLimit * cloudHeight);

        //float density; // density varies from 1 at bottom of chunk to 0 at top of chunk
        //if (globalPos.y >= cloudHeight) // density is always 0 above cloudHeight
        //    density = 0;
        //else if (globalPos.y > upperLimitVoxels && globalPos.y < cloudHeight) // density is always 0 above upper limit
        //    density = 0;
        //else if (globalPos.y > terrainHeightVoxels && globalPos.y <= upperLimitVoxels) // density decreases above ground
        //    density = GetValueBetweenPoints(new Vector2(terrainHeight, 0.5f), new Vector2(cloudHeight, 0), globalPos.y);
        //else if (globalPos.y == terrainHeightVoxels) // terrainHeight acts as mid point where density is constant
        //    density = 0.5f;
        //else if (globalPos.y >= lowerLimitVoxels && globalPos.y < terrainHeightVoxels) // density increases below ground
        //    density = GetValueBetweenPoints(new Vector2(0, 1), new Vector2(terrainHeight, 0.5f), globalPos.y);
        //else
        //    density = 1; // density is always 1 below lower limit

        //float density = GetValueBetweenPoints(new Vector2(0, 1), new Vector2(cloudHeight, 0), globalPos.y);
        float density = terrainHeightPercentChunk;
        return Noise.Get3DPerlin(globalPos, 0, 0.1f, density); // scale sets size of perlin noise, high density = higher perlin noise threshold to return true (isAir)
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
        float terrainHeight = 0f;

        // figure out which spline points the continentalness is between
        for (int i = 0; i < splinePoints.Length - 1; i++)
        {
            if (continentalness > splinePoints[i].x && continentalness < splinePoints[i + 1].x)
                terrainHeight = GetValueBetweenPoints(splinePoints[i], splinePoints[i + 1], continentalness);
        }

        return terrainHeight;
    }

    public int GetBiome(float temperature, float humidity)
    {
        // based on https://minecraft.fandom.com/wiki/Biome

        // From https://minecraft.fandom.com/wiki/Anvil_file_format
        // Minecraft Biomes are saved per X,Z column, rather than being calculated on the fly, which means they can be altered by tools
        // This is useful for map makers. It also prevents bugs where features don't match the biome after changing the terrain algorithm. (Also known as "Biome Shifting").
        // Each Minecraft chunk has a 16×16 byte array with biome IDs called "Biomes".
        // If this array is missing it is filled when the game starts, as well any - 1 values in the array.
        // The converter source provided for developers doesn't include any biome sources, however.

        if (humidity > 0 && humidity < 0.25f) // (dry)
        {
            if (temperature > 0.75f && temperature < 1.0f) // (freezing)
                return 0;
            else if (temperature > 0.5f && temperature < 0.75f) // (cold)
                return 1;
            else if (temperature > 0.25f && temperature < 0.5f) // (warm)
                return 2;
            else // assumes value is between 0f and 0.25f (hot)
                return 3;
        }
        else if (humidity > 0.25f && humidity < 0.5f) // (temperate)
        {
            if (temperature > 0.75f && temperature < 1.0f) // (freezing)
                return 4;
            else if (temperature > 0.5f && temperature < 0.75f) // (cold)
                return 5;
            else if (temperature > 0.25f && temperature < 0.5f) // (warm)
                return 6;
            else // assumes value is between 0f and 0.25f (hot)
                return 3;
        }
        else if (humidity > 0.5f && humidity < 0.75f) // (damp)
        {
            if (temperature > 0.75f && temperature < 1.0f) // (freezing)
                return 7;
            else if (temperature > 0.5f && temperature < 0.75f) // (cold)
                return 8;
            else if (temperature > 0.25f && temperature < 0.5f) // (warm)
                return 6;
            else // assumes value is between 0f and 0.25f (hot)
                return 3;
        }
        else // assumes value is between 0.75f and 1f (wet)
        {
            if (temperature > 0.75f && temperature < 1.0f) // (freezing)
                return 9;
            else if (temperature > 0.5f && temperature < 0.75f) // (cold)
                return 10;
            else if (temperature > 0.25f && temperature < 0.5f) // (warm)
                return 6;
            else // assumes value is between 0f and 0.25f (hot)
                return 3;
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

        if (fertility > 0 && fertility < 0.25f) // (barren)
        {
            if (percolation > 0.75f && percolation < 1.0f) // (gravel)
                return 0;
            else if (percolation > 0.5f && percolation < 0.75f) // (sand)
                return 1;
            else if (percolation > 0.25f && percolation < 0.5f) // (silt)
                return 2;
            else // assumes value is between 0f and 0.25f (clay)
                return 3;
        }
        else if (fertility > 0.25f && fertility < 0.5f) // (sparse)
        {
            if (percolation > 0.75f && percolation < 1.0f) // (gravel)
                return 4;
            else if (percolation > 0.5f && percolation < 0.75f) // (sand)
                return 5;
            else if (percolation > 0.25f && percolation < 0.5f) // (silt)
                return 5;
            else // assumes value is between 0f and 0.25f (clay)
                return 6;
        }
        else if (fertility > 0.5f && fertility < 0.75f) // (dense)
        {
            if (percolation > 0.75f && percolation < 1.0f) // (gravel)
                return 4;
            else if (percolation > 0.5f && percolation < 0.75f) // (sand)
                return 5;
            else if (percolation > 0.25f && percolation < 0.5f) // (silt)
                return 6;
            else // assumes value is between 0f and 0.25f (clay)
                return 6;
        }
        else // assumes value is between 0.75f and 1f (fertile)
        {
            if (percolation > 0.75f && percolation < 1.0f) // (gravel)
                return 5;
            else if (percolation > 0.5f && percolation < 0.75f) // (sand)
                return 6;
            else if (percolation > 0.25f && percolation < 0.5f) // (silt)
                return 6;
            else // assumes value is between 0f and 0.25f (clay)
                return 7;
        }
    }

    public void AddObjectsToChunk(ChunkCoord chunkCoord)
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

                    // if voxel matches Perlin noise pattern
                    if (blocktypes[chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id].studs != null && Noise.Get2DPerlin(new Vector2(x, z), 321, 10f) < 0.1f)
                    {
                        // if studs don't already exist
                        if (!studDictionary.TryGetValue(globalPositionAbove, out _))
                        {
                            // if voxel is solid, and voxel above is air, and voxel is not barrier block
                            if (blocktypes[chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id].isSolid && chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y + 1, z].id == 0 && chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id != 1)
                            {
                                // add studs
                                studDictionary.Add(globalPositionAbove, Instantiate(blocktypes[chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id].studs, globalPositionAbove, Quaternion.identity));
                            }
                        }
                        else
                        {
                            //Debug.Log(globalPositionAbove + " already exists");
                        }
                    }

                    byte blockID = chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id;

                    // if voxel has an object defined
                    if (blocktypes[blockID].voxelBoundObject != null)
                    {
                        // if objects don't already exist
                        if (!objectDictionary.TryGetValue(globalPosition, out _))
                        {
                            //Add VBO to voxel
                            Vector3 VBOPosition = globalPosition;
                            Quaternion VBOorientation = Quaternion.identity;
                            if (blockID == 25 || blockID == 26)
                            {
                                VBOPosition = new Vector3(globalPosition.x + 0.5f, globalPosition.y, globalPosition.z + 0.5f); // make center of the VBO center of the voxel (voxel origin is corner)
                                VBOorientation.eulerAngles = new Vector3(180, 0, 0); // if VBOImport then flip right side up
                            }
                            GameObject VBO;
                            if (blockID == 25 && Settings.Platform != 2)
                            {
                                baseOb = blocktypes[blockID].voxelBoundObject;
                                if (Settings.OnlinePlay)
                                {
                                    if (baseOb.GetComponent<NetworkIdentity>() == null)
                                        baseOb.AddComponent<NetworkIdentity>();
                                }
                                baseOb = Instantiate(blocktypes[blockID].voxelBoundObject, VBOPosition, VBOorientation);
                                baseOb.GetComponent<BoxCollider>().enabled = false; // disable large VBO Box collider used to add placeholder voxels for world procGen
                                AddToBaseChildren(baseOb);
                                VBO = baseOb;
                            }
                            else
                                VBO = Instantiate(blocktypes[blockID].voxelBoundObject, VBOPosition, VBOorientation);
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

    public void AddObjectsToVoxel(Vector3 pos, byte id)
    {
        // if voxel has an object defined, then add object to voxel
        if (blocktypes[id].voxelBoundObject != null)
            objectDictionary.Add(pos, Instantiate(blocktypes[id].voxelBoundObject, pos, Quaternion.identity));
    }

    public void RemoveObjectsFromChunk(ChunkCoord chunkCoord)
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
                    byte blockID = chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id;

                    if (objectDictionary.TryGetValue(globalPosition, out _) && blockID != 25 && blockID != 26) // voxelBoundObjects but not base or procGen.ldr
                    {
                        Destroy(objectDictionary[globalPosition]);
                        objectDictionary.Remove(globalPosition);
                        //Debug.Log("Removed Object at " + globalPosition.x + ", " + globalPosition.z);
                    }
                    else if (studDictionary.TryGetValue(globalPositionAbove, out _)) // studs
                    {
                        Destroy(studDictionary[globalPositionAbove]);
                        studDictionary.Remove(globalPositionAbove);
                        //Debug.Log("Removed Object at " + globalPositionAbove.x + ", " + globalPositionAbove.z);
                    }
                    else
                    {
                        //Debug.Log("No entry found in dictionary for: " + globalPositionAbove.x + "," + globalPositionAbove.y + "," + globalPositionAbove.z);
                    }
                }
            }
        }
    }

    public void RemoveObjectsFromVoxel(Vector3 pos)
    {
        // for voxel specified
        Vector3 positionAbove = new Vector3(pos.x, pos.y + 1, pos.z);

        // Destroy each studs gameObject created
        if (studDictionary.TryGetValue(positionAbove, out _)) // if voxelBoundObject is stored above voxel coord (e.g. studs)
        {
            Destroy(studDictionary[positionAbove]);
            studDictionary.Remove(positionAbove);
        }
        else if (objectDictionary.TryGetValue(pos, out _)) // else if voxelBoundObject is stored within voxel coord (all non-stud objects)
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

    void ApplyModifications()
    {
        applyingModifications = true;

        while (modifications.Count > 0)
        {
            Queue<VoxelMod> queue = modifications.Dequeue();

            while (queue.Count > 0)
            {
                VoxelMod v = queue.Dequeue();

                worldData.SetVoxel(v.position, v.id);
            }
        }
        applyingModifications = false;
    }

    ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth);
        return chunksToDrawArray[x, z];
    }

    public bool CheckForVoxel(Vector3 pos)
    {
        VoxelState voxel = worldData.GetVoxel(pos); // gets the voxel state from saved worldData

        if (voxel == null)
            return false;

        if (voxel.id == 25 || voxel.id == 26)
            return true; // VBO placeholder to prevent player from replacing with a voxel

        if (blocktypes[voxel.id].isSolid) // gives error if the player starts outside of the world
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
        if (coord.x > 0 && coord.x < VoxelData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < VoxelData.WorldSizeInChunks - 1)
            return true;
        else
            return false;
    }

    public bool IsVoxelInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels && pos.y >= 0 && pos.y < VoxelData.ChunkHeight && pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels)
            return true;
        else
            return false;
    }
}

[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isDrawn;
    public bool isSolid;
    public bool isTransparent;
    public Sprite icon;
    public GameObject studs;
    public GameObject voxelBoundObject;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Back, Front, Top, Bottom, Left, Right

    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Error in GetTextureID; invalid face index");
                return 0;
        }
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