using UnityEngine;
using UnityEngine.InputSystem;

public class GameManagerScript : MonoBehaviour
{
    public GameObject playerManagerLocal;
    public GameObject NETWORK;
    public GameObject PlayerManagerNetwork;
    public GameObject LOCAL;
    public GameObject globalLighting;
    public GameObject worldOb;
    public GameObject LDrawImporterRuntime;
    public GameObject XRRigPrefab;
    public GameObject charPrefab;

    private World world;

    void Awake()
    {
        world = worldOb.GetComponent<World>();

        // these values are set immediately and overwritten later if necessary to match server properties
        world.planetNumber = SettingsStatic.LoadedSettings.planetNumber;
        world.seed = SettingsStatic.LoadedSettings.seed;

        if (Settings.OnlinePlay)
        {
            worldOb.SetActive(false); // later enabled by CustomNetworkManager when player selects host or join
            NETWORK.SetActive(true); // activate NetworkMenu where player selects host or join
            PlayerManagerNetwork.SetActive(true);

            LOCAL.SetActive(false); // not needed for online play (i.e. cannot do splitscreen/online play at same time)
        }
        else
            Setup();
    }

    public void Setup() // called by NetworkMenu for online play
    {
        // valid gameplay modes
        // splitscreen (pc = 0, console = 1)
        // network (pc = 0, console = 1)
        // mobile singleplayer

        SettingsStatic.LoadedSettings = SettingsStatic.LoadSettings();

        if (!Settings.OnlinePlay)
            globalLighting.GetComponent<Lighting>().timeOfDay = SettingsStatic.LoadedSettings.timeOfDay;

        if (Settings.Platform == 2)
        {
            LDrawImporterRuntime.SetActive(false);
            world.chunkMeshColliders = false;
            world.VBOs = true;
        }
        else
        {
            world.chunkMeshColliders = true; // values set ahead of world gameObject activation
            world.VBOs = true; // values set ahead of world gameObject activation
        }

        if (Settings.OnlinePlay) // network online multiplayer
        {
            // order of events is important for network ids to be generated correctly

            if (Settings.Platform == 2) // mobile singleplayer network play
            {
                XRRigPrefab.SetActive(true);
            }
            else // console (1) and pc (0) singleplayer network play
            {
                world.baseOb = LDrawImportRuntime.Instance.baseOb; // value set initially right after ldraw importer actiavated, may be overridden by customNetworkManager to sync clients to server
                XRRigPrefab.SetActive(false);
            }
        }
        else // local
        {
            NETWORK.SetActive(false);
            PlayerManagerNetwork.SetActive(false);

            LDrawImporterRuntime.SetActive(true);
            globalLighting.SetActive(true);

            if (Settings.Platform == 2) // mobile singleplayer
            {
                LOCAL.SetActive(false);
            }
            else // console (1) and pc (0) splitscreen
            {
                world.baseOb = LDrawImportRuntime.Instance.baseOb;
                XRRigPrefab.SetActive(false);
                playerManagerLocal.GetComponent<PlayerInputManager>().playerPrefab = charPrefab;
                LOCAL.SetActive(true);
            }
        }
    }
}
