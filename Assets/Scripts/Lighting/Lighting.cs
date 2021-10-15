using UnityEngine;

[ExecuteAlways]
public class Lighting : MonoBehaviour
{
    //References
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;
    [SerializeField] private LightingPreset sunProperties;
    [SerializeField] private LightingPreset moonProperties;
    [SerializeField] private Material skyboxDay;
    [SerializeField] private Material skyboxNight;

    Controller controller;

    //Variables
    [SerializeField, Range(0, 24)] public float timeOfDay = 6.01f;
    public float maxFogDensity = 0.005f;

    private void Update()
    {
        if (Settings.OnlinePlay && World.Instance.playerCount > 1 && World.Instance.players[1].playerGameObject != null) // if player is created, get variable from player syncVar
        {
            if(controller == null)
                controller = World.Instance.players[1].playerGameObject.GetComponent<Controller>();
            timeOfDay = controller.timeOfDay;
        }

        if (sunProperties == null || moonProperties == null)
            return;

        float TimeOfDayIncrement = Time.deltaTime / 60 * 2; // divide by 60 to get 24 min days, multiply by 2 to get 12 min days

        if (Application.isPlaying)
        {
            if (timeOfDay > 6 && timeOfDay < 7 || timeOfDay > 18 && timeOfDay < 19) // golden hour slows time of day for more cinematic looks
                timeOfDay += TimeOfDayIncrement / 10; // sun proceeds at 1/10 speed during golden hour
            else
                timeOfDay += TimeOfDayIncrement;

            timeOfDay %= 24; //Clamp between 0-24
            UpdateLighting(timeOfDay / 24f);
        }
        else
        {
            UpdateLighting(timeOfDay / 24f);
        }

        if (Settings.OnlinePlay && World.Instance.playerCount > 1 && World.Instance.players[1].playerGameObject != null) // if player is created, write variable to player syncVar
            controller.timeOfDay = timeOfDay;
    }

    private void UpdateLighting(float timePercent)
    {
        // FOG
        RenderSettings.fogColor = sunProperties.FogColor.Evaluate(timePercent);

        float amplitude = maxFogDensity / 2;

        float period = 12; // 12 hr period = fog rolls in 2x per day
        float frequency = 2 * Mathf.PI / period;
        float horizontalShift = Mathf.PI / 2;

        float verticalShift = maxFogDensity / 2;

        RenderSettings.fogDensity = amplitude * Mathf.Sin(frequency * (timePercent * 24) - horizontalShift) + verticalShift;

        // DAY/NIGHT
        if (timePercent > 0.25 && timePercent < 0.75) // DAY
        {
            sun.gameObject.SetActive(true);
            moon.gameObject.SetActive(false);
            RenderSettings.ambientLight = sunProperties.AmbientColor.Evaluate(timePercent);
            sun.color = sunProperties.DirectionalColor.Evaluate(timePercent);
            RenderSettings.skybox = skyboxDay;
            RenderSettings.sun = sun;
            RenderSettings.ambientIntensity = 1;
            RenderSettings.reflectionIntensity = 1;
        }
        else // NIGHT
        {
            sun.gameObject.SetActive(false);
            moon.gameObject.SetActive(true);
            RenderSettings.ambientLight = moonProperties.AmbientColor.Evaluate(timePercent);
            sun.color = moonProperties.DirectionalColor.Evaluate(timePercent);
            RenderSettings.skybox = skyboxNight;
            RenderSettings.sun = moon;
            RenderSettings.ambientIntensity = 0.2f;
            RenderSettings.reflectionIntensity = 0.2f;
        }

        transform.localRotation = Quaternion.Euler(new Vector3(-90f - (timePercent * 360f), 0, 0)); // rotate light
    }
}
