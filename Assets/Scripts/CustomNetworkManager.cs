using UnityEngine;
using Mirror;
using System.Collections.Generic;

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
    public GameObject worldOb;
    public GameObject charPrefab;
    GameObject playerGameObject;
    public ServerToClientMessage hostMessage;
    bool messageReceived = false;

    public override void OnStartServer() // happens before controller is instantiated
    {
        base.OnStartServer();

        //int planetNumber = SettingsStatic.LoadedSettings.planetNumber;
        //int seed = SettingsStatic.LoadedSettings.seed;

        //// encode the list of chunkStrings into a single string that is auto-serialized by mirror
        //List<string> chunksList = SaveSystem.LoadChunkFromFile(planetNumber, seed);
        //string chunksServerCombinedString = string.Empty;
        //for (int i = 0; i < chunksList.Count; i++)
        //{
        //    chunksServerCombinedString += chunksList[i];
        //    chunksServerCombinedString += ';'; // has to be a single char to be able to split later on client side
        //}

        //// send the clients a message containing the world data so that users do not have to manually share files before each game
        //hostMessage = new ServerToClientMessage
        //{
        //    planetNumberServer = planetNumber,
        //    seedServer = seed,
        //    baseServer = FileSystemExtension.ReadFileToString("base.ldr"),
        //    chunksServer = chunksServerCombinedString,
        //};

        NetworkServer.RegisterHandler<ClientToServerMessage>(OnCreateCharacter);

        //InitWorld(); // activated in controller
    }

    public void InitWorld()
    {
        worldOb.SetActive(true);
    }

    public void SpawnNetworkOb(GameObject ob)
    {
        Debug.Log("Message Sent");
        NetworkServer.Spawn(ob);
    }

    public override void OnClientConnect(NetworkConnection conn) // happens before controller is instantiated
    {
        base.OnClientConnect(conn);
        //NetworkClient.RegisterHandler<ServerToClientMessage>(OnReceiveHostMessage);

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

        //InitWorld(); // activated in controller
    }

    //private void OnReceiveHostMessage(ServerToClientMessage message)
    //{
    //    if (!messageReceived) // only receive message once upon first joining the world (had to add flag since mirror does not support sending messages to only new clients...)
    //    {
    //        Debug.Log("Message Received");
    //        // these values need to be synced to world before controller is activated bc world is activated before controller
    //        World world = worldOb.GetComponent<World>();
    //        Debug.Log("replace " + world.planetNumber + " with " + message.planetNumberServer + " to get: ");
    //        world.planetNumber = message.planetNumberServer; // preset world planetNumber
    //        Debug.Log(world.planetNumber);
    //        world.seed = message.seedServer; // preset world seed
    //        world.baseOb = LDrawImportRuntime.Instance.ImportLDrawOnline("base", message.baseServer, LDrawImportRuntime.Instance.importPosition, true); // store value so it can be set later at correct time (after ldrawimporter is activated)
    //        if (message.chunksServer != null)
    //        {
    //            string[] serverChunks = message.chunksServer.Split(';'); // splits individual chunk strings using ';' char delimiter

    //            // tell world to draw chunks from server
    //            for (int i = 0; i < serverChunks.Length - 1; i++)
    //            {
    //                ChunkData chunk = new ChunkData();
    //                chunk = chunk.DecodeChunk(serverChunks[i]);
    //                if (world.worldData.chunks.ContainsKey(chunk.position)) // if chunk already included in list, remove before adding new version
    //                    world.worldData.chunks.Remove(chunk.position);
    //                world.worldData.chunks.Add(chunk.position, chunk);
    //            }
    //        }
    //        messageReceived = true;
    //    }
    //}

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
