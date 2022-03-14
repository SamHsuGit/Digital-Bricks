using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;

public struct ServerToClientMessage : NetworkMessage
{
    public int planetNumberServer;
    public int seedServer;
    public string baseServer;
    public string chunksServer;
}

public struct ClientToServerMessage : NetworkMessage
{
    public string playerName;
    public string charIdle;
    public string charRun;
    public string projectile;
}

public class CustomNetworkManager : NetworkManager
{
    public GameObject world;
    public GameObject charPrefab;
    GameObject playerGameObject;

    public override void OnStartServer() // happens before controller is instantiated
    {
        base.OnStartServer();

        ServerToClientMessage hostMessage;

        int planetNumber = SettingsStatic.LoadedSettings.planetNumber;
        int seed = SettingsStatic.LoadedSettings.seed;

        // encode the list of chunkStrings into a single string that is auto-serialized by mirror
        List<string> chunksList = SaveSystem.LoadChunkFromFile(planetNumber, seed);
        string chunksServerCombinedString = string.Empty;
        for (int i = 0; i < chunksList.Count; i++)
        {
            chunksServerCombinedString += chunksList[i];
            chunksServerCombinedString += ';'; // has to be a single char to be able to split later on client side
        }

        // send the clients a message containing the world data so that users do not have to manually share files before each game
        hostMessage = new ServerToClientMessage
        {
            planetNumberServer = planetNumber,
            seedServer = seed,
            baseServer = FileSystemExtension.ReadFileToString("base.ldr"),
            chunksServer = chunksServerCombinedString,
        };
        NetworkServer.SendToAll(hostMessage);

        NetworkServer.RegisterHandler<ClientToServerMessage>(OnCreateCharacter);
    }

    

    public void SpawnNetworkOb(GameObject ob)
    {
        NetworkServer.Spawn(ob);
    }

    public override void OnClientConnect(NetworkConnection conn) // happens before controller is instantiated
    {
        base.OnClientConnect(conn);

        NetworkClient.RegisterHandler<ServerToClientMessage>(OnReceiveHostMessage);

        ClientToServerMessage clientMessage;

        // you can send the message here, or whatever else you want
        clientMessage = new ClientToServerMessage
        {
            playerName = SettingsStatic.LoadedSettings.playerName,
            charIdle = FileSystemExtension.ReadFileToString("charIdle.ldr"),
            charRun = FileSystemExtension.ReadFileToString("charRun.ldr"),
            projectile = FileSystemExtension.ReadFileToString("projectile.ldr"),
        };
        conn.Send(clientMessage);

        world.SetActive(true); // only activate world after sending/receiving all messages to/from server
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
    }

    void OnReceiveHostMessage(ServerToClientMessage message)
    {
        SettingsStatic.LoadedSettings.planetNumber = message.planetNumberServer; // override loaded settings
        SettingsStatic.LoadedSettings.seed = message.seedServer; // override loaded settings
        Settings.serverBase = message.baseServer; // store value so it can be set later at correct time (after ldrawimporter is activated)
        Settings.serverChunks = message.chunksServer; // stores chunksList string for later use when world is set to active
    }

    void OnCreateCharacter(NetworkConnection conn, ClientToServerMessage message)
    {
        // playerPrefab is the one assigned in the inspector in Network
        // Manager but you can use different prefabs per race for example
        //Transform startPos = GetStartPosition(); // not used to set default Spawn pos in settings
        Vector3 startPos = world.GetComponent<World>().defaultSpawnPosition;
        playerGameObject = Instantiate(charPrefab, startPos, Quaternion.identity);

        Controller controller = playerGameObject.GetComponent<Controller>();

        // Apply data from the message however appropriate for your game
        // Typically a Player would be a component you write with syncvars or properties
        controller.playerName = message.playerName;
        controller.playerCharIdle = message.charIdle;
        controller.playerCharRun = message.charRun;
        controller.playerProjectile = message.projectile;

        // call this to use this gameobject as the primary controller
        NetworkServer.AddPlayerForConnection(conn, playerGameObject);
    }
}
