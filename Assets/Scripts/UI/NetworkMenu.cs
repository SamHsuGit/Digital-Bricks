using UnityEngine;
using Mirror;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class NetworkMenu : MonoBehaviour
{
    public GameObject playerManagerNetwork;
    public TMP_InputField networkAddressInputField;
    public TMP_Text connectionStatus;
    public GameObject networkMenuElements;
    public AudioSource buttonSound;
    public GameObject background;
    public GameObject worldOb;
    public GameObject loadingText;
    public GameObject gameManagerObject;

    GameManagerScript gameManager;
    NetworkManager manager;
    CanvasGroup networkMenuElementsCanvasGroup;

    private void Awake()
    {
        manager = playerManagerNetwork.GetComponent<CustomNetworkManager>();

        networkMenuElementsCanvasGroup = networkMenuElements.GetComponent<CanvasGroup>();

        networkMenuElementsCanvasGroup.alpha = 1;
        networkMenuElementsCanvasGroup.interactable = true;
        networkAddressInputField.text = SettingsStatic.LoadedSettings.ipAddress;
        background.GetComponent<CanvasGroup>().alpha = 1;
        gameManager = gameManagerObject.GetComponent<GameManagerScript>();
    }

    public void OnHostClient()
    {
        networkMenuElementsCanvasGroup.alpha = 0;
        networkMenuElementsCanvasGroup.interactable = false;
        loadingText.SetActive(true); // in order for this text to show before world load, would need to change scene before loading next scene with world (like Setup Menu for Splitscreen)

        buttonSound.Play();
        FileSystemExtension.SaveSettings();

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
        networkMenuElementsCanvasGroup.alpha = 0;
        networkMenuElementsCanvasGroup.interactable = false;
        loadingText.SetActive(true); // in order for this text to show before world load, would need to change scene before loading next scene with world (like Setup Menu for Splitscreen)

        buttonSound.Play();
        FileSystemExtension.SaveSettings();

        if (!NetworkClient.active)
        {
            // Client + IP
            NetworkClient.RegisterHandler<ServerToClientMessage>(OnReceiveHostMessage);
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

    void OnReceiveHostMessage(ServerToClientMessage message)
    {
        // these values need to be synced to world before controller is activated bc world is activated before controller
        World world = worldOb.GetComponent<World>();
        Debug.Log("replace " + world.planetNumber + " with " + message.planetNumberServer + " to get: ");
        world.planetNumber = message.planetNumberServer; // preset world planetNumber
        Debug.Log(world.planetNumber);
        world.seed = message.seedServer; // preset world seed
        world.baseOb = LDrawImportRuntime.Instance.ImportLDrawOnline("base", message.baseServer, LDrawImportRuntime.Instance.importPosition, true); // store value so it can be set later at correct time (after ldrawimporter is activated)
        if (message.chunksServer != null)
        {
            string[] serverChunks = message.chunksServer.Split(';'); // splits individual chunk strings using ';' char delimiter

            // tell world to draw chunks from server
            for (int i = 0; i < serverChunks.Length; i++)
            {
                ChunkData chunk = new ChunkData();
                chunk = chunk.DecodeChunk(serverChunks[i]);
                world.worldData.chunks.Add(chunk.position, chunk);
            }
        }
    }

    public void Back()
    {
        buttonSound.Play();
        FileSystemExtension.SaveSettings();
        SceneManager.LoadScene(2);
    }

    // Used to host servers without actually adding a player
    public void OnServerOnly()
    {
        networkMenuElementsCanvasGroup.alpha = 0;
        networkMenuElementsCanvasGroup.interactable = false;
        loadingText.SetActive(true); // in order for this text to show before world load, would need to change scene before loading next scene with world (like Setup Menu for Splitscreen)

        buttonSound.Play();
        FileSystemExtension.SaveSettings();

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
}
