using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Range(0f, 1f)] public float timeOfDay = 0.35f; // 0=midnight .25=sunrise .5=noon .75=sunset
    public float dayLengthMinutes = 12f;
    public Light sun;
    public float ambientIntensity = 1f;

    float baseSunIntensity = 1.2f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (sun == null)
        {
            var go = new GameObject("Sun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            go.transform.SetParent(transform, false);
        }
        baseSunIntensity = sun.intensity;
        RenderSettings.sun = sun;
    }

    void Update()
    {
        timeOfDay = (timeOfDay + Time.deltaTime / (dayLengthMinutes * 60f)) % 1f;
        float sunAngle = (timeOfDay - 0.25f) * 360f; // sunrise at horizon east
        sun.transform.rotation = Quaternion.Euler(sunAngle, 35f, 0f);

        float elev = Mathf.Sin((timeOfDay - 0.25f) * Mathf.PI * 2f); // -1..1 day factor
        float dayness = Mathf.Clamp01(elev * 2.5f + 0.15f);

        sun.intensity = baseSunIntensity * Mathf.Max(0.06f, dayness);
        sun.color = Color.Lerp(new Color(1f, 0.55f, 0.3f), new Color(1f, 0.97f, 0.9f), Mathf.Clamp01(elev * 3f));

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        Color nightAmb = new Color(0.08f, 0.1f, 0.18f);
        Color dayAmb = new Color(0.45f, 0.55f, 0.6f);
        Color duskAmb = new Color(0.5f, 0.32f, 0.25f);
        Color ambSky = Color.Lerp(nightAmb, dayAmb, dayness);
        if (elev > -0.15f && elev < 0.25f) ambSky = Color.Lerp(duskAmb, ambSky, Mathf.Clamp01((elev + 0.15f) / 0.4f));
        RenderSettings.ambientSkyColor = ambSky;
        RenderSettings.ambientEquatorColor = ambSky * 0.9f;
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.2f, 0.15f) * (0.3f + dayness);

        // fog
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        float fogDay = 0.0035f, fogNight = 0.012f;
        RenderSettings.fogDensity = Mathf.Lerp(fogNight, fogDay, dayness);
        RenderSettings.fogColor = Color.Lerp(new Color(0.05f, 0.07f, 0.12f), new Color(0.75f, 0.82f, 0.9f), dayness);

        // skybox procedural gradient via ambient is enough; use default skybox tinted by ambient
        if (sun != null)
        {
            var sh = sun.GetComponent<Light>();
            // moonlight: keep tiny intensity at night
        }
    }

    public string TimeString()
    {
        float hours = timeOfDay * 24f;
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60f);
        return $"{h:00}:{m:00}";
    }
}
