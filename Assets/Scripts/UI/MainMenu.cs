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
        SettingsStatic.LoadedSettings = SettingsStatic.LoadSettings();
        versionText.text = Application.version;
        Screen.fullScreen = SettingsStatic.LoadedSettings.fullscreen;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
    }

    public void Play()
    {
        buttonSound.Play();
        SceneManager.LoadScene(1);
    }

    public void Quit()
    {
        buttonSound.Play();
        Application.Quit();
    }
}
