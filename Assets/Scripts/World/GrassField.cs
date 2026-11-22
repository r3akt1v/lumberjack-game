using UnityEngine;
using UnityEngine.Rendering;

public class GrassField : MonoBehaviour
{
    public int count = 16000;
    public float radius = 115f;

    Mesh mesh;
    Material material;
    Matrix4x4[] matrices;
    Bounds bounds;
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public void Build()
    {
        mesh = BuildBladeMesh();
        var sh = Resources.Load<Shader>("Shaders/GrassWind");
        material = sh != null ? new Material(sh) : new Material(Shader.Find("Standard"));
        material.enableInstancing = true;

        var rng = new System.Random(777);
        matrices = new Matrix4x4[count];
        var greens = new Color[] {
            new Color(0.3f, 0.5f, 0.18f), new Color(0.4f, 0.6f, 0.2f),
            new Color(0.5f, 0.62f, 0.22f), new Color(0.36f, 0.55f, 0.24f)
        };
        int placed = 0, attempts = 0;
        while (placed < count && attempts < count * 20)
        {
            attempts++;
            float x = (float)(rng.NextDouble() * 2 - 1) * radius;
            float z = (float)(rng.NextDouble() * 2 - 1) * radius;
            float m = TerrainMath.Mask(x, z);
            if (m < 0.7f) continue;
            float h = TerrainMath.Height(x, z);
            if (h < 0.4f || h > 5f) continue;
            float s = 0.6f + (float)rng.NextDouble() * 0.9f;
            var pos = new Vector3(x, h - 0.02f, z);
            var rot = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
            matrices[placed++] = Matrix4x4.TRS(pos, rot, Vector3.one * s);
        }
        // shrink array if needed
        if (placed < count)
        {
            var tmp = new Matrix4x4[placed];
            System.Array.Copy(matrices, tmp, placed);
            matrices = tmp;
        }
        bounds = new Bounds(Vector3.zero, new Vector3(TerrainMath.Size, 40f, TerrainMath.Size));
    }

    Mesh BuildBladeMesh()
    {
        // two crossed quads, 4 verts each, slight taper
        var verts = new Vector3[8];
        var uvs = new Vector2[8];
        var cols = new Color[8];
        Color cTop = new Color(0.55f, 0.8f, 0.3f);
        Color cBot = new Color(0.2f, 0.4f, 0.12f);
        int vi = 0;
        for (int blade = 0; blade < 2; blade++)
        {
            float ang = blade * 90f;
            var dir = Quaternion.Euler(0, ang, 0) * Vector3.right;
            float w = 0.09f, hgt = 0.75f;
            Vector3 p1 = -dir * w + Vector3.up * 0;
            Vector3 p2 = dir * w;
            Vector3 p3 = -dir * w * 0.25f + Vector3.up * hgt;
            Vector3 p4 = dir * w * 0.25f + Vector3.up * hgt;
            verts[vi] = p1; uvs[vi] = new Vector2(0, 0); cols[vi] = cBot; vi++;
            verts[vi] = p2; uvs[vi] = new Vector2(1, 0); cols[vi] = cBot; vi++;
            verts[vi] = p3; uvs[vi] = new Vector2(0, 1); cols[vi] = cTop; vi++;
            verts[vi] = p4; uvs[vi] = new Vector2(1, 1); cols[vi] = cTop; vi++;
        }
        var tris = new[] { 0, 2, 1, 1, 2, 3, 4, 6, 5, 5, 6, 7 };
        var m = new Mesh();
        m.vertices = verts;
        m.uv = uvs;
        m.colors = cols;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    void Update()
    {
        if (mesh == null || matrices == null || material == null) return;
        if (material.HasProperty("_TimeX")) material.SetFloat("_TimeX", Time.time);
        // draw in chunks of 1023
        int maxPer = 1023;
        for (int i = 0; i < matrices.Length; i += maxPer)
        {
            int n = Mathf.Min(maxPer, matrices.Length - i);
            var slice = new Matrix4x4[n];
            System.Array.Copy(matrices, i, slice, 0, n);
            Graphics.DrawMeshInstanced(mesh, 0, material, slice, n, null, ShadowCastingMode.Off, false, 0, null, LightProbeUsage.Off);
        }
    }
}
