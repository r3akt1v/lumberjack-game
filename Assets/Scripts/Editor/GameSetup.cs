using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public static class GameSetup
{
    [MenuItem("Lumberjack/Build Main Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var go = new GameObject("Game");
        go.AddComponent<GameBootstrap>();

        // lighting ambient will be driven by DayNightCycle at runtime
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.fog = true;

        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");

        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("Scene built: Assets/Scenes/Main.unity");
    }

    public static void BuildMac()
    {
        Build();
        var opts = new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Builds/Mac/LumberjackGame.app",
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("BUILD RESULT: " + report.summary.result);
    }

    static double smokeEnd;

    public static void SmokeTest()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        smokeEnd = EditorApplication.timeSinceStartup + 30.0;
        EditorApplication.update += SmokeTick;
        EditorApplication.EnterPlaymode();
    }

    static void SmokeTick()
    {
        if (EditorApplication.timeSinceStartup < smokeEnd) return;
        EditorApplication.update -= SmokeTick;
        Debug.Log("SMOKE TEST DONE");
        EditorApplication.Exit(0);
    }
}
