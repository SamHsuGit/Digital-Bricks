using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class Health : NetworkBehaviour
{
    public AudioSource death;
    public PhysicMaterial physicMaterial;

    public int minPieces = 1; // chars must have at least 1 piece
    public int maxPieces = 500; // limited based on performance of min pc spec model load time
    private int minBaseMoveSpeed = 1; // min speed
    private int maxBaseMoveSpeed = 5; // max speed
    public int jumpHungerThreshold = 160; // Minecraft jumpHungerThreshold = 40
    public int blockHungerThreshold = 320; // Minecraft blockHungerThreshold = 320
    [SyncVar(hook = nameof(UpdateHP))] public float hp; // uses hp SyncVar hook to syncronize # pieces an object has across all online players when hp value changes
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

        if (!isAlive)
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
        else if (isAlive)
        {
            int idle = CountPieces(controller.charObIdle);
            int run = CountPieces(controller.charObRun);
            if (idle == run)
                brickCount = idle;
            else
                ErrorMessage.Show("# pieces must be same for 'charIdle.ldr' and 'charRun.ldr'");
            AddToPiecesList(controller.charObIdle);
        }
        else
        {
            brickCount = CountPieces(gameObject);
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

    int CountPieces(GameObject _ob)
    {
        return _ob.transform.GetChild(0).childCount;
    }

    void AddToPiecesList(GameObject _ob)
    {
        GameObject submodel = _ob.transform.GetChild(0).gameObject;
        foreach(Transform child in submodel.transform)
        {
            modelPieces.Add(child.gameObject); // only adds the children of the submodel to the pieces list
        }

        //// PLAYER PIECES MUST TAGGED AS LEGO PIECE (layer 10) AND BE ACTIVE AND HAVE MESH RENDERER TO BE COUNTED TOWARDS HP
        //foreach (Transform child in _ob.transform)
        //{
        //    MeshRenderer mr = child.gameObject.GetComponent<MeshRenderer>();
        //    if(child.gameObject.layer == 10 && child.gameObject.activeSelf && mr != null && mr.enabled)
        //    {
        //        modelPieces.Add(child.gameObject); // add to list of pieces
        //    }
        //    AddToPiecesList(child.gameObject); // recursively check child of child objects if should add to pieces list
        //}
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
            moveSpeed = -1 * (float)(maxBaseMoveSpeed - minBaseMoveSpeed) / (maxPieces - minPieces) * pieces + maxBaseMoveSpeed; // negative slope (more pieces less speed)
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
        else if(gameObject.tag != "Enemy") // if not a player or enemy object
        {
            if (hp < 1)
                Destroy(gameObject);
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
    public void UpdateHP(float oldValue, float newValue)
    {
        hp = newValue;
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
                    //// spawn voxel bits
                    //Vector3 pos = obToSpawn.transform.position;
                    //controller.SpawnObject(3, 3, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
                    //controller.SpawnObject(3, 3, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
                    //controller.SpawnObject(3, 3, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
                    //controller.SpawnObject(3, 3, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));

                    if (obToSpawn.GetComponent<BoxCollider>() != null)
                    {
                        //controller.SpawnObject(4, 0, obToSpawn.transform.position, obToSpawn); // spawn a copy of the character model piece that was shot (WIP)
                        //SpawnCopyRb(obToSpawn); // (replaced)
                    }

                    // turn off components, do not disable gameobject since multiplayer networking needs a reference to the object and disabling gameobject breaks this reference!
                    if (obToSpawn.GetComponent<MeshRenderer>() != null)
                        obToSpawn.GetComponent<MeshRenderer>().enabled = false;
                    if (obToSpawn.GetComponent<BoxCollider>() != null)
                        obToSpawn.GetComponent<BoxCollider>().enabled = false;
                }
            }
        }
    }

    public void SpawnCopyRb(GameObject _obToSpawn)
    {
        // make a new object copy
        ob = Instantiate(_obToSpawn, _obToSpawn.transform);
        ob.GetComponent<MeshRenderer>().enabled = true;

        for (int j = 0; j < ob.transform.childCount; j++)// if copy of part has any children gameObjects, destroy them
            Destroy(ob.transform.GetChild(j).gameObject);

        // unparent copy from original mesh (stops anims)
        // assumes the parts are ordered such that children have smaller numbers than parent objects
        ob.transform.parent = null;

        // add various components to copied object
        Rigidbody rb = ob.AddComponent<Rigidbody>();
        rb.mass = piecesRbMass;
        BoxCollider bc = ob.AddComponent<BoxCollider>();
        bc.material = physicMaterial;
        if (Settings.OnlinePlay)
        {
            if (ob.GetComponent<NetworkIdentity>() == null)
                ob.AddComponent<NetworkIdentity>();
        }

        Destroy(ob, 30); // destroy newly created parts after 30 seconds to clean up scene
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

        // spawn player at last save point
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