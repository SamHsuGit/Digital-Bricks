using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

public class SetupMenu : MonoBehaviour
{
    public GameObject menuElements;
    public Planet[] planets;
    public Biome[] biomes;
    public Slider loadingSlider;
    public TextMeshProUGUI loadingPercentageText;
    public TMP_InputField playerNameInputField;
    public Toggle creativeMode;
    public Toggle randomizeWorldCoord;
    public Toggle loadLDrawBaseFile;
    public TMP_InputField planetSeedInputField;
    public TMP_InputField worldCoordInputField;
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

    public int biomeIndex;
    int planetSeed;
    public int densityRange;

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
        creativeMode.isOn = SettingsStatic.LoadedSettings.developerMode;
        worldRenderDistanceSlider.value = SettingsStatic.LoadedSettings.viewDistance;
        worldRenderText.text = SettingsStatic.LoadedSettings.viewDistance.ToString();
        planetSeedInputField.text = SettingsStatic.LoadedSettings.planetSeed.ToString();
        biomeIndex = SettingsStatic.LoadedSettings.biomeOverride;
        worldDensitySlider.value = SettingsStatic.LoadedSettings.terrainDensity;
        worldCoordInputField.text = SettingsStatic.LoadedSettings.worldCoord.ToString();
        loadLDrawBaseFile.isOn = SettingsStatic.LoadedSettings.loadLdrawBaseFile;
        if(Directory.Exists(Settings.AppSaveDataPath + "/saves/"))
        {
            if (Directory.GetDirectories(Settings.AppSaveDataPath + "/saves/").Length == 0) // if no save files, create random world coord for world generation seed
                RandomizeWorldCoord();
        }
        loadingSlider.gameObject.SetActive(false);

        SetPreviewBrickColor();
        SetPreviewSpriteImage();
        biomeNameText.text = biomes[SettingsStatic.LoadedSettings.biomeOverride].biomeName;
        SetPlanetNameText();
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
        ConvertPlanetSeedToInt();

        if (planetSeed <= 18)
            planetNameText.text = planets[planetSeed].planetName;
        else
            planetNameText.text = "?";
    }

    public void SetBiomeNameText()
    {
        if (planetSeed == 3)
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
        ConvertPlanetSeedToInt();

        if (planetSeed == 3)
            biomeImage.sprite = biomes[biomeIndex].sprites[densityRange];
        else if(planetSeed <= 18)
            biomeImage.sprite = planets[planetSeed].sprites[densityRange];
        else
            biomeImage.sprite = planets[1].sprites[densityRange]; // dark image for mystery planet
    }

    public void ConvertPlanetSeedToInt()
    {
        try
        {
            planetSeed = System.Math.Abs(int.Parse(planetSeedInputField.text)); // Int32 can hold up to 2,147,483,647 numbers
        }
        catch
        {
            planetSeed = 3;
        }
    }

    public void SetPreviewBrickColor()
    {
        ConvertPlanetSeedToInt();

        if (planetSeed == 3)
            modelObjectToSpin.GetComponent<MeshRenderer>().material = biomes[biomeIndex].material;
        else if (planetSeed <= 18)
            modelObjectToSpin.GetComponent<MeshRenderer>().material = planets[planetSeed].material;
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
        loadingSlider.gameObject.SetActive(true);

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

    public void RandomizeWorldCoord()
    {
        worldCoordInputField.text = Random.Range(1, 5000).ToString();
        randomizeWorldCoord.isOn = false;
    }

    public void SaveSettings()
    {
        SettingsStatic.LoadedSettings.playerName = playerNameInputField.text;
        SettingsStatic.LoadedSettings.developerMode = creativeMode.isOn;
        SettingsStatic.LoadedSettings.viewDistance = (int)worldRenderDistanceSlider.value;
        SettingsStatic.LoadedSettings.planetSeed = planetSeed;
        SettingsStatic.LoadedSettings.biomeOverride = biomeIndex;
        SettingsStatic.LoadedSettings.terrainDensity = worldDensitySlider.value;
        SettingsStatic.LoadedSettings.loadLdrawBaseFile = loadLDrawBaseFile.isOn;

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
