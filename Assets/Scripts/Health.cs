using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class Health : NetworkBehaviour
{
    public AudioSource death;
    public PhysicMaterial physicMaterial;

    public int minPieces = 1; // chars must have at least 1 piece
    public int maxPieces = 500; // limited based on performance of min pc spec model load time
    private int minBaseMoveSpeed = 5; // min speed
    private int maxBaseMoveSpeed = 100; // max speed
    public int jumpHungerThreshold = 160; // Minecraft jumpHungerThreshold = 40
    public int blockHungerThreshold = 320; // Minecraft blockHungerThreshold = 320
    [SyncVar(hook = nameof(UpdateHP))] public int hp; // uses hp SyncVar hook to syncronize # pieces an object has across all online players when hp value changes
    public int hpMax;
    public float piecesRbMass = 0.0001f;
    public int jumpCounter = 0;
    public int blockCounter = 0;
    public bool isAlive = false;

    private int brickCount;
    int lastPlayerPos = 0;

    Controller controller;
    PlayerVoxelCollider voxelCollider;
    GameObject ob;
    public List<GameObject> modelPieces;
    readonly SyncList<GameObject> modelPiecesSyncList = new SyncList<GameObject>();

    void Awake()
    {
        if (gameObject.GetComponent<Controller>() != null)
            controller = gameObject.GetComponent<Controller>();
        if(gameObject.GetComponent<PlayerVoxelCollider>() != null)
            voxelCollider = gameObject.GetComponent<PlayerVoxelCollider>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        if (!isAlive && modelPieces.Count != 0)
        {
            for (int i = 0; i < modelPieces.Count; i++)
                modelPiecesSyncList.Add(modelPieces[i]);
        }
    }

    private void Start()
    {
        brickCount = 0;
        modelPieces = new List<GameObject>();
        if (gameObject.layer == 10) // if this object is a single lego Piece
            brickCount = 1;
        else if (isAlive && gameObject == controller.gameObject && controller.charObIdle != null && controller.charObRun != null)
        {
            int idle = SimpleCountCheck(controller.charObIdle);
            int run = SimpleCountCheck(controller.charObRun);
            if (idle != run)
                ErrorMessage.Show("# pieces must be same for 'charIdle.ldr' and 'charRun.ldr'");
            AddToPiecesList(controller.charObIdle);
        }
        else
        {
            AddToPiecesList(gameObject);
        }

        hpMax = brickCount;
        hp = hpMax;

        if (isAlive)
        {
            voxelCollider.baseWalkSpeed = CalculateBaseMoveSpeed(hpMax); // calculate base move speed based on # pieces (already counted in health hpMax)
            controller.baseAnimRate = 9f; // animation speed is fixed regardless of # of pieces
            voxelCollider.baseSprintSpeed = 2 * voxelCollider.baseWalkSpeed;
            lastPlayerPos = Mathf.FloorToInt(gameObject.transform.position.magnitude);
        }  
    }

    int SimpleCountCheck(GameObject ob)
    {
        return ob.transform.GetChild(0).childCount;
    }

    void AddToPiecesList(GameObject ob)
    {
        if (Settings.OnlinePlay)
        {
            foreach (Transform child in ob.transform)
            {
                if (!child.name.Contains("-submodel"))
                {
                    modelPieces.Add(child.gameObject);
                    brickCount++;
                }
            }
        }
        else
        {
            if (ob.transform.GetChild(0).gameObject.name.Contains("-submodel"))
            {
                GameObject submodel = ob.transform.GetChild(0).gameObject;
                foreach (Transform child in submodel.transform)
                {
                    modelPieces.Add(child.gameObject); // only adds the children of the submodel to the pieces list
                    brickCount++;
                }
            }
            else
            {
                foreach (Transform child in ob.transform)
                {
                    modelPieces.Add(child.gameObject); // only adds the children of the submodel to the pieces list
                    brickCount++;
                }
            }
        }
    }

    public float CalculateBaseMoveSpeed(int pieces)
    {
        //return maxBaseMoveSpeed; // override to make all objects move the same speed (max speed)

        float moveSpeed;

        if (pieces > maxPieces)
            pieces = maxPieces;
        else if (pieces < minPieces)
            pieces = minPieces;

        if (!SettingsStatic.LoadedSettings.flight)
            moveSpeed = 1 * (float)(maxBaseMoveSpeed - minBaseMoveSpeed) / (maxPieces - minPieces) * pieces + minBaseMoveSpeed; // positive slope (y intercept min speed) more pieces more speed
        else
            moveSpeed = maxBaseMoveSpeed; // if flight enabled, use max speed

        if (moveSpeed < 0)
            moveSpeed = minBaseMoveSpeed;

        return moveSpeed;
    }

    private void FixedUpdate()
    {
        if (gameObject.layer == 11) // if it is a player
        {
            //Hunger(); // Disabled (causes hp issues upon spawning which breaks online multiplayer)

            // Respawn
            if (hp < 1)
            {
                if (Settings.OnlinePlay && hasAuthority)
                    CmdRespawn();
                else
                    Respawn();
            }
        }

        if (transform.position.y < -20 && hp > 0) // hurt if falling below world
        {
            if (Settings.OnlinePlay && hasAuthority)
                CmdEditSelfHealth(-1);
            if (!Settings.OnlinePlay)
                EditSelfHealth(-1);
            PlayHurtSound();
        }
    }

    public void Hunger()
    {
        // https://gaming.stackexchange.com/questions/30618/does-the-hunger-meter-decrease-at-a-constant-rate
        // Minecraft: if player walked more than 800 blocks cause hunger
        if (gameObject.transform.position.magnitude - lastPlayerPos > 800)
        {
            if (Settings.OnlinePlay && hasAuthority)
                CmdEditSelfHealth(-1);
            if (!Settings.OnlinePlay)
                EditSelfHealth(-1);
            lastPlayerPos = Mathf.FloorToInt(gameObject.transform.position.magnitude);
        }

        // causes hunger if jumpCounter > jumpHungerThreshold
        if (jumpCounter > jumpHungerThreshold)
        {
            if (Settings.OnlinePlay && hasAuthority)
                CmdEditSelfHealth(-1);
            if (!Settings.OnlinePlay)
                EditSelfHealth(-1);
            jumpCounter = 0;
        }

        // causes hunger if blockCounter > blockHungerThreshold
        if (blockCounter > blockHungerThreshold)
        {
            if (Settings.OnlinePlay && hasAuthority)
                CmdEditSelfHealth(-1);
            if (!Settings.OnlinePlay)
                EditSelfHealth(-1);
            blockCounter = 0;
        }
    }

    public void RequestEditSelfHealth(int amount) // gameObjects can only call this for their own hp
    {
        if (Settings.OnlinePlay && hasAuthority)
            CmdEditSelfHealth(amount);
        if (!Settings.OnlinePlay)
            EditSelfHealth(amount);
    }

    [Command]
    public void CmdEditSelfHealth(int amount)
    {
        EditSelfHealth(amount); // calls server to update SyncVar hp which then pushes updates to clients automatically
        RpcUpdateHP(); // after hp update on server, need to update pieces on all clients
    }

    [ClientRpc]
    public void RpcUpdateHP() // server tells all clients to update pieces based on new hp value from server
    {
        UpdateHP(hp, hp);
    }

    public void EditSelfHealth(int amount) // Server updates hp and then updates pieces
    {
        hp = hp + amount;
        UpdateHP(hp, hp);
    }

    // runs from gun script when things are shot, runs in this script when object falls below certain height
    public void UpdateHP(int oldValue, int newValue)
    {
        hp = newValue;
        controller.gameMenu.GetComponent<GameMenu>().UpdateHP();
        SetModelPieceVisibility(modelPieces);
    }

    public void PlayHurtSound()
    {
        death.Play();
    }

    void SetModelPieceVisibility(List<GameObject> modelPartsList)
    {
        if (hp > hpMax)
            hp = hpMax;
        if (hp < 0)
            hp = 0;
        if (modelPartsList.Count > 1)
        {
            for (int i = 0; i < modelPartsList.Count; i++) // for all modelParts
            {
                GameObject obToSpawn = modelPartsList[i];
                if (i >= hp && obToSpawn.GetComponent<MeshRenderer>() != null && obToSpawn.GetComponent<MeshRenderer>().enabled) // if modelPart index >= hp and not hidden, hide it
                {
                    // cannot spawn voxel bits because this code cannot run on clients, gave error cannot spawn objects without active server...
                    // cannot spawn a copy of the character model piece that was shot because cannot pass gameobject to server command CmdSpawnObject

                    // turn off components, do not disable gameobject since multiplayer networking needs a reference to the object and disabling gameobject breaks this reference!
                    if (obToSpawn.GetComponent<MeshRenderer>() != null)
                        obToSpawn.GetComponent<MeshRenderer>().enabled = false;
                    if (obToSpawn.GetComponent<BoxCollider>() != null)
                        obToSpawn.GetComponent<BoxCollider>().enabled = false;
                }
            }
        }
    }

    [Command]
    public void CmdRespawn()
    {
        RpcRespawn();
    }

    [ClientRpc]
    public void RpcRespawn()
    {
        Respawn();
    }

    public void Respawn()
    {
        death.Play();

        // Drop items out of toolbar slot
        Toolbar toolbar = controller.toolbar;
        for (int i = 1; i < toolbar.slots.Length; i++) // empty all but first slot
            toolbar.DropItemsFromSlot(i);

        // teleport player to last save point (do not destroy as it breaks the multiplayer network connection)
        int[] playerStats = SaveSystem.LoadPlayerStats(gameObject, controller.playerName, World.Instance.worldData);
        Vector3 respawnPoint = new Vector3(playerStats[0], playerStats[1], playerStats[2]);
        transform.position = respawnPoint;

        hp = hpMax;

        // turn on components again, do not disable gameobject since multiplayer networking needs a reference to the object and disabling gameobject breaks this reference!
        for (int i = 0; i < modelPieces.Count; i++)
        {
            GameObject ob = modelPieces[i];
            if (ob.GetComponent<MeshRenderer>() != null)
                ob.GetComponent<MeshRenderer>().enabled = true;
            if (ob.GetComponent<BoxCollider>() != null)
                ob.GetComponent<BoxCollider>().enabled = true;
        }
    }

}