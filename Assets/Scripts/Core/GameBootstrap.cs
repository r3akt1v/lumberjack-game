using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        // managers
        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.gameObject.AddComponent<SaveManager>();
        gm.gameObject.AddComponent<DayNightCycle>();
        gm.gameObject.AddComponent<AudioManager>();
        gm.gameObject.AddComponent<QuestManager>();

        // world
        var world = new GameObject("World").AddComponent<WorldBuilder>();

        // player
        var spawn = new Vector3(0f, 0f, -22f);
        spawn.y = TerrainMath.Height(spawn.x, spawn.z) + 0.1f;
        var pgo = new GameObject("Player");
        pgo.tag = "Player";
        pgo.transform.position = spawn;
        pgo.transform.rotation = Quaternion.Euler(0, 15f, 0);

        var cc = pgo.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0);
        cc.stepOffset = 0.4f; cc.slopeLimit = 50f;

        // camera
        var camGo = new GameObject("PlayerCamera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(pgo.transform, false);
        camGo.transform.localPosition = new Vector3(0, 1.62f, 0);
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.05f; cam.fieldOfView = 70f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.45f, 0.65f, 0.9f);
        camGo.AddComponent<AudioListener>();

        var fps = pgo.AddComponent<FPSController>();
        pgo.AddComponent<PlayerTag>();
        var inter = pgo.AddComponent<Interaction>();
        inter.cameraTransform = cam.transform;
        var axe = pgo.AddComponent<AxeController>();
        axe.cameraTransform = cam.transform;

        // UI
        var ui = new GameObject("UI").AddComponent<UIManager>();
        ui.Init(fps);

        // debug: -autostart skips the start screen; -debugview adds diagnostics
        bool dbg = false;
        foreach (var a in System.Environment.GetCommandLineArgs())
        {
            if (a == "-autostart") ui.StartGame();
            if (a == "-debugview") dbg = true;
        }
        if (dbg) StartCoroutine(DebugView());

        // tree felled -> drop logs
        Tree.OnFelled += OnTreeFelled;
    }

    void OnTreeFelled(Tree t)
    {
        var logModel = Resources.Load<GameObject>("Models/log");
        if (logModel == null) return;
        for (int i = 0; i < 3; i++)
        {
            var p = t.transform.position + new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f));
            p.y = TerrainMath.Height(p.x, p.z) + 0.4f;
            var go = Instantiate(logModel, p, Quaternion.Euler(0, Random.Range(0, 360), 0));
            go.name = "PickupLog";
            var pk = go.AddComponent<Pickup>();
            pk.kind = Pickup.Kind.Log;
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(0.7f, 0.7f, 1.5f);
            col.isTrigger = true;
        }
    }

    System.Collections.IEnumerator DebugView()
    {
        yield return new WaitForSeconds(2f);
        // collider census
        var cols = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
        Debug.Log($"[DBG] colliders total={cols.Length}");
        var treesGo = GameObject.Find("Trees");
        int treeCols = 0;
        if (treesGo != null)
            foreach (Transform t in treesGo.transform)
                if (t.GetComponent<Collider>() != null) treeCols++;
        Debug.Log($"[DBG] trees={(treesGo != null ? treesGo.transform.childCount : -1)} withCollider={treeCols}");

        // dump bounds of every model prefab
        foreach (var m in new[] { "tree_pine", "stump", "log", "plank", "rock", "bush", "flower", "axe", "house", "sawmill", "crate", "fence", "sign" })
        {
            var prefab = Resources.Load<GameObject>("Models/" + m);
            if (prefab == null) { Debug.Log($"[DBG] model {m} MISSING"); continue; }
            var tmp = Instantiate(prefab, new Vector3(0, -500, 0), Quaternion.identity);
            var rs = tmp.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                Debug.Log($"[DBG] model {m} bounds={b.size}");
            }
            else Debug.Log($"[DBG] model {m} no renderers");
            Destroy(tmp);
        }

        // teleport player next to nearest tree, face it
        Tree nearest = null; float best = 1e9f;
        if (treesGo != null)
            foreach (Transform t in treesGo.transform)
            {
                float d = (t.position - Vector3.zero).sqrMagnitude;
                if (d < best) { best = d; nearest = t.GetComponent<Tree>(); }
            }
        var p = GameObject.FindGameObjectWithTag("Player");
        if (nearest != null && p != null)
        {
            Vector3 tp = nearest.transform.position;
            Vector3 flat = new Vector3(tp.x - p.transform.position.x, 0, tp.z - p.transform.position.z).normalized;
            // stand 4m away at trunk height, look slightly up at foliage
            var cc = p.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            p.transform.position = tp - flat * 4f + Vector3.up * 0.2f;
            p.transform.position = new Vector3(p.transform.position.x, tp.y + 0.2f, p.transform.position.z);
            if (cc != null) cc.enabled = true;
            var fps = p.GetComponent<FPSController>();
            if (fps != null)
            {
                float yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                fps.SetView(-8f, yaw); // look slightly up at crown
            }
            Debug.Log($"[DBG] teleported near tree at {tp} yaw-check");
            // dump tree transform + visual bounds
            Debug.Log($"[DBG] treeGO pos={nearest.transform.position} rot={nearest.transform.eulerAngles} scale={nearest.transform.lossyScale}");
            var vis = nearest.transform.Find("Visual");
            if (vis != null)
            {
                Debug.Log($"[DBG] visual localPos={vis.localPosition} localRot={vis.localEulerAngles} localScale={vis.localScale}");
                var rs = vis.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                    Debug.Log($"[DBG] visual bounds size={b.size} center={b.center} renderers={rs.Length}");
                }
            }
        }
        yield return new WaitForSeconds(1f);
        // raycast diagnostics from camera
        var camT = Camera.main != null ? Camera.main.transform : null;
        if (camT != null)
        {
            Debug.Log($"[DBG] cam pos={camT.position} fwd={camT.forward}");
            foreach (var off in new[] { Vector3.zero, new Vector3(0, 0.3f, 0), new Vector3(0, -0.3f, 0), new Vector3(0.3f, 0, 0), new Vector3(-0.3f, 0, 0) })
            {
                var d = camT.forward + camT.TransformDirection(off) * 0.15f;
                if (Physics.Raycast(camT.position, d.normalized, out var h, 300f))
                    Debug.Log($"[DBG] ray{off} hit {h.collider.name} d={h.distance:F1}");
                else Debug.Log($"[DBG] ray{off} NOTHING");
            }
        }
        yield return new WaitForSeconds(1f);
        string path = System.IO.Path.Combine(Application.persistentDataPath, "debug_view.png");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log("[DBG] screenshot -> " + path);

        // house check: stand in front of the door and screenshot
        var house = GameObject.Find("House");
        var p2 = GameObject.FindGameObjectWithTag("Player");
        if (house != null && p2 != null)
        {
            Vector3 hp = house.transform.position;
            Vector3 front = house.transform.forward; // door faces +Z local
            var cc2 = p2.GetComponent<CharacterController>();
            if (cc2 != null) cc2.enabled = false;
            p2.transform.position = hp + front * 11f + Vector3.up * 0.2f;
            p2.transform.position = new Vector3(p2.transform.position.x, TerrainMath.Height(p2.transform.position.x, p2.transform.position.z) + 0.2f, p2.transform.position.z);
            if (cc2 != null) cc2.enabled = true;
            var fps2 = p2.GetComponent<FPSController>();
            if (fps2 != null) fps2.SetView(0f, Quaternion.LookRotation(-front).eulerAngles.y);
            yield return new WaitForSeconds(1.5f);
            string hp2 = System.IO.Path.Combine(Application.persistentDataPath, "house_view.png");
            ScreenCapture.CaptureScreenshot(hp2);
            Debug.Log("[DBG] house screenshot -> " + hp2 + " housePos=" + hp);
        }

        // axe check: pitch camera down to frame the viewmodel
        var p3 = GameObject.FindGameObjectWithTag("Player");
        var fps3 = p3 != null ? p3.GetComponent<FPSController>() : null;
        if (fps3 != null)
        {
            fps3.SetView(-30f, 0f);
            yield return new WaitForSeconds(0.5f);
            string ap = System.IO.Path.Combine(Application.persistentDataPath, "axe_view.png");
            ScreenCapture.CaptureScreenshot(ap);
            Debug.Log("[DBG] axe screenshot -> " + ap);
        }
    }

    void OnDestroy()
    {
        Tree.OnFelled -= OnTreeFelled;
    }
}
