using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public struct ClientToServerMessage : NetworkMessage
{
    public string playerName;
}

// https://mirror-networking.gitbook.io/docs/guides/communications/network-messages
// https://mirror-networking.gitbook.io/docs/guides/data-types
// Mirror can only serialize classes as long as each field has a supported data type...
// Mirror does not support complex data types such as Dictionaries and lists, must implement serialization/deserialization methods yourself...
//public struct HostMessage : NetworkMessage
//{
//    //public List<ChunkData> modifiedChunks;
//    //public List<VoxelState[,,]> mapList;
//}

public class CustomNetworkManager : NetworkManager
{
    public GameObject world;
    public GameObject charPrefab;
    GameObject playerGameObject;

    public override void OnStartServer()
    {
        base.OnStartServer();

        //HostMessage hostMessage;

        // create a list of voxelMaps for modified chunks to send in message to clients
        //List<VoxelState[,,]> mapListForMessage = new List<VoxelState[,,]>();
        //for (int i = 0; i < World.Instance.worldData.modifiedChunks.Count; i++)
        //{
        //    mapListForMessage[i] = World.Instance.worldData.modifiedChunks[i].map;
        //}

        // send the clients a message containing the world data so that users do not have to manually share files before each game
        //hostMessage = new HostMessage
        //{
        //    //modifiedChunks = World.Instance.worldData.modifiedChunks,
        //    //mapList = mapListForMessage
        //};

        NetworkServer.RegisterHandler<ClientToServerMessage>(OnCreateCharacter);
    }

    public void SpawnNetworkOb(GameObject ob)
    {
        NetworkServer.Spawn(ob);
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);

        // worldPlayer used to spawn player before world is loaded?
        world.GetComponent<World>().worldPlayer.transform.position = GetStartPosition().position;

        ClientToServerMessage clientMessage;

        // you can send the message here, or whatever else you want
        clientMessage = new ClientToServerMessage
        {
            playerName = SettingsStatic.LoadedSettings.playerName,
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
        Transform startPos = GetStartPosition();
        playerGameObject = Instantiate(charPrefab, startPos.position, startPos.rotation);

        Controller controller = playerGameObject.GetComponent<Controller>();

        // Apply data from the message however appropriate for your game
        // Typically a Player would be a component you write with syncvars or properties
        controller.playerName = message.playerName;

        // call this to use this gameobject as the primary controller
        NetworkServer.AddPlayerForConnection(conn, playerGameObject);
    }
}
