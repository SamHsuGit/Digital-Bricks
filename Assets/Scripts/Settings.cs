using System.Collections;
using System.Collections.Generic;
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

        settings.ipAddress = "localhost";
        settings.viewDistance = 3;
        settings.volume = 0.5f;
        settings.lookSpeed = 0.1f;
        settings.lookAccel = 0.1f;
        settings.fov = 90f;
        settings.invertY = false;
        settings.invertX = false;
        settings.fullscreen = true;
        settings.planetNumber = 3;
        settings.seed = 5;
        settings.timeOfDay = 6.01f;
        settings.playerName = "PlayerName";
        settings.flight = false;

        string path;
        if (Application.isMobilePlatform)
        {
            path = Application.persistentDataPath + "/settings.cfg";
        }
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
    private static bool isMobilePlatform = false;
    private static bool worldLoaded = true; // set to false to prevent players from moving or opening menus upon world load
    private static bool networkPlay = false;

    [Header("Game Data")]
    public string ipAddress;

    [Header("Performance")]
    // NOTE: viewDistance is a radius, just like in Minecraft.
    public int viewDistance; // loadDistance = viewDistance * 3.33 to reduce lag by ensuring player is always moving in loaded chunks

    [Header("Controls")]
    [Range(0.0001f, 1f)]
    public float volume;
    [Range(0.001f, 10f)]
    public float lookSpeed;
    public float lookAccel;
    public float fov;
    //public int graphicsQuality;
    public bool invertY;
    public bool invertX;
    public bool fullscreen;

    [Header("World Gen")]
    public int planetNumber;
    public int seed; // int yields numbers from 0 to 2,147,483,647 inclusively
    public float timeOfDay;

    [Header("Player Customization")]
    public string playerName;
    public bool flight;

    public static bool IsMobilePlatform
    {
        get { return isMobilePlatform; }
        set { isMobilePlatform = value; }
    }

    public static bool WorldLoaded
    {
        get { return worldLoaded; }
        set { worldLoaded = value; }
    }

    public static bool OnlinePlay
    {
        get { return networkPlay; }
        set {  networkPlay = value; }
    }

    public static Vector3 DefaultSpawnPosition
    {
        get { return new Vector3(40008, 60, 40008);}
    }
}
