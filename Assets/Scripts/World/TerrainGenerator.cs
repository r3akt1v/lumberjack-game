using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public Material terrainMaterial;
    public int resolution = 160; // quads per side

    public Mesh Build()
    {
        int n = resolution + 1;
        var verts = new Vector3[n * n];
        var uvs = new Vector2[n * n];
        var tris = new int[resolution * resolution * 6];
        float step = TerrainMath.Size / resolution;

        for (int z = 0; z < n; z++)
        {
            for (int x = 0; x < n; x++)
            {
                float wx = -TerrainMath.Half + x * step;
                float wz = -TerrainMath.Half + z * step;
                float h = TerrainMath.Height(wx, wz);
                verts[z * n + x] = new Vector3(wx, h, wz);
                uvs[z * n + x] = new Vector2((float)x / resolution, (float)z / resolution);
            }
        }

        int t = 0;
        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = z * n + x;
                tris[t++] = i; tris[t++] = i + n; tris[t++] = i + 1;
                tris[t++] = i + 1; tris[t++] = i + n; tris[t++] = i + n + 1;
            }
        }

        var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Texture2D BuildGroundTexture(int size = 512)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        var px = new Color32[size * size];

        Color grassA = new Color(0.32f, 0.52f, 0.2f);
        Color grassB = new Color(0.45f, 0.62f, 0.25f);
        Color grassDry = new Color(0.55f, 0.6f, 0.28f);
        Color dirt = new Color(0.45f, 0.33f, 0.2f);
        Color sand = new Color(0.82f, 0.74f, 0.52f);
        Color rock = new Color(0.48f, 0.47f, 0.45f);
        Color snow = new Color(0.9f, 0.92f, 0.95f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float wx = ((float)x / size - 0.5f) * TerrainMath.Size;
                float wz = ((float)y / size - 0.5f) * TerrainMath.Size;
                float h = TerrainMath.Height(wx, wz);
                float n1 = Noise.Fbm(wx * 0.08f, wz * 0.08f, 3);
                float n2 = Noise.Fbm(wx * 0.25f + 31f, wz * 0.25f - 17f, 3);

                Color c = Color.Lerp(grassA, grassB, n1);
                c = Color.Lerp(c, grassDry, Mathf.Clamp01(n2 * 1.4f - 0.35f) * 0.6f);

                // dirt patches
                float dirtN = Noise.Fbm(wx * 0.05f + 90f, wz * 0.05f + 40f, 3);
                if (dirtN > 0.62f && h > 0.4f) c = Color.Lerp(c, dirt, Mathf.Clamp01((dirtN - 0.62f) * 4f) * 0.8f);

                // sand near water level
                if (h < 0.9f) c = Color.Lerp(sand, c, Mathf.Clamp01((h - 0.1f) / 0.8f));
                if (h < 0.1f) c = sand;

                // rock on steep/high
                float steep = Mathf.Clamp01((Mathf.Abs(TerrainMath.Height(wx + 2f, wz) - h) + Mathf.Abs(TerrainMath.Height(wx, wz + 2f) - h)) * 1.6f - 0.55f);
                if (h > 4f) c = Color.Lerp(c, snow, Mathf.Clamp01((h - 6f) / 3f));
                c = Color.Lerp(c, rock, steep * 0.85f);

                // subtle noise
                float v = 0.92f + n2 * 0.16f;
                px[y * size + x] = new Color32(
                    (byte)(Mathf.Clamp01(c.r * v) * 255),
                    (byte)(Mathf.Clamp01(c.g * v) * 255),
                    (byte)(Mathf.Clamp01(c.b * v) * 255),
                    255);
            }
        }
        tex.SetPixels32(px);
        tex.Apply(true, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Trilinear;
        return tex;
    }
}
