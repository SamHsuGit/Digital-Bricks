using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class GlobalSounds : MonoBehaviour
{
    public GameManagerScript gameManager;
    public GameObject globalLighting;
    public AudioMixer masterMix;
    public bool dayTime = true;
    public bool previousDayTime = true;
    public float fadeTime = 250; // 50 times per second times 5 seconds

    Lighting lighting;

    private void Awake()
    {
        lighting = globalLighting.GetComponent<Lighting>();

        if (gameManager.worldcoordDefault == 3)
        {
            if (lighting.timeOfDay >= 6 && lighting.timeOfDay <= 18)
            {
                StartCoroutine(FadeMixerGroup.StartFade(masterMix, "cricketsVolume", 1, 0.0001f));
                StartCoroutine(FadeMixerGroup.StartFade(masterMix, "birdsongVolume", fadeTime, 0.125f));
            }
            else
            {
                StartCoroutine(FadeMixerGroup.StartFade(masterMix, "birdsongVolume", 1, 0.0001f));
                StartCoroutine(FadeMixerGroup.StartFade(masterMix, "cricketsVolume", fadeTime, 0.125f));
            }
        }
        else // if not on earth, no ambient sounds
        {
            StartCoroutine(FadeMixerGroup.StartFade(masterMix, "cricketsVolume", 0.0001f, 0.0001f));
            StartCoroutine(FadeMixerGroup.StartFade(masterMix, "birdsongVolume", 0.0001f, 0.0001f));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager.webGL || SettingsStatic.LoadedSettings.worldCoord == 3)
        {
            previousDayTime = dayTime;

            if (lighting.timeOfDay >= 6 && lighting.timeOfDay <= 18)
                dayTime = true;
            else if (lighting.timeOfDay >= 0 && lighting.timeOfDay < 6)
                dayTime = false;
            else if (lighting.timeOfDay > 18 && lighting.timeOfDay <= 24)
                dayTime = false;

            if (dayTime && dayTime != previousDayTime)
            {
                StartCoroutine(FadeMixerGroup.StartFade(masterMix, "cricketsVolume", fadeTime, 0.0001f));
                StartCoroutine(FadeMixerGroup.StartFade(masterMix, "birdsongVolume", fadeTime, 0.125f));

            }
            else if (!dayTime && dayTime != previousDayTime)
            {
                StartCoroutine(FadeMixerGroup.StartFade(masterMix, "birdsongVolume", fadeTime, 0.0001f));
                StartCoroutine(FadeMixerGroup.StartFade(masterMix, "cricketsVolume", fadeTime, 0.125f));
            }
        }
    }
}

public static class FadeMixerGroup
{
    public static IEnumerator StartFade(AudioMixer audioMixer, string exposedParam, float duration, float targetVolume)
    {
        float currentTime = 0;
        float currentVol;
        audioMixer.GetFloat(exposedParam, out currentVol);
        currentVol = Mathf.Pow(10, currentVol / 20);
        float targetValue = Mathf.Clamp(targetVolume, 0.0001f, 1);

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float newVol = Mathf.Lerp(currentVol, targetValue, currentTime / duration);
            audioMixer.SetFloat(exposedParam, Mathf.Log10(newVol) * 20);
            yield return null;
        }
        yield break;
    }
}