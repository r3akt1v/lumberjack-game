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

        // debug: -autostart skips the start screen (used for automated testing)
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == "-autostart") ui.StartGame();

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

    void OnDestroy()
    {
        Tree.OnFelled -= OnTreeFelled;
    }
}
