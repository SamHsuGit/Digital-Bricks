using UnityEngine;
using UnityEngine.UI;
using Mirror;
public class Gun : NetworkBehaviour
{
    public float fireRate = 2f;
    public float impactForce = 0.01f;
    public int damage = 1;
    public float range = 100f;

    public Camera fpsCam;
    public AudioSource pewpew;
    public AudioSource hitSound;
    public GameObject reticleChangeColor;
    public GameObject backgroundMask;

    InputHandler inputHandler;
    Controller controller;
    CanvasGroup backgroundMaskCanvasGroup;

    public Health target;

    // Components
    private Image image;

    // private variables
    float nextTimeToFire = 0f;
    float sphereCastRadius = 0.1f;

    RaycastHit hit;

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

        if (Time.time >= nextTimeToFire && !controller.holding && backgroundMaskCanvasGroup.alpha == 0 && !controller.photoMode)
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

        //if hit something
        if (Physics.SphereCast(fpsCam.transform.position, sphereCastRadius, fpsCam.transform.forward, out hit, range))
        {
            if (hit.transform.GetComponent<Health>() != null)
                target = hit.transform.GetComponent<Health>();

            // if hits a model that is not this model
            if (target != null && target.gameObject != gameObject && target.hp != 0)
            {
                image.color = Color.HSVToRGB(0, 100, 50, true); // color reticle red if target found
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

    // Server calculated shoot logic gives players the authority to change hp of other gameObjects
    public void Shoot()
    {
        if (target != null) // if target was found
        {
            hitSound.Play();

            //if(Settings.OnlinePlay && !World.Instance.customNetworkManagerGameObject.GetComponent<CustomNetworkManager>().spawnPrefabs.Contains(target.gameObject))
            //    World.Instance.customNetworkManagerGameObject.GetComponent<CustomNetworkManager>().spawnPrefabs.Add(target.gameObject); // if not already registered, register target gameObject

            if (Settings.OnlinePlay)
                CmdDamage(target); // target has no valid id or network writer to transmit health?
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
}
