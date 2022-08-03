using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SetupMenu : MonoBehaviour
{
    public GameObject menuElements;
    public GameObject loadingText;
    public TMP_InputField playerNameInputField;
    public Toggle creativeMode;
    public TMP_InputField planetInputField;
    public TMP_InputField seedInputField;
    public GameObject modelsObjectToSpin;
    
    public Slider worldRenderDistanceSlider;
    public Slider worldSizeInChunksSlider;
    public TextMeshProUGUI worldRenderText;
    public TextMeshProUGUI worldSizeInChunksText;

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
        creativeMode.isOn = SettingsStatic.LoadedSettings.creativeMode;
        worldRenderDistanceSlider.value = SettingsStatic.LoadedSettings.viewDistance;
        worldSizeInChunksSlider.value = SettingsStatic.LoadedSettings.worldSizeinChunks;
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

    public void SetWorldSize()
    {
        worldSizeInChunksText.text = worldSizeInChunksSlider.value.ToString();
    }

    public void Local()
    {
        buttonSound.Play();
        menuElements.SetActive(false);
        SaveSettings();

        Settings.OnlinePlay = false;
        loadingText.SetActive(true);

        if (Settings.Platform == 2)
            SceneManager.LoadScene(5); // mobile loads smaller scene
        else
            SceneManager.LoadScene(3);
    }

    public void Online()
    {
        buttonSound.Play();
        menuElements.SetActive(false);
        SaveSettings();

        Settings.OnlinePlay = true;

        if (Settings.Platform == 2)
            SceneManager.LoadScene(5); // mobile loads smaller scene
        else
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
        SettingsStatic.LoadedSettings.creativeMode = creativeMode.isOn;
        SettingsStatic.LoadedSettings.viewDistance = (int)worldRenderDistanceSlider.value;
        SettingsStatic.LoadedSettings.worldSizeinChunks = (int)worldSizeInChunksSlider.value;

        if(SettingsStatic.LoadedSettings.worldSizeinChunks < 5) // hard limit on how many chunks can render at low end due to load and draw issues with low # of chunks
            SettingsStatic.LoadedSettings.worldSizeinChunks = 5;

        try
        {
            int result = int.Parse(planetInputField.text); // Int32 can hold up to 2,147,483,647 numbers
            SettingsStatic.LoadedSettings.planetNumber = result;
        }
        catch (System.FormatException)
        {
            SettingsStatic.LoadedSettings.planetNumber = 3;
        }

        try
        {
            int result = int.Parse(seedInputField.text); // Int32 can hold up to 2,147,483,647 numbers
            SettingsStatic.LoadedSettings.seed = result;
        }
        catch (System.FormatException)
        {
            SettingsStatic.LoadedSettings.seed = 1; // default value
        }

        // Save setttings when this function is called, otherwise settings will load from latest settings file upon game start
        FileSystemExtension.SaveSettings();
    }
}
