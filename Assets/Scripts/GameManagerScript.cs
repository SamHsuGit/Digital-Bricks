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
            worldOb.SetActive(false); // later enabled by NetworkMenu
            NETWORK.SetActive(true);
            PlayerManagerNetwork.SetActive(true);
            LOCAL.SetActive(false);
        }
    }

    public void Setup()
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
            LDrawImporterRuntime.SetActive(true);
            world.chunkMeshColliders = true;
            world.VBOs = true;
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
