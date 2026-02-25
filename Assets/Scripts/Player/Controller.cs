
using LDraw;
using Mirror;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public class Controller : NetworkBehaviour
{
    // NOTE: this class assumes world has already been activated

    [SyncVar(hook = nameof(SetName))] public string playerName = "PlayerName";
    [SyncVar(hook = nameof(SetCharIdle))] public string playerCharIdle;
    [SyncVar(hook = nameof(SetCharRun))] public string playerCharRun;
    [SyncVar(hook = nameof(SetProjectile))] public string playerProjectile;
    [SyncVar(hook = nameof(SetCurrentBrickType))] public int currentBrickType;
    [SyncVar(hook = nameof(SetCurrentBrickIndex))] public int currentBrickIndex;
    [SyncVar(hook = nameof(SetCurrentBrickName))] public string placedBrickName;
    [SyncVar(hook = nameof(SetCurrentBrickMaterialIndex))] public int currentBrickMaterialIndex;
    [SyncVar(hook = nameof(SetCurrentBrickRotation))] public int currentBrickRotation;

    // Server Values (server generates these values upon start, all clients get these values from server upon connecting)
    [SyncVar(hook = nameof(SetTime))] private float timeOfDayServer;
    [SyncVar] private string versionServer;
    readonly private SyncList<string> playerNamesServer = new SyncList<string>();

    // These server values cannot be set in controller since world is activated before controller, merely included here to check states match
    [SyncVar(hook = nameof(SetPlanetNumberServer))] private int planetNumberServer;
    [SyncVar(hook = nameof(SetSeedServer))] private int seedServer;
    [SyncVar(hook = nameof(SetBaseServer))] private string baseServer;
    [SyncVar(hook = nameof(SaveChunksString))] private string chunksServer;

    [Header("Debug States")]
    [SerializeField] float collisionDamage;
    public bool isGrounded;
    [SyncVar(hook = nameof(SetIsMoving))] public bool isMoving = false;
    public bool isSprinting;
    public bool dropButtonPressed;
    public bool options = false;
    public bool setBrickType = false;
    public bool setBrickIndex = false;
    public bool rotateBrick = false;
    public bool holdingGrab = false;
    public bool holdingBuild = false;
    public bool heldObjectIsBrick = false;
    public bool movingPlacedBrickUseStoredValues = false;
    public byte blockID;
    public float checkIncrement = 0.1f;
    public float sphereCastRadius;
    public float grabDist = 4f; // defines how far player can reach to use/use voxels (updated later to match player created models)
    public float tpsDist;
    public float maxFocusDistance = 2f;
    public float focusDistanceIncrement = 0.03f;
    public float baseAnimRate = 2; // health script overrides this
    public float animRate = 2; // health script overrides this
    public int camMode;
    public bool setCamMode = false;
    public int inventoryUIMode = 0;

    public float lookVelocity = 0.1f;
    public float lookSpeed = 0.1f;

    public bool invertY = false;

    public byte orientation;

    [Header("GameObject References")]
    public Player player;
    public GameObject charModelDefault;
    public GameObject charModelOrigin;
    public GameObject gameMenu;
    public DragAndDropHandler dragAndDropHandler;
    public GameObject nametag;
    public GameObject backgroundMask;
    public AudioSource brickPickUp;
    public AudioSource brickPlaceDown;
    public AudioSource eat;
    public AudioSource crystal;
    public AudioSource mushroom;
    public AudioSource shootBricks;
    public GameObject playerCamera;
    public GameObject removePosPrefab;
    public GameObject shootPosPrefab;
    public GameObject placePosPrefab;
    public GameObject holdPosPrefab;
    public Toolbar toolbar;
    public GameObject playerHUD;
    public GameObject CinematicBars;
    public GameObject creativeInventoryWindow;
    public GameObject cursorSlot;
    public GameObject reticle;
    public GameObject projectile;
    public GameObject sceneObjectPrefab;
    public GameObject charObIdle;
    public GameObject charObRun;
    public World world;
    public Material[] brickMaterials;

    public bool canMine = false;
    public int miningCounter = 0;
    public int toolID = 0;

    private int[] toolMiningSpeeds = new int[] // defines the mining speed of a given toolID
    {
        45, // tool ID 0 = punch
        30, // tool ID 1 = wood
        20, // tool ID 2 = stone
        10, // tool ID 3 = gold
        0, // tool ID 4 = crystal
    };

    public int[][] allowableDropMatrix = new int[][] // 2d array (array of arrays) that defines which blocks can be mined with various tool ids
    {
        // defines blocks that the given tool can create a drop for
        new int[] {            6,       9,         13, 14,         17, 18,     20 }, // tool ID 0 = punch (can only mine wood, water, grass, mushroom, flower)
        new int[] {   3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 14, 15,     17, 18,     20 }, // tool ID 1 = wood (cannot mine black, gold, or crystal)
        new int[] {   3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 14, 15,     17, 18, 19, 20 }, // tool ID 2 = stone (cannot mine black or crystal)
        new int[] {   3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 17, 18, 19, 20 }, // tool ID 3 = gold (cannot mine black)
        new int[] {2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 14, 15, 16, 17, 18, 19, 20 }, // tool ID 4 = crystal
    };

    public int[] ldrawHexValues = new int[]
        {
            43,
            43,
            0,
            7,
            15,
            4,
            6,
            14,
            2,
            1,
            5,
            3
        };

    [HideInInspector] public GameObject placedBrick;

    private Dictionary<Vector3, GameObject> voxelBoundObjects = new Dictionary<Vector3, GameObject>();

    private Vector3 velocityPlayer;
    private Transform removePos;
    private Transform shootPos;
    private Transform placePos;
    private Transform holdPos;
    private GameObject grabbedPrefab;

    //Components
    private GameManagerScript gameManager;
    private LDrawConfigRuntime _ldrawConfigRuntime;
    private GameObject playerCameraOrigin;
    private LookAtConstraint lookAtConstraint;
    private CapsuleCollider cc;
    private Rigidbody rb;
    private BoxCollider bc;
    private VoxelCollider voxelCollider;
    private Animator animator;
    private PlayerInput playerInput;
    public InputHandler inputHandler;
    private Health health;
    private Mining mining;
    private CanvasGroup backgroundMaskCanvasGroup;
    private GameMenu gameMenuComponent;
    private BoxCollider playerCameraBoxCollider;
    private VoxelCollider playerCameraVoxelCollider;
    public PPFXSetValues worldPPFXSetValues;
    private CharacterController charController;
    private PhysicsMaterial physicMaterial;
    private CustomNetworkManager customNetworkManager;
    private Lighting lighting;
    private GameObject undefinedPrefabToSpawn;
    private RaycastHit raycastHit;
    private Rigidbody heldObRb;

    //Initializers & Constants
    private int blockIDprocGen = 1; // leftover, was 11, now set as barrier
    private int blockIDbase = 12;
    private int blockIDcrystal = 16;
    private int blockIDmushroom = 18;
    private float colliderHeight;
    private float colliderRadius;
    private Vector3 colliderCenter;
    private Vector3 rayCastStart;
    private float rotationY = 0f;
    private float rotationX = 0f;
    private float maxLookVelocity = 5f;
    private float maxCamAngle = 90f;
    private float minCamAngle = -90f;
    private bool daytime = true;
    private float nextTimeToAnim = 0;
    private List<Material> cachedMaterials = new List<Material>();
    private string[][] ldrawPartsTypes;
    private string[] currentLDrawPartsListStringArray = new string[] { };
    private bool obfuscateBRXFILE = true; // set to true to prevent cheaters from importing a base using ldraw file format

    // THE ORDER OF EVENTS IS CRITICAL FOR MULTIPLAYER!!!
    // Order of network events: https://docs.unity3d.com/Manual/NetworkBehaviourCallbacks.html
    // Order of SyncVars: https://mirror-networking.gitbook.io/docs/guides/synchronization/syncvars
    // The state of SyncVars is applied to game objects on clients before OnStartClient() is called, so the state of the object is always up - to - date inside OnStartClient().

    void Awake()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManagerScript>();
        _ldrawConfigRuntime = gameManager.LDrawImporterRuntime.GetComponent<LDrawImportRuntime>().ldrawConfigRuntime;
        world = gameManager.worldOb.GetComponent<World>();
        lighting = gameManager.globalLighting.GetComponent<Lighting>();
        customNetworkManager = gameManager.PlayerManagerNetwork.GetComponent<CustomNetworkManager>();
        NamePlayer(world);

        if (Settings.OnlinePlay && isLocalPlayer)
        {
            RequestSaveWorld(); // When client joins, requests that host saves the game

            // For some reason, can't get this to run on clients... (supposed to make server resend chunkStringSyncVar)
            CmdSetServerChunkStringSyncVar(); // When client joins, requests that host sends latest saved chunks as string (triggers SyncVar update which occurs before OnStartClient())
            // after client sends chunksServer string SyncVar, the syncVars do not update as required to have clients then save the chunks to memory...
        }

        if (!Settings.WebGL && !Settings.OnlinePlay)
            world.baseOb = LDrawImportRuntime.Instance.baseOb;

        voxelCollider = GetComponent<VoxelCollider>();
        voxelCollider.world = world;
        physicMaterial = world.physicMaterial;
        worldPPFXSetValues = world.GetComponent<PPFXSetValues>();

        if(!Settings.WebGL)
            projectile = LDrawImportRuntime.Instance.projectileOb;
        cc = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        inputHandler = GetComponent<InputHandler>();
        health = GetComponent<Health>();
        mining = GetComponent<Mining>();
        
        backgroundMaskCanvasGroup = backgroundMask.GetComponent<CanvasGroup>();
        gameMenuComponent = gameMenu.GetComponent<GameMenu>();
        playerCameraOrigin = playerCamera.transform.parent.gameObject;
        lookAtConstraint = playerCamera.GetComponent<LookAtConstraint>();
        playerCameraBoxCollider = playerCamera.GetComponent<BoxCollider>();
        playerCameraVoxelCollider = playerCamera.GetComponent<VoxelCollider>();
        charController = GetComponent<CharacterController>();

        health.isAlive = true;

        removePos = Instantiate(removePosPrefab).transform;
        shootPos = Instantiate(shootPosPrefab).transform;
        placePos = Instantiate(placePosPrefab).transform;
        holdPos = holdPosPrefab.transform;

        if (SettingsStatic.LoadedSettings.developerMode) // set values if developer mode or not
        {
            toolID = 4; // highest tier tool for developer/creative mode
        }
        else
        {
            toolID = 0; // otherwise initialize to 0, need to set based off toolbar inventory later
        }

        // load brick palettes
        if (!Settings.WebGL)
        {
            string[] brickTypeFilenames = LoadBrickTypeFilenames();
            ldrawPartsTypes = new string[brickTypeFilenames.Length][];
            for (int i = 0; i < brickTypeFilenames.Length; i++)
                ldrawPartsTypes[i] = LoadBrickNumbers(brickTypeFilenames[i]);
            currentLDrawPartsListStringArray = ldrawPartsTypes[currentBrickType];

            // // Debug to show the loaded brick names for a given brick type
            // int brickType = 1;
            // for(int i = 0; i < ldrawPartsTypes[brickType].Length; i++)
            //     UnityEngine.Debug.Log(ldrawPartsTypes[brickType][i]);
        }
        
        // set to zero every time the game starts
        if(Settings.WebGL)
        {
            currentBrickType = 6;
            currentBrickIndex = 0;
            currentBrickRotation = 1;
        }
        else
        {
            currentBrickType = SettingsStatic.LoadedSettings.currentBrickType;
            currentBrickIndex = SettingsStatic.LoadedSettings.currentBrickIndex;
            currentBrickRotation = SettingsStatic.LoadedSettings.currentBrickRotation;
        }

        CinematicBars.SetActive(false);
    }

    void NamePlayer(World world)
    {
        if (world.worldPlayer != null && gameObject != world.worldPlayer) // Need to work out how networked players with same name get instance added to name
        {
            // set this object's name from saved settings so it can be modified by the world script when player joins
            if(Settings.WebGL)
                playerName = "Player";
            else
                playerName = SettingsStatic.LoadedSettings.playerName;

            player = new Player(gameObject, playerName); // create a new player, try to load player stats from save file
        }
    }

    private void Start()
    {
        world.JoinPlayer(gameObject); // must NamePlayer and initialize world before this can be run

        InputComponents();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        options = false;
        inventoryUIMode = 0;

        if (!Settings.OnlinePlay)
        {
            // if (Settings.WebGL)
            //     charObIdle = charModelDefault;
            // else
                charObIdle = Instantiate(LDrawImportRuntime.Instance.charObIdle); // Import character model idle pose
            charObIdle.SetActive(true);

            charObIdle.transform.parent = charModelOrigin.transform;
            bc = charModelOrigin.transform.GetChild(0).GetComponent<BoxCollider>();
            charObIdle.transform.localPosition = new Vector3(0, 0, 0);
            charObIdle.transform.localEulerAngles = new Vector3(0, 180, 180);

            // if (Settings.WebGL)
            //     charObRun = charModelDefault;
            // else
                charObRun = Instantiate(LDrawImportRuntime.Instance.charObRun); // Import character model run pose
            charObRun.SetActive(false);

            charObRun.transform.parent = charModelOrigin.transform;
            charObRun.transform.localPosition = new Vector3(0, 0, 0);
            charObRun.transform.localEulerAngles = new Vector3(0, 180, 180);

            SetPlayerColliderSettings();
            SetName(playerName, playerName);
            nametag.SetActive(false); // disable nametag for singleplayer/splitscreen play

            world.gameObject.SetActive(true);

            if (Settings.WebGL)
            {
                camMode = 1;
            }
            else
            {
                LoadPlacedBricks();
                camMode = SettingsStatic.LoadedSettings.camMode;
            }
        }
    }

    private string[] LoadBrickTypeFilenames()
    {
        string[] originalStringArray;
        string[] returnStringArray;

        string path = Application.streamingAssetsPath + "/ldraw/bricktypes/";
        if (!Directory.Exists(path))
            ErrorMessage.Show("Error: Could not find Directory: " + path);

        originalStringArray = Directory.GetFiles(path);

        // ignore files _DS STORE
        // for(int i = 0; i < originalStringArray.Length; i++)
        // {
        //     originalStringArray[i] = Path.GetFileName(originalStringArray[i]);
        //     if (!originalStringArray[i].Contains(".txt"))
        //     {
                
        //     }
        // }

        // count the number of instances that need to be ignored
        int count = 0;
        for(int i = 0; i < originalStringArray.Length; i++)
        {
            originalStringArray[i] = Path.GetFileName(originalStringArray[i]);
            if (!originalStringArray[i].Contains("meta") && !originalStringArray[i].Contains("DS_Store"))
                count++;
        }
        returnStringArray = new string[count]; // set the correct size of the final string array


        // build the final string array
        count = 0;
        for (int i = 0; i < originalStringArray.Length; i++)
        {
            originalStringArray[i] = Path.GetFileName(originalStringArray[i]);
            if (!originalStringArray[i].Contains("meta") && !originalStringArray[i].Contains("DS_Store"))
            {
                returnStringArray[count] = originalStringArray[i];
                count++;
            }
            //Debug.Log(returnStringArray[i]);
        }
        //Debug.Log(returnStringArray[0]);
        return returnStringArray;
    }

    private string[] LoadBrickNumbers(string name)
    {
        string path = Application.streamingAssetsPath + "/ldraw/bricktypes/" + name;
        if (!File.Exists(path))
            ErrorMessage.Show("Error: Could not find " + path);

        string ldrawPartList = File.ReadAllText(path);
        currentLDrawPartsListStringArray = ldrawPartList.Split("\n");

        // remove empty entries
        var temp = new List<string>();
        foreach (var s in currentLDrawPartsListStringArray)
        {
            if (!string.IsNullOrEmpty(s))
                temp.Add(s);
        }
        currentLDrawPartsListStringArray = temp.ToArray();

        return currentLDrawPartsListStringArray;
    }

    [Command]
    private void CmdLoadPlacedBricks()
    {
        LoadPlacedBricks();
    }

    private void LoadPlacedBricks()
    {
        string path = Settings.AppSaveDataPath + "/saves/" + SettingsStatic.LoadedSettings.planetSeed + "-" + SettingsStatic.LoadedSettings.worldCoord + "/placedBricks.brx";
        if (!File.Exists(path))
            return;

        // Import placed bricks
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(path, FileMode.Open);
        string base64 = formatter.Deserialize(stream) as string;
        stream.Close();

        if (obfuscateBRXFILE)
        {
            // de-obfuscate
            //https://stackoverflow.com/questions/20010374/obfuscating-randomizing-a-string
            var data = System.Convert.FromBase64String(base64);
            string decodedString = System.Text.Encoding.UTF8.GetString(data);
            baseServer = decodedString;
        }
        else
            baseServer = base64;

        LoadPlacedBricksFromString(baseServer);
    }

    public void LoadPlacedBricksFromString(string cmdstr)
    {
        if (cmdstr == null || cmdstr.Length == 0)
            return;

        //separate string into separate strings based on new lines
        string[] cmdstrings = cmdstr.Split("*");
        if (cmdstrings.Length == 0)
            return;

        for (int j = 0; j < cmdstrings.Length - 1; j++) // last entry is blank due to new line delimiter so we stop one away from last string array value
        {
            if (cmdstrings[j].Length == 0)
                return;
            string[] strs = cmdstrings[j].Split(",");

            int color = int.Parse(strs[0]);
            float posx = float.Parse(strs[1]);
            float posy = float.Parse(strs[2]);
            float posz = float.Parse(strs[3]);
            Vector3 pos = new Vector3(posx, posy, posz);

            // SIMPLIFIED FORMAT
            string a = "1.000000";
            string b = "0.000000";
            string c = "0.000000";
            string d = "0.000000";
            string e = "1.000000";
            string f = "0.000000";
            string g = "0.000000";
            string h = "0.000000";
            string i = "1.000000";
            float rotx = float.Parse(strs[4]);
            float roty = float.Parse(strs[5]);
            float rotz = float.Parse(strs[6]);
            float rotw = float.Parse(strs[7]);
            string partname = strs[8];
            Quaternion rot = new Quaternion(rotx, roty, rotz, rotw);

            string commandstring = "1 " + color + " 0.000000 0.000000 0.000000" + " " + a + " " + b + " " + c + " " + d + " " + e + " " + f + " " + g + " " + h + " " + i + " " + partname;

            if (Settings.OnlinePlay && hasAuthority)
                CmdPlaceBrick(true, partname, commandstring, color, pos, rot);
            else
                PlaceBrick(true, partname, commandstring, color, pos, rot);
        }
    }

    void InputComponents()
    {
        if (gameObject.GetComponent<PlayerInput>() != null) { playerInput = gameObject.GetComponent<PlayerInput>(); }
        if (gameObject.GetComponent<InputHandler>() != null) { inputHandler = gameObject.GetComponent<InputHandler>(); }
        if (!Settings.OnlinePlay)
        {
            playerInput.enabled = true;
            inputHandler.enabled = true;
        }
        else
        {
            if (isLocalPlayer)
            {
                playerInput.enabled = false; // for online play, tricks playerInput component to accepting the online player as newest joined player even when more than one player
                playerInput.enabled = true;
                inputHandler.enabled = true;
            }
            else
            {
                playerInput.enabled = false;
                inputHandler.enabled = false;
            }
        }
    }

    public override void OnStartServer() // Only called on Server and Host
    {
        base.OnStartServer();

        // SET SERVER VALUES FROM HOST CLIENT
        planetNumberServer = SettingsStatic.LoadedSettings.planetSeed;
        seedServer = SettingsStatic.LoadedSettings.worldCoord;

        LoadPlacedBricks();

        versionServer = Application.version;

        //SetServerChunkStringSyncVar(); // Server sends initially loaded chunks as chunkStringSyncVar to clients (DISABLED, send chunks over internet manually, figure out how to send data not as strings)

        customNetworkManager.InitWorld();
    }
    
    public override void OnStartClient() // Only called on Client and Host
    {
        base.OnStartClient();

        // Check if client version matches versionServer SyncVar (SyncVars are updated before OnStartClient()
        if (isClientOnly)
        {
            if (Application.version != versionServer) // if client version does not match server version, show error.
                ErrorMessage.Show("Error: Version mismatch. " + Application.version + " != " + versionServer + ". Client game version must match host. Disconnecting Client.");

            foreach (string name in playerNamesServer) // check new client playername against existing server player names to ensure it is unique for savegame purposes
            {
                if (SettingsStatic.LoadedSettings.playerName == name)
                    ErrorMessage.Show("Error: Non-Unique Player Name. Client name already exists on server. Player names must be unique. Disconnecting Client.");
            }
        }

        SetName(playerName, playerName); // called on both clients and host

        if (isClientOnly)
            customNetworkManager.InitWorld(); // activate world only after getting syncVar latest values from server

        LoadPlacedBricksFromString(baseServer);
    }

    void SetPlayerColliderSettings()
    {
        float scale;

        if(Settings.WebGL)
            scale = 1f;
        else
            scale = LDrawImportRuntime.Instance.scale;

        // position/size capsule collider procedurally based on imported char model size
        colliderHeight = bc.size.y * scale;
        colliderRadius = Mathf.Sqrt(Mathf.Pow(bc.size.x * scale, 2) + Mathf.Pow(bc.size.z * scale, 2)) * 0.25f;
        colliderCenter = new Vector3(0, -bc.center.y * scale, 0);
        cc.center = colliderCenter;
        cc.height = colliderHeight;
        cc.radius = colliderRadius;
        charController.height = colliderHeight;
        charController.radius = colliderRadius;
        charController.center = colliderCenter;

        // position camera procedurally based on imported char model size
        playerCameraOrigin.transform.localPosition = transform.up * colliderHeight * 0.8f;
        playerCamera.GetComponent<Camera>().nearClipPlane = 0.01f;

        // position nametag procedurally based on imported char model size
        nametag.transform.localPosition = new Vector3(0, colliderCenter.y + colliderHeight * 0.55f, 0);

        // set reach and mining range procedurally based on imported char model size
        grabDist = cc.radius * 10f;
        tpsDist = -cc.radius * 16; // Controls Cam distance from Model in third person mode, increased to create sense of mini scale
    }

    public void SetPlanetNumberServer(int oldValue, int newValue)
    {
        SettingsStatic.LoadedSettings.planetSeed = newValue;
        customNetworkManager.worldOb.GetComponent<World>().planetNumber = newValue;
        customNetworkManager.worldOb.GetComponent<World>().worldData.planetSeed = newValue;
    }

    public void SetSeedServer(int oldValue, int newValue)
    {
        SettingsStatic.LoadedSettings.worldCoord = newValue;
        customNetworkManager.worldOb.GetComponent<World>().seed = newValue;
        customNetworkManager.worldOb.GetComponent<World>().worldData.worldCoord = newValue;
    }

    [Client]
    public void SetBaseServer(string oldValue, string newValue)
    {
        customNetworkManager.worldOb.GetComponent<World>().baseObString = newValue;
    }

    [Command]
    public void CmdSetServerChunkStringSyncVar()
    {
        SetServerChunkStringSyncVar();
        Debug.Log("CmdSetServerChunkStringSyncVar"); // this command doesn't fire from client for some reason...
    }

    [Server]
    public void SetServerChunkStringSyncVar()
    {
        // encode the list of chunkStrings into a single string that is auto-serialized by mirror
        List<string> chunksList = SaveSystem.LoadChunkListFromFile(planetNumberServer, seedServer);
        string chunksServerCombinedString = string.Empty;
        for (int i = 0; i < chunksList.Count; i++)
        {
            chunksServerCombinedString += chunksList[i];
            chunksServerCombinedString += ';'; // has to be a single char to be able to split later on client side
        }
        chunksServer = chunksServerCombinedString;
    }

    public void SaveChunksString(string oldValue, string newValue) // eventually figure out how to send data over network using long list of bytes instead of a string
    {
        string[] serverChunks = newValue.Split(';'); // splits individual chunk strings using ';' char delimiter
        World world = customNetworkManager.worldOb.GetComponent<World>();

        //tell world to draw chunks from server
        for (int i = 0; i < serverChunks.Length - 1; i++) // serverChunks.Length - 1 since last item is always empty after ';' char
        {
            ChunkData chunkData = new ChunkData();
            chunkData = chunkData.DecodeString(serverChunks[i]);
            world.worldData.modifiedChunks.Add(chunkData); // add chunk to list of chunks to be saved
        }
        world.worldData.planetSeed = planetNumberServer;
        world.worldData.worldCoord = seedServer;
        SaveWorld(world.worldData); // save chunks to disk before loading world
    }

    public void SetName(string oldValue, string newValue)
    {
        // update the player name using the SyncVar pushed from the server to clients
        if (playerName == null)
        {
            Debug.Log("No string found for playerName");
            return;
        }

        playerName = newValue;
        nametag.GetComponent<TextMesh>().text = newValue;
    }

    public void SetCharIdle(string oldValue, string newValue)
    {
        charObIdle = LDrawImportRuntime.Instance.ImportLDrawOnline(playerName + "charIdle", newValue, charModelOrigin.transform.position, false);
        charObIdle.SetActive(true);
        charObIdle.transform.parent = charModelOrigin.transform;
        bc = charModelOrigin.transform.GetChild(0).GetComponent<BoxCollider>();
        charObIdle.transform.localPosition = Vector3.zero;
        charObIdle.transform.localEulerAngles = new Vector3(0, 180, 180);
        SetPlayerColliderSettings();
    }

    public void SetCharRun(string oldValue, string newValue)
    {
        charObRun = LDrawImportRuntime.Instance.ImportLDrawOnline(playerName + "charRun", newValue, charModelOrigin.transform.position, false);
        charObRun.SetActive(false);
        charObRun.transform.parent = charModelOrigin.transform;
        charObRun.transform.localPosition = Vector3.zero;
        charObRun.transform.localEulerAngles = new Vector3(0, 180, 180);
        SetPlayerColliderSettings();
    }

    public void SetProjectile(string oldValue, string newValue)
    {
        projectile = LDrawImportRuntime.Instance.ImportLDrawOnline(playerName + "projectile", newValue, projectile.transform.position, false);
    }

    public void SetTimeOfDayServer()
    {
        timeOfDayServer = lighting.timeOfDay; // update serverTime from lighting component
    }

    public void SetTime(float oldValue, float newValue)
    {
        lighting.timeOfDay = newValue;
    }

    public void SetIsMoving(bool oldValue, bool newValue)
    {
        isMoving = newValue;
    }

    public void SetCurrentBrickType(int oldValue, int newValue)
    {
        currentBrickType = newValue;
    }

    public void SetCurrentBrickIndex(int oldValue, int newValue)
    {
        currentBrickIndex = newValue;
    }

    public void SetCurrentBrickName(string oldValue, string newValue)
    {
        placedBrickName = newValue;
    }

    public void SetCurrentBrickRotation(int oldValue, int newValue)
    {
        currentBrickRotation = newValue;
    }

    public void SetCurrentBrickMaterialIndex(int oldValue, int newValue)
    {
        currentBrickMaterialIndex = newValue;
    }

    private void OnDestroy()
    {
        foreach (Material mat in cachedMaterials)
            Destroy(mat); // Unity makes a clone of the Material every time GetComponent().material is used. Cache it and destroy it in OnDestroy to prevent a memory leak.
    }

    private void OnCollisionEnter(Collision collision)
    {
        // hazards hurt player
        if (collision.gameObject.tag == "Hazard")
            health.EditSelfHealth(-1);

        // if (collision.gameObject.tag == "voxelRb" && collision.gameObject.GetComponent<SceneObject>() != null) // method used to pick up dropped items
        // {
        //     PutAwayBrick((byte)collision.gameObject.GetComponent<SceneObject>().typeVoxel); // put item into player toolbar
        //     Destroy(collision.gameObject);
        // }
    }

    private void Update()
    {
        if (!Settings.WorldLoaded) return; // don't do anything until world is loaded

        if (options)
            gameMenuComponent.OnOptions();
        else if (!options)
            gameMenuComponent.ReturnToGame();
        
        if(inventoryUIMode != 0)
            dragAndDropHandler.OnInventory();
        else if (inventoryUIMode == 0)
            dragAndDropHandler.ReturnToGame();

        if (holdingBuild || holdingGrab)
        {
            if (setBrickType)
            {
                if (inputHandler.next)
                    IncrementBrickType();
                else if (inputHandler.previous)
                    DecrementBrickType();
            }
            if (setBrickIndex)
            {
                if (inputHandler.navUp)
                    IncrementBrickIndex();
                else if (inputHandler.navDown)
                    DecrementBrickIndex();
            }
            if (rotateBrick)
            {
                if (inputHandler.navLeft)
                    RotateBrickLeft();
                else if (inputHandler.navRight)
                    RotateBrickRight();
            }
        }
        else
        {
            setBrickType = false;
            setBrickIndex = false;
            rotateBrick = false;
        }

        if (setCamMode)
            SetCamMode();

        Vector3 XZDirection = transform.forward;
        XZDirection.y = 0;
        if (Vector3.Angle(XZDirection, Vector3.forward) <= 45)
            orientation = 0; // player is facing forwards.
        else if (Vector3.Angle(XZDirection, Vector3.right) <= 45)
            orientation = 5;
        else if (Vector3.Angle(XZDirection, Vector3.back) <= 45)
            orientation = 1;
        else
            orientation = 4;
    }

    void FixedUpdate()
    {
        if (!Settings.WorldLoaded) return; // don't do anything until world is loaded

        if (Settings.OnlinePlay)
            SetTimeOfDayServer();

        if(dropButtonPressed && !inputHandler.previous)
            dropButtonPressed = false;

        //disable virtual camera and exit from FixedUpdate if this is not the local player
        if (Settings.OnlinePlay && !isLocalPlayer)
        {
            Animate();
            playerCamera.SetActive(false);
            return;
        }

        daytime = lighting.daytime;

        // player normally uses voxel collision in voxelCollider to check grounded, unless standing on imported ldraw parts which have physics collisions, then mark player as grounded
        isGrounded = CheckGroundedCollider();

        if (!options) // Prevent moving/interacting with world during UI Menus
        {
            if(camMode != 3)
            {
                // IF DROP ITEM
                if(!holdingBuild && !holdingGrab && inputHandler.previous)
                    DropItemInSlot();
            }

            switch (camMode)
            {
                case 1: // FIRST PERSON CAMERA
                    {
                        if(inventoryUIMode == 0) // only do if not in inventory
                        {
                            rayCastStart = playerCamera.transform.position + playerCamera.transform.forward * colliderRadius * 4;

                            if (charObIdle != null && charObIdle.activeSelf)
                                charObIdle.SetActive(false);
                            if (charObRun != null && charObRun.activeSelf)
                                charObRun.SetActive(false);

                            CheckWorldInteract();
                        }

                        lookAtConstraint.constraintActive = false;
                        MovePlayer(); // MUST BE IN FIXED UPDATE (Causes lag if limited by update framerate)
                        break;
                    }
                case 2: // THIRD PERSON CAMERA
                    {
                        if(inventoryUIMode == 0)
                        {
                            rayCastStart = transform.position + transform.up * colliderHeight * 0.75f + transform.forward * colliderRadius * 4;

                            if (!Settings.WebGL && charObIdle != null && !charObIdle.activeSelf && SettingsStatic.LoadedSettings.developerMode)
                            {
                                charObIdle.SetActive(true);
                                charObRun.SetActive(false);
                            }

                            if (world.playerCount < 2)
                                SetDOF();
                            SetTPSDist();

                            CheckWorldInteract();
                        }

                        lookAtConstraint.constraintActive = true;
                        MovePlayer();

                        if (!Settings.WebGL && !SettingsStatic.LoadedSettings.developerMode && health.hp < 50 && inventoryUIMode == 0) // only animate characters with less than 50 pieces due to rendering performance issues
                            Animate();
                        else
                        {
                            charObIdle.SetActive(true);
                            charObRun.SetActive(false);
                        }
                        break;
                    }
                case 3: // PHOTO MODE
                    {
                        if(inventoryUIMode == 0)
                        {
                            rayCastStart = playerCamera.transform.position + playerCamera.transform.forward * colliderRadius * 4;

                            if (charObIdle != null && !charObIdle.activeSelf)
                                charObIdle.SetActive(true);
                            if (charObRun != null && charObRun.activeSelf)
                                charObRun.SetActive(false);

                            if (world.playerCount < 2)
                                SetDOF();
                        }

                        lookAtConstraint.constraintActive = false;
                        MoveCamera(); // MUST BE IN FIXED UPDATE (Causes lag if limited by update framerate)
                        break;
                    }
            }
        }
    }

    void CheckActiveHotbarSlot()
    {
        if (toolbar.slots[toolbar.slotIndex].HasItem)
            blockID = toolbar.slots[toolbar.slotIndex].itemSlot.stack.id;

        // insert code for setting tool based on certain block (item id)
    }

    void CheckWorldInteract()
    {
        CheckActiveHotbarSlot();

        if (inputHandler.mine && removePos.gameObject.activeSelf)
            miningCounter++;
        else
            miningCounter = 0;


        // NORMAL CONTROLS IF PICKAXE IN HAND?
        // NORMAL MINING
        if (!holdingGrab && !holdingBuild && inputHandler.mine)
            PressedMine();
            
        // NORMAL VOXEL BUILDING
        if (!holdingGrab && !holdingBuild && inputHandler.use)
            PressedPlaceBlock();

        // // GRAB CONTROLS
        // // IF PRESSED GRAB
        // if (!holdingGrab && inputHandler.use)
        //    PressedGrab();

        // // IF HOLDING GRAB
        // if (holdingGrab && inputHandler.use)
        //    HoldingGrab();

        // // IF RELEASED GRAB
        // if (holdingGrab && !inputHandler.use)
        //    ReleasedGrab();

        if(!Settings.WebGL) // DISABLED FOR WEBGL since wasn't working, also LDraw Importer is not enabled due to inability to reference ldraw files
        {
            // // BUILD CONTROLS
            // // IF PRESSED GRAB WHILE HOLDING BUILD
            // if (holdingGrab && inputHandler.mine)
            //    PressedBuildWhileGrab(); // CONVERT BRICK BACK INTO VOXEL

            // // IF PRESSED BUILD (WHILE LOOKING IN AIR WITH VOXEL IN INVENTORY)
            // if (!holdingBuild && inputHandler.mine)
            //    PressedBuild(); // CONVERT INVENTORY VOXEL INTO BRICK

            // // IF HOLDING BUILD
            // if (holdingBuild && inputHandler.mine)
            //    HoldingBuild();

            // // IF RELEASED BUILD
            // if (holdingBuild && !inputHandler.mine)
            //    ReleasedBuild();
        }

        positionCursorBlocks();
    }

    void SetTPSDist()
    {
        //commented out since Third Person Cam is now default, and want to use scroll wheel for inventory
        //if (inputHandler.scrollWheel.y > 0)
        //    tpsDist++;
        //if (inputHandler.scrollWheel.y < 0)
        //    tpsDist--;
        //if (tpsDist >= 0)
        //    tpsDist = -1;
        playerCamera.transform.localPosition = new Vector3(0, cc.height / 4, tpsDist);
    }

    public void ToggleBrickType()
    {
        setBrickType = true;
    }

    public void ToggleBrickIndex()
    {
        setBrickIndex = true;
    }

    public void ToggleRotateBrick()
    {
        rotateBrick = true;
    }

    public void IncrementBrickType()
    {
        if (currentBrickType + 1 <= ldrawPartsTypes.Length - 1)
            currentBrickType++;
        else
            currentBrickType = 0;

        SetCurrentBrickType(currentBrickType, currentBrickType);
        currentLDrawPartsListStringArray = ldrawPartsTypes[currentBrickType];
        if (currentBrickIndex > currentLDrawPartsListStringArray.Length - 1)
            currentBrickIndex = 0;
        if (currentBrickIndex < 0)
            currentBrickIndex = currentLDrawPartsListStringArray.Length - 1;
        SetCurrentBrickIndex(currentBrickIndex, currentBrickIndex);

        UpdateGrabObject(0);
        setBrickType = false;
    }

    public void DecrementBrickType()
    {
        if (currentBrickType - 1 >= 0)
            currentBrickType--;
        else
            currentBrickType = ldrawPartsTypes.Length - 1;

        SetCurrentBrickType(currentBrickType, currentBrickType);
        currentLDrawPartsListStringArray = ldrawPartsTypes[currentBrickType];
        if (currentBrickIndex > currentLDrawPartsListStringArray.Length - 1)
            currentBrickIndex = 0;
        if (currentBrickIndex < 0)
            currentBrickIndex = currentLDrawPartsListStringArray.Length - 1;
        SetCurrentBrickIndex(currentBrickIndex, currentBrickIndex);

        UpdateGrabObject(0);
        setBrickType = false;
    }

    public void IncrementBrickIndex()
    {
        if (currentBrickIndex + 1 <= currentLDrawPartsListStringArray.Length - 1)
            currentBrickIndex++;
        else
            currentBrickIndex = 0;

        SetCurrentBrickIndex(currentBrickIndex, currentBrickIndex);

        UpdateGrabObject(0);

        if (!movingPlacedBrickUseStoredValues)
        {
            SetCurrentBrickIndex(currentBrickIndex, currentBrickIndex);
        }

        setBrickIndex = false;
    }

    public void DecrementBrickIndex()
    {
        if (currentBrickIndex - 1 >= 0)
            currentBrickIndex--;
        else
            currentBrickIndex = currentLDrawPartsListStringArray.Length - 1;

        SetCurrentBrickIndex(currentBrickIndex, currentBrickIndex);

        UpdateGrabObject(0);

        if (!movingPlacedBrickUseStoredValues)
        {
            SetCurrentBrickIndex(currentBrickIndex, currentBrickIndex);
        }

        setBrickIndex = false;
    }

    public void RotateBrickLeft()
    {
        if (currentBrickRotation + 1 <= 3)
            currentBrickRotation++;
        else
            currentBrickRotation = 0;
        SetCurrentBrickRotation(currentBrickRotation, currentBrickRotation);

        UpdateGrabObject(0);
        
        if (!movingPlacedBrickUseStoredValues)
        {
            SetCurrentBrickIndex(currentBrickIndex, currentBrickIndex);
        }
            
        rotateBrick = false;
    }

    public void RotateBrickRight()
    {
        if (currentBrickRotation - 1 >= 0)
            currentBrickRotation--;
        else
            currentBrickRotation = 3;

        UpdateGrabObject(0);
        
        if(!movingPlacedBrickUseStoredValues)
        {
            SetCurrentBrickIndex(currentBrickIndex, currentBrickIndex);
        }
        rotateBrick = false;
    }

    public void SetDOF()
    {
        // allow adjustments for DoF
        float focusDistance = worldPPFXSetValues.depthOfField.focusDistance.value;

        if (inputHandler.navRight || inputHandler.navUp)
        {
            if (focusDistance + focusDistanceIncrement < maxFocusDistance || focusDistance + focusDistanceIncrement < maxFocusDistance)
                worldPPFXSetValues.depthOfField.focusDistance.value += focusDistanceIncrement;
        }
        else if (inputHandler.navLeft || inputHandler.navDown)
        {
            if (focusDistance - focusDistanceIncrement > 0 || focusDistance - focusDistanceIncrement > 0)
                worldPPFXSetValues.depthOfField.focusDistance.value -= focusDistanceIncrement;
        }
    }

    public void PressedBuild()
    {
        // SPAWN A BRICK

        if(shootPos.gameObject.activeSelf) // do not do if looking at voxel
            return;

        if (Time.time < mining.nextTimeToFire) // limit how fast can use this
            return;

        if (toolbar.slots[toolbar.slotIndex].itemSlot.stack == null) // do not spawn object if no voxel in current inventory slot
            return;

        blockID = toolbar.slots[toolbar.slotIndex].itemSlot.stack.id;

        if (blockID < 2 || blockID == 12)// || blockID == 17 || blockID == 18 || blockID == 20) // ignore these blockIDs
        {
            blockID = 0;
            return;
        }
        else if (blockID == 13) //manual color override grass = green
        {
            blockID = 8;
        }
        else if (blockID == 14) //manual color override wood = brown
        {
            blockID = 6;
        }
        else if (blockID == 15) //manual color override leaves  = green
        {
            blockID = 8;
        }
        // spawn brick object
        // if (Settings.OnlinePlay) // disabled for multiplayer
        //     return;

        heldObjectIsBrick = true;
        reticle.SetActive(false);
        holdingBuild = true;
        brickPickUp.Play();
        int brickMaterialIndex = System.Convert.ToInt32(blockID);
        SetCurrentBrickMaterialIndex(brickMaterialIndex, brickMaterialIndex);
        SpawnTempBrick(0);

        ResetPlacedBrickMaterialsAndBoxColliders(currentBrickMaterialIndex);

        // remove qty(1) voxel from slot as "cost"
        if (!Settings.WebGL && SettingsStatic.LoadedSettings.developerMode && toolbar.slotIndex == 0) // do not reduce item count from first slot (creative)
            TakeFromCurrentSlot(0);
        else
            TakeFromCurrentSlot(1);
    }

    private void PressedMine()
    {
        if (Time.time < mining.nextTimeToFire) // limit how fast can use this to avoid spamming
            return;

        if (SettingsStatic.LoadedSettings.developerMode && toolbar.slotIndex == 0) // cannot do this function from first slot if in creative mode
            return;

        RaycastHit hit;

        // if has mushroom, and health is not max and the selected slot has a stack
        if (toolbar.slots[toolbar.slotIndex].HasItem && toolbar.slots[toolbar.slotIndex].itemSlot.stack.id == blockIDmushroom && health.hp < health.hpMax)
        {
            // remove qty 1 from stack
            health.RequestEditSelfHealth(1);
            eat.Play();
            TakeFromCurrentSlot(1);
        }
        // else if (holdingGrab) // IF HOLDING SOMETHING
        // {
        //     holdingGrab = false;
        //     reticle.SetActive(true);

        //     if (grabbedPrefab != null) // IF HOLDING VOXEL
        //     {
        //         Vector3 position = holdPos.position;

        //         shootBricks.Play();

        //         SpawnVoxelRbFromWorld(position, blockID);

        //         UpdateGrabObject(blockID);
        //     }
        //     else if (heldObRb != null) // IF HOLDING NON-VOXEL RB
        //     {
        //         heldObRb.linearVelocity = playerCamera.transform.forward * 25; // give some velocity forwards
        //     }

        //     if (heldObRb != null)
        //     {
        //         heldObRb.useGravity = true;
        //         heldObRb.detectCollisions = true;
        //     }
        //     heldObRb = null;
        // }
        // IF SHOT PLACEDBRICK SITTING IN WORLD, DESTROY IT
        else if (camMode == 1 && Physics.SphereCast(playerCameraOrigin.transform.position + playerCamera.transform.forward * cc.radius * 2, sphereCastRadius, playerCamera.transform.forward, out hit, grabDist, 11) && hit.transform.gameObject.tag == "placedBrick")
        {
            //if(miningCounter >= toolMiningSpeeds[toolID])
            if(!toolbar.slots[toolbar.slotIndex].HasItem)
            {
                GameObject hitObject = hit.transform.gameObject;
                //Vector3 pos = hitObject.transform.position;
                int currentBrickMaterialIndex = GetMaterialIndex(hitObject);
                //PutAwayBrick((byte)currentBrickMaterialIndex);
                SpawnVoxelRbFromWorld(hit.transform.position, (byte)currentBrickMaterialIndex);
                Destroy(hit.transform.gameObject);
            }

            // if (Settings.OnlinePlay)
            // {
            //     CmdSpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
            //     CmdSpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
            //     CmdSpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
            //     CmdSpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));
            // }
            // else
            // {
            //     SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
            //     SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
            //     SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
            //     SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));
            // }
        }
        else if (shootPos.gameObject.activeSelf && camMode == 1) // IF MINE WORLD (NOT HELD) VOXEL (only destroy world in fps camMode)
        {
            blockID = World.Instance.GetVoxelState(shootPos.position).id; // need current blockID to test if can mine

            canMine = false; // set to true only if block is found in allowable matrix
            for (int i = 0; i < allowableDropMatrix[toolID].Length; i++) // for every block in matrix for given toolID
            {
                if (allowableDropMatrix[toolID][i] == blockID) // if the current block matches an allowable mineable block
                    canMine = true; // allow the next code to mine the block
            }

            Vector3 position = shootPos.position;
            blockID = World.Instance.GetVoxelState(position).id;

            // MINING DELAY
            if(miningCounter >= toolMiningSpeeds[toolID] + World.Instance.blockTypes[blockID].hardness)
            {
                if (!World.Instance.IsGlobalPosInsideBorder(position)) // do not let player do this for blocks outside border of world (glitches)
                    return;

                if (blockID == blockIDprocGen || blockID == blockIDbase) // cannot destroy procGen.ldr or base.ldr (imported VBO)
                    return;

                shootBricks.Play();
                PlayerRemoveVoxel();

                //PutAwayBrick(blockID); // give player a voxel for removing one from world (give player voxel until drops can use voxelCollider/no chunk meshes to collide with)
                if(canMine)
                    SpawnVoxelRbFromWorld(position, blockID); // give drop
                
                miningCounter = 0; // reset mining counter after successful mine to avoid instamine after mine 1 block
            }
        }
        // else //if (toolbar.slots[toolbar.slotIndex].HasItem && toolbar.slots[toolbar.slotIndex].itemSlot.stack.id == blockIDcrystal) // if has crystal, spawn projectile
        // {
        //     if (Settings.OnlinePlay)
        //         CmdSpawnObject(2, 0, rayCastStart);
        //     else
        //         SpawnObject(2, 0, rayCastStart);
        //     //TakeFromCurrentSlot(1);
        // }

        reticle.SetActive(true);
    }

    private void PressedPlaceBlock()
    {
        if (Time.time < mining.nextTimeToFire) // limit how fast can use this to prevent spamming
            return;

        // IF Pressed place block while looking at certain voxel id's enter crafting/furnace UI and exit this function, do not continue with placing block code
        blockID = World.Instance.GetVoxelState(shootPos.position).id;
        switch(blockID)
        {
            case 21: // crafting table
                {
                    inventoryUIMode = 2; //enter crafting table UI
                    return;
                }
            case 22: // furnace
                {
                    inventoryUIMode = 3; //enter furnace UI
                    return;
                }
        }

        if (!toolbar.slots[toolbar.slotIndex].HasItem) // cannot place block if no item in slot
            return;

        if (removePos.gameObject.activeSelf && placePos.position.y < VoxelData.ChunkHeight - 1) // IF VOXEL PRESENT, PLACE VOXEL
        {
            health.blockCounter++;
            PlayerPickBrickFromInventory();
            PlaceVoxel(placePos.position);
        }

        placedBrick = null;
        heldObjectIsBrick = false;
        movingPlacedBrickUseStoredValues = false;
        reticle.SetActive(true);
    }

    private void SpawnTempBrick(int materialIndex)
    {
        Quaternion rot = Quaternion.identity;
        switch (currentBrickRotation)
        {
            // ONLY ALLOW ROTATION OF BRICK NORTH SOUTH EAST WEST FOR NOW
            case 0:
                rot = Quaternion.Euler(new Vector3(180, 0, 0));
                break;
            case 1:
                rot = Quaternion.Euler(new Vector3(180, 90, 0));
                break;
            case 2:
                rot = Quaternion.Euler(new Vector3(180, 180, 0));
                break;
            case 3:
                rot = Quaternion.Euler(new Vector3(180, 270, 0));
                break;
            //case 4:
            //    rot = Quaternion.Euler(new Vector3(270, 0, 0));
            //    break;
            //case 5:
            //    rot = Quaternion.Euler(new Vector3(270, 90, 0));
            //    break;
            //case 6:
            //    rot = Quaternion.Euler(new Vector3(270, 180, 0));
            //    break;
            //case 7:
            //    rot = Quaternion.Euler(new Vector3(270, 270, 0));
            //    break;
            //case 8:
            //    rot = Quaternion.Euler(new Vector3(0, 0, 0));
            //    break;
            //case 9:
            //    rot = Quaternion.Euler(new Vector3(0, 90, 0));
            //    break;
            //case 10:
            //    rot = Quaternion.Euler(new Vector3(0, 180, 0));
            //    break;
            //case 11:
            //    rot = Quaternion.Euler(new Vector3(0, 270, 0));
            //    break;
            //case 12:
            //    rot = Quaternion.Euler(new Vector3(90, 0, 0));
            //    break;
            //case 13:
            //    rot = Quaternion.Euler(new Vector3(90, 90, 0));
            //    break;
            //case 14:
            //    rot = Quaternion.Euler(new Vector3(90, 180, 0));
            //    break;
            //case 15:
            //    rot = Quaternion.Euler(new Vector3(90, 270, 0));
            //    break;
        }

        // while holding mine, spawn an object with current partname parented to cursor with light blue material
        string color = "43"; // spawns objects with trans light blue for temp color
        // block position/rotation are not applied to subparts with the ldraw importer plugin so we set these values to default since we are rotating the new parent object.
        string x = "0.000000"; // position x
        string y = "0.000000"; // position y
        string z = "0.000000"; // position z
        string a = "1.000000";
        string b = "0.000000";
        string c = "0.000000";
        string d = "0.000000";
        string e = "1.000000";
        string f = "0.000000";
        string g = "0.000000";
        string h = "0.000000";
        string i = "1.000000";

        string partname = ldrawPartsTypes[currentBrickType][currentBrickIndex];
        Vector3 pos = new Vector3(0, 1, 0);
        string cmdstr = "1" + " " + color + " " + x + " " + y + " " + z + " " + a + " " + b + " " + c + " " + d + " " + e + " " + f + " " + g + " " + h + " " + i + " " + partname;

        if(!Settings.OnlinePlay || (Settings.OnlinePlay && holdingBuild) || (Settings.OnlinePlay && holdingGrab)) // if online and holding build or use, only spawn local temp brick, not on server
            PlaceBrick(false, partname, cmdstr, materialIndex, pos, rot); // spawn with transparent "temp" material
        else if (Settings.OnlinePlay && hasAuthority)
            CmdPlaceBrick(false, partname, cmdstr, materialIndex, pos, rot); // spawn with transparent "temp" material (HOST SPAWNS OBJECT ON CLIENT MACHINES)
    }

    void HoldingBuild()
    {
        if (placedBrick != null) // IF PIECE IS SPAWNED
            MoveBrickToCursor(placedBrick);
    }

    void ReleasedBuild()
    {
        holdingBuild = false;
        reticle.SetActive(true);

        if (!toolbar.slots[toolbar.slotIndex].itemSlot.HasItem) // cannot do this if no items in slot
        {
            // remove temp piece
            if (!heldObjectIsBrick)
                Destroy(placedBrick);
            placedBrick = null;
            return;
        }

        UpdateGrabObject((byte)currentBrickMaterialIndex);

        int brickMaterialIndex = System.Convert.ToInt32(toolbar.slots[toolbar.slotIndex].itemSlot.stack.id);
        SetCurrentBrickMaterialIndex(brickMaterialIndex, brickMaterialIndex);

        if (blockID < 2 || blockID > 11) // cannot use bricks using voxels outside the defined color range
        {
            // remove temp piece
            if (!heldObjectIsBrick)
                Destroy(placedBrick);
            placedBrick = null;
            blockID = 0;
            return;
        }

        // when released, change material to voxelID from slot and stop moving part
        brickPlaceDown.Play();
        ResetPlacedBrickMaterialsAndBoxColliders(currentBrickMaterialIndex);

        

        // reset values
        heldObjectIsBrick = false;
        placedBrick = null;
        blockID = 0;
    }

    void SpawnVoxelRbFromWorld(Vector3 position, byte blockID)
    {
        if (!World.Instance.IsGlobalPosInsideBorder(position) || blockID == 0 || blockID == 1 || blockID == blockIDprocGen || blockID == blockIDbase) // if the blockID at position is air, barrier, base, procGenVBO, then skip to next position
            return;

        EditVoxel(position, 0, true); // destroy voxel at position
        if (Settings.OnlinePlay)
            CmdSpawnObject(0, blockID, position);
        else
            SpawnObject(0, blockID, position);
    }
    
    public void DropItemInSlot()
    {
        // cannot do if moving since this causes an item duplication glitch with colliding with dropped items
        if(isMoving)
            return;

        if (Time.time < mining.nextTimeToFire) // limit how fast can use this to avoid spamming
            return;

        if(dropButtonPressed)
            return;
        dropButtonPressed = true;


        if (SettingsStatic.LoadedSettings.developerMode && toolbar.slotIndex == 0) // cannot run this function if creative mode and first slot selected
            return;

        if (!options && camMode == 1 && toolbar.slots[toolbar.slotIndex].HasItem) // IF NOT IN OPTIONS AND IN FPS VIEW AND ITEM IN SLOT
        {
            // drop a single item from slot
            toolbar.DropItemFromSlot(toolbar.slotIndex);
            TakeFromCurrentSlot(1);
        }
    }

    public int GetRotationIndexFromQuaternion(Quaternion rot)
    {
        int rotationIndex = 0;

        if(rot.eulerAngles == new Vector3(0, 0, 180))
            rotationIndex = 2;
        else if(rot.eulerAngles == new Vector3(0,90, 180))
            rotationIndex = 3;
        else if (rot.eulerAngles == new Vector3(-180, 0, 0))
            rotationIndex = 0;
        else if (rot.eulerAngles == new Vector3(0, 270, 180))
            rotationIndex = 1;

        return rotationIndex;
    }

    public void PressedGrab()
    {
        // if (Time.time < mining.nextTimeToFire) // limit time to use again
        //     return;

        if (!World.Instance.IsGlobalPosInsideBorder(removePos.position)) // do not let player do this for blocks outside border of world (glitches)
            return;

        // check if cursor aimed at previously spawned piece
        RaycastHit hit;
        if (camMode == 1 && Physics.SphereCast(playerCameraOrigin.transform.position + playerCamera.transform.forward * cc.radius * 2, sphereCastRadius, playerCamera.transform.forward, out hit, grabDist, 11))
        {
            GameObject hitObject = hit.transform.gameObject;
            //if (hitObject != null && (hitObject.tag == "placedBrick" || hitObject.tag == "voxelRb")) // allow players to pickup dropped items
            if (hitObject != null && hitObject.tag == "placedBrick")
            {
                // if (Settings.OnlinePlay) // disabled for multiplayer
                //     return;

                reticle.SetActive(false);
                holdingGrab = true;
                heldObjectIsBrick = true;
                brickPickUp.Play();

                // save values from brick object
                int brickMaterialIndex = GetMaterialIndex(hitObject);
                if(brickMaterialIndex ==0) // non loaded colors, move object only
                {
                    // no loaded colors found so reset values to default
                    SetCurrentBrickMaterialIndex(2, 2);
                    SetCurrentBrickType(0, 0);
                    SetCurrentBrickIndex(0, 0);
                    currentBrickRotation = 0;

                    placedBrick = hitObject;
                }
                else // loaded colors found
                {
                    SetCurrentBrickMaterialIndex(brickMaterialIndex, brickMaterialIndex);
                    Vector2Int indexAndType = GetBrickTypeAndIndex(hitObject);
                    SetCurrentBrickType(indexAndType.x, indexAndType.x);
                    SetCurrentBrickIndex(indexAndType.y, indexAndType.y);
                    currentBrickRotation = GetRotationIndexFromQuaternion(hitObject.transform.rotation);
                    
                    placedBrick = hitObject;
                }
                // store values for later if moving bricks
                    movingPlacedBrickUseStoredValues = true;
            }
            // else if (removePos.gameObject.activeSelf) // in the case the world is using chunk Meshes, allows player to pickup voxels
            // {
            //     holdingGrab = true;
            //     heldObjectIsBrick = false;

            //     PlayerRemoveVoxel();
            // }
            else
                return; // do not spawn object if hit previously existing object
        }
        // else if (removePos.gameObject.activeSelf) // if removePos is active from detecting a voxel
        // {
        //     holdingGrab = true;
        //     heldObjectIsBrick = false;

        //     PlayerRemoveVoxel();
        // }
        else if (toolbar.slots[toolbar.slotIndex].itemSlot.stack != null) // if nothing targeted, pull brick from inventory
        {
            holdingGrab = true;
            heldObjectIsBrick = false;
            PlayerPickBrickFromInventory();
        }
    }

    void PlayerRemoveBrick(GameObject ob)
    {
        if (Settings.OnlinePlay && hasAuthority)
            CmdRemoveBrick(ob);
        else
            Destroy(ob);
    }

    [Command]
    void CmdRemoveBrick(GameObject ob)
    {
        RpcRemoveBrick(ob);
    }

    [ClientRpc]
    void RpcRemoveBrick(GameObject ob)
    {
        Destroy(ob);
    }

    void PlayerRemoveVoxel()
    {
        blockID = World.Instance.GetVoxelState(removePos.position).id;
        if (blockID == blockIDprocGen || blockID == blockIDbase) // cannot pickup procGen.ldr or base.ldr (imported VBO)
            return;

        if (blockID == blockIDcrystal)
            crystal.Play();
        else if (blockID == blockIDmushroom)
            mushroom.Play();

        if (blockID != 0 && blockID != 1) // if block is not air or barrier block
        {
            // remove voxel, play sound
            RemoveVoxel(removePos.position);
            brickPickUp.Play();
        }

        //reticle.SetActive(false);

        UpdateGrabObject(blockID);
    }

    void PlayerPickBrickFromInventory()
    {
        blockID = toolbar.slots[toolbar.slotIndex].itemSlot.stack.id;

        if (!Settings.WebGL && SettingsStatic.LoadedSettings.developerMode && toolbar.slotIndex == 0) // do not reduce item count from first slot (creative)
            TakeFromCurrentSlot(0);
        else
            TakeFromCurrentSlot(1);
        reticle.SetActive(false);

        UpdateGrabObject(blockID);
    }

    void UpdateGrabObject(byte blockID)
    {
        //// a switch function to call the correct function depending on online play or not
        if (Settings.OnlinePlay && hasAuthority)
            CmdUpdateGrabObject(blockID);
        else
            EditGrabObject(blockID);
    }

    [Command]
    void CmdUpdateGrabObject(byte blockID)
    {
        EditGrabObject(blockID); // updates/creates new object on server which in turn adds the object to clients
        //RpcUpdateGrabObject(blockID); // fails to create object on host client when called from client
    }

    [ClientRpc]
    void RpcUpdateGrabObject(byte blockID)
    {
        EditGrabObject(blockID);
    }

    void EditGrabObject(byte blockID)
    {
        if (placedBrick != null && heldObjectIsBrick)
        {
            Destroy(placedBrick);
            //PlayerRemoveBrick(placedBrick); // does not work for multiplayer, does not properly destroy object
            placedBrick = null;
            SpawnTempBrick(blockID);
            if(placedBrick != null)
                MoveBrickToCursor(placedBrick);
        }
        else if(holdingGrab)
        {
            grabbedPrefab = Instantiate(World.Instance.voxelPrefabs[blockID], holdPos.transform.position, Quaternion.identity);
            BoxCollider bc = grabbedPrefab.AddComponent<BoxCollider>(); //add a box collider to the grabbedPrefab voxel
            bc.center = new Vector3(0.5f, 0.5f, 0.5f);
            bc.material = physicMaterial;

            if (Settings.OnlinePlay)
            {
                if (grabbedPrefab.GetComponent<NetworkIdentity>() == null)
                    grabbedPrefab.AddComponent<NetworkIdentity>();
                //if (grabbedPrefab.GetComponent<NetworkTransform>() == null) // causes null object error for TransformBase
                //    grabbedPrefab.AddComponent<NetworkTransform>();
                //if(isServer)
                //    customNetworkManager.SpawnNetworkOb(grabbedPrefab); // Does not work for some reason
            }
            grabbedPrefab.transform.parent = holdPos;
        }
        else
            Destroy(grabbedPrefab);
    }

    public void HoldingGrab()
    {
        if (placedBrick != null && heldObjectIsBrick) // IF PIECE IS SPAWNED
        {
            MoveBrickToCursor(placedBrick);
        }
        //else if (heldObRb != null) // IF NON-VOXEL RB
        if (heldObRb != null) // IF NON-VOXEL RB
        {
            heldObRb.MovePosition(playerCamera.transform.position + playerCamera.transform.forward * 3.5f);
        }
        else if (grabbedPrefab != null) // IF HOLDING VOXEL
        {
            if (placePos.gameObject.activeSelf) // IF LOOKING AT CHUNK
            {
                    grabbedPrefab.transform.position = placePos.position; // move instance to position where it would attach
                    grabbedPrefab.transform.rotation = placePos.rotation;
            }
            else // IF HOLDING VOXEL
            {
                grabbedPrefab.transform.eulerAngles = placePos.eulerAngles;
                grabbedPrefab.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);
                grabbedPrefab.transform.Translate(new Vector3(-0.5f, -0.5f, -0.5f));
            }
        }
    }

    public void ReleasedGrab()
    {
        holdingGrab = false;
        reticle.SetActive(true);

        UpdateGrabObject((byte)currentBrickMaterialIndex);

        if (heldObjectIsBrick)
        {
            brickPlaceDown.Play();
            ResetPlacedBrickMaterialsAndBoxColliders(currentBrickMaterialIndex);
        }
        else if (removePos.gameObject.activeSelf && placePos.position.y < VoxelData.ChunkHeight - 1) // IF VOXEL PRESENT, PLACE VOXEL
        {
            health.blockCounter++;
            PlaceVoxel(placePos.position);
        }
        else // IF HOLDING VOXEL AND NOT AIMED AT VOXEL, STORE IN INVENTORY
            PutAwayBrick(blockID, placedBrickName);

        placedBrick = null;
        heldObjectIsBrick = false;
        movingPlacedBrickUseStoredValues = false;
    }

    public void PressedBuildWhileGrab()
    {
        // CONVERT BRICK BACK INTO VOXEL

        if (!heldObjectIsBrick)
            return;
        inputHandler.use = false; // force the input to false
        inputHandler.mine = false; // force the input to false

        holdingGrab = false;
        reticle.SetActive(true);
        brickPlaceDown.Play();
        PutAwayBrick((byte)currentBrickMaterialIndex, placedBrickName);
        Destroy(placedBrick);

        placedBrick = null;
        heldObjectIsBrick = false;
        movingPlacedBrickUseStoredValues = false;
    }

    public void PutAwayBrick(byte blockID, string _placedBrickName)
    {
        int firstSlot;
        if (!Settings.WebGL && SettingsStatic.LoadedSettings.developerMode) // determine first slot
            firstSlot = 1;
        else
            firstSlot = 0;

        UIItemSlot[] slotsToCheck;

        for(int iteration = 0; iteration < 2; iteration++) // run code 2X, once for toolbar, then again for slots
        {
            if(iteration == 0)
                slotsToCheck = toolbar.slots;
            else
                slotsToCheck = gameMenu.GetComponent<DragAndDropHandler>().inventory.inventorySlots;

            if (blockID != 0 && blockID != 1) // if block is not air or barrier block
            {
                for (int i = firstSlot; i < slotsToCheck.Length; i++) // for all slots in toolbar
                {
                    // FIRST CHECK PLACEDBRICKS
                    if(_placedBrickName != null && _placedBrickName != "")
                    {
                        if(slotsToCheck[i].itemSlot.HasItem && slotsToCheck[i].itemSlot.stack.placedBrickID == _placedBrickName)
                        {
                            if(slotsToCheck[i].itemSlot.stack.amount < 64)
                            {
                                slotsToCheck[i].itemSlot.Give(1);
                                return;
                            }
                        }
                    }

                    // SECOND CHECK VOXELS
                    if (slotsToCheck[i].itemSlot.stack != null && slotsToCheck[i].itemSlot.stack.id == blockID) // if toolbar slot has a stack and toolbar stack id matches highlighted block id
                    {
                        if (slotsToCheck[i].itemSlot.stack.amount < World.Instance.blockTypes[blockID].stackMax) // limit stack size to 64 items
                        {
                            // edit voxel, give to toolbar, play sound
                            slotsToCheck[i].itemSlot.Give(1);
                            return;
                        }
                    }
                }

                for (int j = 0; j < slotsToCheck.Length; j++) // for all stacks in toolbar
                {
                    // // FIRST CHECK PLACED BRICKS
                    // if(_placedBrickName != null && _placedBrickName != "")
                    // {
                    //     if (!toolslotsToCheckbar.slots[j].itemSlot.HasItem) // if there is an empty slot
                    //     {
                    //         // insert a new stack with qty 1 of blockID
                    //         ItemStack stack = new ItemStack(blockID, currentBrickName, 1);
                    //         slotsToCheck[j].itemSlot.InsertStack(stack);
                    //         return;
                    //     }
                    // }

                    //// SECOND CHECK VOXELS
                    if (!slotsToCheck[j].itemSlot.HasItem) // if there is an empty slot
                    {
                        // insert a new stack with qty 1 of blockID and currentBrickName
                        ItemStack stack = new ItemStack(blockID, _placedBrickName, 1);
                        slotsToCheck[j].itemSlot.InsertStack(stack);
                        return;
                    }
                }

                // if made it here, toolbar has no empty slots to put voxels into so drop block
                //PressedShoot();
                //return; // if made it here, toolbar has no empty slots to put voxels into so do not add any bricks to slots
            }
        }
    }

    void PlaceVoxel(Vector3 pos)
    {
        // a switch function to call the correct function depending on online play or not
        if (!World.Instance.CheckForVoxel(placePos.position, false))
        {
            if (blockID == blockIDcrystal)
                crystal.Play();
            else if (blockID == blockIDmushroom)
                mushroom.Play();

            // replace voxel, play sound
            if (Settings.OnlinePlay && hasAuthority)
                CmdEditVoxel(pos, blockID, false);
            else
                EditVoxel(pos, blockID, false);
            brickPlaceDown.Play();
        }
    }

    public void TakeFromCurrentSlot(int amount)
    {
        if (toolbar.slots[toolbar.slotIndex].itemSlot.stack.amount == 0) // do not remove if qty is already zero
            return;

        toolbar.slots[toolbar.slotIndex].itemSlot.Take(amount);

        // if after removing qty 1 from stack, qty = 0, then remove the stack from the slot
        if (toolbar.slots[toolbar.slotIndex].itemSlot.stack.amount == 0)
            toolbar.slots[toolbar.slotIndex].itemSlot.EmptySlot();
    }

    public void MoveBrickToCursor(GameObject ob)
    {
        Vector3 pos = playerCamera.transform.position + playerCamera.transform.forward * cc.radius * 8;
        pos = GetGridPos(pos); // snap to grid
        ob.transform.position = pos; // throws error when build is released on host machine
    }

    public Vector3 GetGridPos(Vector3 pos)
    {
        Vector3 returnPos = Vector3.zero;

        float xOffset = 0f;
        float yOffset = 0f;
        float zOffset = 0f;
        if (placedBrick != null)
        {
            if (placedBrick.GetComponent<BoxCollider>() != null)
            {
                BoxCollider bc = placedBrick.GetComponent<BoxCollider>();
                int bcsx = Mathf.RoundToInt(bc.size.x); // eliminate rounding errors
                int bcsz = Mathf.RoundToInt(bc.size.z); // eliminate rounding errors
                if (currentBrickRotation % 2 == 0) // if current rotation index is even
                {
                    if (bcsx % 40 != 0) // if stud width is odd
                    {
                        xOffset = 0.25f; // offset in that direction to align with stud grid
                    }
                    if (bcsz % 40 != 0) // if stud width is odd
                    {
                        zOffset = 0.25f; // offset in that direction to align with stud grid
                    }
                }
                else if (currentBrickRotation % 2 != 0) // if current rotation index is odd
                {
                    if (bcsx % 40 != 0) // if stud width is odd
                    {
                        zOffset = 0.25f; // offset other direction since rotated
                    }
                    if (bcsz % 40 != 0) // if stud width is odd
                    {
                        xOffset = 0.25f; // offset other direction since rotated
                    }
                }
                yOffset = bc.size.y / 40; // move bottom of part to voxel grid
            }
        }

        //FORCES BRICKS TO SNAP TO GRID BASED ON BRICK PROPORTIONS
        // 1 stud is 1/2 the width of a voxel and one stud is 1/5 the height of a voxel
        returnPos = new Vector3((Mathf.Ceil(pos.x * 2) / 2) + xOffset, Mathf.Ceil(pos.y * 5) / 5 + yOffset, (Mathf.Ceil(pos.z * 2) / 2) + zOffset); // snap position to nearest voxel grid

        return returnPos; // round to the nearest grid position
    }

    public void ResetPlacedBrickMaterialsAndBoxColliders(int materialIndex)
    {
        if (placedBrick == null)
            return;

        MeshRenderer[] mrs = placedBrick.transform.GetComponentsInChildren<MeshRenderer>();
        if (mrs.Length != 0)
        {
            foreach (MeshRenderer mr in mrs)
            {
                if(materialIndex <= brickMaterials.Length)
                    mr.material = brickMaterials[materialIndex];
            }
        }
        BoxCollider[] bcs = placedBrick.transform.GetComponentsInChildren<BoxCollider>();
        if (bcs.Length != 0)
        {
            foreach (BoxCollider bc in bcs)
            {
                bc.enabled = false;
            }
        }
        placedBrick.GetComponent<BoxCollider>().enabled = true;
    }

    [Command]
    void CmdPlaceBrick(bool fromFile, string _partname, string cmdstr, int materialIndex, Vector3 pos, Quaternion rot)
    {
        RpcPlaceBrick(fromFile, _partname, cmdstr, materialIndex, pos, rot);
    }

    [ClientRpc]
    void RpcPlaceBrick(bool fromFile, string _partname, string cmdstr, int materialIndex, Vector3 pos, Quaternion rot)
    {
        PlaceBrick(fromFile, _partname, cmdstr, materialIndex, pos, rot);
    }

    public GameObject PlaceBrick(bool fromFile, string _partname, string cmdstr, int materialIndex, Vector3 pos, Quaternion rot)
    {
        GameObject returnOb;

        if (!fromFile)
        {
            pos = playerCamera.transform.position + playerCamera.transform.forward * cc.radius * 8;
            pos = GetGridPos(pos); // snap to grid
        }

        var model = LDrawModelRuntime.Create(cmdstr, cmdstr, false);
        placedBrick = model.CreateMeshGameObject(_ldrawConfigRuntime.ScaleMatrix);

        if (Settings.OnlinePlay)
        {
            // for online play, register the brick with the network manager
            NetworkManager netManager = gameManager.PlayerManagerNetwork.GetComponent<NetworkManager>();
            netManager.spawnPrefabs.Add(placedBrick);
        }

        placedBrick = LDrawImportRuntime.Instance.ConfigureModelOb(placedBrick, pos, false);

        placedBrick.transform.rotation = rot; // set parent object rotation equal to rotation passed into this function

        //SceneObject sceneObject = placedBrick.AddComponent<SceneObject>(); // used for spawning object on the server
        //sceneObject.controller = this;
        //if (placedBrick.GetComponent<BoxCollider>() != null)
        //{
        //    BoxCollider previousBC = placedBrick.GetComponent<BoxCollider>();
        //    Destroy(previousBC);
        //}
        BoxCollider VoxelBc = placedBrick.AddComponent<BoxCollider>();
        VoxelBc.enabled = true;
        VoxelBc.material = physicMaterial;
        placedBrick.SetActive(true);
        placedBrick.name = _partname;
        placedBrickName = _partname;
        placedBrick.tag = "placedBrick";
        placedBrick.layer = 0;
        placedBrick.isStatic = true;

        ResetPlacedBrickMaterialsAndBoxColliders(materialIndex);

        if (fromFile)
            placedBrick = null;

        returnOb = placedBrick;
        return returnOb;
    }

    public void CmdSpawnObject(int type, int item, Vector3 pos)
    {
        SpawnObject(type, item, pos);
    }

    // USED TO SPAWN DROPPED ITEMS THAT CAN BE LATER PICKED UP
    public void SpawnObject(int type, int item, Vector3 pos, GameObject obToSpawn = null)
    {
        Vector3 spawnDir;
        if (camMode == 1) // first person camera spawn object in direction camera
            spawnDir = playerCamera.transform.up;
        else // all other camera modes, spawn object in direction of playerObject
            spawnDir = transform.up;

        GameObject ob = Instantiate(sceneObjectPrefab, pos, Quaternion.identity);
        Rigidbody rb;

        ob.transform.rotation = Quaternion.LookRotation(spawnDir); // orient forwards in direction of camera
        rb = ob.GetComponent<Rigidbody>();
        rb.mass = health.piecesRbMass;
        
        rb.isKinematic = false;
        rb.useGravity = true;
        //rb.linearVelocity = Vector3.up * 0.5f; // spawnDir + Vector3.up * 2; // give some velocity up

        
        SceneObject sceneObject = ob.GetComponent<SceneObject>();
        
        GameObject childOb;
        BoxCollider sceneObBc;
        switch (type)
        {
            case 0: // IF VOXEL
                {
                    sceneObject.SetEquippedItem(type, item); // set the child object on the server
                    sceneObject.typeVoxel = item; // set the SyncVar on the scene object for clients
                    BoxCollider VoxelBc = ob.AddComponent<BoxCollider>();
                    VoxelBc.material = physicMaterial;
                    VoxelBc.center = new Vector3(0.5f, 0.5f, 0.5f);
                    VoxelBc.size = new Vector3 (1f, 1f, 1f) * 2f; // set slightly larger to help players pick up items?
                    VoxelBc.isTrigger = true; // if is trigger cannot collide with world but more easily picked up by player?
                    ob.tag = "voxelRb";
                    sceneObject.controller = this;
                    rb = ob.GetComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.isKinematic = false;
                    VoxelCollider vc = ob.GetComponentInChildren<VoxelCollider>();
                        vc.isItem = true; // set true to spin object
                    ob.transform.rotation = Quaternion.Euler(0, 0, 0); //zero rotation
                    ob.transform.localScale = new Vector3(1, 1, 1) * 0.5f; // scale
                    ob.transform.position += new Vector3(1, 1, 1) * 0.25f; // move to center of voxel position
                    break;
                }
            case 2: // IF PROJECTILE
                {
                    if (Settings.OnlinePlay)
                        sceneObject.projectileString = playerProjectile;
                    else
                        sceneObject.projectile[0] = projectile;
                    sceneObject.typeProjectile = item; // should be 0 for first item in array
                    if(SettingsStatic.LoadedSettings.projectilesHurt)
                        ob.tag = "Hazard";
                    sceneObject.SetEquippedItem(type, item); // update the child object on the server

                    // WIP collider is slightly off center for some reason, has to do with LDrawImportRuntime
                    childOb = ob.transform.GetChild(0).gameObject; // get the projectile (clone) object
                    if (childOb.GetComponent<BoxCollider>() != null)
                    {
                        BoxCollider childObBc = childOb.GetComponent<BoxCollider>();
                        childOb.GetComponent<BoxCollider>().enabled = false;
                        sceneObBc = ob.AddComponent<BoxCollider>();
                        float childScale = childOb.transform.localScale.x;
                        sceneObBc.size = childObBc.size * childScale;
                        sceneObBc.center = childObBc.center * childScale;
                        sceneObBc.material = physicMaterial;
                    }
                    break;
                }
            case 3: // IF VOXEL BIT
                {
                    sceneObject.SetEquippedItem(type, item); // set the child object on the server
                    sceneObject.typeVoxelBit = item;
                    BoxCollider voxelBitBc = ob.AddComponent<BoxCollider>();
                    voxelBitBc.material = physicMaterial;
                    voxelBitBc.center = new Vector3(0, -.047f, 0);
                    voxelBitBc.size = new Vector3(0.5f, 0.3f, 0.5f);
                    ob.tag = "voxelBit";
                    break;
                }
            case 4: // IF PLACED BRICK DROPPED ITEM
                {
                    sceneObject.placedBrickName = placedBrickName;
                    sceneObject.SetEquippedItem(type, item); // set the child object on the server
                    sceneObject.typeVoxel = item; // set the SyncVar on the scene object for clients
                    sceneObject.placedBrickName = placedBrickName; // set the SyncVar on the scene object for clients
                    BoxCollider VoxelBc = ob.AddComponent<BoxCollider>();
                    VoxelBc.material = physicMaterial;
                    VoxelBc.center = new Vector3(0.5f, 0.5f, 0.5f);
                    VoxelBc.size = new Vector3 (1f, 1f, 1f) * 2f; // set slightly larger to help players pick up items?
                    VoxelBc.isTrigger = true; // if is trigger cannot collide with world but more easily picked up by player?
                    ob.tag = "voxelRb";
                    sceneObject.controller = this;
                    rb = ob.GetComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.isKinematic = false;
                    VoxelCollider vc = ob.GetComponentInChildren<VoxelCollider>();
                        vc.isItem = true; // set true to spin object
                    ob.transform.rotation = Quaternion.Euler(0, 0, 0); //zero rotation
                    ob.transform.localScale = new Vector3(1, 1, 1) * 0.5f; // scale
                    ob.transform.position += new Vector3(1, 1, 1) * 0.25f; // move to center of voxel position
                    break;
                }
            case 5: // IF GAMEOBJECT REFERENCE (WIP cannot pass gameobject reference into server commands, so this is not used for now)
                {
                    if(obToSpawn != null)
                    {
                        sceneObject.undefinedPrefab = new GameObject[] { obToSpawn };
                        sceneObject.typeUndefinedPrefab = item; // should be 0 for first item in array
                        sceneObject.SetEquippedItem(type, item); // set the child object on the server
                        ob.transform.localScale = new Vector3(0.025f, 0.025f, 0.025f);
                        childOb = ob.transform.GetChild(0).gameObject;
                        if(childOb.GetComponent<BoxCollider>() != null)
                        {
                            BoxCollider childObBc = childOb.GetComponent<BoxCollider>();
                            //childObBc.enabled = false;
                            sceneObBc = ob.AddComponent<BoxCollider>();
                            sceneObBc.size = childObBc.size;
                            sceneObBc.center = childObBc.center;
                            sceneObBc.material = physicMaterial;
                        }
                    }
                    break;
                }
        }

        if (Settings.OnlinePlay)
        {
            ob.GetComponent<NetworkIdentity>().enabled = true;
            if (ob.GetComponent<NetworkTransform>() == null)
                ob.AddComponent<NetworkTransform>();
            ob.GetComponent<NetworkTransform>().enabled = true;
            customNetworkManager.SpawnNetworkOb(ob);
        }
        Destroy(ob, 30); // clean up objects after 30 seconds
    }

    void RemoveVoxel(Vector3 pos)
    {
        if (blockID != 0 && blockID != 1) // if block is not air or barrier block
        {
            if (Settings.OnlinePlay && hasAuthority)
                CmdEditVoxel(pos, 0, true);
            else
                EditVoxel(pos, 0, true);
        }
    }

    [Command]
    void CmdEditVoxel(Vector3 position, byte id, bool remove)
    {
        RpcEditVoxel(position, id, remove);
    }

    [ClientRpc]
    void RpcEditVoxel(Vector3 position, byte id, bool remove)
    {
        EditVoxel(position, id, remove);
    }

    void EditVoxel(Vector3 position, byte id, bool remove)
    {
        
        byte oldBlockID = World.Instance.GetChunkFromVector3(position).GetVoxelFromGlobalVector3(position).id;
        if (oldBlockID == 1) // cannot use barrier blocks
            return;

        Chunk chunk = World.Instance.GetChunkFromVector3(position);

        chunk.EditVoxel(position, id, this, chunk.chunkData);

        Vector3 centeredPosition = new Vector3(position.x + 0.5f, position.y + 0.5f, position.z + 0.5f);
        if (remove)
        {
            if (voxelBoundObjects.TryGetValue(centeredPosition, out _))
            {
                Destroy(voxelBoundObjects[centeredPosition]);
                voxelBoundObjects.Remove(centeredPosition);
            }
        }

        RequestSaveWorld(); // save the world after every time a block is placed/edited
        gameMenu.GetComponent<GameMenu>().SaveSettings();
    }

    public Vector3[] GetVoxelPositionsInVolume(Vector3 center, int width)
    {
        // DISABLED would allow players to edit more than 1 voxel at a time causing block duplication(breaks gameplay)

        if (width % 2 == 0) // if even number, round up to nearest odd number
            width += 1;

        Vector3[] positions = new Vector3[(int)Mathf.Pow(width, 3)];

        if (width < 2)
        {
            positions[0] = center;
            return positions;
        }

        int radius = (width - 1) / 2;
        int i = 0;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    positions[i] = new Vector3(center.x + x, center.y + y, center.z + z);
                    i++;
                }
            }
        }
        return positions;
    }

    private void positionCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        // adjust distCheck based on how far the camera is from camOrigin (3rd person cam)
        float distCheck;
        if (camMode == 1)
            distCheck = grabDist;
        else
            distCheck = (grabDist + grabDist * (playerCamera.transform.position - playerCameraOrigin.transform.position).magnitude) * 0.75f; // broken does not work

        // All position cursor blocks must be within same loop or causes lag where multiple loops cannot be run at same time (else use a coroutine)
        while (step < distCheck)
        {
            Vector3 pos = playerCamera.transform.position + (playerCamera.transform.forward * step);

            if (world.CheckForVoxel(pos, false))
            {
                removePos.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
                placePos.position = lastPos;
                shootPos.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

                removePos.gameObject.SetActive(true);
                placePos.gameObject.SetActive(true);
                shootPos.gameObject.SetActive(true);

                return;
            }

            lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

            step += checkIncrement;
        }

        removePos.gameObject.SetActive(false);
        shootPos.gameObject.SetActive(false);
        placePos.gameObject.SetActive(false);
    }

    void CCShapeAlternate(CapsuleCollider cc, float offset)
    {
        cc.direction = 2;
        cc.radius = colliderRadius + offset;
        cc.height = colliderHeight * 1.15f + offset;
        cc.center = new Vector3(0, -colliderHeight / 3, 0);
    }

    void CCShapeNormal(CapsuleCollider cc, float offset)
    {
        cc.direction = 1;
        cc.radius = colliderRadius + offset;
        cc.height = colliderHeight + offset;
        cc.center = Vector3.zero;
    }

    bool CheckGroundedCollider()
    {
        float rayLength;
        Vector3 rayStart = transform.position;

        // cast a ray starting from within the capsule collider down to outside the capsule collider.
        rayLength = cc.height * 0.25f + 0.01f;

        sphereCastRadius = cc.radius * 0.5f;

        // Debug tools
        Debug.DrawRay(rayStart, Vector3.down * rayLength, Color.red, 0.02f);

        // check if the char is grounded by casting a ray from rayStart down extending rayLength
        if (Physics.SphereCast(rayStart, sphereCastRadius, Vector3.down, out RaycastHit hit, rayLength))
            return true;
        else
            return false;
    }

    public void MovePlayer()
    {
        if (inputHandler.move != Vector2.zero && inventoryUIMode == 0)
            isMoving = true;
        else
            isMoving = false;

        if (inputHandler.sprint && inventoryUIMode == 0)
            isSprinting = true;
        else
            isSprinting = false;

        // APPLY GRAVITY AND MOVE FORCES
        velocityPlayer = voxelCollider.CalculateVelocity(inputHandler.move.x, inputHandler.move.y, isSprinting, inputHandler.jump);

        if ((Settings.WebGL || !SettingsStatic.LoadedSettings.developerMode) && inputHandler.jump && inventoryUIMode == 0)
        {
            isGrounded = false;
            inputHandler.jump = false;
            health.jumpCounter++;
        }


        // PLAYER LOOK ROTATION
        if(camMode == 1 && inventoryUIMode == 0)
        {
            if (charController.enabled && World.Instance.IsGlobalPosInsideBorder(transform.position + velocityPlayer)) // keep player inside world borders
                charController.Move(velocityPlayer); // used character controller since that was only thing found to collide with imported ldraw models

            Vector2 rotation = CalculateRotation();
            playerCamera.transform.localEulerAngles = new Vector3(rotation.y, 0f, 0f);
            gameObject.transform.localEulerAngles = new Vector3(0f, rotation.x, 0f);
        }
        else if(camMode == 2 && inventoryUIMode == 0)
        {
            if (isMoving)
                gameObject.transform.eulerAngles = new Vector3(0f, playerCameraOrigin.transform.rotation.eulerAngles.y, 0f); // rotate gameobject to face same y direction as camera

            // moves player object forwards
            if (charController.enabled && World.Instance.IsGlobalPosInsideBorder(transform.position + velocityPlayer)) // keep player inside world borders
                charController.Move(velocityPlayer); // used character controller since that was only thing found to collide with imported ldraw models

            // rotate cameraOrigin around player model (LookAtConstraint ensures camera always faces center)
            Vector2 rotation = CalculateRotation();
            playerCameraOrigin.transform.eulerAngles = new Vector3(rotation.y, rotation.x, 0f);

            if (isMoving) // if is moving
                charModelOrigin.transform.eulerAngles = new Vector3(0, playerCameraOrigin.transform.rotation.eulerAngles.y, 0); // rotate char model to face same y direction as camera
        }


        // PLAYER JUMP/SPRINT
        if(!Settings.WebGL && (camMode != 3 && SettingsStatic.LoadedSettings.developerMode && inventoryUIMode == 0))
        {
            if (charController.enabled && inputHandler.jump)
                charController.Move(Vector3.up * 0.5f);
            if (charController.enabled && inputHandler.sprint)
                charController.Move(Vector3.down * 0.5f);
        }
    }

    public void MoveCamera()
    {
        if (inputHandler.move != Vector2.zero)
            isMoving = true;
        else
            isMoving = false;

        Vector3 cameraMoveVelocity = playerCameraVoxelCollider.CalculateVelocityCamera(inputHandler.move.x, inputHandler.move.y, inputHandler.sprint);

        charController.enabled = false;
        playerCamera.transform.position += cameraMoveVelocity;
        charController.enabled = true;

        Vector2 rotation = CalculateRotation();
        playerCamera.transform.localEulerAngles = new Vector3(rotation.y, rotation.x, 0f);
    }

    public Vector2 CalculateRotation()
    {
        if (inputHandler.look.x != 0f && inputHandler.look.y != 0f)
        {
            if (lookVelocity > maxLookVelocity)
            {
                lookVelocity = maxLookVelocity;
            }
            else if (Settings.WebGL)
                lookVelocity += lookVelocity;
            else
                lookVelocity += SettingsStatic.LoadedSettings.lookAccel;
        }
        else
            lookVelocity = 1f;

        // rotate camera left/right, multiply by lookVelocity so controller players get look accel
        if(!Settings.WebGL)
            rotationX += inputHandler.look.x * lookVelocity * SettingsStatic.LoadedSettings.lookSpeed * 0.5f;
        else
            rotationX += inputHandler.look.x * lookVelocity * lookSpeed * 0.5f;

        // rotate camera up/down, multiply by lookVelocity so controller players get look accel
        if(Settings.WebGL)
        {
            if(!invertY)
                rotationY += -inputHandler.look.y * lookVelocity * lookSpeed * 0.5f;
            else
                rotationY += inputHandler.look.y * lookVelocity * lookSpeed * 0.5f;
        }
        else
        {
            if (!SettingsStatic.LoadedSettings.invertY)
                rotationY += -inputHandler.look.y * lookVelocity * SettingsStatic.LoadedSettings.lookSpeed * 0.5f;
            else
                rotationY += inputHandler.look.y * lookVelocity * SettingsStatic.LoadedSettings.lookSpeed * 0.5f;
        }
        

        // limit transform so player cannot look up or down past the specified angles
        rotationY = Mathf.Clamp(rotationY, minCamAngle, maxCamAngle);

        return new Vector2(rotationX, rotationY);
    }

    public void ToggleCamMode()
    {
        if (options) // cannot toggle while in options menu
            return;

        setCamMode = !setCamMode; // checked for in update loop
    }

    void SetCamMode()
    {
        camMode++;

        if (camMode < 1 || camMode > 3)
            camMode = 1;

        switch (camMode)
        {
            case 1: // FIRST PERSON CAMERA MODE
                {
                    playerHUD.SetActive(true);
                    CinematicBars.SetActive(false);

                    if (Settings.OnlinePlay)
                        nametag.SetActive(false);

                    playerCameraBoxCollider.enabled = false;
                    playerCameraVoxelCollider.enabled = false;
                    charController.enabled = false;
                    charController.enabled = true;

                    playerCameraOrigin.transform.localPosition = transform.up * colliderHeight * 0.8f;
                    playerCamera.transform.localPosition = Vector3.zero; // reset camera position
                    playerCamera.transform.eulerAngles = Vector3.zero; // reset camera rotation to face forwards
                    break;
                }
            case 2: // THIRD PERSON CAMERA MODE
                {
                    playerHUD.SetActive(true);
                    CinematicBars.SetActive(true);

                    if (Settings.OnlinePlay)
                        nametag.SetActive(true);

                    playerCameraBoxCollider.enabled = false;
                    playerCameraVoxelCollider.enabled = false;
                    charController.enabled = false;
                    charController.enabled = true;

                    playerCameraOrigin.transform.localPosition = transform.up * colliderHeight * 1.2f;
                    playerCamera.transform.localPosition = new Vector3(0, colliderHeight, tpsDist); // move camera behind character over shoulder
                    playerCamera.transform.eulerAngles = Vector3.zero; // reset camera rotation to face fowards
                    break;
                }
            case 3: // PHOTO MODE
                {
                    playerHUD.SetActive(false);
                    CinematicBars.SetActive(true);

                    if (Settings.OnlinePlay)
                        nametag.SetActive(true);

                    playerCameraBoxCollider.enabled = true;
                    playerCameraVoxelCollider.enabled = true;
                    charController.enabled = false;
                    charController.enabled = true;

                    playerCameraOrigin.transform.localEulerAngles = Vector3.zero; // reset camera origin rotation
                    break;
                }
        }
        setCamMode = false;
    }

    public void ToggleOptions()
    {
        if(!options && inventoryUIMode != 0) // if not in options menu and in inventoryUIMode
        {
            ToggleInventory(); // exit inventory first before can go into options
            return;
        }

        options = !options;
    }

    public void ToggleInventory() // can also be triggered by "E" key or by right clicking certain blocks
    {
        if(inventoryUIMode != 0) // if inventory not hidden and triggered by ESCAPE or E,  hide all inventory UI
            inventoryUIMode = 0;
        else if(!options && inventoryUIMode == 0) // if not in options and inventory is hidden, show basic inventory
            inventoryUIMode = 1; // inventory
    }

    void Animate()
    {
        // adjust player animations to speed up according to player movement speed
        if (Time.time >= nextTimeToAnim)
        {
            if (isMoving)
            {
                if (isSprinting)
                    animRate = baseAnimRate; // animate same but here provides option to increase anim rate
                else
                    animRate = baseAnimRate;

                nextTimeToAnim = Time.time + 1f / animRate;

                charObIdle.SetActive(false);
                charObRun.SetActive(true);

                //// Toggles between run and idle state to simulate low fps animation
                //charObIdle.SetActive(!charObIdle.activeSelf);
                //charObRun.SetActive(!charObRun.activeSelf);
            }
            else
            {
                charObIdle.SetActive(true);
                charObRun.SetActive(false);
            }
        }
    }

    public void RequestSaveWorld()
    {
        if (Settings.OnlinePlay && hasAuthority)
            CmdSaveWorld();
        else
            SaveWorld(World.Instance.worldData);
    }

    [Command]
    public void CmdSaveWorld()
    {
        // tells the server to make all clients (including host client) save the world (moved here since the gameMenu cannot have a network identity).
        RpcSaveWorld();
    }

    [ClientRpc]
    public void RpcSaveWorld()
    {
        // tells all clients (including host client) to save the world data if they have any
        if(World.Instance != null) // World.Instance == null for new clients that just joined
            SaveWorld(World.Instance.worldData);
    }

    public void SaveWorld(WorldData worldData)
    {
        SaveSystem.SaveWorldDataToFile(worldData, world); // save specified worldData to disk (must pass in worldData since, clients set modified chunks from server)
        ExportPlacedBricks(false);
        ExportPlacedBricks(true);
    }

    public void ExportPlacedBricks(bool ldraw)
    {
        string savePath = Settings.AppSaveDataPath + "/saves/" + world.worldData.planetSeed + "-" + world.worldData.worldCoord + "/";
        if (!Directory.Exists(savePath))
            return;

        GameObject[] placedBrickObs = GameObject.FindGameObjectsWithTag("placedBrick");
        string cmdstr = "";
        
        if(!ldraw) // save the brick data to an obfuscated file to be loaded in next time world is loaded
        {
            foreach (GameObject ob in placedBrickObs)
            {
                Vector3 pos = ob.transform.position;
                Quaternion rot = ob.transform.rotation;
                string partname = ob.name;
                if (ob.GetComponentInChildren<MeshRenderer>() == null)
                    continue;
                Material mat = ob.GetComponentInChildren<MeshRenderer>().material;

                string color = "0";
                for (int j = 0; j < brickMaterials.Length; j++)
                {
                    if (mat.name.Contains(brickMaterials[j].name))
                        color = j.ToString();
                }
                cmdstr += color + "," + pos.x + "," + pos.y + "," + pos.z + "," + rot.x + "," + rot.y + "," + rot.z + "," + rot.w + "," + partname + "*\n";
            }

            // obfuscate the text so it cannot be read easily
            //https://stackoverflow.com/questions/20010374/obfuscating-randomizing-a-string
            var bytes = System.Text.Encoding.UTF8.GetBytes(cmdstr);
            var base64 = System.Convert.ToBase64String(bytes);

            // save as binary file (obfuscated so players cannot cheat and create their own bases without "earning" the pieces)
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream;
            stream = new FileStream(savePath + "placedBricks.brx", FileMode.Create); // overwrites any existing files by default
            if (obfuscateBRXFILE)
                formatter.Serialize(stream, base64);
            else
                formatter.Serialize(stream, cmdstr);
            stream.Close();
        }
        else // save file to .ldr format to allow players to use their models in other software
        {
            cmdstr = "0 FILE placedBricks\n" +
            "0 Untitled Model\n" +
            "0 Name: placedBricks\n" +
            "0 Author: \n" +
            "0 CustomBrick\n" +
            "0 NumOfBricks: " + placedBrickObs.Length + "\n" +
            "1 16 0.000000 0.000000 0.000000 1.000000 0.000000 0.000000 0.000000 1.000000 0.000000 0.000000 0.000000 1.000000 placedBricks-submodel.ldr\n" +
            "0 NOFILE\n" +
            "0 FILE placedBricks-submodel.ldr\n" +
            "0 placedBricks-submodel.ldr\n" +
            "0 Name: placedBricks-submodel.ldr\n" +
            "0 Author: \n" +
            "0 CustomBrick\n" +
            "0 NumOfBricks: " + placedBrickObs.Length + "\n";
            foreach (GameObject ob in placedBrickObs)
            {
                
                string partname = ob.name;
                string color = GetLDrawColorNumber(ob).ToString();

                // CALCULATE 4x4 MATRIX ROTATION VALUES
                float scaleFactor = 40f;

                // brick width = 20 LDRAW UNITS LDU https://www.ldraw.org/article/218.html
                // use sup-part for part offset and get position relative to first piece
                Vector3 pos = (ob.transform.GetChild(0).transform.position - placedBrickObs[0].transform.position) * scaleFactor;

                Quaternion rot = ob.transform.rotation;
                Matrix4x4 matrix = Matrix4x4.TRS(pos, rot, new Vector3(1, 1, 1));
                string a = "0.000000";
                string b = "0.000000";
                string c = "0.000000";
                string d = "0.000000";
                string e = "1.000000";
                string f = "0.000000";
                string g = "0.000000";
                string h = "0.000000";
                string i = "0.000000";
                // VALUES BELOW CREATED AFTER COMPARING OUTPUT TO STUD.IO FILE OUTPUT
                switch (Mathf.RoundToInt(ob.transform.rotation.eulerAngles.y))
                {
                    case 0:
                        {
                            a = "1.000000";
                            c = "0.000000";
                            g = "0.000000";
                            i = "1.000000";
                            break;
                        }
                    case -90:
                        {
                            a = "0.000000";
                            c = "-1.000000";
                            g = "1.000000";
                            i = "0.000000";
                            break;
                        }
                    case 270:
                        {
                            a = "0.000000";
                            c = "-1.000000";
                            g = "1.000000";
                            i = "0.000000";
                            break;
                        }
                    case 180:
                        {
                            a = "-1.000000";
                            c = "0.000000";
                            g = "0.000000";
                            i = "-1.000000";
                            break;
                        }
                    case -180:
                        {
                            a = "-1.000000";
                            c = "0.000000";
                            g = "0.000000";
                            i = "-1.000000";
                            break;
                        }
                    case 90:
                        {
                            a = "0.000000";
                            c = "1.000000";
                            g = "-1.000000";
                            i = "0.000000";
                            break;
                        }
                    case -270:
                        {
                            a = "0.000000";
                            c = "1.000000";
                            g = "-1.000000";
                            i = "0.000000";
                            break;
                        }
                }

                //https://www.ldraw.org/article/218.html
                //FILE FORMAT = 1 <colour> x y z a b c d e f g h i <file>
                /// a b c x \
                //| d e f y |
                //| g h i z |
                //\ 0 0 0 1 /

                // BUILD COMMAND STRING
                // y component needs to be inverted since LDRAW uses a -y = up coordinate system https://www.ldraw.org/article/218.html
                cmdstr += "1" + " " + color + " " + Mathf.Round(pos.x) + " " + Mathf.Round(-pos.y) + " " + Mathf.Round(pos.z) + " " + a + " " + b + " " + c + " " + d + " " + e + " " + f + " " + g + " " + h + " " + i + " " + partname + "\n";
            }
            cmdstr += "0 NOFILE";
            FileSystemExtension.SaveStringToFile(cmdstr, savePath + "placedBricks.ldr");
        }
    }

    private int GetLDrawColorNumber(GameObject ob)
    {
        if(ob == null || ob.transform.GetChild(0).GetComponent<MeshRenderer>() == null)
                return 0;

        Material mat = ob.transform.GetChild(0).GetComponent<MeshRenderer>().material;
        int color = 0;
        for (int j = 0; j < brickMaterials.Length; j++)
        {
            if (brickMaterials[j].name + " (Instance)" == mat.name)
                color = ldrawHexValues[j];
        }
        return color;
    }

    private int GetMaterialIndex(GameObject ob)
    {
        if (ob == null || ob.transform.GetComponentInChildren<MeshRenderer>() == null)
            return 0;

        Material mat = ob.GetComponentInChildren<MeshRenderer>().material;
        int color = 0; // default is 0, if none found, return 0
        for (int j = 0; j < brickMaterials.Length; j++)
        {
            if (brickMaterials[j].name + " (Instance)" == mat.name || brickMaterials[j].name == mat.name)
            {
                color = j;
                //UnityEngine.Debug.Log(color);
            }
                
        }
        return color;
    }

    private Vector2Int GetBrickTypeAndIndex(GameObject ob)
    {
        if (ob == null)
            return new Vector2Int(0, 0);

        //bool typeAndIndexFound = false;

        Vector2Int brickTypeAndIndex = new Vector2Int(0, 0);
        for(int j = 0; j < ldrawPartsTypes.Length; j++)
        {
            for (int k = 0; k < ldrawPartsTypes[j].Length; k++)
            {
                //// tested on 2456.dat, same for generated parts but not the same for imported parts???
                //Debug.Log(ob.name);
                //Debug.Log(ldrawPartsTypes[0][11]);

                if (ob.name == ldrawPartsTypes[j][k])
                {
                    brickTypeAndIndex = new Vector2Int(j, k);
                    //typeAndIndexFound = true;
                }
            }
        }

        //if (!typeAndIndexFound)
        //    Debug.Log("brick type and index not found");

        return brickTypeAndIndex;
    }
}