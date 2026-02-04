using UnityEngine;
using System.IO;

// a static class that can be accesssed from anywhere
public static class SettingsStatic
{
    private static Settings loadedSettings;

    // a static variable called LoadedSettings that can be accessed from anywhere
    public static Settings LoadedSettings
    {
        get { return loadedSettings; }
        set { loadedSettings = value; }
    }

    // public static function to load settings
    public static Settings LoadSettings()
    {
        Settings settings = new Settings();

        // world gen values needed to be saved between scenes
        settings.planetSeed = 3;
        settings.worldCoord = 3478;

        // dev tools (not configured in UI)
        settings.developerMode = false;
        settings.loadLdrawBaseFile = false;
        settings.biomeOverride = 12;
        settings.projectilesHurt = false;
        settings.drawChunkMeshes = false;
        settings.timeOfDay = 6.01f;
        settings.dayNightCycle = false;
        settings.useStuds = true;
        settings.chunkLoadAnim = true;
        settings.viewDistance = 3;

        // configured in game menu UI
        settings.playerName = "PlayerName";
        settings.ipAddress = "localhost";
        settings.volume = 0.5f;
        settings.lookSpeed = 0.1f;
        settings.lookAccel = 0.1f;
        settings.fov = 90f;
        settings.invertY = false;
        settings.fullscreen = true;
        settings.graphicsQuality = 0;

        // needed to save current value between game play sessions
        settings.currentBrickType = 0;
        settings.currentBrickIndex = 0;
        settings.currentBrickRotation = 0;
        settings.showControls = true;
        settings.camMode = 2;

        string path;
        if (Settings.Platform == 2)
            path = Settings.AppSaveDataPath + "/settings.cfg";
        else
            path = Application.streamingAssetsPath + "/settings.cfg";
        if (File.Exists(path))
        {
            string JsonImport = File.ReadAllText(path);
            settings = JsonUtility.FromJson<Settings>(JsonImport);
        }

        return settings;
    }
}

[System.Serializable]
public class Settings
{
    // private static variables
    private static bool _worldLoaded = true; // set to false to prevent players from moving or opening menus upon world load
    private static bool _webGL = false;
    private static bool _networkPlay = false;
    private static string _appPath;
    private static string _customModelsPath;

    // world gen values needed to be saved between scenes
    public int planetSeed; // can be 0 to 2,147,483,647 inclusively
    public int worldCoord; // can be 0 to 2,147,483,647 inclusively

    // dev tools (not configured in UI)
    public bool developerMode;
    public bool loadLdrawBaseFile;
    public int biomeOverride;
    public bool projectilesHurt;
    public bool drawChunkMeshes;
    public float timeOfDay;
    public bool dayNightCycle;
    public bool useStuds;
    public bool chunkLoadAnim;
    public int viewDistance; // loadDistance > viewDistance to reduce lag by ensuring player is always moving in loaded chunks

    // configured in game menu UI
    public string playerName;
    public string ipAddress;
    [Range(0.0001f, 1f)] public float volume;
    [Range(0.001f, 10f)] public float lookSpeed;
    public float lookAccel;
    public float fov;
    public bool invertY;
    public bool fullscreen;
    public int graphicsQuality;

    // needed to save current value between game play sessions
    public int currentBrickType; 
    public int currentBrickIndex;
    public int currentBrickRotation;
    public bool showControls;
    public int camMode;

    public static bool WorldLoaded
    {
        get { return _worldLoaded; }
        set { _worldLoaded = value; }
    }

    public static bool WebGL
    {
        get { return _webGL; }
        set { _webGL = value; }
    }

    public static bool OnlinePlay
    {
        get { return _networkPlay; }
        set {  _networkPlay = value; }
    }

    public static string CustomModelsPath
    {
        get { return _customModelsPath; }
        set { _customModelsPath = value; }
    }

    public static int Platform
    {
        // available gameplay options
        // pc singleplayer
        // pc network
        // console splitscreen
        // console network
        // mobile singleplayer
        // mobile network
        get
        {
            if (Application.isMobilePlatform) // iPhone
                return 2;
            else if (Application.isConsolePlatform) // xbox
                return 1;
            else
                return 0; // PC
        }
    }

    public static Vector3 DefaultSpawnPosition
    {
        // player default spawn position is centered above first chunk
        get { return new Vector3(VoxelData.WorldSizeInVoxels / 2f + VoxelData.ChunkWidth / 2, VoxelData.ChunkHeight - 5f, VoxelData.WorldSizeInVoxels / 2f + VoxelData.ChunkWidth / 2); }
    }

    public static string BasePartsPath
    {
        get { return Application.streamingAssetsPath + "/ldraw/partfiles/"; }
    }

    public static string ModelsPath
    {
        get { return Application.streamingAssetsPath + "/ldraw/models/"; }
    }

    public static string ColorConfigPath
    {
        get { return Application.streamingAssetsPath + "/ldraw/LDConfig.ldr"; }
    }

    public static string AppSaveDataPath
    {
        get { return _appPath; }
        set { _appPath = value; }
    }
}

public static class FileSystemExtension
{
    public static string ReadFileToString(string fileName)
    {
        string path = Settings.ModelsPath + fileName;
        if (!File.Exists(path))
            ErrorMessage.Show("File not found: " + path);

        StreamReader reader = new StreamReader(path);
        string result = reader.ReadToEnd();
        reader.Close();

        return result;
    }

    public static void SaveSettings()
    {
        string path;
        if (Settings.Platform == 2)
            path = Settings.AppSaveDataPath + "/settings.cfg";
        else
            path = Application.streamingAssetsPath + "/settings.cfg";
        SaveStringToFile(JsonUtility.ToJson(SettingsStatic.LoadedSettings), path);
    }

    public static void SaveStringToFile(string jsonExport, string savePathAndFileName)
    {
        File.WriteAllText(savePathAndFileName, jsonExport);
    }
}