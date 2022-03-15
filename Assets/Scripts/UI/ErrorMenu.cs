using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ErrorMenu : MonoBehaviour
{
    public AudioSource buttonSound;
    public GameObject ErrorMessageText;

    private void Awake()
    {
        string errorMessage = "Error: Unspecified";

        if(ErrorMessage.message != null)
            errorMessage = ErrorMessage.message;

        ErrorMessageText.GetComponent<Text>().text = errorMessage;
    }

    public void Exit()
    {
        buttonSound.Play();
        Application.Quit();
    }
}

public static class ErrorMessage
{
    public static string message;
    public static void Show(string _message)
    {
        message = _message;
        Debug.Log(_message);
        SceneManager.LoadScene(4);
    }
}
