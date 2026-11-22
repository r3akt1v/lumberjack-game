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
        // dry plateau near center (house/clearing), blending out to natural terrain
        float d = Mathf.Sqrt(x * x + z * z);
        float flat = Mathf.Clamp01((d - 10f) / 42f);
        float minH = Mathf.Lerp(2.0f, -3f, flat);
        h = Mathf.Max(h, minH);
        // beach: push below water near edge
        float beach = Mathf.Clamp01((m - 0.12f) / 0.25f);
        h = Mathf.Lerp(-1.2f, h, beach);
        // flat pads under buildings
        h = ApplyBuildingPads(x, z, h);
        return h * m + (1f - m) * -4f;
    }

    // building pads: pos, radius, target height
    static readonly Vector3[] Pads =
    {
        new Vector3(-14f, 1.7f, -14f), // house
        new Vector3(10f, 1.7f, -6f),   // sawmill
        new Vector3(4f, 1.7f, -12f),   // sell crate
        new Vector3(0f, 1.7f, -18f),   // spawn/sign
    };
    static readonly float[] PadR = { 10f, 6f, 5f, 7f };

    static float ApplyBuildingPads(float x, float z, float h)
    {
        for (int i = 0; i < Pads.Length; i++)
        {
            float dx = x - Pads[i].x, dz = z - Pads[i].z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            float f = Mathf.Clamp01((dist - PadR[i]) / 6f);
            h = Mathf.Lerp(Pads[i].y, h, f);
        }
        return h;
    }
}
