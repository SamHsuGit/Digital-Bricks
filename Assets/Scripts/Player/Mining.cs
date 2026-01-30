using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class Mining : NetworkBehaviour
{
    public float fireRate = 2f;
    public float impactForce = 0.01f;
    private int damage = 1;
    public float nextTimeToFire = 0f;
    public float sphereCastRadius = 0.1f;

    public Camera fpsCam;
    public AudioSource pewpew;
    public AudioSource hitSound;
    public GameObject reticleChangeColor;
    public GameObject backgroundMask;
    public Health target;

    InputHandler inputHandler;
    Controller controller;
    CanvasGroup backgroundMaskCanvasGroup;
    public RaycastHit hit;

    private Vector3 sphereCastStart;
    private Image image;

    private void Awake()
    {
        inputHandler = GetComponent<InputHandler>();
        image = reticleChangeColor.GetComponent<Image>();
        controller = GetComponent<Controller>();
        backgroundMaskCanvasGroup = backgroundMask.GetComponent<CanvasGroup>();
    }

    // Disabled since we are using the mine button to place bricks
    private void FixedUpdate()
    {
    //    if (Settings.OnlinePlay && !isLocalPlayer) return;

    //    target = null; // reset target
    //    target = FindTarget(); // get target gameObject

    //    switch (controller.camMode)
    //    {
    //        case 1:
    //            sphereCastStart = fpsCam.transform.position;
    //            break;
    //        case 2:
    //            sphereCastStart = controller.playerCamera.transform.parent.transform.position;
    //            break;
    //        case 3:
    //            sphereCastStart = controller.playerCamera.transform.parent.transform.position;
    //            break;
    //    }

        // increment time to prevent spamming buttons
        if (Time.time >= nextTimeToFire && !controller.holdingGrab && backgroundMaskCanvasGroup.alpha == 0 && (controller.camMode == 1 || controller.camMode == 2))
        {
           if (inputHandler.mine)
           {
               nextTimeToFire = Time.time + 1f / fireRate;
               //pewpew.Play();
               //Shoot();
           }
        }
    }

    // if raycast hits a destructible object (with health but not this player), turn outer reticle red
    public Health FindTarget()
    {
        image.color = Color.HSVToRGB(0, 0, 50, true);

        //if hit something
        if (Physics.SphereCast(sphereCastStart, sphereCastRadius, fpsCam.transform.forward, out hit, controller.grabDist))
        {
            if (hit.transform.GetComponent<Health>() != null)
                target = hit.transform.GetComponent<Health>();

            if (hit.transform.tag == "BaseObPiece") // else if targeting a base object
            {
                image.color = Color.HSVToRGB(0, 100, 50, true); // turn reticle red
                return null;
            }
            else if (target != null && target.gameObject != gameObject && target.hp != 0) // if hits a model that is not this model
            {
                image.color = Color.HSVToRGB(0, 100, 50, true); // turn reticle red
                return target;
            }
            else
            {
                image.color = Color.HSVToRGB(0, 0, 50, true);
                return null;
            }
        }
        else
            return null;
    }

    // Server calculated mine logic gives players the authority to change hp of other preregistered gameObjects
    public void Shoot()
    {
        if (hit.transform != null && hit.transform.tag == "BaseObPiece") // hit base object
        {
            hitSound.Play();

            for (int i = 0; i < World.Instance.baseObPieces.Count; i++)
            {
                if (World.Instance.baseObPieces[i] == hit.transform.gameObject)
                {
                    Vector3 pos = hit.transform.position;
                    if (Settings.OnlinePlay)
                    {
                        controller.CmdSpawnObject(3, 3, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
                        controller.CmdSpawnObject(3, 3, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
                        controller.CmdSpawnObject(3, 3, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
                        controller.CmdSpawnObject(3, 3, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));
                    }
                    else
                    {
                        controller.SpawnObject(3, 3, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z + 0.25f));
                        controller.SpawnObject(3, 3, new Vector3(pos.x + -0.25f, pos.y + 0, pos.z - 0.25f));
                        controller.SpawnObject(3, 3, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z + 0.25f));
                        controller.SpawnObject(3, 3, new Vector3(pos.x + 0.25f, pos.y + 0, pos.z - 0.25f));
                    }
                    BreakBaseObPiece(i);
                }
            }
        }
        else if (target != null) // if target was found
        {
            hitSound.Play();

            if (Settings.OnlinePlay)
                CmdDamage(target);
            else
                Damage(target);
        }
    }

    [Command]
    // public function called when gun raycast hits target
    public void CmdDamage(Health target)
    {
        // player identity validation logic here
        RpcDamage(target);
    }

    [ClientRpc]
    public void RpcDamage(Health target)
    {
        Damage(target);
    }

    public void Damage(Health target)
    {
        target.hp -= damage; // only edit health on server which pushes syncVar updates to clients
        target.UpdateHP(target.hp, target.hp);
        if (target.isAlive)
            target.PlayHurtSound();
    }

    [Command]
    public void CmdBreakBaseObPiece(int piece)
    {
        RpcBreakBaseObPiece(piece);
    }

    [ClientRpc]
    public void RpcBreakBaseObPiece(int piece)
    {
        BreakBaseObPiece(piece);
    }

    public void BreakBaseObPiece(int piece)
    {
        GameObject ob = World.Instance.baseObPieces[piece];
        Destroy(ob);
    }
}
