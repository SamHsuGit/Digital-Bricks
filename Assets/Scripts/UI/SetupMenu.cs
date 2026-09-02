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
    public Slider loadingSlider;
    public TextMeshProUGUI loadingPercentageText;
    public Toggle creativeMode;
    public TMP_Dropdown worldDropDown;
    public TMP_InputField seedInputField;
    public Toggle loadLDrawBaseFile;
    public GameObject modelObjectToSpin;
    public GameObject levelLoaderObject;
    public AudioSource buttonSound;
    private LevelLoader levelLoader;

    public int seed;
    public int randomSeed;
    public bool noSaves = false;
    public List<string> seeds;

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
        creativeMode.isOn = SettingsStatic.LoadedSettings.developerMode;
        loadingSlider.gameObject.SetActive(false);
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

    private void Update()
    {
        modelObjectToSpin.transform.parent.Rotate(new Vector3(0, 1, 0));
    }

    public void ConvertSeedStringToInt()
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

    public void Play()
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

        Play();
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
        Play();
    }

    public void CopyModelFiles() // Not currently implemented, was testing to see if could read files from world files instead of streamingAssets folder
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
        //SettingsStatic.LoadedSettings.developerMode = creativeMode.isOn; // set manually not with UI button

        // Save setttings when this function is called, otherwise settings will load from latest settings file upon game start
        FileSystemExtension.SaveSettings();
    }
}
