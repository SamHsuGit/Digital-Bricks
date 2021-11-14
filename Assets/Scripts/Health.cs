using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class Health : NetworkBehaviour
{
    // public references
    public AudioSource death;
    public PhysicMaterial physicMaterial;

    // public variables
    [SyncVar(hook = nameof(UpdateHP))] public float hp;
    public int hpMax;
    public float piecesRbMass = 0.0001f;
    public int jumpCounter = 0;
    public int blockCounter = 0;
    public bool isAlive = false;

    // private variables
    private int brickCount;
    float lavaHurtRate = 2f;
    float nextTimeToLavaHurt = 0f;
    int lastPlayerPos = 0;

    // private references
    Controller controller;
    PlayerVoxelCollider voxelCollider;
    GameObject ob;
    public List<GameObject> modelPieces;
    readonly SyncList<GameObject> modelPiecesSyncList = new SyncList<GameObject>();
    MeshRenderer mr;

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
        else
            CountPieces(gameObject);

        hpMax = brickCount;
        hp = hpMax;

        lastPlayerPos = Mathf.FloorToInt(gameObject.transform.position.magnitude);
    }

    void CountPieces(GameObject _ob) // recursively adds parts to list of parts to count as health
    {
        foreach (Transform child in _ob.transform)
        {
            // PLAYER PIECES MUST TAGGED AS LEGO PIECE AND BE ACTIVE AND HAVE MESH RENDERER TO BE COUNTED TOWARDS HP
            if (child.gameObject.layer == 10 && child.gameObject.activeSelf && child.gameObject.GetComponent<MeshRenderer>() != null)
            {
                brickCount++;
                modelPieces.Add(child.gameObject); // add to list of pieces
            }
            CountPieces(child.gameObject);
        }
    }

    private void FixedUpdate()
    {
        //UpdateHP(); // uses hp SyncVar hook to syncronize # pieces an object has across all online players when hp value changes

        if (gameObject.layer == 11) // if it is a player
        {
            Hunger();

            // Respawn
            if (hp < 1)
            {
                if (Settings.OnlinePlay && hasAuthority)
                    CmdRespawn();
                else
                    Respawn();
            }

            // only if voxelCollider component exists
            if (voxelCollider != null)
            {
                // hurt if touching lava
                if (voxelCollider.PlayerIsTouchingBlockID(5) && Time.time >= nextTimeToLavaHurt)
                {
                    nextTimeToLavaHurt = Time.time + 1f / lavaHurtRate;

                    if (Settings.OnlinePlay && hasAuthority)
                        CmdEditSelfHealth(-1);
                    if (!Settings.OnlinePlay)
                        EditSelfHealth(-1);
                    if(gameObject.layer == 11) // if it is a player
                        PlayHurtSound();
                }
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

        // Minecraft: if jumpCounter > 40 cause hunger
        if (jumpCounter > 160)
        {
            if (Settings.OnlinePlay && hasAuthority)
                CmdEditSelfHealth(-1);
            if (!Settings.OnlinePlay)
                EditSelfHealth(-1);
            jumpCounter = 0;
        }

        // Minecraft: if blockCounter > 320 cause hunger
        if (blockCounter > 320)
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
        hpLogicAlive(modelPieces);
    }

    public void PlayHurtSound()
    {
        death.Play();
    }

    void hpLogicAlive(List<GameObject> modelPartsList)
    {
        if (hp > hpMax)
            hp = hpMax;
        if (hp < 0)
            hp = 0;

        if (modelPartsList.Count > 1)
        {
            for (int i = 0; i < modelPartsList.Count; i++) // for all modelParts
            {
                if (i >= hp && modelPartsList[i].GetComponent<MeshRenderer>() != null && modelPartsList[i].GetComponent<MeshRenderer>().enabled) // if modelPart index >= hp and not hidden, hide it
                {
                    GameObject obToSpawn = modelPartsList[i];
                    SpawnCopyRb(obToSpawn);
                    mr = obToSpawn.GetComponent<MeshRenderer>();
                    mr.enabled = false;
                    if(obToSpawn.GetComponent<BoxCollider>() != null)
                    {
                        BoxCollider bc = obToSpawn.GetComponent<BoxCollider>();
                        bc.enabled = false;
                    }
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
            if(ob.GetComponent<NetworkIdentity>() == null)
                ob.AddComponent<NetworkIdentity>();
        }

        Destroy(ob, 5); // destroy newly created parts after 5 seconds to clean up scene
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
        for (int i = 0; i < toolbar.slots.Length; i++)
            toolbar.DropItemsFromSlot(i);

        // spawn player at last save point
        int[] playerStats = SaveSystem.LoadPlayerStats(gameObject, controller.playerName, World.Instance.worldData);
        Vector3 respawnPoint = new Vector3(playerStats[0], playerStats[1], playerStats[2]);
        transform.position = respawnPoint;

        hp = hpMax;

        for (int i = 0; i < modelPieces.Count; i++)
        {
            if(modelPieces[i].GetComponent<MeshRenderer>() != null)
                modelPieces[i].GetComponent<MeshRenderer>().enabled = true; // unhide all original objects
        }
    }

}

public static class TransformDeepChildExtension
{
    //Breadth-first search
    public static Transform FindDeepChild(this Transform aParent, string aName)
    {
        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(aParent);
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (c.name == aName)
                return c;
            foreach (Transform t in c)
                queue.Enqueue(t);
        }
        return null;
    }
}