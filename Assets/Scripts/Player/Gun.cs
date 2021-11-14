using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections.Generic;

public class Gun : NetworkBehaviour
{
    public float fireRate = 2f;
    public float impactForce = 0.01f;
    public int damage = 1;
    public float range = 2f;
    public float nextTimeToFire = 0f;
    public float sphereCastRadius = 0.1f;

    public Camera fpsCam;
    public AudioSource pewpew;
    public AudioSource hitSound;
    public GameObject reticleChangeColor;
    public GameObject backgroundMask;
    public Health target;

    List<GameObject> baseModelPieces;
    InputHandler inputHandler;
    Controller controller;
    CanvasGroup backgroundMaskCanvasGroup;
    RaycastHit hit;

    private Image image;
    bool baseModelPiecesInit = false;

    private void Awake()
    {
        inputHandler = GetComponent<InputHandler>();
        image = reticleChangeColor.GetComponent<Image>();
        controller = GetComponent<Controller>();
        backgroundMaskCanvasGroup = backgroundMask.GetComponent<CanvasGroup>();
    }

    private void FixedUpdate()
    {
        if(Settings.OnlinePlay && !isLocalPlayer) return;

        target = null; // reset target
        target = FindTarget(); // get target gameObject

        
        if(World.Instance.baseOb != null && !baseModelPiecesInit) // if baseOb has been set in world script and did not already run this code
        {
            baseModelPieces = World.Instance.baseOb.GetComponent<Health>().modelPieces; // cannot run in Awake or Start as World.Instance.baseOb has not been set yet
            baseModelPiecesInit = true; // only need to run this code once
        }  

        if (Time.time >= nextTimeToFire && !controller.holdingGrab && backgroundMaskCanvasGroup.alpha == 0 && !controller.photoMode)
        {
            if (inputHandler.shoot)
            {
                nextTimeToFire = Time.time + 1f / fireRate;
                pewpew.Play();
                Shoot();
            }
        }
    }

    // if raycast hits a destructible object (with health but not this player), turn outer reticle red
    public Health FindTarget()
    {
        image.color = Color.HSVToRGB(0, 0, 50, true);

        if (!controller.isHolding) // can only hit other objects if holding a melee weapon
            return null;

        //if hit something
        if (Physics.SphereCast(fpsCam.transform.position, sphereCastRadius, fpsCam.transform.forward, out hit, range))
        {
            if (hit.transform.GetComponent<Health>() != null)
                target = hit.transform.GetComponent<Health>();

            if (target != null && target.gameObject != gameObject && target.hp != 0) // if hits a model that is not this model
            {
                image.color = Color.HSVToRGB(0, 100, 50, true); // turn reticle red
                return target;
            }
            else if(hit.transform.tag == "BaseObPiece") // else if targeting a base object
            {
                image.color = Color.HSVToRGB(0, 100, 50, true); // turn reticle red
                return null;
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

    // Server calculated shoot logic gives players the authority to change hp of other preregistered gameObjects
    public void Shoot()
    {
        if (hit.transform != null && hit.transform.tag == "BaseObPiece") // hit base object
        {
            hitSound.Play();

            for (int i = 0; i < baseModelPieces.Count; i++)
            {
                if (baseModelPieces[i] == hit.transform.gameObject)
                {
                    if (Settings.OnlinePlay)
                        CmdBreakBaseObPiece(i);
                    else
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
        BreakBaseObPiece(piece);
        //RpcBreakBaseObPiece(piece);
    }

    [ClientRpc]
    public void RpcBreakBaseObPiece(int piece)
    {
        BreakBaseObPiece(piece);
    }

    public void BreakBaseObPiece(int piece)
    {
        //gameObject.GetComponent<Health>().SpawnCopyRb(baseModelPieces[piece]);
        baseModelPieces[piece].SetActive(false);
    }
}
