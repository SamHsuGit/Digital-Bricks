using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using Mirror;

public class World : MonoBehaviour
{
    public static World Instance { get { return _instance; } }
    public bool worldLoaded = false;
    public int tick;
    public bool saving = false;
    public bool undrawVoxelBoundObjects = true;
    public bool undrawVoxels = false;

    public GameObject mainCameraGameObject;
    public Lighting globalLighting;
    public GameObject loadingText;
    public GameObject loadingBackground;
    public GameObject baseOb;
    public CustomNetworkManager customNetworkManager;

    [Header("World Generation Values")]
    public Planet[] planets;
    public Biome[] biomes;
    public int solidGroundHeight = 20;
    public GameObject worldPlayer;
    public Vector3 spawnPosition;
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
    public bool activateNewChunks = false;
    public Dictionary<ChunkCoord, Chunk> chunks = new Dictionary<ChunkCoord, Chunk>();
    public Chunk[,] chunksToDrawArray = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    public List<ChunkCoord> chunkCoordsToDrawList = new List<ChunkCoord>();
    public Queue<Chunk> chunksToDrawQueue = new Queue<Chunk>();
    public List<Chunk> chunksToDrawList = new List<Chunk>();
    public List<ChunkCoord> previousChunksToDrawList = new List<ChunkCoord>();
    public List<ChunkCoord> chunksToDrawObjectsList = new List<ChunkCoord>();
    public List<ChunkCoord> copyOfChunksToDrawObjectsList = new List<ChunkCoord>();

    public object ChunkDrawThreadLock = new object();
    public object ChunkLoadThreadLock = new object();
    public object ChunkListThreadLock = new object();
    public Dictionary<Vector3, GameObject> studDictionary = new Dictionary<Vector3, GameObject>();
    public Dictionary<Vector3, GameObject> objectDictionary = new Dictionary<Vector3, GameObject>();
    public string appPath;
    public WorldData worldData;

    private static World _instance;
    private static bool multithreading = true;
    private int loadDistance;
    private int LOD0threshold;
    private int studRenderDistanceInChunks; // acts as a radius like viewDistance

    List<ChunkCoord> playerChunkCoords = new List<ChunkCoord>();
    List<ChunkCoord> playerLastChunkCoords = new List<ChunkCoord>();
    Dictionary<Player, Controller> controllers = new Dictionary<Player, Controller>();
    List<Player> _players = new List<Player>();
    List<ChunkCoord> _playerChunkCoords = new List<ChunkCoord>();
    List<ChunkCoord> _playerLastChunkCoords = new List<ChunkCoord>();

    bool applyingModifications = false;
    Queue<Queue<VoxelMod>> modifications = new Queue<Queue<VoxelMod>>();
    Thread ChunkRedrawThread;
    Camera mainCamera;

    private void Awake()
    {
        Random.InitState(SettingsStatic.LoadedSettings.seed);
        //if (Settings.OnlinePlay && isClientOnly && hasAuthority) // if client only, request worldData and seed from host
        //{
        //    // https://mirror-networking.gitbook.io/docs/guides/data-types
        //    CmdRequestWorldData(); // (Mirror cannot send Lists for modifiedChunks, would need to write custom extension to NetworkWriter and NetworkReader)
        //    CmdRequestSeed();
        //}

        if (SettingsStatic.LoadedSettings.graphicsQuality  == 2)
        {
            undrawVoxelBoundObjects = false;
            undrawVoxels = false;
        }
        else if(SettingsStatic.LoadedSettings.graphicsQuality == 1)
        {
            undrawVoxelBoundObjects = true;
            undrawVoxels = false;
        }
        else if(SettingsStatic.LoadedSettings.graphicsQuality == 0)
        {
            undrawVoxelBoundObjects = false;
            undrawVoxels = false;
        }

        playerCount = 0;

        // lowest acceptable viewDistance is 1
        if (SettingsStatic.LoadedSettings.drawDistance < 1)
            SettingsStatic.LoadedSettings.drawDistance = 1;

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

        appPath = Application.persistentDataPath;
        //activateNewChunks = false;
        firstChunkCoord = new ChunkCoord(VoxelData.WorldSizeInChunks / 2, VoxelData.WorldSizeInChunks / 2);
    }

    //[Command]
    //void CmdRequestWorldData()
    //{
    //    RpcSetWorldData(World.Instance.worldData); // send worldData from server host to all clients
    //}

    //[ClientRpc]
    //void RpcSetWorldData(WorldData worldData)
    //{
    //    World.Instance.worldData = worldData;
    //}

    //[Command]
    //void CmdRequestSeed()
    //{
    //    RpcSetSeed(SettingsStatic.LoadedSettings.seed);
    //}

    //[ClientRpc]
    //void RpcSetSeed(int seed)
    //{
    //    SettingsStatic.LoadedSettings.seed = seed;
    //}

    public void PlayerJoined(GameObject playerGameObject)
    {
        Player player;

        if (playerGameObject == worldPlayer)
        {
            player = new Player(playerGameObject, "WorldPlayer");
            players.Add(player);
        }
        else
            player = playerGameObject.GetComponent<Controller>().player;

        controllers.Add(player, player.playerGameObject.GetComponent<Controller>());

        // Set player position from save file
        if (IsVoxelInWorld(player.spawnPosition)) // if the player position is in world
        {
            CharacterController charController = playerGameObject.GetComponent<CharacterController>();
            bool playerCharControllerActive = charController.enabled; // save active state of player character controller to reset to old value after teleport
            charController.enabled = false; // disable character controller since this prevents teleporting to saved locations
            playerGameObject.transform.position = player.spawnPosition; // teleport player to saved location
            charController.enabled = playerCharControllerActive; // reset character controller to previous state we saved earlier
        }
        else // if player pos is not in world
            playerGameObject.transform.position = spawnPosition; // spawn at world spawn point

        playerChunkCoords.Add(GetChunkCoordFromVector3(playerGameObject.transform.position));
        playerLastChunkCoords.Add(playerChunkCoords[playerCount]);

        int firstLoadDrawDistance;

        if (playerCount < 1 && playerGameObject.transform.position == spawnPosition) // for world player
            firstLoadDrawDistance = loadDistance; // SettingsStatic.LoadedSettings.drawDistance; // first load distance is just large enough to render world for world player
        else
            firstLoadDrawDistance = loadDistance; // max value is 3 to ensure older PCs can still handle the CPU Load

        if (firstLoadDrawDistance < loadDistance) // checks to ensure that firstLoadDrawDistance is at least as large as loadDistance
            firstLoadDrawDistance = loadDistance;

        //if(playerGameObject != worldPlayer)
            FirstCheckDrawDistance(GetChunkCoordFromVector3(playerGameObject.transform.position), playerCount, firstLoadDrawDistance); // used to help draw the world faster upon scene start for first player

        playerCount++;
        //Debug.Log("Player Joined");
        //Debug.Log("Current Players: " + playerCount);
    }

    private void Start()
    {
        worldLoaded = false;
        worldData = SaveSystem.LoadWorld(SettingsStatic.LoadedSettings.seed);
        WorldDataOverrides(SettingsStatic.LoadedSettings.seed);

        blocktypes[25].voxelBoundObject = LDrawImportRuntime.Instance.baseOb;
        // if classic space planet
        //blocktypes[30].voxelBoundObject = crystal1;
        // else if aqua raiders planet
        //blocktypes[30].voxelBoundObject = crystal2;
        // else // rock raiders crystals
        //blocktypes[30].voxelBoundObject = crystal3;

        if (Settings.OnlinePlay)
        {
            customNetworkManager.spawnPrefabs.Add(LDrawImportRuntime.Instance.baseOb);
            customNetworkManager.spawnPrefabs.Add(LDrawImportRuntime.Instance.vehicleOb);
        }

        LoadWorld();

        // player default spawn position is centered above first chunk
        spawnPosition = new Vector3(VoxelData.WorldSizeInVoxels / 2f + VoxelData.ChunkWidth / 2, VoxelData.ChunkHeight - 5f, VoxelData.WorldSizeInVoxels / 2f + VoxelData.ChunkWidth / 2);

        worldPlayer.transform.position = spawnPosition;

        PlayerJoined(worldPlayer);

        //if (chunksToDrawQueue.Count > 0)
        //    lock (chunksToDrawQueue)
        //    {
        //        chunksToDrawQueue.Dequeue().CreateMesh();
        //    }

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

        Settings.WorldLoaded = true;
        mainCamera = mainCameraGameObject.GetComponent<Camera>();
        mainCamera.enabled = false;
    }

    public int GetGalaxy(int seed)
    {
        int galaxy = Mathf.CeilToInt(seed / 64.0f);
        return galaxy;
    }

    public int GetSystem(int seed)
    {
        int solarSystem = Mathf.CeilToInt(seed / 8.0f);
        return solarSystem;
    }

    public int GetDistToStar(int seed)
    {
        int solarSystem = Mathf.CeilToInt(seed / 8.0f);
        int distToStar = (int)(seed - 8.0f * (solarSystem - 1));
        return distToStar;
    }

    public int GetSeedFromSpaceCoords (int galaxy, int solarSystem, int distToStar)
    {
        int seed = (int)((galaxy - 1) * 64.0f + (solarSystem - 1) * 8.0f + distToStar);
        return seed;
    }

    public void WorldDataOverrides(int worldseed)
    {
        //override worldData with planet data for specific planets in our solar system, otherwise randomize the blockIDs/colors
        int minRandBlockID = 2;
        int maxRandBlockID = 24;

        worldData.system = GetSystem(worldseed);
        worldData.distToStar = GetDistToStar(worldseed);
        worldData.galaxy = GetGalaxy(worldseed);
        int distToStar = worldData.distToStar;
        //Debug.Log("Seed:" + GetSeedFromSpaceCoords(worldData.galaxy, worldData.solarSystem, worldData.distToStar));
        //Debug.Log("Universe Coords (galaxy, system, planet)" + worldData.galaxy + "-" + worldData.solarSystem + "-" + distToStar);

        if (worldseed < 9) // 8 planets
        {
            Planet planet = planets[worldseed];

            worldData.blockIDsubsurface = planet.blockIDsubsurface;
            worldData.blockIDcore = planet.blockIDcore;
            worldData.blockIDForest = planet.blockIDForest;
            worldData.blockIDGrasslands = planet.blockIDGrasslands;
            worldData.blockIDDesert = planet.blockIDDesert;
            worldData.blockIDDeadForest = planet.blockIDDeadForest;
            worldData.blockIDHugeTree = planet.blockIDHugeTree;
            worldData.blockIDMountain = planet.blockIDMountain;
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
        if (worldseed >= 9)
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
            worldData.blockIDForest = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDGrasslands = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDDesert = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDDeadForest = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDHugeTree = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            worldData.blockIDMountain = (byte)Random.Range(minRandBlockID, maxRandBlockID);
            //int generateFlora = Random.Range(0, 1);
            //if (generateFlora == 0)
            //    worldData.isAlive = false; // controls if the world is hospitable to flora
            //else
                worldData.isAlive = true; // controls if the world is hospitable to flora
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

        biomes[0].surfaceBlock = worldData.blockIDForest;
        biomes[1].surfaceBlock = worldData.blockIDGrasslands;
        biomes[2].surfaceBlock = worldData.blockIDDesert;
        biomes[3].surfaceBlock = worldData.blockIDDeadForest;
        biomes[4].surfaceBlock = worldData.blockIDHugeTree;
        biomes[5].surfaceBlock = worldData.blockIDMountain;
    }

    public void LoadWorld()
    {
        // loadDistance must always be greater than viewDistance, the larger the multiplier, the less frequent load times
        loadDistance = Mathf.CeilToInt(SettingsStatic.LoadedSettings.drawDistance * 1.333f); //Mathf.CeilToInt(SettingsStatic.LoadedSettings.drawDistance * 1.99f); // cannot be larger than firstLoadDist (optimum value is 4, any larger yields > 30 sec exist world load time)
        LOD0threshold = 1; // Mathf.CeilToInt(SettingsStatic.LoadedSettings.drawDistance * 0.333f);

        for (int x = (VoxelData.WorldSizeInChunks / 2) - loadDistance; x < (VoxelData.WorldSizeInChunks / 2) + loadDistance; x++)
        {
            for (int z = (VoxelData.WorldSizeInChunks / 2) - loadDistance; z < (VoxelData.WorldSizeInChunks / 2) + loadDistance; z++)
                worldData.LoadChunkFromFile(new Vector2Int(x, z));
        }
    }

    private void FixedUpdate()
    {
        if (Settings.OnlinePlay)
        {
            saving = false;

            // AutoSave feature every hour
            if (tick > 50 * 60 * 60 * 24) // 1 sec = 50 ticks, 60 sec = 1 min, 60 min = 1 hr, 24 hr = 1 day (save every day for server maintenance)
            {
                tick = 0;
                saving = true;
                //Debug.Log("Saved Game: " + System.DateTime.Now);
                SaveSystem.SaveWorld(Instance.worldData);
            }
            else
                tick++;
        }
    }

    public void ActivateChunks()
    {
        chunkLoadSound.Play();

        foreach (KeyValuePair<ChunkCoord, Chunk> chunk in chunks) // activate all currently loaded chunks
        {
            chunk.Value.isActive = true;
            chunksToDrawList.Add(chunk.Value); // redraw all chunks
        }

        activateNewChunks = true; // tell world to set new chunks to be active (out of tutorial mode)
    }

    private void Update()
    {
        if (!worldLoaded) // don't continue with main loop if world has not been loaded.
            return;

        // only if more than one player and local splitcreen mode, do not undraw chunks
        if(!undrawVoxels && playerCount > 2)
        {
            previousChunksToDrawList = new List<ChunkCoord>(chunkCoordsToDrawList);
            chunkCoordsToDrawList.Clear();
        }

        copyOfChunksToDrawObjectsList = new List<ChunkCoord>(chunksToDrawObjectsList);
        chunksToDrawObjectsList.Clear();

        // create copies of the lists to use so the original lists can be modified during the update loop (was causing errors)
        _players = players;
        _playerChunkCoords = playerChunkCoords;
        _playerLastChunkCoords = playerLastChunkCoords;

        for (int i = 1; i < _players.Count; i++) // for all players (not including world player, thus start at 1)
        {
            // if the player disconnected, remove their gameobject from the dictionary and go to the next dictionary value
            if (_players[i] == null)//|| playerChunkCoords.Count > 1 && player.Key == worldPlayer)
            {
                _players.RemoveAt(i);
                _playerChunkCoords.RemoveAt(i);
                _playerLastChunkCoords.RemoveAt(i);
                //Debug.Log("Player Quit");
                continue;
            }

            // if the player is not the worldPlayer (checks for null players if the client disconnects before host). Also ensures that the chunk coords and players have same number of indices
            if (_players[i].playerGameObject != worldPlayer && _players[i].playerGameObject != null && _players.Count == _playerChunkCoords.Count)
            {
                _playerChunkCoords[i] = GetChunkCoordFromVector3(controllers[_players[i]].playerCamera.transform.position); // get the current chunkCoords for given player camera

                // Only update the chunks if the player has moved from the chunk they were previously on.
                if (!_playerChunkCoords[i].Equals(_playerLastChunkCoords[i]))
                {
                    CheckDrawDistance(_playerChunkCoords[i], i); // re-draw chunks
                    CheckObDrawDist(_playerChunkCoords[i], i); // re-draw studs
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
                    RemoveObjectsFromChunk(c); // remove voxel bound objects in previousChunksToDrawObjectsList
            }
        }
    }

    void CheckObDrawDist(ChunkCoord playerChunkCoord, int playerIndex)
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

        // if single player or network play, undraw chunks to save memory (Disabled until we can find a way to remove far away chunks selectively. Current method removes all chunks outside viewDist)
        if (undrawVoxels && playerCount < 3)
        {
            previousChunksToDrawList = new List<ChunkCoord>(chunkCoordsToDrawList);
            chunkCoordsToDrawList.Clear();
        }

        // Loop through all chunks currently within view distance of the player.
        for (int x = playerChunkCoord.x - SettingsStatic.LoadedSettings.drawDistance; x < playerChunkCoord.x + SettingsStatic.LoadedSettings.drawDistance; x++)
        {
            for (int z = playerChunkCoord.z - SettingsStatic.LoadedSettings.drawDistance; z < playerChunkCoord.z + SettingsStatic.LoadedSettings.drawDistance; z++)
            {
                ChunkCoord thisChunkCoord = new ChunkCoord(x, z);

                // If the current chunk is in the world...
                if (IsChunkInWorld(thisChunkCoord))
                {
                    // Check if its in view distance, if not, mark it to be re-drawn.
                    if (chunksToDrawArray[x, z] == null) // if the chunksToDrawArray is empty at thisChunkCoord
                        chunksToDrawArray[x, z] = new Chunk(thisChunkCoord); // adds this chunk to the array at this position
                    if(activateNewChunks)
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

    public byte GetVoxel(Vector3 globalPos) // The main algorithm used to calculate voxels for world generation. Runs whenever voxel ids need to be calculated (modified voxels are saved to a serialized file).
    {
        int yGlobalPos = Mathf.FloorToInt(globalPos.y);
        int xGlobalPos = Mathf.FloorToInt(globalPos.x);
        int zGlobalPos = Mathf.FloorToInt(globalPos.z);
        Vector2 xzCoords = new Vector2(xGlobalPos, zGlobalPos);

        /* IMMUTABLE PASS */

        // If outside world, return air.
        if (!IsVoxelInWorld(globalPos))
            return 0;

        // If bottom block of chunk, return barrier block
        if (yGlobalPos == 0)
            return 0; // Disabled to allow players to fall thru world as unbreakable barrier blocks break immersion)

        // Lava level (DO NOT MAKE VOXEL BOUND OBJECT, SIGNIFICANTLY SLOWS DOWN GAME)
        if (yGlobalPos == 1)
        {
            //if (Noise.Get2DPerlin(new Vector2(xGlobalPos, zGlobalPos), 52, 0.2f) > 0.2f) // determines if water or lava
            //    return 21; // water
            //else
                return worldData.blockIDcore; // planet core block (e.g. lava)
        }

        // If between certain height range, return clouds.
        if (yGlobalPos > VoxelData.ChunkHeight - 15 && yGlobalPos < VoxelData.ChunkHeight - 10)
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

        /* BIOME SELECTION PASS */
        //float sumOfHeights = 0f;
        //int count = 0;
        float strongestWeight = 0f;
        int strongestBiomeIndex = 0;

        foreach (int biomeIndex in worldData.biomes)
        {
            float weight = Noise.Get2DPerlin(xzCoords, biomes[biomeIndex].offset, biomes[biomeIndex].scale);

            // Keep track of which weight is strongest.
            if (weight > strongestWeight)
            {
                strongestWeight = weight;
                strongestBiomeIndex = biomeIndex;
            }

            //// Get the height of the terrain (for the current biome) and multiply it by its weight.
            //float height = biomes[i].terrainHeight * Noise.Get2DPerlin(xzCoords, 0, biomes[i].terrainScale) * weight;

            //// If the height value is greater than 0 add it to the sum of heights.
            //if (height > 0)
            //{
            //    sumOfHeights += height;
            //    count++;
            //}
        }

        // Set biome to the one with the strongest weight.
        Biome biome = biomes[strongestBiomeIndex];

        //// Get the average of the heights.
        //sumOfHeights /= count;
        //int terrainHeight = Mathf.FloorToInt(sumOfHeights + solidGroundHeight);

        // Use perlin noise function for more varied height
        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(xzCoords.x, xzCoords.y), 0, biome.terrainScale)) + solidGroundHeight;

        if (xGlobalPos == Mathf.FloorToInt(VoxelData.WorldSizeInVoxels / 2 + VoxelData.ChunkWidth / 2) && zGlobalPos == Mathf.FloorToInt(VoxelData.WorldSizeInVoxels / 2 + VoxelData.ChunkWidth / 2) && yGlobalPos == terrainHeight)
            modifications.Enqueue(Structure.GenerateMajorFlora(0, globalPos, 0, 0, 0, 0)); // make base at center of first chunk at terrain height

        /* BASIC TERRAIN PASS */

        byte voxelValue = 0;
        bool subsurfaceBlock = false;

        if (yGlobalPos > terrainHeight)
            return 0;
        else if (yGlobalPos == terrainHeight) // if surface block
        {
            if (Mathf.CeilToInt(System.DateTime.Now.Month / 3f) == 1)
                voxelValue = 5; // snow for winter season
            else
                voxelValue = biome.surfaceBlock;
        }
        else // must be subsurface block
        {
            voxelValue = worldData.blockIDsubsurface;
            subsurfaceBlock = true;
        }

        /* LODE PASS */
        if (subsurfaceBlock && yGlobalPos < terrainHeight - 2)
        {
            foreach (Lode lode in biome.lodes)
            {
                if (yGlobalPos > lode.minHeight && yGlobalPos < lode.maxHeight && yGlobalPos < terrainHeight)
                    if (Noise.Get3DPerlin(globalPos, lode.noiseOffset, lode.scale, lode.threshold))
                        voxelValue = lode.blockID;
            }
        }

        /* MAJOR FLORA PASS */
        if (worldData.isAlive && biome.placeFlora) // only place flora on worlds marked isAlive
        {
            for (int i = 0; i < biome.flora.Length; i++) // for all floras
            {
                if (yGlobalPos == terrainHeight)
                {
                    if (Noise.Get2DPerlin(xzCoords, 0, biome.flora[i].floraZoneScale) > biome.flora[i].floraZoneThreshold)
                    {
                        if (Noise.Get2DPerlin(xzCoords, 0, biome.flora[i].floraPlacementScale) > biome.flora[i].floraPlacementThreshold)
                        {
                            // add a flora structure
                            modifications.Enqueue(Structure.GenerateMajorFlora(biome.flora[i].floraIndex, globalPos, biome.flora[i].minHeight, biome.flora[i].maxHeight, biome.flora[i].minRadius, biome.flora[i].maxRadius));
                        }
                    }
                }
            }
        }

        return voxelValue;
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

                    // if studs or objects don't already exist, add them
                    if (!studDictionary.TryGetValue(globalPositionAbove, out _) && !objectDictionary.TryGetValue(globalPosition, out _))
                    {
                        // if voxel is solid, and voxel above is air, and voxel is not barrier block
                        if (blocktypes[chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id].isSolid && chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y + 1, z].id == 0 && chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id != 1)
                        {
                            // if voxel matches Perlin noise pattern, add studs object
                            if (blocktypes[chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id].studs != null && Noise.Get2DPerlin(new Vector2(x, z), 321, 10f) < 0.1f)
                                studDictionary.Add(globalPositionAbove, Instantiate(blocktypes[chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id].studs, globalPositionAbove, Quaternion.identity));
                        }

                        byte blockID = chunksToDrawArray[chunkCoord.x, chunkCoord.z].chunkData.map[x, y, z].id;

                        // if voxel has an object defined, then add object to voxel
                        if (blocktypes[blockID].voxelBoundObject != null)
                        {
                            Vector3 VBOPosition = globalPosition;
                            Quaternion VBOorientation = Quaternion.identity;
                            if (blockID == 25 || blockID == 26)
                            {
                                VBOPosition = new Vector3(globalPosition.x + 0.5f, globalPosition.y, globalPosition.z + 0.5f); // make center of the VBO center of the voxel (voxel origin is corner)
                                VBOorientation.eulerAngles = new Vector3(180, 0, 0); // if VBOImport then flip right side up
                            }
                            GameObject VBO = Instantiate(blocktypes[blockID].voxelBoundObject, VBOPosition, VBOorientation);
                            if(blockID == 25 || blockID == 26)
                            {
                                if (blockID == 25) // if base, cache object as baseOb
                                    baseOb = VBO;
                                AddBoxColliderMaterialToChildren(VBO);
                                if (Settings.OnlinePlay)
                                    VBO.AddComponent<NetworkIdentity>();
                                VBO.GetComponent<BoxCollider>().enabled = true; // VBO Box collider used to add placeholder voxels for world procGen
                            }
                            objectDictionary.Add(globalPosition, VBO);
                        }
                    }
                    else
                    {
                        //Debug.Log(globalPositionAbove + " already exists");
                    }
                }
            }
        }
    }

    public void AddBoxColliderMaterialToChildren(GameObject _go)
    {
        BoxCollider[] children = _go.GetComponentsInChildren<BoxCollider>();
        for(int i = 0; i < children.Length; i++)
        {
            if(children[i].gameObject.layer == 10)
                children[i].gameObject.GetComponent<BoxCollider>().material = physicMaterial;
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

                    if (objectDictionary.TryGetValue(globalPosition, out _) && blockID != 25 && blockID != 26) // voxelBoundObjects but not procGen.ldr
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
        VoxelState voxel = worldData.GetVoxel(pos);

        if (voxel == null)
            return false;

        if (voxel.id == 26)
            return true; // VBO placeholder to allow player to jump off of VBO

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

    public VoxelMod(Vector3 _position, byte _id)
    {
        position = _position;
        id = _id;
    }
}