using System.Collections.Generic;
using UnityEngine;

public class WorldBuilder : MonoBehaviour
{
    public Transform treesRoot;
    public Transform propsRoot;

    [Header("Counts")]
    public int treeCount = 380;
    public int grassCount = 16000;
    public int bushCount = 140;
    public int flowerCount = 260;
    public int rockCount = 60;
    public int fenceCount = 40;

    public List<Tree> trees = new List<Tree>();

    GameObject treePrefab;

    void Start()
    {
        float t0 = Time.realtimeSinceStartup;
        treePrefab = Resources.Load<GameObject>("Models/tree_pine");
        BuildTerrain();
        Debug.Log($"[World] terrain built {Time.realtimeSinceStartup - t0:F1}s");
        BuildWater();
        BuildClearing();
        ScatterVegetation();
        Debug.Log($"[World] vegetation done {Time.realtimeSinceStartup - t0:F1}s, trees={trees.Count}");
        BuildGrass();
        Debug.Log($"[World] READY total {Time.realtimeSinceStartup - t0:F1}s");
    }

    public float GroundY(float x, float z) => TerrainMath.Height(x, z);

    void BuildTerrain()
    {
        var gen = gameObject.AddComponent<TerrainGenerator>();
        var mesh = gen.Build();
        var tex = TerrainGenerator.BuildGroundTexture(512);
        var mat = new Material(Shader.Find("Standard"));
        mat.mainTexture = tex;
        mat.color = Color.white;
        var go = new GameObject("Terrain");
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    void BuildWater()
    {
        var go = new GameObject("Water");
        go.transform.SetParent(transform, false);
        var mf = go.AddComponent<MeshFilter>();
        var mesh = new Mesh();
        float s = 1200f;
        mesh.vertices = new[] {
            new Vector3(-s, 0, -s), new Vector3(-s, 0, s), new Vector3(s, 0, s), new Vector3(s, 0, -s)
        };
        mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.15f, 0.4f, 0.55f, 0.75f);
        mat.SetFloat("_Mode", 3f); // transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;
        mr.sharedMaterial = mat;
    }

    Vector3 FlatPos(float x, float z)
    {
        return new Vector3(x, TerrainMath.Height(x, z), z);
    }

    void BuildClearing()
    {
        // clearing around spawn: house at (-8,0), sawmill at (10,6), crate at (4,-8)
        SpawnHouse(new Vector3(-14f, 0, -10f), 35f);
        SpawnSawmill(new Vector3(12f, 0, -4f), 200f);
        SpawnCrate(new Vector3(2f, 0, -14f), 160f);
        SpawnSign(new Vector3(-4f, 0, -20f), 90f);
        // welcome sign
        SpawnSign(new Vector3(0f, 0, -24f), 0f);
    }

    void SpawnHouse(Vector3 pos, float yaw)
    {
        var model = Resources.Load<GameObject>("Models/house");
        if (model == null) return;
        pos.y = TerrainMath.Height(pos.x, pos.z);
        var go = Instantiate(model, pos, Quaternion.Euler(0, yaw, 0), propsRoot ?? transform);
        go.name = "House";
        var hm = go.AddComponent<HouseManager>();
        // disable upgrade parts initially handled by HouseManager
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 3f, 0);
        col.size = new Vector3(7f, 6f, 6f);
        // porch part collider not needed (decor)
    }

    void SpawnSawmill(Vector3 pos, float yaw)
    {
        var model = Resources.Load<GameObject>("Models/sawmill");
        if (model == null) return;
        pos.y = TerrainMath.Height(pos.x, pos.z);
        var go = Instantiate(model, pos, Quaternion.Euler(0, yaw, 0), propsRoot ?? transform);
        go.name = "Sawmill";
        var sm = go.AddComponent<Sawmill>();
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 1.2f, 0);
        col.size = new Vector3(3.4f, 2.4f, 1.6f);
        col.isTrigger = true;
        var inter = go.AddComponent<Interactable>();
        inter.prompt = "[E] Загрузить брёвна в пилораму";
        inter.onInteract = () => sm.DepositLogs();
    }

    void SpawnCrate(Vector3 pos, float yaw)
    {
        var model = Resources.Load<GameObject>("Models/crate");
        if (model == null) return;
        pos.y = TerrainMath.Height(pos.x, pos.z);
        var go = Instantiate(model, pos, Quaternion.Euler(0, yaw, 0), propsRoot ?? transform);
        go.name = "SellCrate";
        var sp = go.AddComponent<SellPoint>();
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 0.8f, 0);
        col.size = new Vector3(1.4f, 1.6f, 1.4f);
        col.isTrigger = true;
        var inter = go.AddComponent<Interactable>();
        inter.prompt = "[E] Продать ресурсы";
        inter.onInteract = () => sp.Sell();
        // floating text handled by HUD prompt
    }

    void SpawnSign(Vector3 pos, float yaw)
    {
        var model = Resources.Load<GameObject>("Models/sign");
        if (model == null) return;
        pos.y = TerrainMath.Height(pos.x, pos.z);
        Instantiate(model, pos, Quaternion.Euler(0, yaw, 0), propsRoot ?? transform).name = "Sign";
    }

    bool ValidSpot(float x, float z, float minR, float maxSlope)
    {
        float m = TerrainMath.Mask(x, z);
        if (m < 0.75f) return false;
        float r = Mathf.Sqrt(x * x + z * z);
        if (r < minR || r > TerrainMath.Half * 0.88f) return false;
        // slope check
        float h = TerrainMath.Height(x, z);
        float hx = TerrainMath.Height(x + 1.5f, z);
        float hz = TerrainMath.Height(x, z + 1.5f);
        float slope = Mathf.Sqrt((hx - h) * (hx - h) + (hz - h) * (hz - h));
        if (slope > maxSlope) return false;
        return true;
    }

    void ScatterVegetation()
    {
        var rng = new System.Random(12345);
        treesRoot = new GameObject("Trees").transform;
        treesRoot.SetParent(transform, false);
        propsRoot = new GameObject("Props").transform;
        propsRoot.SetParent(transform, false);

        // trees
        int attempts = 0;
        while (trees.Count < treeCount && attempts < treeCount * 30)
        {
            attempts++;
            float x = (float)(rng.NextDouble() * 2 - 1) * TerrainMath.Half * 0.9f;
            float z = (float)(rng.NextDouble() * 2 - 1) * TerrainMath.Half * 0.9f;
            if (!ValidSpot(x, z, 20f, 0.55f)) continue;
            // keep clearing near buildings
            if (Mathf.Abs(x + 14) < 9 && Mathf.Abs(z + 10) < 9) continue;
            if (Mathf.Abs(x - 12) < 7 && Mathf.Abs(z + 4) < 7) continue;
            if (Mathf.Abs(x - 2) < 6 && Mathf.Abs(z + 14) < 6) continue;

            float y = TerrainMath.Height(x, z);
            var go = new GameObject("Tree");
            go.transform.SetParent(treesRoot, false);
            go.transform.position = new Vector3(x, y - 0.05f, z);
            go.transform.rotation = Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0);
            float s = 0.8f + (float)rng.NextDouble() * 0.7f;
            go.transform.localScale = Vector3.one * s;

            if (treePrefab != null)
            {
                var vis = Instantiate(treePrefab, go.transform);
                vis.name = "Visual";
            }
            var tree = go.AddComponent<Tree>();
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 1.6f, 0);
            col.radius = 0.5f;
            col.height = 4f;
            trees.Add(tree);
        }

        // bushes
        SpawnScattered("Models/bush", bushCount, 0.7f, 1.4f, 0.4f, rng);
        // flowers
        SpawnScattered("Models/flower", flowerCount, 0.6f, 1.2f, 0.5f, rng);
        // rocks
        SpawnScattered("Models/rock", rockCount, 0.8f, 2.6f, 0.7f, rng);
        // fences around clearing edge
        SpawnFences(rng);
    }

    void SpawnScattered(string modelPath, int count, float minScale, float maxScale, float maxSlope, System.Random rng)
    {
        var model = Resources.Load<GameObject>(modelPath);
        if (model == null) return;
        int attempts = 0, spawned = 0;
        while (spawned < count && attempts < count * 30)
        {
            attempts++;
            float x = (float)(rng.NextDouble() * 2 - 1) * TerrainMath.Half * 0.88f;
            float z = (float)(rng.NextDouble() * 2 - 1) * TerrainMath.Half * 0.88f;
            if (!ValidSpot(x, z, 15f, maxSlope)) continue;
            float y = TerrainMath.Height(x, z);
            var go = Instantiate(model, new Vector3(x, y, z),
                Quaternion.Euler(0, (float)rng.NextDouble() * 360f, 0), propsRoot);
            float s = minScale + (float)rng.NextDouble() * (maxScale - minScale);
            go.transform.localScale = Vector3.one * s;
            spawned++;
        }
    }

    void SpawnFences(System.Random rng)
    {
        var model = Resources.Load<GameObject>("Models/fence");
        if (model == null) return;
        // ring of fence around clearing
        int n = fenceCount;
        for (int i = 0; i < n; i++)
        {
            float ang = i / (float)n * Mathf.PI * 2f;
            float r = 17f + (float)rng.NextDouble() * 4f;
            float x = Mathf.Cos(ang) * r;
            float z = Mathf.Sin(ang) * r - 6f;
            if (!ValidSpot(x, z, 10f, 0.6f)) continue;
            var go = Instantiate(model, new Vector3(x, TerrainMath.Height(x, z), z),
                Quaternion.Euler(0, -ang * Mathf.Rad2Deg + 90f, 0), propsRoot);
            go.transform.localScale = Vector3.one * 1.2f;
        }
    }

    void BuildGrass()
    {
        var gf = gameObject.AddComponent<GrassField>();
        gf.count = grassCount;
        gf.Build();
    }
}
