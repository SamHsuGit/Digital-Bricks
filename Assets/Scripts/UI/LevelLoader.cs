using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelLoader : MonoBehaviour
{
    public void LoadLevel(int _sceneIndex, Slider _slider, TextMeshProUGUI _percentageText)
    {
        StartCoroutine(LoadAsynchronously(_sceneIndex, _slider, _percentageText));
    }

    IEnumerator LoadAsynchronously(int _sceneIndex, Slider _slider, TextMeshProUGUI _percentageText)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(_sceneIndex);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            _slider.value = progress;
            _percentageText.text = progress * 100f + "%";

            yield return null;
        }
    }
}
