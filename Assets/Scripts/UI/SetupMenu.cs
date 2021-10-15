using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class SetupMenu : MonoBehaviour
{
    public GameObject menuElements;
    public GameObject loadingText;
    public TMP_InputField playerNameInputField;
    public TMP_InputField seedInputField;
    private int[] charTypes;
    public GameObject[] charTypeModels;
    public GameObject modelsObjectToSpin;
    public int currentCharType;
    
    public Slider worldRenderDistanceSlider;
    public TextMeshProUGUI worldRenderText;

    public Slider colorSlider;
    public Dropdown limbSelect;
    public Material[] playerMaterials;
    public AudioSource buttonSound;

    int selectedLimb;

    private void Awake()
    {
        SettingsStatic.LoadedSettings = SettingsStatic.LoadSettings();
        currentCharType = SettingsStatic.LoadedSettings.charType;
        charTypes = new int[1];
        for (int i = 0; i < charTypes.Length; i++)
            charTypeModels[i].SetActive(false);
        charTypeModels[currentCharType].SetActive(true);

        selectedLimb = 0;

        LDrawColors.GenerateColorLib();

        LDrawColors.GetSavedColorHexValues();

        for (int i = 0; i < LDrawColors.savedPlayerColors.Length; i++) // for all player limb materials
        {
            if (ColorUtility.TryParseHtmlString(LDrawColors.savedPlayerColors[i], out Color newCol))
                playerMaterials[i].color = newCol; // set the material from saved hex values
        }
    }

    private void Start()
    {
        //reset framerate of game for video animation
        Application.targetFrameRate = 60;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // always sets default limb select as torso upon opening
        limbSelect.value = 0;
        colorSlider.value = SettingsStatic.LoadedSettings.playerColorTorso; // loads value from save file
        
        playerNameInputField.text = SettingsStatic.LoadedSettings.playerName;
        worldRenderDistanceSlider.value = SettingsStatic.LoadedSettings.drawDistance;
        worldRenderText.text = SettingsStatic.LoadedSettings.drawDistance.ToString();
        seedInputField.text = SettingsStatic.LoadedSettings.seed.ToString();
    }

    private void Update()
    {
        modelsObjectToSpin.transform.Rotate(new Vector3(0, 1, 0));
    }

    public void SelectLimb()
    {
        buttonSound.Play();
        selectedLimb = limbSelect.value;
    }

    public void SetColor()
    {
        string htmlValue = LDrawColors.colorLib[(int)colorSlider.value];
        Color newCol;

        if(ColorUtility.TryParseHtmlString(htmlValue, out newCol))
        {
            playerMaterials[selectedLimb].SetColor("_BaseColor", newCol);
            LDrawColors.savedPlayerColors[selectedLimb] = htmlValue;
        }
    }

    public void SetRenderDistance()
    {
        worldRenderText.text = worldRenderDistanceSlider.value.ToString();
    }

    public void Next()
    {
        buttonSound.Play();
        currentCharType++;
        if (currentCharType > charTypes.Length)
        {
            currentCharType = 0;
            charTypeModels[charTypes.Length].SetActive(false);
            charTypeModels[currentCharType].SetActive(true);
        }
        else
        {
            charTypeModels[currentCharType - 1].SetActive(false);
            charTypeModels[currentCharType].SetActive(true);
        }
    }

    public void Previous()
    {
        buttonSound.Play();
        currentCharType--;
        if (currentCharType < 0)
        {
            currentCharType = charTypes.Length;
            charTypeModels[0].SetActive(false);
            charTypeModels[currentCharType].SetActive(true);
        }  
        else
        {
            charTypeModels[currentCharType + 1].SetActive(false);
            charTypeModels[currentCharType].SetActive(true);
        }
    }

    public void Tutorial()
    {
        buttonSound.Play();
        Settings.OnlinePlay = false;
        menuElements.SetActive(false);
        loadingText.SetActive(true);
        SaveSettings();
        SceneManager.LoadScene(4);
    }

    public void Splitscreen()
    {
        buttonSound.Play();
        Settings.OnlinePlay = false;
        menuElements.SetActive(false);
        loadingText.SetActive(true);
        SaveSettings();
        SceneManager.LoadScene(2);
    }

    public void Online()
    {
        buttonSound.Play();
        Settings.OnlinePlay = true;
        menuElements.SetActive(false);
        SaveSettings();
        SceneManager.LoadScene(2);
    }

    public void Back()
    {
        buttonSound.Play();
        SaveSettings();
        SceneManager.LoadScene(0);
    }

    public void SaveSettings()
    {
        SettingsStatic.LoadedSettings.charType = currentCharType;

        // Save Colors for next time
        for (int i = 0; i < LDrawColors.colorLib.Count; i++) // Torso
        {
            if (LDrawColors.colorLib[i] == LDrawColors.savedPlayerColors[0])
                SettingsStatic.LoadedSettings.playerColorTorso = i;
        }
        for (int i = 0; i < LDrawColors.colorLib.Count; i++) // ArmL
        {
            if (LDrawColors.colorLib[i] == LDrawColors.savedPlayerColors[1])
                SettingsStatic.LoadedSettings.playerColorArmL = i;
        }
        for (int i = 0; i < LDrawColors.colorLib.Count; i++) // ArmR
        {
            if (LDrawColors.colorLib[i] == LDrawColors.savedPlayerColors[2])
                SettingsStatic.LoadedSettings.playerColorArmR = i;
        }
        for (int i = 0; i < LDrawColors.colorLib.Count; i++) // LegL
        {
            if (LDrawColors.colorLib[i] == LDrawColors.savedPlayerColors[3])
                SettingsStatic.LoadedSettings.playerColorLegL = i;
        }
        for (int i = 0; i < LDrawColors.colorLib.Count; i++) // LegR
        {
            if (LDrawColors.colorLib[i] == LDrawColors.savedPlayerColors[4])
                SettingsStatic.LoadedSettings.playerColorLegR = i;
        }

        SettingsStatic.LoadedSettings.playerName = playerNameInputField.text;
        SettingsStatic.LoadedSettings.drawDistance = (int)worldRenderDistanceSlider.value;
        try
        {
            ulong result = System.UInt64.Parse(seedInputField.text);
            SettingsStatic.LoadedSettings.seed = result;
        }
        catch (System.FormatException)
        {
            SettingsStatic.LoadedSettings.seed = 1234;
        }

        // Save setttings when this function is called, otherwise settings will load from latest settings file upon game start
        string jsonExport = JsonUtility.ToJson(SettingsStatic.LoadedSettings);
        File.WriteAllText(Application.dataPath + "/settings.cfg", jsonExport);
    }
}
