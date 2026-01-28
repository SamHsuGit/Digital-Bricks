using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;
using System;

public class NetworkMenu : MonoBehaviour
{
    public GameObject playerManagerNetwork;
    public TMP_InputField playerNameInputField;
    public TMP_Dropdown worldDropDown;
    public TMP_InputField networkAddressInputField;
    public TMP_Text connectionStatus;
    public GameObject networkMenuElements;
    public AudioSource buttonSound;
    public GameObject background;
    public GameObject loadingText;
    public GameObject gameManagerObject;
    public GameObject LDrawImporterRuntime;
    public bool randomizeSeed;

    public List<string> seeds;

    GameManagerScript gameManager;
    NetworkManager manager;
    CanvasGroup networkMenuElementsCanvasGroup;

    private void Awake()
    {
        SettingsStatic.LoadedSettings.worldCoord = RandomizeWorldCoord();
        SaveSettings();

        manager = playerManagerNetwork.GetComponent<CustomNetworkManager>();

        networkMenuElementsCanvasGroup = networkMenuElements.GetComponent<CanvasGroup>();

        networkMenuElementsCanvasGroup.alpha = 1;
        networkMenuElementsCanvasGroup.interactable = true;
        networkAddressInputField.text = SettingsStatic.LoadedSettings.ipAddress;
        background.GetComponent<CanvasGroup>().alpha = 1;
        gameManager = gameManagerObject.GetComponent<GameManagerScript>();
        playerNameInputField.text = SettingsStatic.LoadedSettings.playerName;

        GetSaves();
    }

    private void GetSaves()
    {
        // get list of strings of seeds
        string[] seedsArray = Directory.GetDirectories(Settings.AppSaveDataPath + "/saves/");

        if (seedsArray.Length == 0) // if no seeds, use current saved randomized one
        {
            SettingsStatic.LoadedSettings.worldCoord = RandomizeWorldCoord();
            seeds.Add(SettingsStatic.LoadedSettings.worldCoord.ToString());
        }
        foreach (string seed in seedsArray)
        {
            string newstring;
            int index = seed.LastIndexOf("-");
            newstring = seed.Substring(index + 1);
            seeds.Add(newstring);
        }

        worldDropDown.ClearOptions();
        if (!seeds.Contains(SettingsStatic.LoadedSettings.worldCoord.ToString()))
            seeds.Add(SettingsStatic.LoadedSettings.worldCoord.ToString());
        worldDropDown.AddOptions(seeds);
    }

    public int RandomizeWorldCoord()
    {
        return UnityEngine.Random.Range(1, 5000);
        randomizeSeed = false;
    }

    public void OnHostClient()
    {
        SaveSettings();
        networkMenuElementsCanvasGroup.alpha = 0;
        networkMenuElementsCanvasGroup.interactable = false;
        loadingText.SetActive(true); // in order for this text to show before world load, would need to change scene before loading next scene with world (like Setup Menu for Splitscreen)

        buttonSound.Play();
        FileSystemExtension.SaveSettings();

        LDrawImporterRuntime.SetActive(true);

        if (!NetworkClient.active)
        {
            // Server + Client
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                manager.StartHost();
                manager.networkAddress = networkAddressInputField.text;
            }
        }
        else
        {
            // Connecting
            connectionStatus.text = "Connecting to " + manager.networkAddress + "..";
        }

        StatusLabels();
        gameManager.Setup(); // activate ldraw importer, etc.
    }

    public void OnClientOnly()
    {
        SaveSettings();
        networkMenuElementsCanvasGroup.alpha = 0;
        networkMenuElementsCanvasGroup.interactable = false;
        loadingText.SetActive(true); // in order for this text to show before world load, would need to change scene before loading next scene with world (like Setup Menu for Splitscreen)

        buttonSound.Play();
        FileSystemExtension.SaveSettings();

        LDrawImporterRuntime.SetActive(true);

        if (!NetworkClient.active)
        {
            // Client + IP
            manager.StartClient();
            manager.networkAddress = networkAddressInputField.text;
        }
        else
        {
            // Connecting
            connectionStatus.text = "Connecting to " + manager.networkAddress + "..";
        }

        StatusLabels();
        gameManager.Setup(); // activate ldraw importer, etc.
    }

    public void Back()
    {
        buttonSound.Play();
        FileSystemExtension.SaveSettings();
        SceneManager.LoadScene(2);
        SaveSettings();
    }

    // Used to host servers without actually adding a player
    public void OnServerOnly()
    {
        SaveSettings();
        networkMenuElementsCanvasGroup.alpha = 0;
        networkMenuElementsCanvasGroup.interactable = false;
        loadingText.SetActive(true); // in order for this text to show before world load, would need to change scene before loading next scene with world (like Setup Menu for Splitscreen)

        buttonSound.Play();
        FileSystemExtension.SaveSettings();

        LDrawImporterRuntime.SetActive(true);

        if (!NetworkClient.active)
        {
            // Server Only
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                // cant be a server in webgl build
                connectionStatus.text = "(  WebGL cannot be server  )";
            }
            else
            {
                manager.StartServer();
                manager.networkAddress = networkAddressInputField.text;
            }
        }
        else
        {
            // Connecting
            connectionStatus.text = "Connecting to " + manager.networkAddress + "..";
        }

        StatusLabels();
        gameManager.Setup();
    }

    public void OnChangeNetworkAddress()
    {
        manager.networkAddress = networkAddressInputField.text;
        SettingsStatic.LoadedSettings.ipAddress = networkAddressInputField.text;
    }

    void StatusLabels()
    {
        // host mode
        // display separately because this always confused people:
        //   Server: ...
        //   Client: ...
        if (NetworkServer.active && NetworkClient.active)
        {
            connectionStatus.text = "Host " + manager.networkAddress + " running via " + Transport.activeTransport;
        }
        // server only
        else if (NetworkServer.active)
        {
            connectionStatus.text = "Server " + manager.networkAddress + " running via " + Transport.activeTransport;
        }
        // client only
        else if (NetworkClient.isConnected)
        {
            connectionStatus.text = "Client connected to " + manager.networkAddress + " via " + Transport.activeTransport;
        }
    }

    public void SaveSettings()
    {
        //try to load the saved world coord, otherwise default to 1
        try
        {
            int result = Int32.Parse(worldDropDown.options[worldDropDown.value].text); // Int32 can hold up to 2,147,483,647 numbers
            SettingsStatic.LoadedSettings.worldCoord = result;
        }
        catch (System.FormatException)
        {
            SettingsStatic.LoadedSettings.worldCoord = 1; // default value
        }
        SettingsStatic.LoadedSettings.playerName = playerNameInputField.text;

        // Save setttings when this function is called, otherwise settings will load from latest settings file upon game start
        FileSystemExtension.SaveSettings();
    }
}
