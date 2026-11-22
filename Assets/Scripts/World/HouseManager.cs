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
    const float DoorW = 1.25f;
    const float DoorH = 2.3f;
    // visual door leaf size per stage (matches doorway in model)
    static readonly float[] DoorWidths = { 0f, 1.0f, 1.05f, 1.15f, 1.3f, 1.5f };
    static readonly float[] DoorHeights = { 0f, 1.9f, 2.1f, 2.2f, 2.3f, 2.5f };

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

        BuildDoor(lvl, w, d);
    }

    void BuildDoor(int lvl, float w, float d)
    {
        float dw = DoorWidths[lvl];
        float dh = DoorHeights[lvl];
        // hinge pivot at left edge of doorway (as seen from outside, +Z)
        var pivot = new GameObject("DoorPivot");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = new Vector3(-dw / 2f, 0f, d / 2f - 0.08f);

        var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leaf.name = "DoorLeaf";
        leaf.transform.SetParent(pivot.transform, false);
        leaf.transform.localPosition = new Vector3(dw / 2f, dh / 2f, 0f);
        leaf.transform.localScale = new Vector3(dw - 0.04f, dh, 0.08f);
        var lr = leaf.GetComponent<Renderer>();
        lr.sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.42f, 0.26f, 0.13f) };
        // knob
        var knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        knob.name = "Knob";
        knob.transform.SetParent(leaf.transform, false);
        knob.transform.localPosition = new Vector3(0.85f, 0f, -0.09f);
        knob.transform.localScale = Vector3.one * 0.09f;
        knob.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Standard")) { color = new Color(0.75f, 0.6f, 0.25f) };

        var col = leaf.GetComponent<BoxCollider>();
        col.size = Vector3.one; // cube already sized via scale
        var door = pivot.AddComponent<Door>();
        door.leaf = pivot.transform; // rotate the hinge pivot, not the leaf centre
        door.leafCol = col;
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
