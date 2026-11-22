using System;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int money;
    public int axeDamageLevel = 1;
    public int axeSpeedLevel = 1;
    public int carryLevel = 1;
    public int houseLevel = 1;
    public int treesChopped;
    public int planksSold;
    public int totalEarned;
    public float timeOfDay = 0.35f;
    public string goalsJson;
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    string path;
    float autosaveTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        path = Path.Combine(Application.persistentDataPath, "lumberjack_save.json");
    }

    public bool HasSave => File.Exists(path);

    public void Save()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        var d = new SaveData {
            money = gm.money,
            axeDamageLevel = gm.axeDamageLevel,
            axeSpeedLevel = gm.axeSpeedLevel,
            carryLevel = gm.carryLevel,
            houseLevel = gm.houseLevel,
            treesChopped = gm.treesChopped,
            planksSold = gm.planksSold,
            totalEarned = gm.totalEarned,
            timeOfDay = DayNightCycle.Instance != null ? DayNightCycle.Instance.timeOfDay : 0.35f,
        };
        File.WriteAllText(path, JsonUtility.ToJson(d, true));
    }

    public void Load()
    {
        if (!HasSave) return;
        try
        {
            var d = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.money = d.money;
            gm.axeDamageLevel = d.axeDamageLevel;
            gm.axeSpeedLevel = d.axeSpeedLevel;
            gm.carryLevel = d.carryLevel;
            gm.houseLevel = d.houseLevel;
            gm.treesChopped = d.treesChopped;
            gm.planksSold = d.planksSold;
            gm.totalEarned = d.totalEarned;
            if (DayNightCycle.Instance != null) DayNightCycle.Instance.timeOfDay = d.timeOfDay;
            gm.Notify();
        }
        catch (Exception e) { Debug.LogWarning("Load failed: " + e.Message); }
    }

    void Update()
    {
        autosaveTimer += Time.deltaTime;
        if (autosaveTimer > 10f) { autosaveTimer = 0; Save(); }
    }

    void OnApplicationQuit() => Save();
    void OnApplicationPause(bool p) { if (p) Save(); }
}
