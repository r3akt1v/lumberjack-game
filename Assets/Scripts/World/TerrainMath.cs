using UnityEngine;

public static class TerrainMath
{
    public const float Size = 280f;
    public const float Half = Size / 2f;

    // island mask: 1 inside, 0 outside; irregular edge via noise
    public static float Mask(float x, float z)
    {
        float r = Mathf.Sqrt(x * x + z * z) / (Half * 0.92f);
        float edge = Noise.Fbm(x * 0.02f + 7.3f, z * 0.02f - 3.1f, 3) * 0.22f;
        return Mathf.Clamp01((1f - r + edge) * 6f);
    }

    public static float Height(float x, float z)
    {
        float m = Mask(x, z);
        if (m <= 0f) return -4f;
        float hills = Noise.Fbm(x, z, 60f, 4) ;          // rolling 0..1
        float detail = Noise.Fbm(x + 100f, z - 50f, 18f, 3);
        float h = (hills - 0.35f) * 14f + (detail - 0.5f) * 2.5f;
        // flatten near center spawn/house area
        float d = Mathf.Sqrt(x * x + z * z);
        float flat = Mathf.Clamp01((d - 12f) / 30f);
        h *= Mathf.Lerp(0.15f, 1f, flat);
        // beach: push below water near edge
        float beach = Mathf.Clamp01((m - 0.12f) / 0.25f);
        h = Mathf.Lerp(-1.2f, h, beach);
        return h * m + (1f - m) * -4f;
    }
}
