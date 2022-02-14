using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SetupMenu : MonoBehaviour
{
    public GameObject menuElements;
    public GameObject loadingText;
    public TMP_InputField playerNameInputField;
    public Toggle toggleFlight;
    public TMP_InputField planetInputField;
    public TMP_InputField seedInputField;
    public GameObject charObIdle;
    public GameObject modelsObjectToSpin;
    
    public Slider worldRenderDistanceSlider;
    public TextMeshProUGUI worldRenderText;

    public Slider colorSlider;
    public Dropdown colorSelector;
    public Dropdown typeSelector;
    public Material[] playerMaterials;
    public AudioSource buttonSound;

    public int index;

    private void Awake()
    {
        // import this player's char model as a preview before entering the game
        SettingsStatic.LoadedSettings = SettingsStatic.LoadSettings();
    }

    private void Start()
    {
        //reset framerate of game for video animation
        Application.targetFrameRate = 60;
        
        playerNameInputField.text = SettingsStatic.LoadedSettings.playerName;
        toggleFlight.isOn = SettingsStatic.LoadedSettings.flight;
        worldRenderDistanceSlider.value = SettingsStatic.LoadedSettings.viewDistance;
        worldRenderText.text = SettingsStatic.LoadedSettings.viewDistance.ToString();
        planetInputField.text = SettingsStatic.LoadedSettings.planetNumber.ToString();
        seedInputField.text = SettingsStatic.LoadedSettings.seed.ToString();
        loadingText.SetActive(false);
    }

    private void Update()
    {
        modelsObjectToSpin.transform.Rotate(new Vector3(0, 1, 0));
    }

    public void SetRenderDistance()
    {
        worldRenderText.text = worldRenderDistanceSlider.value.ToString();
    }

    public void SetFlightEnabled()
    {
        buttonSound.Play();
    }

    public void Splitscreen()
    {
        buttonSound.Play();
        Settings.OnlinePlay = false;
        menuElements.SetActive(false);
        loadingText.SetActive(true);
        SaveSettings();
        SceneManager.LoadScene(3);
    }

    public void Online()
    {
        buttonSound.Play();
        Settings.OnlinePlay = true;
        menuElements.SetActive(false);
        SaveSettings();
        SceneManager.LoadScene(3);
    }

    public void Back()
    {
        buttonSound.Play();
        SaveSettings();
        SceneManager.LoadScene(0);
    }

    public void SaveSettings()
    {
        SettingsStatic.LoadedSettings.playerName = playerNameInputField.text;
        SettingsStatic.LoadedSettings.flight = toggleFlight.isOn;
        SettingsStatic.LoadedSettings.viewDistance = (int)worldRenderDistanceSlider.value;

        try
        {
            int result = System.Int32.Parse(planetInputField.text); // Int32 can hold up to 2,147,483,647 numbers
            SettingsStatic.LoadedSettings.planetNumber = result;
        }
        catch (System.FormatException)
        {
            SettingsStatic.LoadedSettings.planetNumber = 3;
        }

        try
        {
            int result = System.Int32.Parse(seedInputField.text); // Int32 can hold up to 2,147,483,647 numbers
            SettingsStatic.LoadedSettings.seed = result;
        }
        catch (System.FormatException)
        {
            SettingsStatic.LoadedSettings.seed = 5;
        }

        // Save setttings when this function is called, otherwise settings will load from latest settings file upon game start
        string jsonExport = JsonUtility.ToJson(SettingsStatic.LoadedSettings);
        string path;
        if (Application.isMobilePlatform)
        {
            path = Application.persistentDataPath + "/settings.cfg";
        }
        else
            path = Application.streamingAssetsPath + "/settings.cfg";
        File.WriteAllText(path, jsonExport);
    }
}
