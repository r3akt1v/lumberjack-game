using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public class Goal
    {
        public string desc;
        public int target;
        public int reward;
        public Func<int> progress;
        public bool done;
    }

    public List<Goal> goals = new List<Goal>();
    public int currentIndex;
    public string toast;
    public float toastTimer;

    public static event Action OnGoalCompleted;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildGoals();
    }

    void BuildGoals()
    {
        goals.Clear();
        goals.Add(new Goal { desc = "Срубите 5 деревьев", target = 5, reward = 50, progress = () => GameManager.Instance.treesChopped });
        goals.Add(new Goal { desc = "Продайте 10 досок", target = 10, reward = 80, progress = () => GameManager.Instance.planksSold });
        goals.Add(new Goal { desc = "Заработайте 300 монет", target = 300, reward = 100, progress = () => GameManager.Instance.totalEarned });
        goals.Add(new Goal { desc = "Улучшите дом до 2 уровня", target = 2, reward = 150, progress = () => GameManager.Instance.houseLevel });
        goals.Add(new Goal { desc = "Срубите 25 деревьев", target = 25, reward = 150, progress = () => GameManager.Instance.treesChopped });
        goals.Add(new Goal { desc = "Заработайте 1000 монет", target = 1000, reward = 250, progress = () => GameManager.Instance.totalEarned });
        goals.Add(new Goal { desc = "Улучшите дом до 3 уровня", target = 3, reward = 300, progress = () => GameManager.Instance.houseLevel });
        goals.Add(new Goal { desc = "Продайте 40 досок", target = 40, reward = 300, progress = () => GameManager.Instance.planksSold });
    }

    void Update()
    {
        if (toastTimer > 0) toastTimer -= Time.deltaTime;
        if (currentIndex >= goals.Count) return;
        var g = goals[currentIndex];
        if (g.progress() >= g.target)
        {
            g.done = true;
            GameManager.Instance.AddMoney(g.reward);
            toast = $"Цель выполнена! +{g.reward} монет";
            toastTimer = 4f;
            currentIndex++;
            AudioManager.Instance?.Play("levelup");
            OnGoalCompleted?.Invoke();
            if (currentIndex >= goals.Count)
            {
                toast = "Все цели выполнены! Лес в надёжных руках.";
                toastTimer = 6f;
            }
        }
    }

    public Goal Current => currentIndex < goals.Count ? goals[currentIndex] : null;
    public int CurrentProgress => Current != null ? Mathf.Min(Current.progress(), Current.target) : 0;

    public void ResetAll()
    {
        currentIndex = 0;
        foreach (var g in goals) g.done = false;
        // NOTE: progress funcs are cumulative stats, so replaying goals needs stat reset; handled by new game
    }
}
