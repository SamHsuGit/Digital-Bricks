using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

//public enum EquippedItem: int
//{
//    v000,
//    v001,
//    v002,
//    v003,
//    v004,
//    v005,
//    v006,
//    v007,
//    v008,
//    v009,
//    v010,
//    v011,
//    v012,
//    v013,
//    v014,
//    v015,
//    v016,
//    v017,
//    v018,
//    v019,
//    v020,
//    v021,
//    v022,
//    v023,
//    v024,
//    v025,
//    v026,
//    v027,
//    v028,
//    v029,
//    v030,
//    v031,
//    v032,
//    v033,
//    v034,
//    t000,
//    t001,
//    t002,
//    t003,
//    t004,
//    t005,
//    t006,
//    t007,
//    t008,
//    t009,
//    t010,
//    t011,
//    t012,
//    t013,
//    t014,
//    t015,
//    t016,
//    t017,
//    t018,
//    t019,
//    t020,
//    t021,
//    t022,
//    t023,
//    t024,
//    t025,
//    t026,
//    t027,
//    t028,
//    t029,
//    t030,
//    t031,
//    t032,
//    t033,
//    t034,
//    t035,
//    t036,
//    t037,
//    t038,
//    t039,
//    t040,
//    t041,
//    t042,
//    t043,
//    t044,
//    t045,
//    t046,
//    t047,
//    t048,
//    t049,
//    t050,
//    t051,
//    t052,
//    t053,
//    t054,
//    t055,
//    t056,
//    t057,
//    t058,
//    t059,
//    t060,
//    t061,
//    t062,
//    t063,
//    t064,
//    t065,
//    t066,
//    t067,
//    t068,
//    t069,
//    t070,
//    t071,
//    t072,
//    t073,
//    t074,
//    t075,
//    t076,
//    t077,
//    t078,
//    t079,
//    t080,
//    t081,
//    t082,
//    t083,
//    t084,
//    t085,
//    t086,
//    t087,
//    t088,
//    t089,
//    t090,
//    t091,
//    t092,
//    t093,
//    t094,
//    t095,
//    t096,
//    p000,
//    p001,
//    p002,
//    p003,
//    a000,
//    a001,
//    a002,
//    a003,
//    a004,
//    a005,
//    a006,
//    a007,
//    a008,
//    a009,
//    a010,
//    a011,
//    a012,
//    a013,
//    a014,
//    a015,
//    a016,
//    a017,
//    a018,
//    a019,
//    a020,
//    a021,
//    a022,
//    a023,
//    a024,
//    a025,
//    a026,
//    a027,
//    a028,
//    a029,
//    a030,
//    a031,
//    a032,
//    a033,
//    a034,
//    a035,
//    a036,
//    a037,
//    a038,
//    a039,
//    a040,
//    a041,
//    a042,
//    a043,
//    a044,
//    a045,
//    a046,
//    a047,
//    h000,
//    h001,
//    h002,
//    h003,
//    h004,
//    h005,
//    h006,
//    h007,
//    h008,
//    h009,
//    h010,
//    h011,
//    h012,
//    h013,
//    h014,
//    h015,
//    h016,
//    h017,
//    h018,
//    h019,
//    h020,
//    h021,
//    h022,
//    h023,
//    h024,
//    h025,
//    h026,
//    h027,
//    h028,
//    h029,
//    h030,
//    h031,
//    h032,
//    h033,
//    h034,
//    h035,
//    h036,
//    h037,
//    h038,
//    h039,
//    h040,
//    h041,
//    h042,
//    h043,
//    h044,
//    h045,
//    h046,
//    h047,
//    h048,
//    h049,
//    h050,
//    h051,
//    h052,
//    h053,
//    h054,
//    h055,
//    h056,
//    h057,
//    h058,
//    h059,
//    h060,
//    h061,
//    h062,
//    h063,
//    h064,
//    h065,
//    h066,
//    h067,
//    h068,
//    h069,
//    h070,
//    h071,
//    h072,
//    h073,
//    h074,
//    h075,
//    h076,
//    h077,
//    h078,
//    h079,
//    h080,
//    h081,
//    h082,
//    h083,
//    h084,
//    h085,
//    h086,
//    h087,
//    h088,
//    h089,
//    h090,
//    h091,
//    h092,
//    h093,
//    h094,
//    h095,
//    h096,
//    h097,
//    h098,
//    h099,
//    h100,
//    h101,
//    h102,
//    h103,
//    h104,
//    h105,
//    h106,
//    h107,
//    h108,
//    h109,
//    h110,
//    h111,
//    h112,
//    h113,
//    h114,
//    h115,
//    h116,
//    h117,
//    h118,
//    h119,
//    h120,
//    h121,
//    h122,
//    h123,
//    h124,
//    h125,
//    h126,
//    h127,
//    h128,
//    h129,
//    h130,
//    h131,
//    h132,
//    h133,
//    h134,
//    h135,
//    h136,
//    h137,
//    h138,
//    h139,
//    h140,
//    h141,
//    h142,
//    h143,
//    h144,
//    h145,
//    h146,
//    h147,
//    h148,
//    h149,
//    h150,
//    h151,
//    h152,
//    h153,
//    h154,
//    h155,
//    h156,
//    h157,
//    h158,
//    h159,
//    h160,
//    h161,
//    h162,
//    h163,
//    h164,
//    h165,
//    h166,
//    h167,
//    h168,
//    h169,
//    h170,
//    h171,
//    h172,
//    h173,
//    h174,
//    h175,
//    h176,
//    h177,
//    h178,
//    h179,
//    h180,
//    h181,
//    h182,
//    h183,
//    h184,
//}

public class SceneObject : NetworkBehaviour
{
    [SyncVar(hook = nameof(onChangeVoxel))] public int typeVoxel;
    [SyncVar(hook = nameof(onChangeTool))] public int typeTool;
    [SyncVar(hook = nameof(onChangeProjectile))] public int typeProjectile;
    [SyncVar(hook = nameof(onChangeVoxelBit))] public int typeVoxelBit;
    [SyncVar(hook = nameof(onChangeUndefinedPrfab))] public int typeUndefinedPrefab;

    public GameObject[] voxel;
    public GameObject[] tool;
    public GameObject[] projectile;
    public GameObject[] voxelBit;
    public GameObject[] undefinedPrefab;
    public Controller controller;
    int collisions = 0;

    void onChangeVoxel(int oldValue, int newValue)
    {
        StartCoroutine(ChangeEquipment(0, newValue));
    }

    void onChangeTool(int oldValue, int newValue)
    {
        StartCoroutine(ChangeEquipment(1, newValue));
    }

    void onChangeProjectile(int oldValue, int newValue)
    {
        StartCoroutine(ChangeEquipment(2, newValue));
    }

    void onChangeVoxelBit(int oldValue, int newValue)
    {
        StartCoroutine(ChangeEquipment(3, newValue));
    }

    void onChangeUndefinedPrfab(int oldValue, int newValue)
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
            case 4:
                {
                    array = undefinedPrefab;
                    break;
                }
        }
        
        GameObject ob = Instantiate(array[typeItem], transform.position, Quaternion.identity);
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
        if(gameObject.tag == "voxelRB")
        {
            if (collisions < 4) // only count a few collisions not all
            {
                collisions++;
                if (controller != null && collisions > 2) // after a few collisions break into pieces
                {
                    Vector3 pos = transform.position;
                    controller.SpawnObject(3, typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
                    controller.SpawnObject(3, typeVoxel, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
                    controller.SpawnObject(3, typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
                    controller.SpawnObject(3, typeVoxel, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));
                    Destroy(gameObject);
                }
            }
        }
    }
}
