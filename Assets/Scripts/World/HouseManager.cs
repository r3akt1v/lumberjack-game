using UnityEngine;

public class HouseManager : MonoBehaviour
{
    static readonly string[] ModelNames = { "", "house_shed", "house_cabin", "house", "house_big", "house_palace" };
    // collider footprint per stage: x=width, y=height, z=depth (door on +Z)
    static readonly Vector3[] Footprints =
    {
        Vector3.zero,
        new Vector3(3.8f, 3.0f, 3.2f),
        new Vector3(4.8f, 4.4f, 4.2f),
        new Vector3(7.2f, 6.6f, 6.2f),
        new Vector3(9.2f, 7.2f, 8.2f),
        new Vector3(11.2f, 10.6f, 9.2f),
    };
    const float DoorW = 1.4f;
    const float DoorH = 2.4f;

    int appliedLevel = -1;
    Light interiorLight;

    void Update()
    {
        int lvl = GameManager.Instance != null ? GameManager.Instance.houseLevel : 1;
        if (lvl != appliedLevel)
        {
            appliedLevel = lvl;
            Apply(lvl);
        }
    }

    void Apply(int lvl)
    {
        lvl = Mathf.Clamp(lvl, 1, 5);

        // clear previous model/walls/light
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
        interiorLight = null;

        var prefab = Resources.Load<GameObject>("Models/" + ModelNames[lvl]);
        if (prefab != null)
        {
            var m = Instantiate(prefab, transform);
            m.name = "Model";
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            m.transform.localScale = Vector3.one;
        }

        // rebuild colliders: walls with doorway gap on +Z (front)
        var fp = Footprints[lvl];
        float w = fp.x, h = fp.y, d = fp.z;
        float t = 0.4f;
        Wall(new Vector3(0, h / 2, -d / 2 + t / 2), new Vector3(w + t, h, t));                    // back
        Wall(new Vector3(-w / 2 + t / 2, h / 2, 0), new Vector3(t, h, d));                        // left
        Wall(new Vector3(w / 2 - t / 2, h / 2, 0), new Vector3(t, h, d));                         // right
        // front wall split around door
        float sideW = (w - DoorW) / 2f;
        float sideCX = DoorW / 2f + sideW / 2f;
        Wall(new Vector3(-sideCX, h / 2, d / 2 - t / 2), new Vector3(sideW, h, t));
        Wall(new Vector3(sideCX, h / 2, d / 2 - t / 2), new Vector3(sideW, h, t));
        Wall(new Vector3(0, (h + DoorH) / 2f, d / 2 - t / 2), new Vector3(DoorW + 0.2f, h - DoorH, t)); // lintel

        // interior light
        if (interiorLight == null)
        {
            var lg = new GameObject("HouseLight");
            lg.transform.SetParent(transform, false);
            interiorLight = lg.AddComponent<Light>();
            interiorLight.type = LightType.Point;
            interiorLight.color = new Color(1f, 0.75f, 0.45f);
        }
        interiorLight.transform.localPosition = new Vector3(0, h * 0.55f, -d * 0.1f);
        interiorLight.range = Mathf.Max(7f, w * 0.9f);
        interiorLight.intensity = lvl >= 4 ? 1.8f : 1.1f;
        interiorLight.enabled = true;
    }

    void Wall(Vector3 center, Vector3 size)
    {
        var go = new GameObject("WallCol");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = center;
        var b = go.AddComponent<BoxCollider>();
        b.size = size;
    }
}
