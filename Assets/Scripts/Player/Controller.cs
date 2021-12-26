using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public class Controller : NetworkBehaviour
{
    public Player player;

    [Header("GameObject Arrays")]
    public GameObject[] voxels;

    int typeChar = 1;
    [SyncVar(hook = nameof(SetName))] public string playerName = "PlayerName";
    [SyncVar(hook = nameof(SetTime))] public float timeOfDay = 6.01f; // all clients use server timeOfDay which is loaded from host client
    [SyncVar] public int seed; // all clients can see server syncVar seed to check against
    [SyncVar] public string version = "0.0.0.0"; // all clients can see server syncVar version to check against
    readonly public SyncList<string> playerNames = new SyncList<string>(); // all clients can see server SyncList playerNames to check against

    [Header("Debug States")]
    [SerializeField] float collisionDamage;
    public bool isGrounded;
    public bool isMoving = false;
    public bool isSprinting;
    [SyncVar] public bool isHolding = false;
    public bool photoMode = false;
    public bool options = false;
    public float checkIncrement = 0.1f;
    public float reach = 4f;
    public float maxFocusDistance = 2f;
    public float focusDistanceIncrement = 0.03f;
    public bool holdingGrab = false;
    public byte blockID;
    [SyncVar] public int day = 1;

    [SerializeField] float lookVelocity = 1f;

    [Header("GameObject References")]
    public GameObject charOb;
    public GameObject charModelOrigin;
    public GameObject gameMenu;
    public GameObject nametag;
    public GameObject backgroundMask;
    public AudioSource brickPickUp;
    public AudioSource brickPlaceDown;
    public AudioSource brickMove;
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

    Dictionary<Vector3, GameObject> voxelBoundObjects = new Dictionary<Vector3, GameObject>();

    Vector3 velocityPlayer;
    private World world;
    private Transform removePos;
    private Transform shootPos;
    private Transform placePos;
    private Transform holdPos;
    GameObject grabbedPrefab;

    //Components
    CapsuleCollider cc;
    BoxCollider bc;
    PlayerVoxelCollider voxelCollider;
    Animator animator;
    PlayerInput playerInput;
    InputHandler inputHandler;
    Health health;
    Gun gun;
    CanvasGroup backgroundMaskCanvasGroup;
    GameMenu gameMenuComponent;
    BoxCollider playerCameraBoxCollider;
    PlayerVoxelCollider playerCameraVoxelCollider;
    PPFXSetValues worldPPFXSetValues;
    CharacterController charController;
    PhysicMaterial physicMaterial;
    CustomNetworkManager customNetworkManager;
    GameObject undefinedPrefabToSpawn;
    RaycastHit raycastHit;
    Transform grabbedOb;

    //Initializers & Constants
    float colliderHeight;
    float colliderRadius;
    Vector3 colliderCenter;
    float sphereCastRadius;
    float grabRange;
    float rotationY = 0f;
    float rotationX = 0f;
    float maxLookVelocity = 5f;
    float maxCamAngle = 90f;
    float minCamAngle = -90f;
    bool wasDaytime = true;
    bool daytime = true;
    List<Material> cachedMaterials = new List<Material>();
    int[] helmetToChangeColor;
    int[] armorToChangeColor;
    int[] toolsToChangeColor;

    void Awake()
    {
        NamePlayer();

        isHolding = false;
        ToggleLights(isHolding);

        world = World.Instance;
        physicMaterial = world.physicMaterial;
        cc = GetComponent<CapsuleCollider>();
        //animator = modelPrefab.GetComponent<Animator>();
        inputHandler = GetComponent<InputHandler>();
        health = GetComponent<Health>();
        gun = GetComponent<Gun>();
        voxelCollider = GetComponent<PlayerVoxelCollider>();
        backgroundMaskCanvasGroup = backgroundMask.GetComponent<CanvasGroup>();
        gameMenuComponent = gameMenu.GetComponent<GameMenu>();
        playerCameraBoxCollider = playerCamera.GetComponent<BoxCollider>();
        playerCameraVoxelCollider = playerCamera.GetComponent<PlayerVoxelCollider>();
        worldPPFXSetValues = world.GetComponent<PPFXSetValues>();
        charController = GetComponent<CharacterController>();
        customNetworkManager = World.Instance.customNetworkManager;
        projectile = LDrawImportRuntime.Instance.projectileOb;

        health.isAlive = true;

        removePos = Instantiate(removePosPrefab).transform;
        shootPos = Instantiate(shootPosPrefab).transform;
        placePos = Instantiate(placePosPrefab).transform;
        holdPos = holdPosPrefab.transform;

        CinematicBars.SetActive(false);

        helmetToChangeColor = new int[]
        {
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8,
            9,
            10,
            11,
            12,
            13,
            14,
            15,
            16,
            17,
            18,
            19,
            20,
            21,
            22,
            23,
            24,
            25,
            26,
            27,
            28,
            29,
            30,
            31,
            32,
            33,
            34,
            35,
            36,
            37,
            38,
            39,
            40,
            41,
            42,
            43,
            44,
            45,
            46,
            47,
            48,
            49,
            50,
            51,
            52,
            53,
            54,
            55,
            56,
            57,
            58,
            59,
            60,
            61,
            62,
            63,
            64,
            65,
            66,
            67,
            68,
            69,
            70,
            72,
            73,
            75,
            76,
            79,
            81,
            82,
            85,
            86,
            87,
            88,
            89,
            93,
            94,
            95,
            100,
            104,
            105,
            106,
            107,
            127,
            129,
            130,
            131,
            133,
            137,
            138,
            139,
            147,
            150,
            152,
            155,
            158,
            160,
            161,
            162,
            163,
            164,
            166,
            167,
            168,
            176,
        };

        armorToChangeColor = new int[]
        {
            1,
            19,
            20,
            28,
            29,
            30,
            31,
            38,
            40,
        };

        toolsToChangeColor = new int[]  // only change colors of tools in this list
        {
            34,
            39,
            40,
            75,
            76,
            77,
            91,
            92,
            93,
        };

    }

    void NamePlayer()
    {
        if (gameObject != World.Instance.worldPlayer) // Need to work out how networked players with same name get instance added to name
        {
            // set this object's name from saved settings so it can be modified by the world script when player joins
            playerName = SettingsStatic.LoadedSettings.playerName;

            player = new Player(gameObject, playerName); // set this player from world players
            World.Instance.players.Add(player);
        }
    }

    private void Start()
    {
        InputComponents();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (!Settings.OnlinePlay)
        {
            // Import character model
            charOb = LDrawImportRuntime.Instance.charOb;
            charOb.SetActive(true);
            charOb.transform.parent = charModelOrigin.transform;
            bc = charModelOrigin.transform.GetChild(0).GetComponent<BoxCollider>();
            charOb.transform.localPosition = new Vector3(0, 0, 0);
            charOb.transform.localEulerAngles = new Vector3(0, 180, 180);

            // position/size capsule collider procedurally based on imported character model
            colliderHeight = bc.size.y * LDrawImportRuntime.Instance.scale;
            colliderRadius = Mathf.Sqrt(Mathf.Pow(bc.size.x * LDrawImportRuntime.Instance.scale, 2) + Mathf.Pow(bc.size.z * LDrawImportRuntime.Instance.scale, 2)) * 0.25f;
            colliderCenter = new Vector3(0, -bc.center.y * LDrawImportRuntime.Instance.scale, 0);
            cc.center = colliderCenter;
            cc.height = colliderHeight;
            cc.radius = colliderRadius;

            // position camera procedurally based on imported character model
            playerCamera.transform.parent.transform.localPosition = new Vector3(0, colliderCenter.y * 1.8f, 0);
            playerCamera.GetComponent<Camera>().nearClipPlane = cc.radius * 0.5f;

            timeOfDay = SettingsStatic.LoadedSettings.timeOfDay;

            SetPlayerAttributes();
            nametag.SetActive(false); // disable nametag for singleplayer/splitscreen play
        }

        grabRange = 10f;

        switch (typeChar)
        {
            case 0:
                grabRange = 10f;
                break;
            case 1:
                grabRange = 10f;
                break;
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

    public override void OnStartServer()
    {
        base.OnStartServer();

        // SET SERVER VALUES FROM HOST CLIENT
        timeOfDay = SettingsStatic.LoadedSettings.timeOfDay;
        seed = SettingsStatic.LoadedSettings.seed;
        version = Application.version;
        for (int i = 0; i < World.Instance.players.Count; i++)
        {
            playerNames.Add(World.Instance.players[i].name);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // SET CLIENT SYNCVAR FROM SERVER
        SetTime(timeOfDay, timeOfDay);

        // GAME LOAD VALIDATION FOR ONLINE PLAY
        if (Application.version != version) // if client version does not match server version, show error.
            ErrorMessage.Show("Error: Version mismatch. Client game version must match host. Disconnecting Client.");

        if (SettingsStatic.LoadedSettings.seed != seed)
            ErrorMessage.Show("Error: Seed mismatch. Client seed must match host. Disconnecting Client.");

        if (isClientOnly) // check new client playername against existing server player names to ensure it is unique for savegame purposes
        {
            foreach (string name in playerNames)
            {
                if (SettingsStatic.LoadedSettings.playerName == name)
                    ErrorMessage.Show("Error: Non-Unique Player Name. Client name already exists on server. Player names must be unique. Disconnecting Client.");
            }
        }

        SetPlayerAttributes();
        //SetTypeProjectile();
    }

    void SetPlayerAttributes()
    {
        SetName(playerName, playerName);
    }

    //void SetTypeProjectile()
    //{
    //    if (typeTool == 0)
    //    {
    //        gun.range = 0f;
    //        projectile = brick1x1;
    //        typeProjectile = 1; // brick1x1
    //    }
    //    else if (typeTool >= 32 && typeTool <= 43) // shield & claws
    //    {
    //        gun.range = 2f;
    //        return;
    //    }
    //    else if (typeTool >= 55 && typeTool <= 77) // melee weapon
    //    {
    //        isMeleeWeapon = true;
    //        gun.range = 2f;
    //        return;
    //    }
    //    else if (typeTool >= 78 && typeTool <= 81) // bows
    //    {
    //        gun.range = 0f;
    //        projectile = arrow;
    //        typeProjectile = 2; // arrow
    //    }
    //    else if (typeTool >= 84 && typeTool <= 90) // laser guns
    //    {
    //        gun.range = 0f;
    //        projectile = laser;
    //        typeProjectile = 3; // laser
    //    }
    //    else if (typeTool >= 92 && typeTool <= 94) // magic wand/staff
    //    {
    //        gun.range = 0f;
    //        projectile = laser;
    //        typeProjectile = 3; // laser
    //    }
    //    else
    //    {
    //        gun.range = 0f;
    //        projectile = tool[typeTool]; // throw whatever object you are holding
    //        typeProjectile = typeTool;
    //    }
    //}

    //public void SetTypeChar(int oldValue, int newValue)
    //{
    //    typeChar = newValue;
    //}

    public void SetName(string oldName, string newName) // update the player visuals using the SyncVars pushed from the server to clients
    {
        if (playerName == null)
        {
            Debug.Log("No string found for playerName");
            return;
        }

        playerName = newName;
        nametag.GetComponent<TextMesh>().text = newName;
    }

    public void SetTime(float oldTime, float newTime)
    {
        timeOfDay = newTime;
    }
    
    //public void SetTypeHelmet(int oldValue, int newValue)
    //{
    //    typeHelmet = newValue;
    //    if(typeChar == 1 && helmet[typeHelmet] != null)
    //    {
    //        helmet[typeHelmet].SetActive(true);
    //        helmet[typeHelmet].layer = 10; // tag to be able to shoot
    //    }
    //}

    //public void SetTypeArmor(int oldValue, int newValue)
    //{
    //    typeArmor = newValue;
    //    if (typeChar == 1 && armor[typeArmor] != null)
    //    {
    //        armor[typeArmor].SetActive(true);
    //        armor[typeArmor].layer = 10; // tag to be able to shoot
    //    }   
    //}

    //public void SetTypeTool(int oldValue, int newValue)
    //{
    //    typeTool = newValue;
    //    if (typeChar == 1 && tool[typeTool] != null)
    //    {
    //        tool[typeTool].SetActive(true);
    //        tool[typeTool].layer = 10; // tag to be able to shoot
    //    }
    //}

    //public void SetColorHelmet(int oldValue, int newValue)
    //{
    //    SetColor(0, newValue);
    //}

    //public void SetColorHead(int oldValue, int newValue)
    //{
    //    SetColor(1, newValue);
    //}

    //public void SetColorArmor(int oldValue, int newValue)
    //{
    //    SetColor(2, newValue);
    //}

    //public void SetColorTorso(int oldValue, int newValue)
    //{
    //    SetColor(3, newValue);
    //}

    //public void SetColorTool(int oldValue, int newValue)
    //{
    //    SetColor(4, newValue);
    //}

    //public void SetColorArmL(int oldValue, int newValue)
    //{
    //    SetColor(5, newValue);
    //}

    //public void SetColorHandL(int oldValue, int newValue)
    //{
    //    SetColor(6, newValue);
    //}

    //public void SetColorArmR(int oldValue, int newValue)
    //{
    //    SetColor(7, newValue);
    //}

    //public void SetColorHandR(int oldValue, int newValue)
    //{
    //    SetColor(8, newValue);
    //}

    //public void SetColorBelt(int oldValue, int newValue)
    //{
    //    SetColor(9, newValue);
    //}

    //public void SetColorLegL(int oldValue, int newValue)
    //{
    //    SetColor(10, newValue);
    //}

    //public void SetColorLegR(int oldValue, int newValue)
    //{
    //    SetColor(11, newValue);
    //}

    //public void SetColor(int index, int newColor)
    //{
    //    if (index == 1 && typeHelmet == 0) // if no helmet, do not color head
    //        return;

    //    if(index == 0) // if helmet
    //    {
    //        bool match = false;
    //        for (int i = 0; i < helmetToChangeColor.Length; i++) // if not an approved helmet to change color do not change color
    //        {
    //            if (typeHelmet == helmetToChangeColor[i])
    //                match = true;
    //        }
    //        if (!match)
    //            return;
    //    }
    //    else if(index == 2) // if armor
    //    {
    //        bool match = false;
    //        for (int i = 0; i < armorToChangeColor.Length; i++) // if not an approved armor to change color do not change color
    //        {
    //            if (typeArmor == armorToChangeColor[i])
    //                match = true;
    //        }
    //        if (!match)
    //            return;
    //    }
    //    else if (index == 4) // for tool, only color child objects (if any, otherwise try and color main objects)
    //    {
    //        bool match = false;
    //        for (int i = 0; i < toolsToChangeColor.Length; i++) // if not an approved tool to change color do not change color
    //        {
    //            if (typeTool == toolsToChangeColor[i])
    //                match = true;
    //        }
    //        if (!match)
    //            return;

    //        for (int k = 0; k < playerLimbs[index].Length; k++)
    //        {
    //            if (playerLimbs[index][k].transform.childCount == 0) // if no children, color main object as normal (if possible)
    //            {
    //                Material cachedMaterial = playerLimbs[index][k].GetComponent<MeshRenderer>().material;
    //                cachedMaterials.Add(cachedMaterial);

    //                Color newCol = LDrawColors.IntToColor(newColor);
    //                newCol.a = 0.5f; // for tools newColor alpha is 50% for transparent materials
    //                cachedMaterial.color = newCol;
    //                if (cachedMaterial.IsKeywordEnabled("_EMISSION"))
    //                    cachedMaterial.SetColor("_EmissionColor", cachedMaterial.color);
    //            }
    //            else // if has children
    //            {
    //                GameObject ob = playerLimbs[index][k];
    //                foreach (Transform child in ob.transform)
    //                {
    //                    Material cachedMaterial = child.GetComponent<MeshRenderer>().material;
    //                    cachedMaterials.Add(cachedMaterial);
    //                    Color newCol = LDrawColors.IntToColor(newColor);
    //                    newCol.a = 0.5f; // for tools newColor alpha is 50% for transparent materials
    //                    cachedMaterial.color = newCol; // color children
    //                    if (cachedMaterial.IsKeywordEnabled("_EMISSION"))
    //                        cachedMaterial.SetColor("_EmissionColor", cachedMaterial.color);
    //                }
    //            }
    //        }
    //    }
    //    else // color gameObjects
    //    {
    //        for (int j = 0; j < playerLimbs[index].Length; j++)
    //        {
    //            Material cachedMaterial = playerLimbs[index][j].GetComponent<MeshRenderer>().material;
    //            cachedMaterials.Add(cachedMaterial);
    //            cachedMaterial.color = LDrawColors.IntToColor(newColor);
    //        }
    //    }
    //}

    private void OnDestroy()
    {
        foreach(Material mat in cachedMaterials)
            Destroy(mat); // Unity makes a clone of the Material every time GetComponent().material is used. Cache it and destroy it in OnDestroy to prevent a memory leak.
    }

    //private void OnControllerColliderHit(ControllerColliderHit hit)
    //{
    //    //hazards hurt player
    //    if (hit.gameObject.tag == "Hazard")
    //        health.EditSelfHealth(-1);

    //    GameObject ob = hit.collider.gameObject;
    //    // if touches a LegoPiece
    //    if (ob.GetComponent<Brick>() != null) // if is a lego brick
    //    {
    //        int obBlockID;
    //        if (System.Int32.TryParse(ob.name.Substring(6, 2), out obBlockID)) // Assumes the voxel prefabs are named with syntax: "Voxel_##"
    //        {
    //            if (blockID != 25) // cannot pickup procGen.ldr (imported VBO)
    //            {
    //                PutAwayBrick((byte)obBlockID); // try to add item to toolbar
    //                Destroy(ob); // destroy LegoPiece
    //            }
    //        }
    //    }
    //}

    private void OnCollisionEnter(Collision collision)
    {
        // hazards hurt player
        if (collision.gameObject.tag == "Hazard")
            health.EditSelfHealth(-1);
    }

    private void Update()
    {
        if (!Settings.WorldLoaded) return; // don't do anything until world is loaded

        if (!photoMode && !options)
        {
            playerHUD.SetActive(true);
            CinematicBars.SetActive(false);

            if(Settings.OnlinePlay)
                nametag.SetActive(true);
        }
        else if (photoMode && !options)
        {
            playerHUD.SetActive(false);
            CinematicBars.SetActive(true);

            if (Settings.OnlinePlay)
                nametag.SetActive(false);
        }

        if (options)
            gameMenuComponent.OnOptions();
        else if (!options)
            gameMenuComponent.ReturnToGame();
    }

    void FixedUpdate()
    {
        if (!Settings.WorldLoaded) return; // don't do anything until world is loaded

        //disable virtual camera and exit from FixedUpdate if this is not the local player
        if (Settings.OnlinePlay && !isLocalPlayer)
        {
            playerCamera.SetActive(false);
            return;
        }

        timeOfDay = World.Instance.globalLighting.timeOfDay; // update time of day from lighting component
        daytime = World.Instance.globalLighting.daytime;

        //if localplay or if online and is server, calculate current wave
        if (!Settings.OnlinePlay || (Settings.OnlinePlay && isServer))
            CalculateCurrentDay();

        isGrounded = CheckGroundedCollider();

        // if not in photo mode
        if (!photoMode)
        {
            playerCameraBoxCollider.enabled = false;
            playerCameraVoxelCollider.enabled = false;
            if (typeChar == 1)
            {
                charController.enabled = false;
                playerCamera.transform.localPosition = Vector3.zero; // reset camera to FPS view
                charController.enabled = true;
            }
        }
        else
        {
            playerCameraBoxCollider.enabled = true;
            playerCameraVoxelCollider.enabled = true;

            float focusDistance = worldPPFXSetValues.depthOfField.focusDistance.value;

            if (inputHandler.navigate.x > 0 || inputHandler.navigate.y > 0)
            {
                if (focusDistance + inputHandler.navigate.x < maxFocusDistance || focusDistance + inputHandler.navigate.y < maxFocusDistance)
                    worldPPFXSetValues.depthOfField.focusDistance.value += focusDistanceIncrement;
            }
            else if (inputHandler.navigate.x < 0 || inputHandler.navigate.y < 0)
            {
                if (focusDistance - inputHandler.navigate.x > 0 || focusDistance - inputHandler.navigate.y > 0)
                    worldPPFXSetValues.depthOfField.focusDistance.value -= focusDistanceIncrement;
            }
        }

        if (!photoMode && !options) // IF NOT IN OPTIONS OR PHOTO MODE
        {
            //bool isInActiveChunk = voxelCollider.playerChunkIsActive;
            // IF PRESSED GRAB
            if (!holdingGrab && inputHandler.grab)// && isInActiveChunk)
                PressedGrab();

            // IF HOLDING GRAB
            if (holdingGrab && inputHandler.grab)// && isInActiveChunk)
                HoldingGrab();

            // IF PRESSED SHOOT
            if (inputHandler.shoot)
                pressedShoot();

            // IF RELEASED GRAB
            if (holdingGrab && !inputHandler.grab)// && isInActiveChunk)
                ReleasedGrab();
            
            positionCursorBlocks();
            MovePlayer(); // MUST BE IN FIXED UPDATE (Causes lag if limited by update framerate)
        }
        else if (photoMode && !options)
        {
            MoveCamera(); // MUST BE IN FIXED UPDATE (Causes lag if limited by update framerate)
        }

        if (Settings.OnlinePlay)
            CmdToggleLights(isHolding);
        else
            ToggleLights(isHolding);

        //animate player
        Animate();
    }

    public void CalculateCurrentDay()
    {
        
        if (!wasDaytime && daytime) // if turns daytime
        {
            day++;
            wasDaytime = true;
        }

        if (wasDaytime && !daytime) // if turns nighttime, start next wave
        {
            wasDaytime = false;
        }
    }

    [Command]
    void CmdActivateChunks()
    {
        RpcActivateChunks();
    }

    [ClientRpc]
    void RpcActivateChunks()
    {
        World.Instance.ActivateChunks();
    }

    [Command]
    public void CmdToggleLights(bool lightsOn)
    {
        RpcToggleLights(lightsOn);
    }

    [ClientRpc]
    public void RpcToggleLights(bool lightsOn)
    {
        ToggleLights(lightsOn);
    }

    public void ToggleLights(bool lightsOn)
    {
        //foreach (GameObject ob in lightGameObjects)
        //{
        //    ob.SetActive(lightsOn); // toggle lights on/off based on state of bool
        //}
        //if (typeChar == 1 && tool[typeTool] != null)
        //    tool[typeTool].SetActive(lightsOn); // toggle tool on/off based on state of bool
    }

    public void pressedShoot()
    {
        // if not holding anything and pointing at a voxel, then spawn a voxel rigidbody at position
        if (!holdingGrab && shootPos.gameObject.activeSelf && Time.time >= gun.nextTimeToFire) // if shooting world voxels
        {
            Vector3 position = shootPos.position;
            blockID = World.Instance.GetVoxelState(position).id;

            if (blockID == 25 || blockID == 26) // cannot destroy procGen.ldr or base.ldr (imported VBO)
                return;

            shootBricks.Play();

            SpawnVoxelRbFromWorld(position, blockID);
        }
        else if (holdingGrab) // if holding spawn held ob
        {
            Vector3 position = holdPos.position;

            shootBricks.Play();

            SpawnVoxelRbFromWorld(position, blockID);

            holdingGrab = false;
            reticle.SetActive(true);

            UpdateShowGrabObject(holdingGrab, blockID);
        }
        //else if ((typeTool == 0 || isHolding) && Time.time >= gun.nextTimeToFire) // if not shooting world voxels or holding voxel and is holding weapon that is not melee type, spawn projectile
        //{
        //    int typePrefab;
        //    int type;

        //    if (typeTool == 0) // brick1x1
        //    {
        //        typePrefab = 2; // projectile
        //        type = typeProjectile;
        //    }
        //    else if (typeTool >= 32 && typeTool <= 43) // shield
        //    {
        //        return;
        //    }
        //    else if (typeTool >= 55 && typeTool <= 77) // melee weapon
        //    {
        //        return;
        //    }
        //    else if (typeTool >= 78 && typeTool <= 81) // bows
        //    {
        //        typePrefab = 2; // projectile
        //        type = typeProjectile;
        //    }
        //    else if (typeTool >= 84 && typeTool <= 90) // laser guns
        //    {
        //        typePrefab = 2; // projectile
        //        type = typeProjectile;
        //    }
        //    else if (typeTool >= 92 && typeTool <= 94) // magic wand/staff
        //    {
        //        typePrefab = 2; // projectile
        //        type = typeProjectile;
        //    }
        //    else
        //    {
        //        typePrefab = 1; // tool
        //        type = typeTool;
        //    }
        //    // spawn projectile just outside player capsule collider
        //    Vector3 position = playerCamera.transform.position + playerCamera.transform.forward * (cc.radius + 2);
        //    if (Settings.OnlinePlay)
        //        CmdSpawnPreDefinedPrefab(typePrefab, type, position);
        //    else
        //        SpawnPreDefinedPrefab(typePrefab, type, position);
        //}
    }

    void SpawnVoxelRbFromWorld(Vector3 position, byte blockID)
    {
        if (blockID == 0 || blockID == 1 || blockID == 26) // if the blockID at position is air or barrier blocks, then skip to next position
            return;

        if (Settings.OnlinePlay && hasAuthority)
        {
            CmdEditVoxel(position, 0, true); // destroy voxel at position (online play)
            CmdSpawnPreDefinedPrefab(0, blockID, position);
        }
        else
        {
            EditVoxel(position, 0, true); // destroy voxel at position
            SpawnPreDefinedPrefab(0, blockID, position);
        }
    }

    public void DropItemsInSlot()
    {
        if (!options && !photoMode && toolbar.slots[toolbar.slotIndex].HasItem) // IF NOT IN OPTIONS OR PHOTO MODE AND ITEM IN SLOT
            toolbar.DropItemsFromSlot(toolbar.slotIndex);
    }

    public void PressedGrab()
    {
        bool hitCollider = Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out raycastHit, grabRange, 11); // ignore player layer

        if (removePos.gameObject.activeSelf) // IF VOXEL PRESENT
        {
            blockID = World.Instance.GetVoxelState(removePos.position).id;
            if (blockID == 25 || blockID == 26) // cannot pickup procGen.ldr or base.ldr (imported VBO)
                return;
            holdingGrab = true;

            PickupBrick(removePos.position);
            reticle.SetActive(false);
        }
        //else if (hitCollider && raycastHit.transform.tag != "Chunk") // WIP (Do not need to be able to hold rigidbodies since they dissapear soon after spawning anyways)
        //{
        //    grabbedOb = raycastHit.transform;
        //    if (grabbedOb.gameObject.GetComponent<Rigidbody>() != null) // if has a rigidbody
        //    {
        //        Debug.Log(grabbedOb.name);
        //        grabbedOb.parent = playerCamera.transform;
        //        holdingGrab = true;
        //    }
        //}
        else if (toolbar.slots[toolbar.slotIndex].itemSlot.stack != null) // if toolbar slot has a stack
        {
            blockID = toolbar.slots[toolbar.slotIndex].itemSlot.stack.id;
            holdingGrab = true;

            TakeFromCurrentSlot(1);
            reticle.SetActive(false);
        }
        if (Settings.OnlinePlay)
            CmdUpdateGrabObject(holdingGrab, blockID);
        else
            UpdateShowGrabObject(holdingGrab, blockID);
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
        if (grabbedPrefab == null)
            return;

        if (placePos.gameObject.activeSelf)
        {
            grabbedPrefab.transform.position = placePos.position; // move instance to position where it would attach
            grabbedPrefab.transform.rotation = placePos.rotation;
            //brickMove.Play(); // Does not work for some reason
        }
        else
        {
            grabbedPrefab.transform.eulerAngles = placePos.eulerAngles;
            grabbedPrefab.transform.localPosition = new Vector3(0.5f, 0.5f, 0.5f);
            grabbedPrefab.transform.Translate(new Vector3(-0.5f, -0.5f, -0.5f));
        }
    }

    public void ReleasedGrab()
    {
        holdingGrab = false;
        if (removePos.gameObject.activeSelf && grabbedOb == null) // IF VOXEL PRESENT AND NOT HOLDING COLLIDER, PLACE VOXEL
        {
            health.blockCounter++;
            PlaceBrick(placePos.position);
        }
        //else if (grabbedOb != null) // IF HOLDING OB (WIP)
        //{
        //    grabbedOb.parent = null;
        //    grabbedOb = null;
        //}
        else // IF HOLDING VOXEL AND NOT AIMED AT VOXEL, STORE IN INVENTORY
            PutAwayBrick(blockID);

        reticle.SetActive(true);
        if (Settings.OnlinePlay)
            CmdUpdateGrabObject(holdingGrab, blockID);
        else
            UpdateShowGrabObject(holdingGrab, blockID);
    }

    void PutAwayBrick(byte blockID)
    {
        if (blockID != 0 && blockID != 1) // if block is not air or barrier block
        {
            for (int i = 0; i < toolbar.slots.Length; i++) // for all slots in toolbar
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
        if (blockID != 0 && blockID != 1) // if the stored blockID is not air or barrier block
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

    public void Use()
    {
        if (options || photoMode) // IF NOT IN OPTIONS OR PHOTO MODE
            return;
        
        Vector3 pos = removePos.position;

        byte blockID = World.Instance.GetVoxelState(pos).id;

        // else if block is mushroom, and health is not max and the selected slot has a stack 
        if (blockID == 32 && health.hp < health.hpMax)
        {
            // remove qty 1 from stack
            health.RequestEditSelfHealth(1);
            eat.Play();
            RemoveVoxel(pos);
        }
        else if (toolbar.slots[toolbar.slotIndex].HasItem && shootPos.gameObject.activeSelf && toolbar.slots[toolbar.slotIndex].itemSlot.stack.id == 30) // if has crystal, spawn projectile
        {
            // spawn projectile at shootPos
            if (Settings.OnlinePlay)
                CmdSpawnUndefinedPrefab(0, shootPos.position);
            else
                SpawnUndefinedPrefab(0, shootPos.position);

            TakeFromCurrentSlot(1);
        }
        //else if (!World.Instance.activateNewChunks) // if player presses use button and entire world not loaded
        //{
        //    if (!Settings.OnlinePlay)
        //        World.Instance.ActivateChunks(); // activate the rest of the world chunks
        //    else
        //        CmdActivateChunks();
        //}
        else
        {
            isHolding = !isHolding; // toggle lights
        }
    }

    [Command]
    public void CmdSpawnPreDefinedPrefab(int type, int item, Vector3 pos) // cannot pass in GameObjects to Commands... causes error
    {
        SpawnPreDefinedPrefab(type, item, pos);
    }

    public void SpawnPreDefinedPrefab(int type, int item, Vector3 pos)
    {
        GameObject ob = Instantiate(sceneObjectPrefab, pos, Quaternion.identity);
        Rigidbody rb;

        ob.transform.rotation = Quaternion.LookRotation(playerCamera.transform.forward); // orient forwards in direction of camera
        rb = ob.GetComponent<Rigidbody>();
        rb.mass = health.piecesRbMass;
        rb.isKinematic = false;
        rb.velocity = playerCamera.transform.forward * 25; // give some velocity away from where player is looking

        SceneObject sceneObject = ob.GetComponent<SceneObject>();
        sceneObject.SetEquippedItem(type, item); // set the child object on the server
        switch (type)
        {
            case 0:
                sceneObject.typeVoxel = item; // set the SyncVar on the scene object for clients
                break;
            //case 1:
            //    sceneObject.typeTool = item;
            //    if (typeTool >= 44 && typeTool <= 55) // throwing knives, blades, axes
            //        ob.tag = "Hazard";
            //    break;
            case 2:
                sceneObject.typeProjectile = item;
                ob.tag = "Hazard";
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
        ob.layer = 10;
        Destroy(ob, 5); // clean up objects after 5 seconds
    }

    [Command]
    public void CmdSpawnUndefinedPrefab(int option, Vector3 pos)
    {
        SpawnUndefinedPrefab(option, pos);
    }

    public void SpawnUndefinedPrefab(int option, Vector3 pos)
    {
        switch (option)
        {
            case 0:
                undefinedPrefabToSpawn = LDrawImportRuntime.Instance.projectileOb;
                break;
        }
        GameObject ob = Instantiate(undefinedPrefabToSpawn, new Vector3(pos.x + 0.5f, pos.y + undefinedPrefabToSpawn.GetComponent<BoxCollider>().size.y / 40 + 0.5f, pos.z + 0.5f), Quaternion.identity);
        ob.transform.Rotate(new Vector3(180, 0, 0));
        ob.SetActive(true);
        Rigidbody rb = ob.AddComponent<Rigidbody>();
        float mass = gameObject.GetComponent<Health>().piecesRbMass;
        rb.mass = mass;
        rb.isKinematic = false;
        ob.AddComponent<Health>();
        if (Settings.OnlinePlay)
        {
            if (ob.GetComponent<NetworkIdentity>() == null)
                ob.AddComponent<NetworkIdentity>();
            if (ob.GetComponent<NetworkTransform>() == null) // DISABLED (Chose to use Rb instead?) Network transform base error?
                ob.AddComponent<NetworkTransform>();
        }
        MeshRenderer[] mrs = ob.transform.GetComponentsInChildren<MeshRenderer>();

        int count = 0;
        for (int i = 0; i < mrs.Length; i++)
            if (mrs[i].gameObject.transform.childCount > 0)
                count++;
        ob.GetComponent<Health>().hp = count;
        rb.mass = rb.mass + 2 * mass * count;
        if (Settings.OnlinePlay)
            customNetworkManager.SpawnNetworkOb(ob); // no assetId or sceneId???
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

    public Vector3[] GetVoxelPositionsInVolume(Vector3 center, int width) // DISABLED would allow players to edit more than 1 voxel at a time causing block duplication (breaks gameplay)
    {
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

        while (step < reach) // All position cursor blocks must be within same loop or causes lag where multiple loops cannot be run at same time (else use a coroutine)
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

    //public void ReshapeCollider()
    //{
    //    if (typeChar == 0)
    //    {
    //        if (!inputHandler.sprint)
    //        {
    //            CCShapeNormal(cc, 0);
    //            voxelCollider.width = colliderRadius * 2;
    //            voxelCollider.height = colliderHeight;
    //            voxelCollider.halfColliderHeight = Mathf.Abs(cc.center.y - (cc.height / 2));
    //            if (!photoMode)
    //            {
    //                playerCamera.transform.localPosition = Vector3.zero;
    //                playerCameraVoxelCollider.enabled = true;
    //            }
    //            charController.center = cc.center;
    //            charController.radius = cc.radius;
    //            charController.height = cc.height;
    //        }
    //        else // ALT MODE
    //        {
    //            CCShapeAlternate(cc, 0);
    //            voxelCollider.width = colliderRadius * 2;
    //            voxelCollider.height = colliderRadius * 2;
    //            voxelCollider.halfColliderHeight = Mathf.Abs(cc.center.y - cc.radius);
    //            if (!photoMode)
    //            {
    //                playerCamera.transform.localPosition = new Vector3(0, -5.5f, 0);
    //                playerCameraVoxelCollider.enabled = false;
    //            }
    //            charController.center = cc.center;
    //            charController.radius = cc.radius;
    //            charController.height = cc.radius;
    //        }
    //    }

    //    switch (typeChar)
    //    {
    //        case 0:
    //            CheckObjectAbove();
    //            break;
    //        case 1:
    //            break;
    //    }
    //}

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

    //void CheckObjectAbove()
    //{
    //    RaycastHit hit;

    //    Physics.Raycast(transform.position, Vector3.up, out hit, colliderHeight * 0.5f - 0.1f);

    //    // keep the player in alt mode if there is an object above to prevent getting stuck under platforms
    //    if (hit.transform != null && hit.transform.gameObject.layer != 12)
    //        inputHandler.sprint = true;
    //}

    bool CheckGroundedCollider()
    {
        float rayLength;
        Vector3 rayStart = transform.position;

        // cast a ray starting from within the capsule collider down to just outside the capsule collider.
        rayLength = cc.height * 0.25f + 0.01f;

        if (projectile.GetComponent<BoxCollider>() != null)
        {
            rayLength = Mathf.Abs(transform.position.y - projectile.transform.position.y);
        }

        sphereCastRadius = cc.radius * 0.5f;

        switch (typeChar)
        {
            case 0:
                if (inputHandler.sprint)
                {
                    // for alt mode, we need to move the ray start down to be within the new collider volume and use the radius instead of height because the capsule was laid down
                    rayStart.y -= cc.radius;
                    rayLength = cc.radius * 0.25f + 0.01f;
                    sphereCastRadius = cc.radius * 0.5f;
                }
                break;
            case 1:
                break;
        }

        // Adjust the raycast to be slightly below the collider to allow the collider to climb small slopes
        rayLength += cc.height * 0.6f;

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

        velocityPlayer = voxelCollider.CalculateVelocity(inputHandler.move.x, inputHandler.move.y, inputHandler.sprint, inputHandler.jump);

        if (inputHandler.jump)
        {
            isGrounded = false;
            inputHandler.jump = false;
            health.jumpCounter++;
        }

        if (charController.enabled)
            charController.Move(velocityPlayer);
        //transform.Translate(velocityPlayer, Space.World);

        Vector2 rotation = CalculateRotation();
        playerCamera.transform.localEulerAngles = new Vector3(rotation.y, 0f, 0f);
        gameObject.transform.localEulerAngles = new Vector3(0f, rotation.x, 0f);
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
        if (!SettingsStatic.LoadedSettings.invertX)
            rotationX += inputHandler.look.x * lookVelocity * SettingsStatic.LoadedSettings.lookSpeed * 0.5f;
        else
            rotationX += -inputHandler.look.x * lookVelocity * SettingsStatic.LoadedSettings.lookSpeed * 0.5f;

        // rotate camera up/down, multiply by lookVelocity so controller players get look accel
        if (!SettingsStatic.LoadedSettings.invertY)
            rotationY += -inputHandler.look.y * lookVelocity * SettingsStatic.LoadedSettings.lookSpeed * 0.5f;
        else
            rotationY += inputHandler.look.y * lookVelocity * SettingsStatic.LoadedSettings.lookSpeed * 0.5f;

        // limit transform so player cannot look up or down past the specified angles
        rotationY = Mathf.Clamp(rotationY, minCamAngle, maxCamAngle);

        return new Vector2(rotationX, rotationY);
    }

    public void TogglePhotoMode()
    {
        photoMode = !photoMode;
    }

    public void ToggleOptions()
    {
        options = !options;
    }

    void Animate()
    {
        //// set animation speed of walk anim to match normalized speed of character.
        //if (!photoMode)
        //{
        //    animator.SetFloat("Speed", velocityPlayer.magnitude * 3f);
        //    animator.SetBool("isMoving", isMoving);
        //    animator.SetBool("isGrounded", voxelCollider.isGrounded);
        //}

        //switch (typeChar)
        //{
        //    case 0:
        //        animator.SetBool("isSprinting", inputHandler.sprint);
        //        break;
        //    case 1:
        //        animator.SetBool("isHolding", isHolding);
        //        if(isHolding)
        //            animator.SetBool("isMelee", inputHandler.shoot);
        //        break;
        //}
    }

    public void RequestSaveWorld()
    {
        if (Settings.OnlinePlay && hasAuthority)
        {
            CmdSaveWorld();
            SaveWorld(World.Instance.worldData);
        }
        else
            SaveWorld(World.Instance.worldData);
    }

    [Command]
    public void CmdSaveWorld() // tells the server to save the world (moved here since the gameMenu cannot have a network identity).
    {
        // server host and clients must save world before clients disconnect
        SaveWorld(World.Instance.worldData);

        RpcSaveWorld(World.Instance.worldData); // tell clients to save world so they don't have to re-share world files before playing again.
    }

    [ClientRpc]
    public void RpcSaveWorld(WorldData worldData)
    {
        // server host and clients must save world before clients disconnect
        SaveWorld(worldData);
    }

    public void SaveWorld(WorldData worldData)
    {
        SaveSystem.SaveWorld(worldData);
    }
}