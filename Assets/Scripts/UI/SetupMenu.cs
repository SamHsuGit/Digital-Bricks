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
    public GameObject[] charTypeModels;
    public GameObject[] helmet;
    public GameObject[] armor;
    public GameObject modelsObjectToSpin;
    public int currentIndexChar;
    public int currentIndexHelmet;
    public int currentIndexArmor;
    
    public Slider worldRenderDistanceSlider;
    public TextMeshProUGUI worldRenderText;

    public Slider colorSlider;
    public Dropdown limbSelect;
    public Dropdown typeSelect;
    public Material[] playerMaterials;
    public AudioSource buttonSound;

    int selectedLimb;
    int selectedType;

    List<int> helmetTypes;
    List<int> armorTypes;
    List<Vector2> restrictedCombos;

    private void Awake()
    {
        SettingsStatic.LoadedSettings = SettingsStatic.LoadSettings();
        currentIndexChar = SettingsStatic.LoadedSettings.playerTypeChar;
        currentIndexHelmet = SettingsStatic.LoadedSettings.playerTypeHelmet;
        currentIndexArmor = SettingsStatic.LoadedSettings.playerTypeArmor;
        for (int i = 0; i < charTypeModels.Length; i++)
            charTypeModels[i].SetActive(false);
        for (int i = 0; i < helmet.Length; i++)
            helmet[i].SetActive(false);
        for (int i = 0; i < armor.Length; i++)
            armor[i].SetActive(false);
        charTypeModels[currentIndexChar].SetActive(true);
        helmet[currentIndexHelmet].SetActive(true);
        armor[currentIndexArmor].SetActive(true);

        helmetTypes = new List<int>()
        {
            7,
            3,
            5,
            5,
            5,
            5,
            5,
            2,
            5,
            5,
            5,
            2,
            3,
            3,
            3,
            3,
            3,
            3,
            3,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            3,
            3,
            2,
            5,
            5,
            0,
            1,
            4,
            3,
            1,
            1,
            3,
            4,
            5,
            5,
            3
        };

        armorTypes = new List<int>()
        {
            6,
            2,
            1,
            2,
            1,
            1,
            1,
            1,
            1,
            3,
            0,
            3,
            5,
            5,
            5,
            0,
            2,
            3,
            1,
            3,
            3,
            3,
            1,
            2,
            1,
            1,
            2,
            1,
            0,
            1,
            1,
            3,
            4,
            5,
            3,
            3,
            3,
            5,
            5,
            5,
            3,
            2,
            0,
            1,
            1,
            1,
            1,
            1,
            5,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            5,
            1,
            1,
            1,
            3,
            3,
            1,
            1,
            1,
            1,
            1,
            1,
            5,
            1,
            5,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            0,
            3,
            1,
            2,
            1,
            0,
            4,
            1,
            1,
            5,
            5,
            5,
            1,
            1,
            3,
            3,
            5,
            5,
            1,
            1,
            0,
            1,
            1,
            1,
            5,
            1,
            3,
            1,
            1,
            1,
            1,
            1,
            1,
            0,
            1,
            0,
            1,
            4,
            2,
            2,
            2,
            1,
            0,
            3,
            4,
            4,
            5,
            0,
            5,
            5,
            2,
            1,
            1,
            1
        };

        // Matrix of restricted helmet and armor type combinations to prevent players from selecting combos which create interferences
        // Syntax = Vector2(helmet, armor)
        // helmet           armor
        // 0 = all          0 = all
        // 1 = head         1 = head
        // 2 = sides        2 = sides
        // 3 = back         3 = back
        // 4 = front        4 = front
        // 5 = backsides    5 = frontback
        // 6 = none         6 = none
        restrictedCombos = new List<Vector2>()
        {
            new Vector2(0,0),
            new Vector2(0,1),
            new Vector2(0,2),
            new Vector2(0,3),
            new Vector2(0,4),
            new Vector2(0,5),
            new Vector2(1,0),
            new Vector2(2,0),
            new Vector2(3,0),
            new Vector2(4,0),
            new Vector2(5,0),
            new Vector2(1,1),
            new Vector2(2,2),
            new Vector2(3,3),
            new Vector2(3,5),
            new Vector2(4,4),
            new Vector2(4,5),
            new Vector2(5,2),
            new Vector2(5,3)
        };

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

    public void SelectType()
    {
        buttonSound.Play();
        selectedType = typeSelect.value;
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
        int index = 0;
        GameObject[] array = new GameObject[] { };

        switch (selectedType)
        {
            case 0: // charType
                {
                    index = currentIndexChar;
                    array = charTypeModels;
                    break;
                }
            case 1: // helmetType
                {
                    index = currentIndexHelmet;
                    array = helmet;
                    break;
                }
            case 2: // armorType
                {
                    index = currentIndexArmor;
                    array = armor;
                    break;
                }
        }

        index++;
        int gameObjectCount = array.Length - 1;

        // if selected type is helmet, check currentIndexHelmet against list, if matches, mark armor as none
        if((selectedType == 1 && currentIndexArmor != 0) || (selectedType == 2 && currentIndexHelmet != 0)) // if choosing helmet and armor selected, or if choosing armor and helmet selected
        {
            // while current combo is restricted, toggle next index
            if (CurrentComboIsRestricted())
                Debug.Log("interference");
        }

        if(index > gameObjectCount)
        {
            index = 0;
            array[gameObjectCount].SetActive(false);
            array[index].SetActive(true);
        }
        else
        {
            array[index - 1].SetActive(false);
            array[index].SetActive(true);
        }

        UpdateFromIndex(index);
    }

    bool CurrentComboIsRestricted()
    {
        Vector2 combo = new Vector2(currentIndexHelmet, currentIndexArmor);
        bool restricted = false;
        foreach (Vector2 restrictedCombo in restrictedCombos)
        {
            if (combo == restrictedCombo)
                restricted = true;
        }
        return restricted;
    }

    void UpdateFromIndex(int _index)
    {
        switch (selectedType)
        {
            case 0: // charType
                currentIndexChar = _index;
                break;
            case 1: // helmetType
                currentIndexHelmet = _index;
                break;
            case 2: // armorType
                currentIndexArmor = _index;
                break;
        }
    }

    public void Previous()
    {
        buttonSound.Play();
        int index = 0;
        GameObject[] array = new GameObject[] { };

        switch (selectedType)
        {
            case 0: // charType
                {
                    index = currentIndexChar;
                    array = charTypeModels;
                    break;
                }
            case 1: // helmetType
                {
                    index = currentIndexHelmet;
                    array = helmet;
                    break;
                }
            case 2: // armorType
                {
                    index = currentIndexArmor;
                    array = armor;
                    break;
                }
        }

        index--;
        int gameObjectCount = array.Length - 1;
        if(index < 0)
        {
            index = gameObjectCount;
            array[0].SetActive(false);
            array[index].SetActive(true);
        }
        else
        {
            array[index + 1].SetActive(false);
            array[index].SetActive(true);
        }

        UpdateFromIndex(index);
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
        SettingsStatic.LoadedSettings.playerTypeChar = currentIndexChar;

        SettingsStatic.LoadedSettings.playerTypeHelmet = currentIndexHelmet;
        SettingsStatic.LoadedSettings.playerTypeArmor = currentIndexArmor;

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
        for (int i = 0; i < LDrawColors.colorLib.Count; i++) // Helmet
        {
            if (LDrawColors.colorLib[i] == LDrawColors.savedPlayerColors[5])
                SettingsStatic.LoadedSettings.playerColorHelmet = i;
        }
        for (int i = 0; i < LDrawColors.colorLib.Count; i++) // Armor
        {
            if (LDrawColors.colorLib[i] == LDrawColors.savedPlayerColors[6])
                SettingsStatic.LoadedSettings.playerColorArmor = i;
        }

        SettingsStatic.LoadedSettings.playerName = playerNameInputField.text;
        SettingsStatic.LoadedSettings.drawDistance = (int)worldRenderDistanceSlider.value;
        try
        {
            int result = System.Int32.Parse(seedInputField.text); // Int32 can hold up to 2,147,483,647 numbers
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
