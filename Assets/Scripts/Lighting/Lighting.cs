using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class Lighting : MonoBehaviour
{
    public GameManagerScript gameManager;
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;
    [SerializeField] private LightingPreset sunProperties;
    [SerializeField] private Material skyboxAtmosphere;
    [SerializeField] private Material skyboxSpace;

    public bool daytime = true;
    public bool wasDaytime = false;
    [SerializeField] private Color skyTint;

    [SerializeField] private float skyTintValue = 1;
    [SerializeField] private float ambientIntensity;
    [SerializeField] private float reflectionIntensity;

    [SerializeField, Range(0, 24)] public float timeOfDay = 6.01f;
    public float maxFogDensity = 0.02f;

    //NOTE: MacBook Air requires Gamma Color Space. Cannot use Linear.

    private void Awake()
    {
        if(gameManager.webGL)
            timeOfDay = 6;
        else if (!Settings.OnlinePlay)
            timeOfDay = SettingsStatic.LoadedSettings.timeOfDay;
    }

    private void FixedUpdate()
    {
        if (sunProperties == null || !SettingsStatic.LoadedSettings.dayNightCycle)
            return;

        float TimeOfDayIncrement = Time.deltaTime / 60 * 12 / 5; // divide by 60 to get 24 min days, multiply by 12 to get 1 min days, divide by 5 to get 5  min day
        if (daytime)
            TimeOfDayIncrement *= 1;
        else
            TimeOfDayIncrement *= 2; // nights are half as short as daytime

        //float TimeOfDayIncrement = Time.deltaTime / 60 * 48; // divide by 60 to get 24 min days, multiply by 48 to get 15 sec days (TESTING)

        if (Application.isPlaying)
        {
            if (timeOfDay > 5 && timeOfDay < 7)
                timeOfDay += TimeOfDayIncrement / 2; // sun proceeds at 1/2 speed during dawn golden hour
            else if(timeOfDay > 17 && timeOfDay < 19)
                timeOfDay += TimeOfDayIncrement / 2; // sun proceeds at 1/2 speed during dusk golden hour
            else
                timeOfDay += TimeOfDayIncrement;

            timeOfDay %= 24; //Clamp between 0-24
            
            UpdateLighting(timeOfDay / 24f);
        }
        else
        {
            UpdateLighting(timeOfDay / 24f);
        }

        daytime = CheckDaytime(timeOfDay);
    }

    public bool CheckDaytime(float timeOfDay)
    {
        bool _dayTime;

        if (timeOfDay >= 6 && timeOfDay <= 18)
            _dayTime = true;
        else
            _dayTime = false;

        return _dayTime;
    }

    private void UpdateLighting(float timePercent)
    {
        // Dynamic Fog
        RenderSettings.fogColor = sunProperties.FogColor.Evaluate(timePercent);
        float amplitude = maxFogDensity / 2;
        float period = 12; // 12 hr period = fog rolls in 2x per day
        float frequency = 2 * Mathf.PI / period;
        float horizontalShift = Mathf.PI / 2;
        float verticalShift = maxFogDensity / 2;
        RenderSettings.fogDensity = amplitude * Mathf.Cos(frequency * (timePercent * 24) - horizontalShift) + verticalShift;

        RenderSettings.ambientLight = sunProperties.AmbientColor.Evaluate(timePercent);
        sun.color = sunProperties.DirectionalColor.Evaluate(timePercent);

        skyTint = Color.HSVToRGB(0, 0, skyTintValue);
        if (RenderSettings.skybox.HasProperty("_SkyTint"))
            RenderSettings.skybox.SetColor("_SkyTint", skyTint);

        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.reflectionIntensity = reflectionIntensity;

        // SET DAY OR NIGHT BASED ON TIME
        if (World.Instance != null && !World.Instance.worldData.hasAtmosphere)
        {
            SetSpace();
        }
        else
        {
            if (daytime && !wasDaytime) // only show daytime skybox and daytime for planets which host life (isAlive)
                SetDay();
            else if (!daytime && wasDaytime)
                SetNight();
        }

        transform.localRotation = Quaternion.Euler(new Vector3(-90f - (timePercent * 360f), 0, 0)); // rotate light
    }

    void SetDay()
    {
        wasDaytime = true;

        sun.gameObject.SetActive(true);
        moon.gameObject.SetActive(false);

        RenderSettings.skybox = skyboxAtmosphere;
        StartCoroutine(LerpSkyTintValue(1, 10));

        RenderSettings.sun = sun;
        StartCoroutine(LerpAmbientIntensity(1f, 10));
        StartCoroutine(LerpReflectionIntensity(1f, 10));
    }

    void SetNight()
    {
        wasDaytime = false;

        sun.gameObject.SetActive(false);
        moon.gameObject.SetActive(true);

        RenderSettings.skybox = skyboxAtmosphere;
        StartCoroutine(LerpSkyTintValue(0.5f, 10));

        RenderSettings.sun = moon;
        StartCoroutine(LerpAmbientIntensity(0.12f, 10));
        StartCoroutine(LerpReflectionIntensity(0.12f, 10));
    }

    void SetSpace()
    {
        wasDaytime = false;

        sun.gameObject.SetActive(false);
        moon.gameObject.SetActive(true);

        RenderSettings.skybox = skyboxSpace;
        StartCoroutine(LerpSkyTintValue(0, 10));

        RenderSettings.sun = moon;
        StartCoroutine(LerpAmbientIntensity(0.75f, 10));
        StartCoroutine(LerpReflectionIntensity(0.75f, 10));
    }

    IEnumerator LerpSkyTintValue(float endValue, float duration)
    {
        float time = 0;
        float startValue;

        startValue = skyTintValue;
        while (time < duration)
        {
            skyTintValue = Mathf.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        skyTintValue = endValue;
    }

    IEnumerator LerpAmbientIntensity(float endValue, float duration)
    {
        float time = 0;
        float startValue;

        startValue = ambientIntensity;
        while (time < duration)
        {
            ambientIntensity = Mathf.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        ambientIntensity = endValue;
    }

    IEnumerator LerpReflectionIntensity(float endValue, float duration)
    {
        float time = 0;
        float startValue;

        startValue = reflectionIntensity;
        while (time < duration)
        {
            reflectionIntensity = Mathf.Lerp(startValue, endValue, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        reflectionIntensity = endValue;
    }
}
