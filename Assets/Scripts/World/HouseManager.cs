using UnityEngine;

public class HouseManager : MonoBehaviour
{
    int appliedLevel = -1;

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
        foreach (Transform child in transform)
        {
            string n = child.name;
            if (n == "Part_Porch") child.gameObject.SetActive(lvl >= 2);
            else if (n == "Part_Tower") child.gameObject.SetActive(lvl >= 3);
            else if (n == "Part_Trim") child.gameObject.SetActive(lvl >= 4);
        }
        // lvl 5: warm light + slight scale
        var light = GetComponentInChildren<Light>();
        if (light == null)
        {
            var lg = new GameObject("HouseLight");
            lg.transform.SetParent(transform, false);
            lg.transform.localPosition = new Vector3(0, 3.2f, 2f);
            light = lg.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
        }
        light.intensity = lvl >= 5 ? 1.6f : 0.9f;
        light.color = lvl >= 5 ? new Color(1f, 0.8f, 0.45f) : new Color(1f, 0.7f, 0.4f);
        light.enabled = true;
        transform.localScale = Vector3.one * (1f + 0.03f * (lvl - 1));
    }
}
