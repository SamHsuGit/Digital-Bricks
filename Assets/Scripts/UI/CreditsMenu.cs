using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditsMenu : MonoBehaviour
{
    public AudioSource buttonSound;

    public void Back()
    {
        buttonSound.Play();
        SceneManager.LoadScene(0);
    }
}
