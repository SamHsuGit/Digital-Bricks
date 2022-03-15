using UnityEngine;
using Mirror;
using System.Collections.Generic;

public struct ClientToServerMessage : NetworkMessage
{
    public string playerName;
    public string charIdle;
    public string charRun;
    public string projectile;
}

public class CustomNetworkManager : NetworkManager
{
    public GameObject worldOb;
    public GameObject charPrefab;
    GameObject playerGameObject;

    public override void OnStartServer() // happens before controller is instantiated
    {
        base.OnStartServer();

        NetworkServer.RegisterHandler<ClientToServerMessage>(OnCreateCharacter);
    }

    public void InitWorld()
    {
        worldOb.SetActive(true);
    }

    public void SpawnNetworkOb(GameObject ob)
    {
        NetworkServer.Spawn(ob);
    }

    public override void OnClientConnect(NetworkConnection conn) // happens before controller is instantiated
    {
        base.OnClientConnect(conn);

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
    }

    void OnCreateCharacter(NetworkConnection conn, ClientToServerMessage message)
    {
        // playerPrefab is the one assigned in the inspector in Network
        // Manager but you can use different prefabs per race for example
        //Transform startPos = GetStartPosition(); // not used to set default Spawn pos in settings
        Vector3 startPos = worldOb.GetComponent<World>().defaultSpawnPosition;
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

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
    }
}
