using Mirror;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SpatialTracking;
using UnityEngine.Animations;
public class Controller : NetworkBehaviour
{
    public Player player;

    [Header("GameObject Arrays")]
    public GameObject[] voxels;

    [SyncVar(hook = nameof(SetName))] public string playerName = "PlayerName";
    [SyncVar(hook = nameof(SetCharIdle))] public string playerCharIdleString;
    [SyncVar(hook = nameof(SetCharRun))] public string playerCharRunString;
    [SyncVar(hook = nameof(SetProjectile))] public string playerProjectileString;
    [SyncVar(hook = nameof(SetTime))] public float timeOfDay = 6.01f; // all clients use server timeOfDay which is loaded from host client
    [SyncVar] public int seed; // all clients can see server syncVar seed to check against
    [SyncVar] public string version = "0.0.0.0"; // all clients can see server syncVar version to check against
    readonly public SyncList<string> playerNames = new SyncList<string>(); // all clients can see server SyncList playerNames to check against

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

    Dictionary<Vector3, GameObject> voxelBoundObjects = new Dictionary<Vector3, GameObject>();

    Vector3 velocityPlayer;
    private World world;
    private Transform removePos;
    private Transform shootPos;
    private Transform placePos;
    private Transform holdPos;
    GameObject grabbedPrefab;

    //Components
    GameObject playerCameraOrigin;
    LookAtConstraint lookAtConstraint;
    CapsuleCollider cc;
    Rigidbody rb;
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
    GameObject hitOb;
    Rigidbody heldObRb;

    //Initializers & Constants
    float colliderHeight;
    float colliderRadius;
    Vector3 colliderCenter;
    float sphereCastRadius;
    float rotationY = 0f;
    float rotationX = 0f;
    float maxLookVelocity = 5f;
    float maxCamAngle = 90f;
    float minCamAngle = -90f;
    bool daytime = true;
    float nextTimeToAnim = 0;
    List<Material> cachedMaterials = new List<Material>();

    void Awake()
    {
        NamePlayer();

        world = World.Instance;
        physicMaterial = world.physicMaterial;
        cc = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        inputHandler = GetComponent<InputHandler>();
        health = GetComponent<Health>();
        gun = GetComponent<Gun>();
        voxelCollider = GetComponent<PlayerVoxelCollider>();
        backgroundMaskCanvasGroup = backgroundMask.GetComponent<CanvasGroup>();
        gameMenuComponent = gameMenu.GetComponent<GameMenu>();
        playerCameraOrigin = playerCamera.transform.parent.gameObject;
        lookAtConstraint = playerCamera.GetComponent<LookAtConstraint>();
        playerCameraBoxCollider = playerCamera.GetComponent<BoxCollider>();
        playerCameraVoxelCollider = playerCamera.GetComponent<PlayerVoxelCollider>();
        worldPPFXSetValues = world.GetComponent<PPFXSetValues>();
        charController = GetComponent<CharacterController>();
        customNetworkManager = World.Instance.customNetworkManager;

        health.isAlive = true;

        removePos = Instantiate(removePosPrefab).transform;
        shootPos = Instantiate(shootPosPrefab).transform;
        placePos = Instantiate(placePosPrefab).transform;
        holdPos = holdPosPrefab.transform;

        CinematicBars.SetActive(false);

        if (Settings.IsMobilePlatform)
        {
            GetComponent<XRMove>().enabled = true;
            playerCamera.transform.parent.GetComponent<TrackedPoseDriver>().enabled = true;
            world.PlayerJoined(gameObject);

            gameMenu.SetActive(true);
            playerCamera.SetActive(true);
            charModelOrigin.SetActive(false);
            nametag.SetActive(true);
            rb.isKinematic = true;
            cc.enabled = false;
            charController.enabled = false;
            inputHandler.enabled = true;
            health.enabled = false;
            gun.enabled = false;
            voxelCollider.enabled = false;
        }
        else
        {
            GetComponent<XRMove>().enabled = false;
            playerCamera.transform.parent.GetComponent<TrackedPoseDriver>().enabled = false;
            projectile = LDrawImportRuntime.Instance.projectileOb;
        }
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

        if (!Settings.OnlinePlay & !Settings.IsMobilePlatform)
        {
            timeOfDay = SettingsStatic.LoadedSettings.timeOfDay;
            // Import character model idle pose
            charObIdle = Instantiate(LDrawImportRuntime.Instance.charObIdle);
            charObIdle.SetActive(true);
            charObIdle.transform.parent = charModelOrigin.transform;
            bc = charModelOrigin.transform.GetChild(0).GetComponent<BoxCollider>();
            charObIdle.transform.localPosition = new Vector3(0, 0, 0);
            charObIdle.transform.localEulerAngles = new Vector3(0, 180, 180);

            // Import character model run pose
            charObRun = new GameObject();
            charObRun = Instantiate(LDrawImportRuntime.Instance.charObRun);
            charObRun.SetActive(false);
            charObRun.transform.parent = charModelOrigin.transform;
            charObRun.transform.localPosition = new Vector3(0, 0, 0);
            charObRun.transform.localEulerAngles = new Vector3(0, 180, 180);

            SetPlayerColliderSettings();

            SetPlayerAttributes();
            nametag.SetActive(false); // disable nametag for singleplayer/splitscreen play
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
    }

    void SetPlayerAttributes()
    {
        SetName(playerName, playerName);
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
        playerCamera.transform.parent.transform.localPosition = new Vector3(0, colliderCenter.y * 1.8f, 0);
        playerCamera.GetComponent<Camera>().nearClipPlane = 0.01f;

        // position nametag procedurally based on imported char model size
        nametag.transform.localPosition = new Vector3(0, colliderCenter.y + colliderHeight * 0.55f, 0);

        // set reach and gun range procedurally based on imported char model size
        grabDist = cc.radius * 2f * 6f;
        tpsDist = -cc.radius * 4;
    }

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

    public void SetCharIdle(string oldCharIdle, string newCharIdle)
    {
        charObIdle = LDrawImportRuntime.Instance.ImportLDrawOnline(playerName + "charIdle", newCharIdle, charModelOrigin.transform.position, false);
        charObIdle.SetActive(true);
        charObIdle.transform.parent = charModelOrigin.transform;
        bc = charModelOrigin.transform.GetChild(0).GetComponent<BoxCollider>();
        charObIdle.transform.localPosition = new Vector3(0, 0, 0);
        charObIdle.transform.localEulerAngles = new Vector3(0, 180, 180);

        SetPlayerColliderSettings();
    }

    public void SetCharRun(string oldCharRun, string newCharRun)
    {
        charObRun = LDrawImportRuntime.Instance.ImportLDrawOnline(playerName + "charRun", newCharRun, charModelOrigin.transform.position, false);
        charObRun.SetActive(false);
        charObRun.transform.parent = charModelOrigin.transform;
        charObRun.transform.localPosition = new Vector3(0, 0, 0);
        charObRun.transform.localEulerAngles = new Vector3(0, 180, 180);

        SetPlayerColliderSettings();
    }

    public void SetProjectile(string oldProjectile, string newProjectile)
    {
        projectile = LDrawImportRuntime.Instance.ImportLDrawOnline(playerName + "projectile", newProjectile, projectile.transform.position, false);
    }

    public void SetTime(float oldTime, float newTime)
    {
        timeOfDay = newTime;
    }

    public void SetIsMoving(bool oldIsMoving, bool newIsMoving)
    {
        isMoving = newIsMoving;
    }

    private void OnDestroy()
    {
        foreach(Material mat in cachedMaterials)
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

        //disable virtual camera and exit from FixedUpdate if this is not the local player
        if (Settings.OnlinePlay && !isLocalPlayer)
        {
            Animate();
            playerCamera.SetActive(false);
            return;
        }

        timeOfDay = World.Instance.globalLighting.timeOfDay; // update time of day from lighting component
        daytime = World.Instance.globalLighting.daytime;

        isGrounded = CheckGroundedCollider();

        if (Settings.IsMobilePlatform)
            camMode = 3;

        if (!options)
        {
            switch (camMode)
            {
                case 1: // FIRST PERSON CAMERA
                    {
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
                        if (charObIdle != null && !charObIdle.activeSelf && SettingsStatic.LoadedSettings.flight)
                        {
                            charObIdle.SetActive(true);
                            charObRun.SetActive(false);
                        }

                        SetDOF();
                        SetTPSDist();

                        lookAtConstraint.constraintActive = true;
                        MovePlayer();

                        if(!SettingsStatic.LoadedSettings.flight && health.hp < 50) // only animate characters with less than 50 pieces due to rendering performance issues
                            Animate();
                        break;
                    }
                case 3: // PHOTO MODE
                    {
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

    //[Command]
    //void CmdActivateChunks()
    //{
    //    RpcActivateChunks();
    //}

    //[ClientRpc]
    //void RpcActivateChunks()
    //{
    //    World.Instance.ActivateChunks();
    //}

    public void pressedShoot()
    {
        if (Time.time < gun.nextTimeToFire) // limit how fast can shoot
            return;

        if (toolbar.slotIndex == 0) // cannot do this function from first slot (creative)
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
            // spawn projectile where camera is looking
            if (Settings.OnlinePlay)
                CmdSpawnObject(2, 0, playerCamera.transform.position + playerCamera.transform.forward * colliderRadius);
            else
                SpawnObject(2, 0, playerCamera.transform.position + playerCamera.transform.forward * colliderRadius);
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
        else if (shootPos.gameObject.activeSelf) // IF SHOT WORLD (NOT HELD) VOXEL
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
            SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
            SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
            SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
            SpawnObject(3, hitObject.GetComponent<SceneObject>().typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));
        }
    }

    void SpawnVoxelRbFromWorld(Vector3 position, byte blockID)
    {
        if (blockID == 0 || blockID == 1 || blockID == 26) // if the blockID at position is air or barrier blocks, then skip to next position
            return;

        if (Settings.OnlinePlay && hasAuthority)
        {
            CmdEditVoxel(position, 0, true); // destroy voxel at position (online play)
            CmdSpawnObject(0, blockID, position);
        }
        else
        {
            EditVoxel(position, 0, true); // destroy voxel at position
            SpawnObject(0, blockID, position);
        }
    }

    // needed to empty slot with many pieces all at once instead of manually removing one by one
    public void DropItemsInSlot()
    {
        if (!options && camMode == 1 && toolbar.slotIndex != 0 && toolbar.slots[toolbar.slotIndex].HasItem) // IF NOT IN OPTIONS AND IN FPS VIEW AND ITEM IN SLOT
            toolbar.DropItemsFromSlot(toolbar.slotIndex);
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

            if (removePos.gameObject.activeSelf && hitOb.tag != "voxelRb" && hitOb.tag != "voxelBit") // IF GRABBED VOXEL CHUNK
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
            if (hitOb.GetComponent<Rigidbody>() != null) // if ob has rigidbody (and collider)
            {
                heldObRb = hitOb.GetComponent<Rigidbody>();
                heldObRb.isKinematic = false;
                heldObRb.velocity = Vector3.zero;
                heldObRb.angularVelocity = Vector3.zero;
                heldObRb.useGravity = false;
                heldObRb.detectCollisions = true;
            }
        }
        else if (toolbar.slots[toolbar.slotIndex].itemSlot.stack != null) // IF HIT COLLIDER AND TOOLBAR HAS STACK
        {
            holdingGrab = true;
            blockID = toolbar.slots[toolbar.slotIndex].itemSlot.stack.id;

            if(toolbar.slotIndex != 0) // do not reduce item count from first slot (creative)
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
        if (blockID != 0 && blockID != 1) // if block is not air or barrier block
        {
            for (int i = 1; i < toolbar.slots.Length; i++) // for all slots in toolbar except first slot
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

    [Command]
    public void CmdSpawnObject(int type, int item, Vector3 pos) // cannot pass in GameObjects to Commands... causes error
    {
        SpawnObject(type, item, pos);
    }

    public void SpawnObject(int type, int item, Vector3 pos, GameObject obToSpawn = null)
    {
        GameObject ob = Instantiate(sceneObjectPrefab, pos, Quaternion.identity);
        Rigidbody rb;

        ob.transform.rotation = Quaternion.LookRotation(playerCamera.transform.forward); // orient forwards in direction of camera
        rb = ob.GetComponent<Rigidbody>();
        rb.mass = health.piecesRbMass;
        rb.isKinematic = false;
        rb.velocity = playerCamera.transform.forward * 25; // give some velocity away from where player is looking

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
                sceneObject.projectile[0] = LDrawImportRuntime.Instance.projectileOb;
                sceneObject.typeProjectile = item; // should be 0 for first item in array
                ob.tag = "Hazard";
                sceneObject.SetEquippedItem(type, item); // set the child object on the server
                childOb = ob.transform.GetChild(1).gameObject; // get the projectile (clone) object
                GameObject deepChildOb = childOb.transform.GetChild(0).GetChild(0).gameObject; // get the deep child of the projectile object to get correct collider
                if(deepChildOb.GetComponent<BoxCollider>() != null)
                {
                    BoxCollider deepChildObBc = deepChildOb.GetComponent<BoxCollider>();
                    childOb.GetComponent<BoxCollider>().enabled = false;
                    deepChildObBc.enabled = false;
                    sceneObBc = ob.AddComponent<BoxCollider>();
                    float childScale = childOb.transform.localScale.x;
                    childOb.transform.Rotate(0, 0, 180);
                    sceneObBc.size = deepChildObBc.size * childScale;
                    sceneObBc.center = deepChildObBc.center * childScale;
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
            case 4: // IF GAMEOBJECT REFERENCE
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
                        childObBc.enabled = false;
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
        //ob.layer = 10; (was causing bugs with picking up rigidbody objects after spawned)
        Destroy(ob, 30); // clean up objects after 30 seconds
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
                undefinedPrefabToSpawn.tag = "Hazard";
                break;
        }
        GameObject ob = Instantiate(undefinedPrefabToSpawn, new Vector3(pos.x + 0.5f, pos.y + undefinedPrefabToSpawn.GetComponent<BoxCollider>().size.y / 40 + 0.5f, pos.z + 0.5f), Quaternion.identity);
        ob.transform.Rotate(new Vector3(180, 0, 0));
        ob.SetActive(true);

        if(option == 0 && ob.GetComponent<BoxCollider>() != null) // IF PROJECTILE
        {
            BoxCollider bc = ob.GetComponent<BoxCollider>();
            bc.enabled = true;
            bc.material = physicMaterial;
        }

        Rigidbody rb = ob.AddComponent<Rigidbody>();
        float mass = gameObject.GetComponent<Health>().piecesRbMass;
        rb.mass = mass;
        rb.isKinematic = false;
        rb.velocity = playerCamera.transform.forward * 25; // give some velocity away from where player is looking
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

        while (step < grabDist) // All position cursor blocks must be within same loop or causes lag where multiple loops cannot be run at same time (else use a coroutine)
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

        if (!SettingsStatic.LoadedSettings.flight && inputHandler.jump)
        {
            isGrounded = false;
            inputHandler.jump = false;
            health.jumpCounter++;
        }

        if(camMode == 1)
        {
            if (charController.enabled)
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
            if (charController.enabled)
                charController.Move(velocityPlayer); // used character controller since that was only thing found to collide with imported ldraw models

            // rotate cameraOrigin around player model (LookAtConstraint ensures camera always faces center)
            Vector2 rotation = CalculateRotation();
            playerCameraOrigin.transform.eulerAngles = new Vector3(rotation.y, rotation.x, 0f);

            if (isMoving) // if is moving
                charModelOrigin.transform.eulerAngles = new Vector3(0, playerCameraOrigin.transform.rotation.eulerAngles.y, 0); // rotate char model to face same y direction as camera
        }
        if(camMode != 3 && SettingsStatic.LoadedSettings.flight)
        {
            if (charController.enabled && inputHandler.jump)
                charController.Move(Vector3.up * 0.5f);
            if (charController.enabled && inputHandler.crouch)
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

                    playerCamera.transform.localPosition = Vector3.zero; // reset camera position
                    playerCamera.transform.eulerAngles = Vector3.zero; // reset camera rotation to face forwards
                    break;
                }
            case 2: // THIRD PERSON CAMERA MODE
                {
                    playerHUD.SetActive(false);
                    CinematicBars.SetActive(true);

                    if (Settings.OnlinePlay)
                        nametag.SetActive(false);

                    playerCameraBoxCollider.enabled = false;
                    playerCameraVoxelCollider.enabled = false;
                    charController.enabled = false;
                    charController.enabled = true;

                    playerCamera.transform.localPosition = new Vector3(0, cc.height / 4, tpsDist); // move camera behind character over shoulder
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
                    animRate = baseAnimRate * 2f;
                else
                    animRate = baseAnimRate;

                nextTimeToAnim = Time.time + 1f / animRate;
                charObIdle.SetActive(!charObIdle.activeSelf);
                charObRun.SetActive(!charObRun.activeSelf);
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