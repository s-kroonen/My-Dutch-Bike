using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// One-time project setup: assigns URP, merge-friendly editor settings, and a runnable starter scene.
/// Run via Tools/Project Bootstrap/Run Once, or headless: -executeMethod ProjectBootstrap.Run
/// </summary>
public static class ProjectBootstrap
{
    private const string UrpAssetPath = "Assets/Settings/MyDutchBike_URP.asset";
    private const string RendererAssetPath = "Assets/Settings/MyDutchBike_Renderer.asset";
    private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

    [MenuItem("Tools/Project Bootstrap/Run Once")]
    public static void Run()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        EditorSettings.externalVersionControl = "Visible Meta Files";

        SetUpUrp();
        CreateBootstrapScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ProjectBootstrap: done.");
    }

    private static void SetUpUrp()
    {
        if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpAssetPath) != null)
            return;

        var urpAsset = UniversalRenderPipelineAsset.Create();
        AssetDatabase.CreateAsset(urpAsset, UrpAssetPath);

        urpAsset.LoadBuiltinRendererData();
        AssetDatabase.SaveAssets();

        // LoadBuiltinRendererData creates the renderer asset at a fixed default path; relocate it next to the pipeline asset.
        const string defaultRendererPath = "Assets/UniversalRenderer.asset";
        if (AssetDatabase.LoadAssetAtPath<Object>(defaultRendererPath) != null)
            AssetDatabase.MoveAsset(defaultRendererPath, RendererAssetPath);

        GraphicsSettings.defaultRenderPipeline = urpAsset;
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
            QualitySettings.renderPipeline = urpAsset;
        }
    }

    private static void CreateBootstrapScene()
    {
        if (System.IO.File.Exists(BootstrapScenePath))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(5f, 1f, 5f);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.0f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        camGo.transform.position = new Vector3(0f, 2f, -6f);
        camGo.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

        EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(BootstrapScenePath, true) };
    }
}
