using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System;

public class SetupMenu : MonoBehaviour
{
    public GameObject menuElements;
    public Planet[] planets;
    public Biome[] biomes;
    public Slider loadingSlider;
    public TextMeshProUGUI loadingPercentageText;
    public TMP_InputField playerNameInputField;
    public Toggle creativeMode;
    public TMP_Dropdown worldDropDown;
    public TMP_InputField seedInputField;
    public Toggle loadLDrawBaseFile;
    public GameObject modelObjectToSpin;
    public GameObject levelLoaderObject;
    
    public Image biomeImage;

    public Slider worldDensitySlider;
    public Slider worldRenderDistanceSlider;
    public TextMeshProUGUI worldRenderText;
    public TextMeshProUGUI worldDensityText;
    public TextMeshProUGUI planetNameText;
    public TextMeshProUGUI biomeNameText;

    public AudioSource buttonSound;

    public int seed;
    public int randomSeed;
    public bool noSaves = false;
    public List<string> seeds;

    public int biomeIndex;
    public int densityRange;

    private LevelLoader levelLoader;

    private void Awake()
    {
        // import this player's char model as a preview before entering the game
        SettingsStatic.LoadedSettings = SettingsStatic.LoadSettings();

        levelLoader = levelLoaderObject.GetComponent<LevelLoader>();

        RandomizeSeed(); // gets and stores a random seed in seed input field
        GetSaves();
    }

    private void Start()
    {
        //reset framerate of game for video animation
        Application.targetFrameRate = 60;
        
        playerNameInputField.text = SettingsStatic.LoadedSettings.playerName;
        creativeMode.isOn = SettingsStatic.LoadedSettings.developerMode;
        worldRenderDistanceSlider.value = SettingsStatic.LoadedSettings.viewDistance;
        worldRenderText.text = SettingsStatic.LoadedSettings.viewDistance.ToString();
        biomeIndex = SettingsStatic.LoadedSettings.biomeOverride;
        worldDensitySlider.value = SettingsStatic.LoadedSettings.terrainDensity;
        loadLDrawBaseFile.isOn = SettingsStatic.LoadedSettings.loadLdrawBaseFile;
        // if(Directory.Exists(Settings.AppSaveDataPath + "/saves/")) // disabled since we want to load value from settings file
        // {
        //     if (Directory.GetDirectories(Settings.AppSaveDataPath + "/saves/").Length == 0) // if no save files, create random world coord for world generation seed
        //         RandomizeWorldCoord();
        // }
        loadingSlider.gameObject.SetActive(false);

        SetPreviewBrickColor();
        SetPreviewSpriteImage();
        biomeNameText.text = biomes[SettingsStatic.LoadedSettings.biomeOverride].biomeName;
        SetPlanetNameText();
    }

    private void GetSaves()
    {
        // get list of strings of seeds
        string[] seedsArray = Directory.GetDirectories(Settings.AppSaveDataPath + "/saves/");

        if (seedsArray.Length == 0) // if no seeds, use current saved randomized one
        {
            noSaves = true;
            worldDropDown.options[worldDropDown.value].text = seedInputField.text;
            seeds.Add(randomSeed.ToString());
            SettingsStatic.LoadedSettings.worldCoord = randomSeed;
        }
        else
            noSaves = false;


        foreach (string seed in seedsArray)
        {
            string newstring;
            int index = seed.LastIndexOf("-");
            newstring = seed.Substring(index + 1);
            seeds.Add(newstring);
        }

        worldDropDown.ClearOptions();
        if (!seeds.Contains(SettingsStatic.LoadedSettings.worldCoord.ToString()))
            seeds.Add(SettingsStatic.LoadedSettings.worldCoord.ToString());
        worldDropDown.AddOptions(seeds);
    }

    public void IncrementBiome()
    {
        if (biomeIndex + 1 > 12)
            biomeIndex = 0;
        else
            biomeIndex++;
    }

    public void DecrementBiome()
    {
        if(biomeIndex - 1 < 0)
            biomeIndex = 12;
        else
            biomeIndex--;
    }

    public void CheckDensity()
    {
        if (worldDensitySlider.value <= 0.333f)
        {
            densityRange = 0;
        }
        else if (worldDensitySlider.value > 0.333f && worldDensitySlider.value < 0.666f)
        {
            densityRange = 2;
        }
        else
        {
            densityRange = 1;
        }

        worldDensityText.text = worldDensitySlider.value.ToString();
    }

    private void Update()
    {
        modelObjectToSpin.transform.parent.Rotate(new Vector3(0, 1, 0));
        SetPlanetNameText();
        SetPreviewSpriteImage();
        SetBiomeNameText();
        SetPreviewBrickColor();
    }

    public void SetPlanetNameText()
    {
        ConvertseedToInt();

        if (seed <= 18)
            planetNameText.text = planets[seed].planetName;
        else
            planetNameText.text = "?";
    }

    public void SetBiomeNameText()
    {
        if (seed == 3)
        {
            if (biomeIndex <= 11)
                biomeNameText.text = biomes[biomeIndex].biomeName;
            else
                biomeNameText.text = "Mixed";
        }
        else
            biomeNameText.text = "?";
        
    }

    public void SetPreviewSpriteImage()
    {
        ConvertseedToInt();

        if (seed == 3)
            biomeImage.sprite = biomes[biomeIndex].sprites[densityRange];
        else if(seed <= 18)
            biomeImage.sprite = planets[seed].sprites[densityRange];
        else
            biomeImage.sprite = planets[1].sprites[densityRange]; // dark image for mystery planet
    }

    public void ConvertseedToInt()
    {
        try
        {
            seed = System.Math.Abs(int.Parse(seedInputField.text)); // Int32 can hold up to 2,147,483,647 numbers
        }
        catch
        {
            seed = 3;
        }
    }

    public void SetPreviewBrickColor()
    {
        ConvertseedToInt();

        if (seed == 3)
            modelObjectToSpin.GetComponent<MeshRenderer>().material = biomes[biomeIndex].material;
        else if (seed <= 18)
            modelObjectToSpin.GetComponent<MeshRenderer>().material = planets[seed].material;
        else
            modelObjectToSpin.GetComponent<MeshRenderer>().material = planets[1].material; // black material for mystery planet
    }

    public void SetRenderDistance()
    {
        worldRenderText.text = worldRenderDistanceSlider.value.ToString();
    }

    public void Local()
    {
        buttonSound.Play();
        menuElements.SetActive(false);
        SaveSettings();

        Settings.OnlinePlay = false;
        loadingSlider.gameObject.SetActive(false); // disabled since this doesn't work

        if (Settings.Platform == 2)
        {
            SceneManager.LoadScene(6); // mobile VR loads smaller scene
            //levelLoader.LoadLevel(5, loadingSlider, loadingPercentageText); // doesn't work since most of level loading is done by world after scene is loaded
        }
        else
        {
            SceneManager.LoadScene(4);
            //levelLoader.LoadLevel(3, loadingSlider, loadingPercentageText); // doesn't work since most of level loading is done by world after scene is loaded
        }
    }

    public void Online()
    {
        buttonSound.Play();
        menuElements.SetActive(false);
        SaveSettings();

        Settings.OnlinePlay = true;

        if (Settings.Platform == 2)
        {
            SceneManager.LoadScene(6); // mobile VR loads smaller scene
            //levelLoader.LoadLevel(5, loadingSlider, loadingPercentageText); // doesn't work since most of level loading is done by world after scene is loaded
        }
        else
        {
            SceneManager.LoadScene(4);
            //levelLoader.LoadLevel(3, loadingSlider, loadingPercentageText); // doesn't work since most of level loading is done by world after scene is loaded
        }
    }

    public void Back()
    {
        buttonSound.Play();
        SaveSettings();
        SceneManager.LoadScene(1);
    }

    public void PlaySelectedWorld()
    {
        OnChangeWorldSelect(); // use value from world select (converted string to int)

        if(noSaves)
            CreateNewWorld();

        Local();
    }

    public void CreateNewWorld() // use input field value for seed
    {
        // make world coord equal input field value
        //try to load the saved world coord, otherwise default to 1
        try
        {
            int result = Int32.Parse(seedInputField.text); // Int32 can hold up to 2,147,483,647 numbers
            SettingsStatic.LoadedSettings.worldCoord = result;
        }
        catch (System.FormatException)
        {
            SettingsStatic.LoadedSettings.worldCoord = 1; // default value
        }
        //CopyModelFiles(); // broken, could not resolve issue with loading from Settings.CustomModelsPath in LDrawConfigRuntime
        Local();
    }

    public void CopyModelFiles()
    {
        string savePath = Settings.AppSaveDataPath + "/saves/" + SettingsStatic.LoadedSettings.planetSeed + "-" + SettingsStatic.LoadedSettings.worldCoord + "/";

        //copy all files from ldraw streamedAssets/ldraw/models folder to this folder
        if(!Settings.WebGL)
        {
            string sourceDir = Settings.ModelsPath;
            string destDir = savePath + "models/";
            Directory.CreateDirectory(destDir);
            Settings.CustomModelsPath = destDir; // save new model path to be referenced later by ldraw importer

            string[] modelFiles = Directory.GetFiles(sourceDir);
            foreach (string f in modelFiles)
            {
                string fName = f.Substring(sourceDir.Length);
                //Debug.Log("source: " + Path.Combine(sourceDir, fName));
                //Debug.Log("dest: " + Path.Combine(destDir, fName));
                File.Copy(Path.Combine(sourceDir, fName), Path.Combine(destDir, fName), true);
            }
        }
    }

    public void OnChangeWorldSelect() // convert string in world select to int and save it
    {
        //try to load the saved world coord, otherwise default to 1
        try
        {
            int result = Int32.Parse(worldDropDown.options[worldDropDown.value].text); // Int32 can hold up to 2,147,483,647 numbers
            SettingsStatic.LoadedSettings.worldCoord = result;
        }
        catch (System.FormatException)
        {
            SettingsStatic.LoadedSettings.worldCoord = 1; // default value
        }
        SaveSettings();
    }

    public void RandomizeSeed() // set input field to new random int
    {
        randomSeed = UnityEngine.Random.Range(1, 5000); // Int32 can hold up to 2,147,483,647 numbers

        //update seed input and saved world text to new random value
        seedInputField.text = randomSeed.ToString();
        if(noSaves)
        {
            //broken
            //worldDropDown.options[0].text = seedInputField.text;
            //worldDropDown[0].text = seedInputField.text; // error: shows different values for dropdown and option
        }
    }

    public void SaveSettings()
    {
        //SettingsStatic.LoadedSettings.playerName = playerNameInputField.text;
        SettingsStatic.LoadedSettings.developerMode = creativeMode.isOn;
        //SettingsStatic.LoadedSettings.viewDistance = (int)worldRenderDistanceSlider.value;
        //SettingsStatic.LoadedSettings.biomeOverride = biomeIndex;
        //SettingsStatic.LoadedSettings.worldCoord = seed;
        //SettingsStatic.LoadedSettings.terrainDensity = worldDensitySlider.value;
        //SettingsStatic.LoadedSettings.loadLdrawBaseFile = loadLDrawBaseFile.isOn;

        // Save setttings when this function is called, otherwise settings will load from latest settings file upon game start
        FileSystemExtension.SaveSettings();
    }
}
