using System.Collections;
using UnityEngine;
using Mirror;

public class SceneObject : NetworkBehaviour
{
    [SyncVar(hook = nameof(SetVoxel))] public int typeVoxel;
    [SyncVar(hook = nameof(SetTool))] public int typeTool;
    [SyncVar(hook = nameof(SetProjectileInt))] public int typeProjectile;
    [SyncVar(hook = nameof(SetProjectileString))] public string projectileString;
    [SyncVar(hook = nameof(SetVoxelBitInt))] public int typeVoxelBit;
    [SyncVar(hook = nameof(SetUndefinedPrfab))] public int typeUndefinedPrefab;

    public GameObject[] voxel;
    public GameObject[] tool;
    public GameObject[] projectile;
    public GameObject[] voxelBit;
    public GameObject[] undefinedPrefab;
    public Controller controller;
    int collisions = 0;

    void SetVoxel(int oldValue, int newValue)
    {
        StartCoroutine(ChangeEquipment(0, newValue));
    }

    void SetTool(int oldValue, int newValue)
    {
        StartCoroutine(ChangeEquipment(1, newValue));
    }

    void SetProjectileInt(int oldValue, int newValue)
    {
        StartCoroutine(ChangeEquipment(2, newValue));
    }

    void SetProjectileString(string oldValue, string newValue)
    {
        GameObject ob = LDrawImportRuntime.Instance.ImportLDrawOnline("projectile", newValue, Vector3.zero, false);
        projectile[0] = ob;
        StartCoroutine(ChangeEquipment(2, 0));
    }

    void SetVoxelBitInt(int oldValue, int newValue)
    {
        StartCoroutine(ChangeEquipment(3, newValue));
    }

    void SetUndefinedPrfab(int oldValue, int newValue)
    {
        StartCoroutine(ChangeEquipment(4, newValue));
    }

    // Since Destroy is delayed to the end of the current frame, we use a coroutine
    // to clear out any child objects before instantiating the new one
    IEnumerator ChangeEquipment(int type, int newEquippedItem)
    {
        while (transform.childCount > 0)
        {
            Destroy(transform.GetChild(0).gameObject);
            yield return null;
        }
        // Use the new value, not the SyncVar property value
        SetEquippedItem(type, newEquippedItem);
    }
    // SetEquippedItem is called on the client from OnChangeEquipment (above),
    // and on the server from CmdDropItem in the PlayerEquip script.
    public void SetEquippedItem(int type, int typeItem)
    {
        GameObject[] array = voxel;
        switch (type)
        {
            case 0:
                {
                    array = voxel;
                    break;
                }
            case 1:
                {
                    array = tool; // not currently used
                    break;
                }
            case 2:
                {
                    array = projectile;
                    break;
                }
            case 3:
                {
                    array = voxelBit;
                    break;
                }
            case 4: // not used right now since gameobjects cannot be passed into server commands
                {
                    array = undefinedPrefab;
                    break;
                }
        }
        
        GameObject ob = Instantiate(array[typeItem], transform.position, Quaternion.identity);

        // manually remove any unwanted -submodel objects (messy, need to improve by preventing submodel from spawning in first place)
        if (Settings.OnlinePlay)
        {
            foreach (Transform child in ob.transform)
                if (child.name.Contains("-submodel"))
                    Destroy(child.gameObject);
        }

        if (ob.GetComponent<BoxCollider>() != null)
            ob.GetComponent<BoxCollider>().enabled = true;
        ob.SetActive(true);
        if (type == 3 && ob.transform.localScale != new Vector3(2.5f, 2.5f, 2.5f)) // adjust scale for voxelBits
            ob.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        ob.transform.rotation = Quaternion.LookRotation(transform.forward); // orient forwards in direction of camera
        ob.transform.parent = transform;
    }

    private void OnCollisionEnter(Collision collision) // DESTROY VOXEL RB AFTER CERTAIN NUMBER OF COLLISIONS
    {
        if (collisions < 4) // only count a few collisions not all
        {
            collisions++;
            if (gameObject.tag == "voxelRb" && controller != null && collisions > 2) // after a few collisions break into pieces
            {
                Vector3 pos = transform.position;
                if (Settings.OnlinePlay)
                {
                    controller.CmdSpawnObject(3, typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
                    controller.CmdSpawnObject(3, typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
                    controller.CmdSpawnObject(3, typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
                    controller.CmdSpawnObject(3, typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));
                }
                else
                {
                    controller.SpawnObject(3, typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
                    controller.SpawnObject(3, typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
                    controller.SpawnObject(3, typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
                    controller.SpawnObject(3, typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));
                }
                Destroy(gameObject);
            }
        }
    }
}
