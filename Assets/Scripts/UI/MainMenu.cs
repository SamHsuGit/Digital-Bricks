using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public TMP_Text versionText;
    public GameObject menuElements;
    public AudioSource buttonSound;

    private void Awake()
    {
        Settings.AppPath = Application.persistentDataPath;
        SettingsStatic.LoadedSettings = SettingsStatic.LoadSettings();
        versionText.text = Application.version;
        Screen.fullScreen = SettingsStatic.LoadedSettings.fullscreen;
    }

    private void OnGUI()
    {
        
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    public void Play()
    {
        buttonSound.Play();
        SceneManager.LoadScene(2);
    }

    public void Quit()
    {
        buttonSound.Play();
        Application.Quit();
    }

    public void Credits()
    {
        buttonSound.Play();
        SceneManager.LoadScene(1);
    }
}
