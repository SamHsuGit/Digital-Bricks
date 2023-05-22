using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class SetupMenu : MonoBehaviour
{
    public GameObject menuElements;
    public Slider loadingSlider;
    public TextMeshProUGUI loadingPercentageText;
    public TMP_InputField playerNameInputField;
    public Toggle creativeMode;
    public Toggle randomizeWorldCoord;
    public TMP_InputField planetSeedInputField;
    public TMP_InputField worldCoordInputField;
    public GameObject modelsObjectToSpin;
    public GameObject levelLoaderObject;
    
    public Slider worldRenderDistanceSlider;
    public TextMeshProUGUI worldRenderText;

    public AudioSource buttonSound;

    public int index;

    private LevelLoader levelLoader;

    private void Awake()
    {
        // import this player's char model as a preview before entering the game
        SettingsStatic.LoadedSettings = SettingsStatic.LoadSettings();

        levelLoader = levelLoaderObject.GetComponent<LevelLoader>();
    }

    private void Start()
    {
        //reset framerate of game for video animation
        Application.targetFrameRate = 60;
        
        playerNameInputField.text = SettingsStatic.LoadedSettings.playerName;
        creativeMode.isOn = SettingsStatic.LoadedSettings.creativeMode;
        worldRenderDistanceSlider.value = SettingsStatic.LoadedSettings.viewDistance;
        worldRenderText.text = SettingsStatic.LoadedSettings.viewDistance.ToString();
        planetSeedInputField.text = SettingsStatic.LoadedSettings.planetSeed.ToString();
        worldCoordInputField.text = SettingsStatic.LoadedSettings.worldCoord.ToString();
        if (SettingsStatic.LoadedSettings.worldCoord == 0)
            RandomizeWorldCoord();
        loadingSlider.gameObject.SetActive(false);
    }

    private void Update()
    {
        modelsObjectToSpin.transform.Rotate(new Vector3(0, 1, 0));
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
        loadingSlider.gameObject.SetActive(true);

        if (Settings.Platform == 2)
        {
            SceneManager.LoadScene(5); // mobile VR loads smaller scene
            //levelLoader.LoadLevel(5, loadingSlider, loadingPercentageText); // doesn't work since most of level loading is done by world after scene is loaded
        }
        else
        {
            SceneManager.LoadScene(3);
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
            SceneManager.LoadScene(5); // mobile VR loads smaller scene
            //levelLoader.LoadLevel(5, loadingSlider, loadingPercentageText); // doesn't work since most of level loading is done by world after scene is loaded
        }
        else
        {
            SceneManager.LoadScene(3);
            //levelLoader.LoadLevel(3, loadingSlider, loadingPercentageText); // doesn't work since most of level loading is done by world after scene is loaded
        }
    }

    public void Back()
    {
        buttonSound.Play();
        SaveSettings();
        SceneManager.LoadScene(0);
    }

    public void RandomizeWorldCoord()
    {
        worldCoordInputField.text = Random.Range(1, 5000).ToString();
        randomizeWorldCoord.isOn = false;
    }

    public void SaveSettings()
    {
        SettingsStatic.LoadedSettings.playerName = playerNameInputField.text;
        SettingsStatic.LoadedSettings.creativeMode = creativeMode.isOn;
        SettingsStatic.LoadedSettings.viewDistance = (int)worldRenderDistanceSlider.value;

        //// try to load the saved planet seed, otherwise default to 3
        //try
        //{
        //    int planetSeed = int.Parse(planetSeedInputField.text); // Int32 can hold up to 2,147,483,647 numbers
        //    SettingsStatic.LoadedSettings.planetSeed = planetSeed;
        //}
        //catch (System.FormatException)
        //{
        //    SettingsStatic.LoadedSettings.planetSeed = 3;
        //}

        //// try to load the saved world coord, otherwise default to 1
        //try
        //{
        //    int result = int.Parse(worldCoordInputField.text); // Int32 can hold up to 2,147,483,647 numbers
        //    SettingsStatic.LoadedSettings.worldCoord = result;
        //}
        //catch (System.FormatException)
        //{
        //    SettingsStatic.LoadedSettings.worldCoord = 1; // default value
        //}

        // try to load the saved planet seed, otherwise default to 3
        try
        {
            int planetSeed = int.Parse(planetSeedInputField.text); // Int32 can hold up to 2,147,483,647 numbers
            SettingsStatic.LoadedSettings.planetSeed = planetSeed;
        }
        catch (System.FormatException)
        {
            SettingsStatic.LoadedSettings.planetSeed = 3;
        }

        // try to load the saved world coord, otherwise default to 1
        try
        {
            int result = int.Parse(worldCoordInputField.text); // Int32 can hold up to 2,147,483,647 numbers
            SettingsStatic.LoadedSettings.worldCoord = result;
        }
        catch (System.FormatException)
        {
            SettingsStatic.LoadedSettings.worldCoord = 1; // default value
        }

        // Save setttings when this function is called, otherwise settings will load from latest settings file upon game start
        FileSystemExtension.SaveSettings();
    }
}
