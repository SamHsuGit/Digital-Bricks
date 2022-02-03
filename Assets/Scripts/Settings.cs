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
        get
        {
            return loadedSettings;
        }
        set
        {
            loadedSettings = value;
        }
    }

    // public static function to load settings
    public static Settings LoadSettings()
    {
        Settings settings = new Settings();

        settings.ipAddress = "localhost";
        settings.drawDistance = 3;
        settings.volume = 0.5f;
        settings.lookSpeed = 0.1f;
        settings.lookAccel = 0.1f;
        settings.fov = 90f;
        //settings.graphicsQuality = 0;
        settings.invertY = false;
        settings.invertX = false;
        settings.fullscreen = true;
        settings.planetNumber = 3;
        settings.seed = 5;
        settings.timeOfDay = 6.01f;
        settings.playerName = "PlayerName";
        //settings.playerTypeChar = 1;
        //settings.playerTypeHelmet = 0;
        //settings.playerTypeArmor = 0;
        //settings.playerTypeTool = 0;
        //settings.playerColorHelmet = 17;
        //settings.playerColorHead = 9;
        //settings.playerColorArmor = 17;
        //settings.playerColorTorso = 17;
        //settings.playerColorTool = 0;
        //settings.playerColorArmL = 17;
        //settings.playerColorHandL = 9;
        //settings.playerColorArmR = 17;
        //settings.playerColorHandR = 9;
        //settings.playerColorBelt = 17;
        //settings.playerColorLegL = 17;
        //settings.playerColorLegR = 17;

        if (File.Exists(Application.streamingAssetsPath + "/settings.cfg"))
        {
            string JsonImport = File.ReadAllText(Application.streamingAssetsPath + "/settings.cfg");
            settings = JsonUtility.FromJson<Settings>(JsonImport);
        }

        return settings;
    }
}

[System.Serializable]
public class Settings
{
    // private static variables
    private static bool worldLoaded = true; // set to false to prevent players from moving or opening menus upon world load
    private static bool networkPlay = false;
    private static bool gameMenuActive = false;

    [Header("Game Data")]
    public string ipAddress;

    [Header("Performance")]
    // NOTE: viewDistance is a radius, just like in Minecraft.
    public int drawDistance; // loadDistance = viewDistance * 3.33 to reduce lag by ensuring player is always moving in loaded chunks

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
    //public int playerTypeChar;
    //public int playerTypeHelmet;
    //public int playerTypeArmor;
    //public int playerTypeTool;
    //public int playerColorHelmet;
    //public int playerColorHead;
    //public int playerColorArmor;
    //public int playerColorTorso;
    //public int playerColorTool;
    //public int playerColorArmL;
    //public int playerColorHandL;
    //public int playerColorArmR;
    //public int playerColorHandR;
    //public int playerColorBelt;
    //public int playerColorLegL;
    //public int playerColorLegR;

    public static bool WorldLoaded
    {
        get
        {
            return worldLoaded;
        }
        set
        {
            worldLoaded = value;
        }
    }

    public static bool OnlinePlay
    {
        get
        {
            return networkPlay;
        }
        set
        {
            networkPlay = value;
        }
    }

    public static bool GameMenuActive
    {
        get
        {
            return gameMenuActive;
        }
        set
        {
            gameMenuActive = value;
        }
    }
}

public static class LDrawColors
{
    public static Dictionary<int, string> colorLib = new Dictionary<int, string>();
    public static string[] savedPlayerColors;

    static string[] hexList = new string[] // 85 Solid Colors from LDraw hex values color reference https://www.ldraw.org/article/547.html
        {
            "#1B2A34", // 00 Black
            "#8A928D", // 01 Light_Grey
            "#F4F4F4", // 02 White
            "#B40000", // 03 Red
            "#720012", // 04 Dark_Red
            "#D67923", // 05 Orange
            "#91501C", // 06 Dark_Orange
            "#B0A06F", // 07 Tan
            "#543324", // 08 Brown
            "#FAC80A", // 09 Yellow
            "#FCAC00", // 10 Bright_Light_Orange
            "#A5CA18", // 11 Lime
            "#00852B", // 12 Green
            "#708E7C", // 13 Sand_Green
            "#68C3E2", // 14 Medium_Azure
            "#069D9F", // 15 Dark_Turquoise
            "#7396C8", // 16 Medium_Blue
            "#1E5AA8", // 17 Blue
            "#19325A", // 18 Dark_Blue
            "#A06EB9", // 19 Medium_Lavender
            "#901F76", // 20 Magenta
            "#FF9ECD", // 21 Bright_Pink
            "#D3359D", // 22 Dark_Pink
        };

    public static void GenerateColorLib() // only needs to be called once.
    {
        if(colorLib.Count == 0)
        {
            // make the list of hex values for the color dictionary
            for (int i = 0; i < hexList.Length; i++)
            {
                colorLib.Add(i, hexList[i]);
            }
        }
    }

    //public static void GetSavedColorHexValues() // only needs to be called once since the savedPlayerColorHexValues array is static
    //{
    //    // get material hex values from loaded settings
    //    savedPlayerColors = new string[]
    //    {
    //        // use color dictionary to convert to hex values
    //        colorLib[SettingsStatic.LoadedSettings.playerColorHelmet],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorHead],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorArmor],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorTorso],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorTool],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorArmL],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorHandL],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorArmR],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorHandR],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorBelt],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorLegL],
    //        colorLib[SettingsStatic.LoadedSettings.playerColorLegR],
    //    };
    //}

    public static Color IntToColor(int colorLibIndex)
    {
        if (ColorUtility.TryParseHtmlString(LDrawColors.colorLib[colorLibIndex], out Color newCol))
            return newCol;
        else
        {
            Debug.Log("No matching color ID found");
            return Color.black;
        }   
    }
}
