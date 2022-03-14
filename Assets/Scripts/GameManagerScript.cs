using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;
using Mirror;

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
        if (!Settings.OnlinePlay)
            Setup();
        else
        {
            worldOb.SetActive(false); // later enabled by NetworkMenu when player selects host or join
            NETWORK.SetActive(true); // activate NetworkMenu where player selects host or join
            PlayerManagerNetwork.SetActive(true);

            LOCAL.SetActive(false); // not needed for online play (i.e. cannot do splitscreen/online play at same time)
        }
    }

    public void Setup() // called by NetworkMenu for online play
    {
        // valid gameplay modes
        // splitscreen (pc = 0, console = 1)
        // network (pc = 0, console = 1)
        // mobile singleplayer

        world = worldOb.GetComponent<World>();
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
            LDrawImporterRuntime.SetActive(true); // activated by NetworkMenu for online play
            world.chunkMeshColliders = true; // values set ahead of world gameObject activation
            world.VBOs = true; // values set ahead of world gameObject activation
        }

        if (Settings.OnlinePlay) // network online multiplayer
        {
            // order of events is important for network ids to be generated correctly

            if (Settings.Platform == 2) // mobile singleplayer network play
            {
                XRRigPrefab.SetActive(true);
                charPrefab.SetActive(false);
            }
            else // console (1) and pc (0) singleplayer network play
            {
                XRRigPrefab.SetActive(false);
                charPrefab.SetActive(false);
            }
        }
        else // local
        {
            NETWORK.SetActive(false);
            PlayerManagerNetwork.SetActive(false);

            worldOb.SetActive(true);

            if (Settings.Platform == 2) // mobile singleplayer
            {
                LOCAL.SetActive(false);
            }
            else // console (1) and pc (0) splitscreen
            {
                XRRigPrefab.SetActive(false);
                charPrefab.SetActive(false);
                playerManagerLocal.GetComponent<PlayerInputManager>().playerPrefab = charPrefab;
                LOCAL.SetActive(true);
            }
        }
    }
}
