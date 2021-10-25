using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public struct ClientToServerMessage : NetworkMessage
{
    public string playerName;
    public CharType charType;
    public int colorTorso;
    public int colorArmL;
    public int colorArmR;
    public int colorLegL;
    public int colorLegR;
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

public enum CharType
{
    BrickFormer,
    Minifig
}

public class CustomNetworkManager : NetworkManager
{
    public GameObject world;

    public GameObject PlayerPrefab0;
    public GameObject PlayerPrefab1;
    public GameObject brick1x1;

    GameObject playerGameObject;
    GameObject undefinedPrefabToSpawn;
    GameObject predefinedPrefabToSpawn;


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

        switch (SettingsStatic.LoadedSettings.charType)
        {
            case 0:
                // you can send the message here, or whatever else you want
                clientMessage = new ClientToServerMessage
                {
                    playerName = SettingsStatic.LoadedSettings.playerName,
                    charType = CharType.BrickFormer,
                    colorTorso = SettingsStatic.LoadedSettings.playerColorTorso,
                    colorArmL = SettingsStatic.LoadedSettings.playerColorArmL,
                    colorArmR = SettingsStatic.LoadedSettings.playerColorArmR,
                    colorLegL = SettingsStatic.LoadedSettings.playerColorLegL,
                    colorLegR = SettingsStatic.LoadedSettings.playerColorLegR
                };
                conn.Send(clientMessage);
                break;
            case 1:
                // you can send the message here, or whatever else you want
                clientMessage = new ClientToServerMessage
                {
                    playerName = SettingsStatic.LoadedSettings.playerName,
                    charType = CharType.Minifig,
                    colorTorso = SettingsStatic.LoadedSettings.playerColorTorso,
                    colorArmL = SettingsStatic.LoadedSettings.playerColorArmL,
                    colorArmR = SettingsStatic.LoadedSettings.playerColorArmR,
                    colorLegL = SettingsStatic.LoadedSettings.playerColorLegL,
                    colorLegR = SettingsStatic.LoadedSettings.playerColorLegR
                };
                conn.Send(clientMessage);
                break;
        }
    }

    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
    }

    void OnCreateCharacter(NetworkConnection conn, ClientToServerMessage message)
    {
        //// checks client name
        //for(int i = 0; i < World.Instance.players.Count; i++)
        //{
        //    if(message.playerName == World.Instance.players[i].name)
        //    {
        //        Debug.Log("Error: Non-Unique Player Name. Client name already exists on server. Player names must be unique. Disconnecting Client.");
        //        conn.Disconnect();
        //        return;
        //    }
        //}

        // playerPrefab is the one assigned in the inspector in Network
        // Manager but you can use different prefabs per race for example
        Transform startPos = GetStartPosition();

        switch (message.charType)
        {
            case CharType.BrickFormer:
                {
                    playerGameObject = Instantiate(PlayerPrefab0, startPos.position, startPos.rotation);
                    break;
                }
            case CharType.Minifig:
                {
                    playerGameObject = Instantiate(PlayerPrefab1, startPos.position, startPos.rotation);
                    break;
                }
        }

        // Apply data from the message however appropriate for your game
        // Typically a Player would be a component you write with syncvars or properties
        playerGameObject.GetComponent<Controller>().playerName = message.playerName;
        playerGameObject.GetComponent<Controller>().colorTorso = message.colorTorso;
        playerGameObject.GetComponent<Controller>().colorArmL = message.colorArmL;
        playerGameObject.GetComponent<Controller>().colorArmR = message.colorArmR;
        playerGameObject.GetComponent<Controller>().colorLegL = message.colorLegL;
        playerGameObject.GetComponent<Controller>().colorLegR = message.colorLegR;

        // call this to use this gameobject as the primary controller
        NetworkServer.AddPlayerForConnection(conn, playerGameObject);
    }
}
