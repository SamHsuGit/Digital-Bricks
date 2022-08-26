using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations;

public class Controller : NetworkBehaviour
{
    // NOTE: this class assumes world has already been activated

    public Player player;

    [Header("GameObject Arrays")]
    public GameObject[] voxels;

    [SyncVar(hook = nameof(SetName))] public string playerName = "PlayerName";
    [SyncVar(hook = nameof(SetCharIdle))] public string playerCharIdle;
    [SyncVar(hook = nameof(SetCharRun))] public string playerCharRun;
    [SyncVar(hook = nameof(SetProjectile))] public string playerProjectile;

    // Server Values (server generates these values upon start, all clients get these values from server upon connecting)
    [SyncVar(hook = nameof(SetTime))] private float timeOfDayServer;
    [SyncVar] private string versionServer;
    readonly private SyncList<string> playerNamesServer = new SyncList<string>();

    // These server values cannot be set in controller since world is activated before controller, merely included here to check states match
    [SyncVar(hook = nameof(SetPlanetNumberServer))] private int planetNumberServer;
    [SyncVar(hook = nameof(SetSeedServer))] private int seedServer;
    [SyncVar(hook = nameof(SetWorldSizeInChunksServer))] private int worldSizeInChunksServer;
    [SyncVar(hook = nameof(SetBaseServer))] private string baseServer;
    [SyncVar(hook = nameof(SaveChunks))] private string chunksServer;

    [Header("Debug States")]
    [SerializeField] float collisionDamage;
    public bool isGrounded;
    [SyncVar(hook = nameof(SetIsMoving))] public bool isMoving = false;
    public bool isSprinting;
    public bool options = false;
    public float checkIncrement = 0.1f;
    public float grabDist = 4f; // defines how far player can reach to grab/place voxels
    public float tpsDist;
    public float maxFocusDistance = 2f;
    public float focusDistanceIncrement = 0.03f;
    public bool holdingGrab = false;
    public byte blockID;
    public float baseAnimRate = 2; // health script overrides this
    public float animRate = 2; // health script overrides this
    public int camMode = 1;
    public bool setCamMode = false;

    [SerializeField] float lookVelocity = 1f;

    [Header("GameObject References")]
    public GameObject charModelOrigin;
    public GameObject gameMenu;
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

    private Dictionary<Vector3, GameObject> voxelBoundObjects = new Dictionary<Vector3, GameObject>();

    private Vector3 velocityPlayer;
    private Transform removePos;
    private Transform shootPos;
    private Transform placePos;
    private Transform holdPos;
    private GameObject grabbedPrefab;

    //Components
    private GameManagerScript gameManager;
    private GameObject playerCameraOrigin;
    private LookAtConstraint lookAtConstraint;
    private CapsuleCollider cc;
    private Rigidbody rb;
    private BoxCollider bc;
    private PlayerVoxelCollider voxelCollider;
    private Animator animator;
    private PlayerInput playerInput;
    private InputHandler inputHandler;
    private Health health;
    private Gun gun;
    private CanvasGroup backgroundMaskCanvasGroup;
    private GameMenu gameMenuComponent;
    private BoxCollider playerCameraBoxCollider;
    private PlayerVoxelCollider playerCameraVoxelCollider;
    private PPFXSetValues worldPPFXSetValues;
    private CharacterController charController;
    private PhysicMaterial physicMaterial;
    private CustomNetworkManager customNetworkManager;
    private Lighting lighting;
    private GameObject undefinedPrefabToSpawn;
    private RaycastHit raycastHit;
    private GameObject hitOb;
    private Rigidbody heldObRb;

    //Initializers & Constants
    private float colliderHeight;
    private float colliderRadius;
    private Vector3 colliderCenter;
    private Vector3 rayCastStart;
    private float sphereCastRadius;
    private float rotationY = 0f;
    private float rotationX = 0f;
    private float maxLookVelocity = 5f;
    private float maxCamAngle = 90f;
    private float minCamAngle = -90f;
    private bool daytime = true;
    private float nextTimeToAnim = 0;
    private List<Material> cachedMaterials = new List<Material>();

    // THE ORDER OF EVENTS IS CRITICAL FOR MULTIPLAYER!!!
    // Order of network events: https://docs.unity3d.com/Manual/NetworkBehaviourCallbacks.html
    // Order of SyncVars: https://mirror-networking.gitbook.io/docs/guides/synchronization/syncvars
    // The state of SyncVars is applied to game objects on clients before OnStartClient() is called, so the state of the object is always up - to - date inside OnStartClient().

    void Awake()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManagerScript>();
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
            //SaveChunks(chunksServer, chunksServer);
        }

        if (!Settings.OnlinePlay)
            world.baseOb = LDrawImportRuntime.Instance.baseOb;

        voxelCollider = GetComponent<PlayerVoxelCollider>();
        voxelCollider.world = world;
        physicMaterial = world.physicMaterial;
        worldPPFXSetValues = world.GetComponent<PPFXSetValues>();

        projectile = LDrawImportRuntime.Instance.projectileOb;
        cc = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        inputHandler = GetComponent<InputHandler>();
        health = GetComponent<Health>();
        gun = GetComponent<Gun>();
        
        backgroundMaskCanvasGroup = backgroundMask.GetComponent<CanvasGroup>();
        gameMenuComponent = gameMenu.GetComponent<GameMenu>();
        playerCameraOrigin = playerCamera.transform.parent.gameObject;
        lookAtConstraint = playerCamera.GetComponent<LookAtConstraint>();
        playerCameraBoxCollider = playerCamera.GetComponent<BoxCollider>();
        playerCameraVoxelCollider = playerCamera.GetComponent<PlayerVoxelCollider>();
        charController = GetComponent<CharacterController>();

        health.isAlive = true;

        removePos = Instantiate(removePosPrefab).transform;
        shootPos = Instantiate(shootPosPrefab).transform;
        placePos = Instantiate(placePosPrefab).transform;
        holdPos = holdPosPrefab.transform;

        CinematicBars.SetActive(false);
    }

    void NamePlayer(World world)
    {
        if (world.worldPlayer != null && gameObject != world.worldPlayer) // Need to work out how networked players with same name get instance added to name
        {
            // set this object's name from saved settings so it can be modified by the world script when player joins
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

        if (!Settings.OnlinePlay)
        {
            // Import character model idle pose
            charObIdle = Instantiate(LDrawImportRuntime.Instance.charObIdle);
            charObIdle.SetActive(true);
            charObIdle.transform.parent = charModelOrigin.transform;
            bc = charModelOrigin.transform.GetChild(0).GetComponent<BoxCollider>();
            charObIdle.transform.localPosition = new Vector3(0, 0, 0);
            charObIdle.transform.localEulerAngles = new Vector3(0, 180, 180);

            // Import character model run pose
            charObRun = Instantiate(LDrawImportRuntime.Instance.charObRun);
            charObRun.SetActive(false);
            charObRun.transform.parent = charModelOrigin.transform;
            charObRun.transform.localPosition = new Vector3(0, 0, 0);
            charObRun.transform.localEulerAngles = new Vector3(0, 180, 180);

            SetPlayerColliderSettings();
            SetName(playerName, playerName);
            nametag.SetActive(false); // disable nametag for singleplayer/splitscreen play

            world.gameObject.SetActive(true);
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
        baseServer = FileSystemExtension.ReadFileToString("base.ldr");
        versionServer = Application.version;

        SetServerChunkStringSyncVar(); // Server sends initially loaded chunks as chunkStringSyncVar to clients

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
    }

    void SetPlayerColliderSettings()
    {
        // position/size capsule collider procedurally based on imported char model size
        colliderHeight = bc.size.y * LDrawImportRuntime.Instance.scale;
        colliderRadius = Mathf.Sqrt(Mathf.Pow(bc.size.x * LDrawImportRuntime.Instance.scale, 2) + Mathf.Pow(bc.size.z * LDrawImportRuntime.Instance.scale, 2)) * 0.25f;
        colliderCenter = new Vector3(0, -bc.center.y * LDrawImportRuntime.Instance.scale, 0);
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

        // set reach and gun range procedurally based on imported char model size
        grabDist = cc.radius * 2f * 6f;
        tpsDist = -cc.radius * 4;
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

    public void SetWorldSizeInChunksServer(int oldValue, int newValue)
    {
        SettingsStatic.LoadedSettings.worldSizeInChunks = newValue;
        customNetworkManager.worldOb.GetComponent<World>().worldData.worldSizeInChunks = newValue;
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
        List<string> chunksList = SaveSystem.LoadChunkListFromFile(planetNumberServer, seedServer, worldSizeInChunksServer);
        string chunksServerCombinedString = string.Empty;
        for (int i = 0; i < chunksList.Count; i++)
        {
            chunksServerCombinedString += chunksList[i];
            chunksServerCombinedString += ';'; // has to be a single char to be able to split later on client side
        }
        chunksServer = chunksServerCombinedString;
    }

    public void SaveChunks(string oldValue, string newValue)
    {
        string[] serverChunks = newValue.Split(';'); // splits individual chunk strings using ';' char delimiter
        World world = customNetworkManager.worldOb.GetComponent<World>();

        // tell world to draw chunks from server
        for (int i = 0; i < serverChunks.Length - 1; i++) // serverChunks.Length - 1 since last item is always empty after ';' char
        {
            ChunkData chunkData = new ChunkData();
            chunkData = chunkData.DecodeChunkDataFromString(serverChunks[i]);
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
    }

    private void Update()
    {
        if (!Settings.WorldLoaded) return; // don't do anything until world is loaded

        if (options)
        {
            gameMenuComponent.OnOptions();
        }
        else if (!options)
            gameMenuComponent.ReturnToGame();

        if (setCamMode)
            SetCamMode();
    }

    void FixedUpdate()
    {
        if (!Settings.WorldLoaded) return; // don't do anything until world is loaded

        if (Settings.OnlinePlay)
            SetTimeOfDayServer();

        //disable virtual camera and exit from FixedUpdate if this is not the local player
        if (Settings.OnlinePlay && !isLocalPlayer)
        {
            Animate();
            playerCamera.SetActive(false);
            return;
        }

        daytime = lighting.daytime;

        isGrounded = CheckGroundedCollider();

        if (!options)
        {
            switch (camMode)
            {
                case 1: // FIRST PERSON CAMERA
                    {
                        rayCastStart = playerCamera.transform.position + playerCamera.transform.forward * colliderRadius * 4;

                        if (charObIdle != null && charObIdle.activeSelf)
                            charObIdle.SetActive(false);
                        if (charObRun != null && charObRun.activeSelf)
                            charObRun.SetActive(false);

                        // IF PRESSED GRAB
                        if (!holdingGrab && inputHandler.grab)
                            PressedGrab();

                        // IF HOLDING GRAB
                        if (holdingGrab && inputHandler.grab)
                            HoldingGrab();

                        // IF PRESSED SHOOT
                        if (inputHandler.shoot)
                            pressedShoot();

                        // IF RELEASED GRAB
                        if (holdingGrab && !inputHandler.grab)
                            ReleasedGrab();

                        positionCursorBlocks();

                        lookAtConstraint.constraintActive = false;
                        MovePlayer(); // MUST BE IN FIXED UPDATE (Causes lag if limited by update framerate)
                        break;
                    }
                case 2: // THIRD PERSON CAMERA
                    {
                        rayCastStart = transform.position + transform.up * colliderHeight * 0.75f + transform.forward * colliderRadius * 4;

                        if (charObIdle != null && !charObIdle.activeSelf && SettingsStatic.LoadedSettings.creativeMode)
                        {
                            charObIdle.SetActive(true);
                            charObRun.SetActive(false);
                        }

                        SetDOF();
                        SetTPSDist();

                        //// IF PRESSED GRAB
                        //if (!holdingGrab && inputHandler.grab)
                        //    PressedGrab();

                        //// IF HOLDING GRAB
                        //if (holdingGrab && inputHandler.grab)
                        //    HoldingGrab();

                        // IF PRESSED SHOOT
                        if (inputHandler.shoot)
                            pressedShoot();

                        //// IF RELEASED GRAB
                        //if (holdingGrab && !inputHandler.grab)
                        //    ReleasedGrab();

                        //positionCursorBlocks();

                        lookAtConstraint.constraintActive = true;
                        MovePlayer();

                        if (!SettingsStatic.LoadedSettings.creativeMode && health.hp < 50) // only animate characters with less than 50 pieces due to rendering performance issues
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
                        rayCastStart = playerCamera.transform.position + playerCamera.transform.forward * colliderRadius * 4;

                        if (charObIdle != null && !charObIdle.activeSelf)
                            charObIdle.SetActive(true);
                        if (charObRun != null && charObRun.activeSelf)
                            charObRun.SetActive(false);

                        SetDOF();

                        lookAtConstraint.constraintActive = false;
                        MoveCamera(); // MUST BE IN FIXED UPDATE (Causes lag if limited by update framerate)
                        break;
                    }
            }
        }
    }

    void SetTPSDist()
    {
        if (inputHandler.scrollWheel.y > 0)
            tpsDist++;
        if (inputHandler.scrollWheel.y < 0)
            tpsDist--;
        if (tpsDist >= 0)
            tpsDist = -1;
        playerCamera.transform.localPosition = new Vector3(0, cc.height / 4, tpsDist);
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

    public void pressedShoot()
    {
        if (Time.time < gun.nextTimeToFire) // limit how fast can shoot
            return;

        if (SettingsStatic.LoadedSettings.creativeMode && toolbar.slotIndex == 0) // cannot do this function from first slot if in creative mode
            return;

        // if has mushroom, and health is not max and the selected slot has a stack
        if (toolbar.slots[toolbar.slotIndex].HasItem && toolbar.slots[toolbar.slotIndex].itemSlot.stack.id == 32 && health.hp < health.hpMax)
        {
            // remove qty 1 from stack
            health.RequestEditSelfHealth(1);
            eat.Play();
            TakeFromCurrentSlot(1);
        }
        else if (toolbar.slots[toolbar.slotIndex].HasItem && toolbar.slots[toolbar.slotIndex].itemSlot.stack.id == 30) // if has crystal, spawn projectile
        {
            if(Settings.OnlinePlay)
                CmdSpawnObject(2, 0, rayCastStart);
            else
                SpawnObject(2, 0, rayCastStart);
            TakeFromCurrentSlot(1);
        }
        else if (holdingGrab) // IF HOLDING SOMETHING
        {
            holdingGrab = false;
            reticle.SetActive(true);

            if (grabbedPrefab != null) // IF HOLDING VOXEL
            {
                Vector3 position = holdPos.position;

                shootBricks.Play();

                SpawnVoxelRbFromWorld(position, blockID);

                UpdateShowGrabObject(holdingGrab, blockID);
            }
            else if (heldObRb != null) // IF HOLDING NON-VOXEL RB
            {
                heldObRb.velocity = playerCamera.transform.forward * 25; // give some velocity forwards
            }

            if (heldObRb != null)
            {
                heldObRb.useGravity = true;
                heldObRb.detectCollisions = true;
            }
            hitOb = null;
            heldObRb = null;
        }
        else if (shootPos.gameObject.activeSelf && camMode == 1) // IF SHOT WORLD (NOT HELD) VOXEL (only destroy world in fps camMode)
        {
            Vector3 position = shootPos.position;
            blockID = World.Instance.GetVoxelState(position).id;

            if (blockID == 25 || blockID == 26) // cannot destroy procGen.ldr or base.ldr (imported VBO)
                return;

            shootBricks.Play();

            SpawnVoxelRbFromWorld(position, blockID); // if not holding anything and pointing at a voxel, then spawn a voxel rigidbody at position
        }
        else if (gun.hit.transform != null && gun.hit.transform.gameObject.tag == "voxelRb") // IF SHOT VOXELRB SITTING IN WORLD, DESTROY IT
        {
            GameObject hitObject = gun.hit.transform.gameObject;
            Destroy(gun.hit.transform.gameObject);
            Vector3 pos = hitObject.transform.position;
            if (Settings.OnlinePlay)
            {
                CmdSpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
                CmdSpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
                CmdSpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
                CmdSpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));
            }
            else
            {
                SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
                SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
                SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
                SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));
            }
        }
    }

    void SpawnVoxelRbFromWorld(Vector3 position, byte blockID)
    {
        if (!World.Instance.IsGlobalPosInsideBorder(position) || blockID == 0 || blockID == 1 || blockID == 25 || blockID == 26) // if the blockID at position is air, barrier, base, procGenVBO, then skip to next position
            return;

        EditVoxel(position, 0, true); // destroy voxel at position
        if (Settings.OnlinePlay)
            CmdSpawnObject(0, blockID, position);
        else
            SpawnObject(0, blockID, position);
    }

    
    public void DropItemsInSlot()
    {
        if (SettingsStatic.LoadedSettings.creativeMode && toolbar.slotIndex == 0) // cannot run this function if creative mode and first slot selected
            return;

        if (!options && camMode == 1 && toolbar.slots[toolbar.slotIndex].HasItem) // IF NOT IN OPTIONS AND IN FPS VIEW AND ITEM IN SLOT
        {
            // this function is needed to able to empty slot with many pieces all at once (otherwise players would need to manually remove blocks one at a time)
            toolbar.DropItemsFromSlot(toolbar.slotIndex);
        }
    }

    public void PressedGrab()
    {
        if (Time.time < gun.nextTimeToFire) // cannot grab right after shooting
            return;

        bool hitCollider = Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out raycastHit, grabDist, 11); // ignore player layer
        if (hitCollider) // IF HIT COLLIDER (can be rb or chunk)
        {
            hitOb = raycastHit.transform.gameObject;
            holdingGrab = true;

            if (hitOb.GetComponent<Rigidbody>() != null) // if ob has rigidbody (and collider)
            {
                heldObRb = hitOb.GetComponent<Rigidbody>();
                heldObRb.isKinematic = false;
                heldObRb.velocity = Vector3.zero;
                heldObRb.angularVelocity = Vector3.zero;
                heldObRb.useGravity = false;
                heldObRb.detectCollisions = true;
            }
            else if (removePos.gameObject.activeSelf && hitOb.tag != "voxelRb" && hitOb.tag != "voxelBit") // IF GRABBED VOXEL CHUNK
            {
                blockID = World.Instance.GetVoxelState(removePos.position).id;
                if (blockID == 25 || blockID == 26) // cannot pickup procGen.ldr or base.ldr (imported VBO)
                    return;

                PickupBrick(removePos.position);
                reticle.SetActive(false);

                if (Settings.OnlinePlay)
                    CmdUpdateGrabObject(holdingGrab, blockID);
                else
                    UpdateShowGrabObject(holdingGrab, blockID);
            }
        }
        else if (toolbar.slots[toolbar.slotIndex].itemSlot.stack != null) // IF HIT COLLIDER AND TOOLBAR HAS STACK
        {
            holdingGrab = true;
            blockID = toolbar.slots[toolbar.slotIndex].itemSlot.stack.id;

            if (SettingsStatic.LoadedSettings.creativeMode && toolbar.slotIndex == 0) // do not reduce item count from first slot (creative)
                TakeFromCurrentSlot(0);
            else
                TakeFromCurrentSlot(1);
            reticle.SetActive(false);

            if (Settings.OnlinePlay)
                CmdUpdateGrabObject(holdingGrab, blockID);
            else
                UpdateShowGrabObject(holdingGrab, blockID);
        }
    }

    [Command]
    void CmdUpdateGrabObject(bool holding, byte blockID)
    {
        RpcUpdateGrabObject(holding, blockID);
    }

    [ClientRpc]
    void RpcUpdateGrabObject(bool holding, byte blockID)
    {
        UpdateShowGrabObject(holding, blockID);
    }

    void UpdateShowGrabObject(bool holding, byte blockID)
    {
        if (holding)
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

        if(grabbedPrefab != null || (hitOb != null && hitOb.tag == "voxelRb")) // IF HOLDING VOXEL OR VOXEL RB
        {
            if (hitOb != null && hitOb.tag == "voxelRb") // If voxel Rb
            {
                blockID = (byte)hitOb.GetComponent<SceneObject>().typeVoxel; //determine type of voxel to store back in inventory
                Destroy(hitOb); // destroy the gameobject as it has been 'stored' in inventory

                //reset heldOb and heldObRb properties
                heldObRb.useGravity = true;
                heldObRb.detectCollisions = true;
                hitOb = null;
                heldObRb = null;
            }
            else
            {
                if (Settings.OnlinePlay)
                    CmdUpdateGrabObject(holdingGrab, blockID);
                else
                    UpdateShowGrabObject(holdingGrab, blockID);
            }

            if (removePos.gameObject.activeSelf) // IF VOXEL PRESENT, PLACE VOXEL
            {
                health.blockCounter++;
                PlaceBrick(placePos.position);
            }
            else // IF HOLDING VOXEL AND NOT AIMED AT VOXEL, STORE IN INVENTORY
                PutAwayBrick(blockID);
        }
        else if (hitOb != null && heldObRb != null) // IF HOLDING NON-VOXEL OBJECT
        {
            //reset heldOb and heldObRb properties
            heldObRb.useGravity = true;
            heldObRb.detectCollisions = true;
            hitOb = null;
            heldObRb = null;
        }
    }

    void PutAwayBrick(byte blockID)
    {
        int firstSlot;
        if (SettingsStatic.LoadedSettings.creativeMode) // determine first slot
            firstSlot = 1;
        else
            firstSlot = 0;

        if (blockID != 0 && blockID != 1) // if block is not air or barrier block
        {
            for (int i = firstSlot; i < toolbar.slots.Length; i++) // for all slots in toolbar
            {
                if (toolbar.slots[i].itemSlot.stack != null && toolbar.slots[i].itemSlot.stack.id == blockID) // if toolbar slot has a stack and toolbar stack id matches highlighted block id
                {
                    if (toolbar.slots[i].itemSlot.stack.amount < 64) // limit stack size to 64 items
                    {
                        // edit voxel, give to toolbar, play sound
                        toolbar.slots[i].itemSlot.Give(1);
                        return;
                    }
                }
            }

            for (int j = 0; j < toolbar.slots.Length; j++) // for all stacks in toolbar
            {
                if (toolbar.slots[j].itemSlot.stack == null) // if there is an empty slot
                {
                    // insert a new stack with qty 1 of blockID
                    ItemStack stack = new ItemStack(blockID, 1);
                    toolbar.slots[j].itemSlot.InsertStack(stack);
                    return;
                }
            }

            // if made it here, toolbar has no empty slots to put voxels into so shoot held voxel off into space
            pressedShoot();
        }
    }

    void PickupBrick(Vector3 pos)
    {
        if (blockID == 30)
            crystal.Play();
        else if (blockID == 32)
            mushroom.Play();

        if (blockID != 0 && blockID != 1) // if block is not air or barrier block
        {
            // remove voxel, play sound
            RemoveVoxel(pos);
            brickPickUp.Play();
        }
    }

    void PlaceBrick(Vector3 pos)
    {
        if (!World.Instance.CheckForVoxel(placePos.position))
        {
            if (blockID == 30)
                crystal.Play();
            else if (blockID == 32)
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
        toolbar.slots[toolbar.slotIndex].itemSlot.Take(amount);

        // if after removing qty 1 from stack, qty = 0, then remove the stack from the slot
        if (toolbar.slots[toolbar.slotIndex].itemSlot.stack.amount == 0)
            toolbar.slots[toolbar.slotIndex].itemSlot.EmptySlot();
    }

    public void CmdSpawnObject(int type, int item, Vector3 pos)
    {
        SpawnObject(type, item, pos);
    }

    public void SpawnObject(int type, int item, Vector3 pos, GameObject obToSpawn = null)
    {
        Vector3 spawnDir;
        if (camMode == 1) // first person camera spawn object in direction camera
            spawnDir = playerCamera.transform.forward;
        else // all other camera modes, spawn object in direction of playerObject
            spawnDir = transform.forward;

        GameObject ob = Instantiate(sceneObjectPrefab, pos, Quaternion.identity);
        Rigidbody rb;

        ob.transform.rotation = Quaternion.LookRotation(spawnDir); // orient forwards in direction of camera
        rb = ob.GetComponent<Rigidbody>();
        rb.mass = health.piecesRbMass;
        rb.isKinematic = false;
        rb.velocity = spawnDir * 25; // give some velocity away from where player is looking

        SceneObject sceneObject = ob.GetComponent<SceneObject>();
        GameObject childOb;
        BoxCollider sceneObBc;
        switch (type)
        {
            case 0: // IF VOXEL
                sceneObject.SetEquippedItem(type, item); // set the child object on the server
                sceneObject.typeVoxel = item; // set the SyncVar on the scene object for clients
                BoxCollider VoxelBc = ob.AddComponent<BoxCollider>();
                VoxelBc.material = physicMaterial;
                VoxelBc.center = new Vector3(0.5f, 0.5f, 0.5f);
                ob.tag = "voxelRb";
                sceneObject.controller = this;
                break;
            case 2: // IF PROJECTILE
                if (Settings.OnlinePlay)
                    sceneObject.projectileString = playerProjectile;
                else
                    sceneObject.projectile[0] = projectile;
                sceneObject.typeProjectile = item; // should be 0 for first item in array
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
            case 3: // IF VOXEL BIT
                sceneObject.SetEquippedItem(type, item); // set the child object on the server
                sceneObject.typeVoxelBit = item;
                BoxCollider voxelBitBc = ob.AddComponent<BoxCollider>();
                voxelBitBc.material = physicMaterial;
                voxelBitBc.center = new Vector3(0, -.047f, 0);
                voxelBitBc.size = new Vector3(0.5f, 0.3f, 0.5f);
                ob.tag = "voxelBit";
                break;
            case 4: // IF GAMEOBJECT REFERENCE (WIP cannot pass gameobject reference into server commands, so this is not used for now)
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
        if (oldBlockID == 1) // cannot place barrier blocks
            return;

        World.Instance.GetChunkFromVector3(position).EditVoxel(position, id);

        Vector3 centeredPosition = new Vector3(position.x + 0.5f, position.y + 0.5f, position.z + 0.5f);
        if (remove)
        {
            if (voxelBoundObjects.TryGetValue(centeredPosition, out _))
            {
                Destroy(voxelBoundObjects[centeredPosition]);
                voxelBoundObjects.Remove(centeredPosition);
            }
        }
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

            if (world.CheckForVoxel(pos))
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
        if (inputHandler.move != Vector2.zero)
            isMoving = true;
        else
            isMoving = false;

        if (inputHandler.sprint)
            isSprinting = true;
        else
            isSprinting = false;

        velocityPlayer = voxelCollider.CalculateVelocity(inputHandler.move.x, inputHandler.move.y, isSprinting, inputHandler.jump);

        if (!SettingsStatic.LoadedSettings.creativeMode && inputHandler.jump)
        {
            isGrounded = false;
            inputHandler.jump = false;
            health.jumpCounter++;
        }

        if(camMode == 1)
        {
            if (charController.enabled && World.Instance.IsGlobalPosInsideBorder(transform.position + velocityPlayer)) // keep player inside world borders
                charController.Move(velocityPlayer); // used character controller since that was only thing found to collide with imported ldraw models

            Vector2 rotation = CalculateRotation();
            playerCamera.transform.localEulerAngles = new Vector3(rotation.y, 0f, 0f);
            gameObject.transform.localEulerAngles = new Vector3(0f, rotation.x, 0f);
        }
        else if(camMode == 2)
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
        if(camMode != 3 && SettingsStatic.LoadedSettings.creativeMode)
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
            else
            {
                lookVelocity += SettingsStatic.LoadedSettings.lookAccel;
            }
        }
        else
            lookVelocity = 1f;

        // rotate camera left/right, multiply by lookVelocity so controller players get look accel
        rotationX += inputHandler.look.x * lookVelocity * SettingsStatic.LoadedSettings.lookSpeed * 0.5f;

        // rotate camera up/down, multiply by lookVelocity so controller players get look accel
        if (!SettingsStatic.LoadedSettings.invertY)
            rotationY += -inputHandler.look.y * lookVelocity * SettingsStatic.LoadedSettings.lookSpeed * 0.5f;
        else
            rotationY += inputHandler.look.y * lookVelocity * SettingsStatic.LoadedSettings.lookSpeed * 0.5f;

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
        options = !options;
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
    }
}