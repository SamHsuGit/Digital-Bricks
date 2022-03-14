using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;

public struct ClientToServerMessage : NetworkMessage
{
    public string playerName;
    public string serializedCharIdle;
    public string serializedCharRun;
    public string serializedProjectile;
}

public class CustomNetworkManager : NetworkManager
{
    public GameObject world;
    public GameObject charPrefab;
    GameObject playerGameObject;

    public override void OnStartServer()
    {
        base.OnStartServer();

        NetworkServer.RegisterHandler<ClientToServerMessage>(OnCreateCharacter);
    }

    public void SpawnNetworkOb(GameObject ob)
    {
        NetworkServer.Spawn(ob);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        //world.GetComponent<World>().defaultSpawnPosition = GetStartPosition().position; // not used to set default Spawn pos in settings

        ClientToServerMessage clientMessage;

        // you can send the message here, or whatever else you want
        clientMessage = new ClientToServerMessage
        {
            playerName = SettingsStatic.LoadedSettings.playerName,
            serializedCharIdle = LDrawImportRuntime.Instance.ReadFileToString("charIdle.ldr"),
            serializedCharRun = LDrawImportRuntime.Instance.ReadFileToString("charRun.ldr"),
            serializedProjectile = LDrawImportRuntime.Instance.ReadFileToString("projectile.ldr"),
        };
        conn.Send(clientMessage);
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
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
        controller.playerCharIdleString = message.serializedCharIdle;
        controller.playerCharRunString = message.serializedCharRun;
        controller.playerProjectileString = message.serializedProjectile;

        // call this to use this gameobject as the primary controller
        NetworkServer.AddPlayerForConnection(conn, playerGameObject);
    }
}
