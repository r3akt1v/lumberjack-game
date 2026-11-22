using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Resources")]
    public int money;
    public int logsCarried;
    public int planksCarried;
    public int logsInSawmillQueue;

    [Header("Upgrades")]
    public int axeDamageLevel = 1;   // 1..4
    public int axeSpeedLevel = 1;    // 1..4
    public int carryLevel = 1;       // 1..4
    public int houseLevel = 1;       // 1..5

    public int AxeDamage => axeDamageLevel;
    public float AxeCooldown => new[] { 0f, 0.9f, 0.75f, 0.6f, 0.45f }[axeSpeedLevel];
    public int CarryCapacity => new[] { 0, 5, 8, 12, 20 }[carryLevel];
    public float SellBonus => 1f + 0.1f * (houseLevel - 1);

    public const int LogPrice = 8;
    public const int PlankPrice = 15;
    public const int LogsPerPlank = 1;

    // stats for quests
    public int treesChopped;
    public int planksSold;
    public int totalEarned;

    public event Action OnChanged;
    public void Notify() => OnChanged?.Invoke();

    [Serializable]
    public class UpgradeDef
    {
        public string id;
        public string name;
        public string desc;
        public int[] costs;
        public int level;
        public Func<bool> canBuy;
        public Action apply;
    }

    public List<UpgradeDef> upgrades = new List<UpgradeDef>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUpgrades();
    }

    void BuildUpgrades()
    {
        upgrades.Clear();
        upgrades.Add(new UpgradeDef {
            id = "axe_dmg", name = "Урон топора",
            desc = "Больше урона по дереву за удар",
            costs = new[] { 0, 100, 300, 800 }, level = 1,
            canBuy = () => axeDamageLevel < 4,
            apply = () => { axeDamageLevel++; }
        });
        upgrades.Add(new UpgradeDef {
            id = "axe_spd", name = "Скорость рубки",
            desc = "Быстрее взмах топора",
            costs = new[] { 0, 150, 400, 1000 }, level = 1,
            canBuy = () => axeSpeedLevel < 4,
            apply = () => { axeSpeedLevel++; }
        });
        upgrades.Add(new UpgradeDef {
            id = "carry", name = "Вместимость",
            desc = "Больше брёвен и досок за раз",
            costs = new[] { 0, 200, 500, 1200 }, level = 1,
            canBuy = () => carryLevel < 4,
            apply = () => { carryLevel++; }
        });
        upgrades.Add(new UpgradeDef {
            id = "house", name = "Дом",
            desc = "+10% к цене продажи за уровень, улучшает внешний вид",
            costs = new[] { 0, 500, 1500, 4000, 10000 }, level = 1,
            canBuy = () => houseLevel < 5,
            apply = () => { houseLevel++; }
        });
    }

    public bool TryBuy(UpgradeDef u)
    {
        int idx = Mathf.Clamp(u.level, 0, u.costs.Length - 1);
        int cost = u.costs[idx];
        if (money < cost || !u.canBuy()) return false;
        money -= cost;
        u.apply();
        // sync def level
        if (u.id == "axe_dmg") u.level = axeDamageLevel;
        else if (u.id == "axe_spd") u.level = axeSpeedLevel;
        else if (u.id == "carry") u.level = carryLevel;
        else if (u.id == "house") u.level = houseLevel;
        Notify();
        return true;
    }

    public int NextCost(UpgradeDef u)
    {
        int idx = Mathf.Clamp(u.level, 0, u.costs.Length - 1);
        return u.costs[idx];
    }

    public bool HasCarrySpace(int n = 1) => logsCarried + planksCarried + n <= CarryCapacity;

    public void AddMoney(int v)
    {
        money += v;
        totalEarned += Mathf.Max(0, v);
        Notify();
    }

    public void SellResources()
    {
        int gain = Mathf.RoundToInt((logsCarried * LogPrice + planksCarried * PlankPrice) * SellBonus);
        if (gain <= 0) return;
        planksSold += planksCarried;
        logsCarried = planksCarried = 0;
        AddMoney(gain);
    }

    public int PendingSellValue => Mathf.RoundToInt((logsCarried * LogPrice + planksCarried * PlankPrice) * SellBonus);
}
