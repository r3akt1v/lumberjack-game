using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    Canvas canvas;
    Font font;
    GameObject startPanel, shopPanel, pausePanel;
    Text moneyText, carryText, timeText, promptText, goalText, goalProgressText, toastText, sellHintText;
    Image goalBarFill;
    FPSController player;
    bool shopOpen;
    List<GameObject> shopRows = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Helvetica", 16);
    }

    public void Init(FPSController p)
    {
        player = p;
        BuildCanvas();
        BuildHUD();
        BuildShop();
        BuildStartScreen();
        BuildPause();
        gameObject.AddComponent<Minimap>().Init(p.transform, canvas);
        player.SetCursorLocked(false); // start screen needs cursor
    }

    // ---------- helpers ----------
    GameObject Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.55f);
        return go;
    }

    Text Label(string name, Transform parent, string content, int size, TextAnchor anchor, Vector2 pos, Vector2 sz, Color? color = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sz;
        var t = go.GetComponent<Text>();
        t.font = font; t.fontSize = size; t.alignment = anchor;
        t.text = content; t.color = color ?? Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    Button MakeButton(string name, Transform parent, string label, Vector2 pos, Vector2 size, System.Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        go.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.25f, 0.95f);
        var btn = go.GetComponent<Button>();
        var t = Label("Label", go.transform, label, 18, TextAnchor.MiddleCenter, Vector2.zero, size);
        t.color = Color.white;
        btn.onClick.AddListener(() => onClick?.Invoke());
        return btn;
    }

    // ---------- canvas ----------
    void BuildCanvas()
    {
        var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cgo.transform.SetParent(transform, false);
        canvas = cgo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = cgo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(transform, false);
        }
    }

    // ---------- HUD ----------
    void BuildHUD()
    {
        // crosshair
        var ch = Label("Crosshair", canvas.transform, "+", 28, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40, 40), new Color(1, 1, 1, 0.8f));
        ch.raycastTarget = false;

        // top-left resources panel
        var res = Panel("Resources", canvas.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(175, -80), new Vector2(350, 150));
        moneyText = Label("Money", res.transform, "Монеты: 0", 22, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(320, 36), new Color(1f, 0.9f, 0.4f));
        moneyText.rectTransform.anchorMin = moneyText.rectTransform.anchorMax = new Vector2(0, 1);
        moneyText.rectTransform.pivot = new Vector2(0, 1);
        moneyText.rectTransform.anchoredPosition = new Vector2(14, -10);
        carryText = Label("Carry", res.transform, "Груз: 0/5", 16, TextAnchor.MiddleLeft, Vector2.zero, new Vector2(320, 60), new Color(0.8f, 0.95f, 0.8f));
        carryText.rectTransform.anchorMin = carryText.rectTransform.anchorMax = new Vector2(0, 1);
        carryText.rectTransform.pivot = new Vector2(0, 1);
        carryText.rectTransform.anchoredPosition = new Vector2(14, -50);
        carryText.horizontalOverflow = HorizontalWrapMode.Wrap;

        // time top-right
        var tp = Panel("TimePanel", canvas.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-120, -50), new Vector2(220, 70));
        timeText = Label("Time", tp.transform, "12:00", 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(200, 50));

        // prompt bottom-center
        var pp = Panel("PromptPanel", canvas.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(600, 60));
        pp.GetComponent<Image>().color = new Color(0, 0, 0, 0.0f);
        promptText = Label("Prompt", pp.transform, "", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(580, 50), new Color(1f, 0.95f, 0.6f));
        promptText.raycastTarget = false;

        // goal panel right
        var gp = Panel("GoalPanel", canvas.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-225, 120), new Vector2(390, 120));
        goalText = Label("Goal", gp.transform, "", 16, TextAnchor.UpperLeft, Vector2.zero, new Vector2(360, 40));
        goalText.rectTransform.anchorMin = goalText.rectTransform.anchorMax = new Vector2(0, 1);
        goalText.rectTransform.pivot = new Vector2(0, 1);
        goalText.rectTransform.anchoredPosition = new Vector2(14, -8);
        goalText.horizontalOverflow = HorizontalWrapMode.Wrap;
        var barBg = Panel("BarBg", gp.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(350, 14));
        barBg.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(barBg.transform, false);
        var frt = (RectTransform)fillGo.transform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(0, 1);
        frt.offsetMin = Vector2.zero; frt.offsetMax = Vector2.zero;
        goalBarFill = fillGo.GetComponent<Image>();
        goalBarFill.color = new Color(0.4f, 0.85f, 0.4f);
        goalProgressText = Label("GoalProg", gp.transform, "", 15, TextAnchor.MiddleCenter, new Vector2(0, -40), new Vector2(360, 22), new Color(0.8f, 0.9f, 0.8f));
        goalProgressText.horizontalOverflow = HorizontalWrapMode.Wrap;

        // toast
        toastText = Label("Toast", canvas.transform, "", 26, TextAnchor.MiddleCenter, new Vector2(0, 220), new Vector2(900, 50), new Color(1f, 0.9f, 0.5f));

        // controls hint bottom-left
        var hp = Panel("Hint", canvas.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(300, 30), new Vector2(600, 60));
        hp.GetComponent<Image>().color = new Color(0, 0, 0, 0.35f);
        var hintT = Label("HintText", hp.transform, "WASD — ход  |  Shift — бег  |  ЛКМ — рубить  |  E — действие  |  B — магазин", 15,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(580, 50), new Color(0.85f, 0.9f, 0.85f));
        hintT.horizontalOverflow = HorizontalWrapMode.Wrap;

        // sell value hint near prompt (shown dynamically)
        sellHintText = Label("SellHint", canvas.transform, "", 20, TextAnchor.MiddleCenter, new Vector2(0, 100), new Vector2(500, 40), new Color(0.6f, 1f, 0.6f));
    }

    // ---------- shop ----------
    void BuildShop()
    {
        shopPanel = Panel("Shop", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 620));
        shopPanel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.08f, 0.96f);
        var title = Label("Title", shopPanel.transform, "МАГАЗИН  [B] — закрыть", 28, TextAnchor.MiddleCenter, new Vector2(0, 270), new Vector2(520, 50), new Color(1f, 0.9f, 0.5f));
        title.raycastTarget = false;
        var moneyLbl = Label("ShopMoney", shopPanel.transform, "Монеты: 0", 22, TextAnchor.MiddleCenter, new Vector2(0, 225), new Vector2(520, 40), new Color(1f, 0.9f, 0.4f));

        var gm = GameManager.Instance;
        float y = 175f;
        foreach (var u in gm.upgrades)
        {
            var row = new GameObject("Row_" + u.id, typeof(RectTransform), typeof(Image));
            row.transform.SetParent(shopPanel.transform, false);
            var rt = (RectTransform)row.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, y);
            rt.sizeDelta = new Vector2(520, 80);
            row.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.15f, 0.9f);

            var nameT = Label("Name", row.transform, u.name, 20, TextAnchor.MiddleLeft, new Vector2(-130, 20), new Vector2(250, 30), Color.white);
            nameT.rectTransform.pivot = new Vector2(0, 0.5f);
            nameT.rectTransform.anchoredPosition = new Vector2(-250, 18);
            var descT = Label("Desc", row.transform, u.desc, 14, TextAnchor.MiddleLeft, new Vector2(0, -12), new Vector2(360, 26), new Color(0.75f, 0.8f, 0.75f));
            descT.rectTransform.pivot = new Vector2(0, 0.5f);
            descT.rectTransform.anchoredPosition = new Vector2(-250, -14);
            descT.horizontalOverflow = HorizontalWrapMode.Wrap;

            var def = u;
            var btn = MakeButton("Buy", row.transform, "", new Vector2(190, 0), new Vector2(150, 46), () => BuyUpgrade(def, row));
            // level label
            var lvlT = Label("Lvl", row.transform, "", 15, TextAnchor.MiddleLeft, new Vector2(-250, -32), new Vector2(200, 22), new Color(0.6f, 0.9f, 0.6f));
            lvlT.name = "Lvl_" + u.id;
            lvlT.rectTransform.pivot = new Vector2(0, 0.5f);
            lvlT.rectTransform.anchoredPosition = new Vector2(-250, -34);
            shopRows.Add(row);
            y -= 95f;
        }
        shopPanel.SetActive(false);
    }

    void BuyUpgrade(GameManager.UpgradeDef u, GameObject row)
    {
        if (GameManager.Instance.TryBuy(u))
        {
            AudioManager.Instance?.Play("buy");
            RefreshShop();
        }
        else
        {
            AudioManager.Instance?.Play("error");
        }
    }

    void RefreshShop()
    {
        var gm = GameManager.Instance;
        var moneyLbl = shopPanel.transform.Find("ShopMoney")?.GetComponent<Text>();
        if (moneyLbl != null) moneyLbl.text = "Монеты: " + gm.money;
        foreach (var row in shopRows)
        {
            string id = row.name.Replace("Row_", "");
            var u = gm.upgrades.Find(x => x.id == id);
            if (u == null) continue;
            var btn = row.transform.Find("Buy")?.GetComponent<Button>();
            var btnText = btn != null ? btn.GetComponentInChildren<Text>() : null;
            var lvlT = row.transform.Find("Lvl_" + id)?.GetComponent<Text>();
            bool maxed = !u.canBuy();
            int pc = gm.NextPlankCost(u);
            if (btnText != null)
            {
                if (maxed) btnText.text = "МАКС";
                else if (pc > 0) btnText.text = $"{pc} досок" + (gm.NextCost(u) > 0 ? $" + {gm.NextCost(u)} монет" : "");
                else btnText.text = gm.NextCost(u) + " монет";
            }
            if (lvlT != null)
            {
                int lvl = u.id == "axe_dmg" ? gm.axeDamageLevel : u.id == "axe_spd" ? gm.axeSpeedLevel
                        : u.id == "carry" ? gm.carryLevel : gm.houseLevel;
                lvlT.text = "Уровень " + lvl + "/" + (u.costs.Length);
                if (u.id == "house")
                {
                    string[] stages = { "", "Сарай", "Домик", "Дом", "Вилла", "Особняк" };
                    lvlT.text = "Этап " + lvl + ": " + stages[Mathf.Clamp(lvl, 1, 5)];
                }
            }
            if (btn != null) btn.interactable = !maxed && gm.money >= gm.NextCost(u) && gm.planksCarried >= gm.NextPlankCost(u);
        }
    }

    public void StartGame()
    {
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave;
        if (hasSave) SaveManager.Instance.Load();
        startPanel.SetActive(false);
        player.SetCursorLocked(true);
    }

    // ---------- start & pause ----------
    void BuildStartScreen()
    {
        startPanel = Panel("Start", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        startPanel.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.05f, 0.97f);
        var title = Label("Title", startPanel.transform, "LUMBERJACK", 72, TextAnchor.MiddleCenter, new Vector2(0, 120), new Vector2(1200, 100), new Color(0.55f, 0.8f, 0.4f));
        var sub = Label("Sub", startPanel.transform, "Лесорубный симулятор", 30, TextAnchor.MiddleCenter, new Vector2(0, 40), new Vector2(800, 50), new Color(0.85f, 0.9f, 0.8f));
        var desc = Label("Desc", startPanel.transform,
            "Рубите деревья, пилите доски, улучшайте дом и инструменты.\nWASD — движение | Мышь — обзор | ЛКМ — топор | E — взаимодействие | B — магазин",
            20, TextAnchor.MiddleCenter, new Vector2(0, -160), new Vector2(900, 120), new Color(0.7f, 0.78f, 0.7f));

        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave;
        MakeButton("PlayBtn", startPanel.transform, "ИГРАТЬ", new Vector2(0, -280), new Vector2(300, 60), StartGame);
        if (hasSave)
        {
            MakeButton("NewBtn", startPanel.transform, "НОВАЯ ИГРА", new Vector2(0, -360), new Vector2(300, 50), () =>
            {
                // wipe save
                var p = System.IO.Path.Combine(Application.persistentDataPath, "lumberjack_save.json");
                if (System.IO.File.Exists(p)) System.IO.File.Delete(p);
                startPanel.SetActive(false);
                player.SetCursorLocked(true);
            });
        }
    }

    void BuildPause()
    {
        pausePanel = Panel("Pause", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        pausePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        Label("PauseTitle", pausePanel.transform, "ПАУЗА", 56, TextAnchor.MiddleCenter, new Vector2(0, 100), new Vector2(600, 80), Color.white);
        MakeButton("Resume", pausePanel.transform, "ПРОДОЛЖИТЬ", new Vector2(0, -40), new Vector2(300, 55), () =>
        {
            pausePanel.SetActive(false);
            player.SetCursorLocked(true);
        });
        MakeButton("SaveBtn", pausePanel.transform, "СОХРАНИТЬ", new Vector2(0, -120), new Vector2(300, 55), () =>
        {
            SaveManager.Instance?.Save();
            AudioManager.Instance?.Play("buy");
        });
        MakeButton("QuitBtn", pausePanel.transform, "В МЕНЮ", new Vector2(0, -200), new Vector2(300, 55), () =>
        {
            SaveManager.Instance?.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
        pausePanel.SetActive(false);
    }

    // ---------- runtime ----------
    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || player == null) return;

        // keep cursor free while any menu is open
        if (startPanel.activeSelf || pausePanel.activeSelf || shopOpen)
        {
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible) Cursor.visible = true;
        }

        // start game with Enter or any mouse click on the start screen
        if (startPanel.activeSelf && (Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)))
        {
            Debug.Log("[UI] start game triggered");
            var btn = startPanel.transform.Find("PlayBtn")?.GetComponent<Button>();
            if (btn != null) btn.onClick.Invoke();
        }

        // cursor/pause handling
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (shopOpen) { shopOpen = false; shopPanel.SetActive(false); player.SetCursorLocked(true); }
            else if (!startPanel.activeSelf)
            {
                bool willPause = pausePanel.activeSelf == false;
                pausePanel.SetActive(willPause);
                player.SetCursorLocked(!willPause);
            }
        }
        if (Input.GetKeyDown(KeyCode.B) && !startPanel.activeSelf && !pausePanel.activeSelf)
        {
            shopOpen = !shopOpen;
            shopPanel.SetActive(shopOpen);
            if (shopOpen) RefreshShop();
            player.SetCursorLocked(!shopOpen);
        }

        // HUD refresh
        moneyText.text = "Монеты: " + gm.money;
        carryText.text = $"Груз: {gm.logsCarried + gm.planksCarried}/{gm.CarryCapacity}  (брёвна {gm.logsCarried}, доски {gm.planksCarried})";
        var dn = DayNightCycle.Instance;
        if (dn != null) timeText.text = dn.TimeString();

        var q = QuestManager.Instance;
        if (q != null && q.Current != null)
        {
            goalText.text = "Цель: " + q.Current.desc;
            goalProgressText.text = $"{q.CurrentProgress}/{q.Current.target}  → награда {q.Current.reward}";
            float f = q.Current.target > 0 ? (float)q.CurrentProgress / q.Current.target : 0;
            var rt = (RectTransform)goalBarFill.transform;
            rt.anchorMax = new Vector2(Mathf.Max(0.001f, f), 1);
            rt.offsetMax = Vector2.zero;
            toastText.text = q.toastTimer > 0 ? q.toast : "";
        }
        else if (q != null)
        {
            goalText.text = "Все цели выполнены!";
            goalProgressText.text = "";
            toastText.text = q.toastTimer > 0 ? q.toast : "";
        }
    }

    public void SetPrompt(string prompt)
    {
        if (promptText == null) return;
        promptText.text = prompt ?? "";
        var gm = GameManager.Instance;
        if (sellHintText != null)
            sellHintText.text = (gm != null && gm.PendingSellValue > 0) ? $"Можно продать за ~{gm.PendingSellValue} монет [E у ящика]" : "";
    }
}
