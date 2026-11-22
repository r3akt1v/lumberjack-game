using UnityEngine;
using UnityEngine.UI;

// Top-down minimap: baked heightmap texture + blips for buildings and player.
public class Minimap : MonoBehaviour
{
    RectTransform mapRt;
    Transform playerMarker;
    Transform player;
    const float WorldHalf = 145f;
    const float MapSize = 180f;

    public void Init(Transform playerT, Canvas canvas)
    {
        player = playerT;
        StartCoroutine(Setup(canvas));
    }

    System.Collections.IEnumerator Setup(Canvas canvas)
    {
        yield return null; // let WorldBuilder finish
        var panel = new GameObject("Minimap", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = prt.anchorMax = new Vector2(1, 1);
        prt.anchoredPosition = new Vector2(-125, -190);
        prt.sizeDelta = new Vector2(MapSize + 14, MapSize + 14);
        panel.GetComponent<Image>().color = new Color(0.02f, 0.05f, 0.03f, 0.7f);
        mapRt = prt;

        var rawGo = new GameObject("MapTex", typeof(RectTransform), typeof(RawImage));
        rawGo.transform.SetParent(panel.transform, false);
        var rrt = (RectTransform)rawGo.transform;
        rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one;
        rrt.offsetMin = new Vector2(7, 7); rrt.offsetMax = new Vector2(-7, -7);
        var raw = rawGo.GetComponent<RawImage>();
        raw.texture = BuildTexture(180);
        raw.raycastTarget = false;

        // building blips (find live objects, fallback to known coords)
        Blip("House", new Color(1f, 0.55f, 0.15f));
        Blip("Sawmill", new Color(0.55f, 0.35f, 0.15f));
        Blip("SellCrate", new Color(1f, 0.9f, 0.25f));

        // player marker
        var pm = new GameObject("PlayerMark", typeof(RectTransform), typeof(Image));
        pm.transform.SetParent(rawGo.transform, false);
        var pmt = (RectTransform)pm.transform;
        pmt.sizeDelta = new Vector2(12, 12);
        pm.GetComponent<Image>().color = new Color(0.3f, 1f, 0.4f);
        playerMarker = pm.transform;
    }

    void Blip(string objName, Color color)
    {
        Vector3 pos;
        var go = GameObject.Find(objName);
        if (go != null) pos = go.transform.position;
        else return;
        var m = new GameObject("Blip_" + objName, typeof(RectTransform), typeof(Image));
        m.transform.SetParent(mapRt, false);
        var rt = (RectTransform)m.transform;
        rt.sizeDelta = new Vector2(9, 9);
        rt.anchoredPosition = WorldToMap(pos.x, pos.z);
        m.GetComponent<Image>().color = color;
    }

    Vector2 WorldToMap(float x, float z)
    {
        float u = (x + WorldHalf) / (WorldHalf * 2f);
        float v = (z + WorldHalf) / (WorldHalf * 2f);
        return new Vector2((u - 0.5f) * MapSize, (v - 0.5f) * MapSize);
    }

    void Update()
    {
        if (playerMarker == null || player == null) return;
        playerMarker.GetComponent<RectTransform>().anchoredPosition = WorldToMap(player.position.x, player.position.z);
        playerMarker.localRotation = Quaternion.Euler(0, 0, -player.eulerAngles.y);
    }

    Texture2D BuildTexture(int res)
    {
        var tex = new Texture2D(res, res, TextureFormat.RGB24, false);
        var px = new Color[res * res];
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float wx = (x / (float)(res - 1) * 2f - 1f) * WorldHalf;
                float wz = (y / (float)(res - 1) * 2f - 1f) * WorldHalf;
                px[y * res + x] = Sample(wx, wz);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    Color Sample(float x, float z)
    {
        float h = TerrainMath.Height(x, z);
        float m = TerrainMath.Mask(x, z);
        if (m < 0.35f || h < 0.05f) return new Color(0.1f, 0.3f, 0.5f);           // water
        if (h < 0.9f) return new Color(0.78f, 0.7f, 0.48f);                          // sand
        float n = Noise.Fbm(x * 0.04f, z * 0.04f, 3);
        Color grass = Color.Lerp(new Color(0.22f, 0.45f, 0.16f), new Color(0.38f, 0.58f, 0.22f), n);
        grass = Color.Lerp(grass, new Color(0.5f, 0.5f, 0.28f), Mathf.Clamp01(n * 1.5f - 0.55f) * 0.5f);
        if (h > 4.5f) grass = Color.Lerp(grass, new Color(0.45f, 0.45f, 0.45f), Mathf.Clamp01((h - 4.5f) / 2f)); // rock
        if (h > 6.5f) grass = Color.Lerp(grass, new Color(0.9f, 0.92f, 0.95f), Mathf.Clamp01((h - 6.5f) / 2f)); // snow
        return grass;
    }
}
