using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;
using Mirror;
using TMPro;

public class GameMenu : MonoBehaviour
{
    public Slider hpSlider;
    public Slider volumeSlider;
    public Slider lookSpeedSlider;
    public Slider lookAccelerationSlider;
    public Slider fovSlider;
    public Dropdown graphicsQualityDropdown;
    public Toggle fullScreenToggle;
    public Toggle invertYToggle;
    public GameObject player;
    public GameObject playerCamera;
    public AudioMixer masterAudioMixer;
    public GameObject GameMenuCanvas;
    public GameObject backgroundMask;
    public GameObject playerHUD;
    public GameObject optionsMenu;
    public GameObject debugText;
    public TextMeshProUGUI PlanetSeedWorldCoordText;
    public AudioSource buttonSound;
    public GameObject basicControlsText;
    public GameObject brickControlsText;
    public GameObject camControlsText;
    public bool showControls;

    private int previousGraphicsQuality;
    private Material newMaterial;

    CanvasGroup backgroundMaskCanvasGroup;
    CanvasGroup playerHUDCanvasGroup;
    CanvasGroup optionsMenuCanvasGroup;
    Lighting lighting;
    Controller controller;
    Canvas canvas;
    Health health;
    CustomNetworkManager customNetworkManager;

    UnityEngine.Rendering.Universal.UniversalAdditionalCameraData uac;

    private void Awake()
    {
        backgroundMaskCanvasGroup = backgroundMask.GetComponent<CanvasGroup>();
        playerHUDCanvasGroup = playerHUD.GetComponent<CanvasGroup>();
        optionsMenuCanvasGroup = optionsMenu.GetComponent<CanvasGroup>();
        controller = player.GetComponent<Controller>();
        canvas = GetComponent<Canvas>();
        health = player.GetComponent<Health>();
        showControls = SettingsStatic.LoadedSettings.showControls;
    }

    private void Start()
    {
        // these must happen in start since world is not instantiated until after Awake...
        lighting = World.Instance.globalLighting;
        customNetworkManager = World.Instance.customNetworkManager;

        // set settings from loaded saved file
        volumeSlider.value = SettingsStatic.LoadedSettings.volume;
        lookSpeedSlider.value = SettingsStatic.LoadedSettings.lookSpeed;
        lookAccelerationSlider.value = SettingsStatic.LoadedSettings.lookAccel;
        fovSlider.value = SettingsStatic.LoadedSettings.fov;
        graphicsQualityDropdown.value = SettingsStatic.LoadedSettings.graphicsQuality;
        QualitySettings.SetQualityLevel(SettingsStatic.LoadedSettings.graphicsQuality);
        fullScreenToggle.isOn = SettingsStatic.LoadedSettings.fullscreen;
        invertYToggle.isOn = SettingsStatic.LoadedSettings.invertY;
        UpdateHP();

        uac = playerCamera.GetComponent<Camera>().GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

        // initialize objects when starting the game
        playerHUDCanvasGroup.alpha = 1;
        playerHUDCanvasGroup.interactable = true;
        optionsMenuCanvasGroup.alpha = 0;
        optionsMenuCanvasGroup.interactable = false;
        debugText.SetActive(false);
        basicControlsText.SetActive(false);
    }

    private void Update()
    {
        CheckSplitscreenCanvasRenderMode();

        if (showControls)
        {
            if (controller.camMode != 1)
            {
                basicControlsText.SetActive(false);
                brickControlsText.SetActive(false);
                camControlsText.SetActive(true);
            }
            else if (controller.camMode == 1 && (controller.holdingBuild || (controller.holdingGrab && controller.heldObjectIsBrick)))
            {
                basicControlsText.SetActive(false);
                brickControlsText.SetActive(true);
                camControlsText.SetActive(false);
            }
            else
            {
                basicControlsText.SetActive(true);
                brickControlsText.SetActive(false);
                camControlsText.SetActive(false);
            }
        }
        else
        {
            camControlsText.SetActive(false);
            brickControlsText.SetActive(false);
            basicControlsText.SetActive(false);
        }
    }

    public void UpdateHP()
    {
        hpSlider.value = (float)health.hp / (float)health.hpMax;
    }

    void CheckSplitscreenCanvasRenderMode()
    {
        if (World.Instance.playerCount < 3)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // if only 1 player (+ 1 worldPlayer dummy player) then set options to render infront of other objects
        else
            canvas.renderMode = RenderMode.ScreenSpaceCamera; // if multiplayer splitscreen, need to keep UI canvas as screenspace camera so splitscreen ui can work.
    }

    public void OnOptions()
    {
        if (backgroundMaskCanvasGroup.alpha == 1) // if menu already shown
            return;
        SettingsStatic.LoadedSettings = SettingsStatic.LoadSettings();

        uac.renderPostProcessing = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        backgroundMaskCanvasGroup.alpha = 1;
        playerHUDCanvasGroup.alpha = 0;
        playerHUDCanvasGroup.interactable = false;
        optionsMenuCanvasGroup.alpha = 1;
        optionsMenuCanvasGroup.interactable = true;
        PlanetSeedWorldCoordText.text = "Planet Seed = " + SettingsStatic.LoadedSettings.planetSeed + ", World Coord = " + SettingsStatic.LoadedSettings.worldCoord;
    }

    public void ReturnToGame()
    {
        if (backgroundMaskCanvasGroup.alpha == 0) // if menu already disabled
            return;
        buttonSound.Play();

        uac.renderPostProcessing = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        backgroundMaskCanvasGroup.alpha = 0;
        playerHUDCanvasGroup.alpha = 1;
        playerHUDCanvasGroup.interactable = true;
        optionsMenuCanvasGroup.alpha = 0;
        optionsMenuCanvasGroup.interactable = false;

        SaveSettings(); // save settings when exiting menu
    }

    public void Save()
    {
        buttonSound.Play();
        // before quit, save world
        controller.RequestSaveWorld(); // server host and clients must save world before clients disconnect
        // before quit, save settings
        SaveSettings();
    }

    public void SaveSettings()
    {
        // Save setttings when this function is called, otherwise settings will load from latest settings file upon game start
        SettingsStatic.LoadedSettings.timeOfDay = lighting.timeOfDay; // only write this value when saving instead of every frame update
        SettingsStatic.LoadedSettings.currentBrickType = controller.currentBrickType;
        SettingsStatic.LoadedSettings.currentBrickIndex = controller.currentBrickIndex;
        SettingsStatic.LoadedSettings.currentBrickRotation = controller.currentBrickRotation;
        SettingsStatic.LoadedSettings.showControls = showControls;

        World.Instance.SetUndrawVoxels();

        FileSystemExtension.SaveSettings();
        SettingsStatic.LoadSettings();

        if(previousGraphicsQuality != SettingsStatic.LoadedSettings.graphicsQuality)
        {
            if (SettingsStatic.LoadedSettings.graphicsQuality == 0)
                newMaterial = World.Instance.blockMaterialUnlit;
            else
                newMaterial = World.Instance.blockMaterialLit;

            World.Instance.blockMaterial = newMaterial;
            foreach(KeyValuePair<ChunkCoord,Chunk> keyValue in World.Instance.chunksDict)
                keyValue.Value.chunkObject.gameObject.GetComponent<Renderer>().material = newMaterial;
        }
    }

    public void Quit()
    {
        //Save();

        // if splitscreen play and more than one player and the first player is not this player, destroy the gameObject
        if (!Settings.OnlinePlay && World.Instance.playerCount > 2 && World.Instance.players[1].playerGameObject != player.gameObject)
        {
            World.Instance.playerCount--;
            Destroy(player);
            return;
        }

        Settings.WorldLoaded = false;

        // stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            customNetworkManager.StopHost();
        }
        // stop client if client-only
        else if (NetworkClient.isConnected)
        {
            customNetworkManager.StopClient();
        }
        // stop server if server-only
        else if (NetworkServer.active)
        {
            customNetworkManager.StopServer();
        }

        SceneManager.LoadScene(1);
    }

    public void SetVolume(float value)
    {
        SettingsStatic.LoadedSettings.volume = value;
        masterAudioMixer.SetFloat("masterVolume", Mathf.Log10(value) * 20); // uses log base 10 for log scale of master mixer volume (decibels)
    }

    public void SetLookSensitivity(float value)
    {
        SettingsStatic.LoadedSettings.lookSpeed = value;
    }

    public void SetLookAccel(float value)
    {
        SettingsStatic.LoadedSettings.lookAccel = value;
    }

    public void SetFoV(float value)
    {
        SettingsStatic.LoadedSettings.fov = value;
        playerCamera.GetComponent<Camera>().fieldOfView = value;
    }

    public void SetGraphicsQuality(int value)
    {
        previousGraphicsQuality = SettingsStatic.LoadedSettings.graphicsQuality;
        SettingsStatic.LoadedSettings.graphicsQuality = value;
        QualitySettings.SetQualityLevel(value, true);
    }

    public void SetFullScreen (bool value)
    {
        buttonSound.Play();
        Screen.fullScreen = value;
        SettingsStatic.LoadedSettings.fullscreen = value;
    }

    public void SetInvertY (bool value)
    {
        buttonSound.Play();
        SettingsStatic.LoadedSettings.invertY = value;
    }

    public void ToggleControls()
    {
        showControls = !showControls;
    }

    public void ToggleDebug()
    {
        debugText.SetActive(!debugText.activeSelf);
    }
}