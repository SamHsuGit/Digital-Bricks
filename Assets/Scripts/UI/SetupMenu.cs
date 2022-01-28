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
        worldRenderDistanceSlider.value = SettingsStatic.LoadedSettings.drawDistance;
        worldRenderText.text = SettingsStatic.LoadedSettings.drawDistance.ToString();
        planetInputField.text = SettingsStatic.LoadedSettings.planetNumber.ToString();
        seedInputField.text = SettingsStatic.LoadedSettings.seed.ToString();
        loadingText.SetActive(false);

        GetImportedCharModelAfterAwake();
    }

    private void GetImportedCharModelAfterAwake()
    {
        // has to occur after Awake since the importer needs time to import during awake
        charObIdle = LDrawImportRuntime.Instance.charObIdle;
        charObIdle.transform.parent = modelsObjectToSpin.transform;
        modelsObjectToSpin.transform.Translate(new Vector3(0, 0, charObIdle.GetComponent<BoxCollider>().size.y * 0.025f));

        charObIdle.SetActive(true);
        charObIdle.transform.localPosition = new Vector3(0, charObIdle.GetComponent<BoxCollider>().center.y * LDrawImportRuntime.Instance.scale, 0);

        Destroy(LDrawImportRuntime.Instance.baseOb);
        Destroy(LDrawImportRuntime.Instance.projectileOb);
    }

    private void Update()
    {
        modelsObjectToSpin.transform.Rotate(new Vector3(0, 1, 0));

    }

    public void SetRenderDistance()
    {
        worldRenderText.text = worldRenderDistanceSlider.value.ToString();
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
        SettingsStatic.LoadedSettings.drawDistance = (int)worldRenderDistanceSlider.value;

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
        File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
    }
}
