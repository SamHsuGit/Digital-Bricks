using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
public struct ClientToServerMessage : NetworkMessage
{
    public string playerName;
    public CharType typeChar;
    public int typeHelmet;
    public int typeArmor;
    public int typeTool;
    public int colorHelmet;
    public int colorHead;
    public int colorArmor;
    public int colorTorso;
    public int colorTool;
    public int colorArmL;
    public int colorHandL;
    public int colorArmR;
    public int colorHandR;
    public int colorBelt;
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

        switch (SettingsStatic.LoadedSettings.playerTypeChar)
        {
            case 0:
                // you can send the message here, or whatever else you want
                clientMessage = new ClientToServerMessage
                {
                    playerName = SettingsStatic.LoadedSettings.playerName,
                    typeChar = CharType.BrickFormer,
                    typeHelmet = SettingsStatic.LoadedSettings.playerTypeHelmet,
                    typeArmor = SettingsStatic.LoadedSettings.playerTypeArmor,
                    typeTool = SettingsStatic.LoadedSettings.playerTypeTool,
                    colorHelmet = SettingsStatic.LoadedSettings.playerColorHelmet,
                    colorHead = SettingsStatic.LoadedSettings.playerColorHead,
                    colorArmor = SettingsStatic.LoadedSettings.playerColorArmor,
                    colorTorso = SettingsStatic.LoadedSettings.playerColorTorso,
                    colorTool = SettingsStatic.LoadedSettings.playerColorTool,
                    colorArmL = SettingsStatic.LoadedSettings.playerColorArmL,
                    colorHandL = SettingsStatic.LoadedSettings.playerColorHandL,
                    colorArmR = SettingsStatic.LoadedSettings.playerColorArmR,
                    colorHandR = SettingsStatic.LoadedSettings.playerColorHandR,
                    colorBelt = SettingsStatic.LoadedSettings.playerColorBelt,
                    colorLegL = SettingsStatic.LoadedSettings.playerColorLegL,
                    colorLegR = SettingsStatic.LoadedSettings.playerColorLegR,
                };
                conn.Send(clientMessage);
                break;
            case 1:
                // you can send the message here, or whatever else you want
                clientMessage = new ClientToServerMessage
                {
                    playerName = SettingsStatic.LoadedSettings.playerName,
                    typeChar = CharType.Minifig,
                    typeHelmet = SettingsStatic.LoadedSettings.playerTypeHelmet,
                    typeArmor = SettingsStatic.LoadedSettings.playerTypeArmor,
                    typeTool = SettingsStatic.LoadedSettings.playerTypeTool,
                    colorHelmet = SettingsStatic.LoadedSettings.playerColorHelmet,
                    colorHead = SettingsStatic.LoadedSettings.playerColorHead,
                    colorArmor = SettingsStatic.LoadedSettings.playerColorArmor,
                    colorTorso = SettingsStatic.LoadedSettings.playerColorTorso,
                    colorTool = SettingsStatic.LoadedSettings.playerColorTool,
                    colorArmL = SettingsStatic.LoadedSettings.playerColorArmL,
                    colorHandL = SettingsStatic.LoadedSettings.playerColorHandL,
                    colorArmR = SettingsStatic.LoadedSettings.playerColorArmR,
                    colorHandR = SettingsStatic.LoadedSettings.playerColorHandR,
                    colorBelt = SettingsStatic.LoadedSettings.playerColorBelt,
                    colorLegL = SettingsStatic.LoadedSettings.playerColorLegL,
                    colorLegR = SettingsStatic.LoadedSettings.playerColorLegR,
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
        // playerPrefab is the one assigned in the inspector in Network
        // Manager but you can use different prefabs per race for example
        Transform startPos = GetStartPosition();

        int typeChar = 1;
        switch (message.typeChar)
        {
            case CharType.BrickFormer:
                {
                    playerGameObject = Instantiate(PlayerPrefab0, startPos.position, startPos.rotation);
                    typeChar = 0;
                    break;
                }
            case CharType.Minifig:
                {
                    playerGameObject = Instantiate(PlayerPrefab1, startPos.position, startPos.rotation);
                    typeChar = 1;
                    break;
                }
        }

        Controller controller = playerGameObject.GetComponent<Controller>();

        // Apply data from the message however appropriate for your game
        // Typically a Player would be a component you write with syncvars or properties
        controller.typeChar = typeChar;
        controller.playerName = message.playerName;
        controller.typeHelmet = message.typeHelmet;
        controller.typeArmor = message.typeArmor;
        controller.typeTool = message.typeTool;
        controller.colorHelmet = message.colorHelmet;
        controller.colorHead = message.colorHead;
        controller.colorArmor = message.colorArmor;
        controller.colorTorso = message.colorTorso;
        controller.colorTool = message.colorTool;
        controller.colorArmL = message.colorArmL;
        controller.colorHandL = message.colorHandL;
        controller.colorArmR = message.colorArmR;
        controller.colorHandR = message.colorHandR;
        controller.colorBelt = message.colorBelt;
        controller.colorLegL = message.colorLegL;
        controller.colorLegR = message.colorLegR;

        // call this to use this gameobject as the primary controller
        NetworkServer.AddPlayerForConnection(conn, playerGameObject);
    }
}
