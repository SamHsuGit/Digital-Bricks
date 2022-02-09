using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.InputSystem;
using Mirror;

public class GameManagerScript : MonoBehaviour
{
    public GameObject[] playerPrefabs;
    public GameObject playerManagerLocal;
    public GameObject NETWORK;
    public GameObject PlayerManagerNetwork;
    public GameObject LOCAL;
    public GameObject globalLighting;
    public GameObject worldOb;
    public GameObject LDrawImporterRuntime;
    private World world;

    void Awake()
    {
        world = worldOb.GetComponent<World>();
        SettingsStatic.LoadedSettings = SettingsStatic.LoadSettings();

        if (!Settings.OnlinePlay)
            globalLighting.GetComponent<Lighting>().timeOfDay = SettingsStatic.LoadedSettings.timeOfDay;

        if (Settings.OnlinePlay) // order of events is important for network ids to be generated correctly
        {
            worldOb.SetActive(false); // later enabled by NetworkMenu
            NETWORK.SetActive(true);
            PlayerManagerNetwork.SetActive(true);
            LOCAL.SetActive(false);
        }
        else if (!Settings.OnlinePlay)
        {
            NETWORK.SetActive(false);
            PlayerManagerNetwork.SetActive(false);
            worldOb.SetActive(true);
            playerManagerLocal.GetComponent<PlayerInputManager>().playerPrefab = playerPrefabs[0]; //SettingsStatic.LoadedSettings.playerTypeChar];
            LOCAL.SetActive(true);
        }
        if (Application.isMobilePlatform)
        {
            Settings.IsMobilePlatform = true;
            LDrawImporterRuntime.SetActive(false);
            world.chunkMeshColliders = false;
            world.VBOs = false;
        }
        else
        {
            LDrawImporterRuntime.SetActive(true);
            world.chunkMeshColliders = true;
            world.VBOs = true;
        }
    }
}
