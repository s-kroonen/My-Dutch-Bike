using System.IO;
using MyDutchBike.Bike;
using MyDutchBike.Interaction;
using MyDutchBike.Parts;
using MyDutchBike.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds the two M1 greybox test environments (ADR-0005): a city street loop with a city-bike
/// BOM scattered nearby, and a race-track loop with a race-bike BOM. Idempotent — re-running
/// skips scenes that already exist on disk.
/// </summary>
public static class GreyboxSceneAuthoring
{
    private const string CityScenePath = "Assets/Scenes/City.unity";
    private const string RaceScenePath = "Assets/Scenes/RaceTrack.unity";

    [MenuItem("Tools/Project Bootstrap/Author Greybox Scenes")]
    public static void Run()
    {
        BikeBomAuthoring.Run();

        BuildCityScene();
        BuildRaceTrackScene();

        var scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true),
            new EditorBuildSettingsScene(CityScenePath, true),
            new EditorBuildSettingsScene(RaceScenePath, true),
        };
        EditorBuildSettings.scenes = scenes;

        AssetDatabase.SaveAssets();
        Debug.Log("GreyboxSceneAuthoring: done.");
    }

    private static void BuildCityScene()
    {
        if (File.Exists(CityScenePath))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateGround("Ground", new Vector3(30f, 1f, 30f), new Color(0.42f, 0.47f, 0.35f));
        CreateRoad(new Vector3(0f, 0.01f, 0f), new Vector3(10f, 1f, 16f), new Color(0.3f, 0.3f, 0.32f));
        CreatePerimeterMarkers(new Vector3(10f, 1f, 16f), 2f, new Vector3(0.2f, 0.6f, 0.2f), new Color(0.6f, 0.55f, 0.5f), "Building", true);
        CreateLight();

        var standPosition = new Vector3(3f, 0f, 2f);
        BuildBikeStand("CityBikeStand", standPosition, BikeBomAuthoring.FrameCity, BikeBomAuthoring.CityMechanics);
        CreatePlayerRig(standPosition + new Vector3(0f, 0f, -3f));

        EditorSceneManager.SaveScene(scene, CityScenePath);
    }

    private static void BuildRaceTrackScene()
    {
        if (File.Exists(RaceScenePath))
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateGround("Ground", new Vector3(60f, 1f, 60f), new Color(0.35f, 0.42f, 0.3f));
        CreateRoad(new Vector3(0f, 0.01f, 0f), new Vector3(14f, 1f, 34f), new Color(0.25f, 0.25f, 0.27f));
        CreatePerimeterMarkers(new Vector3(14f, 1f, 34f), 3f, new Vector3(0.15f, 0.5f, 0.15f), new Color(0.85f, 0.15f, 0.1f), "Pylon", false);
        CreateLight();

        var standPosition = new Vector3(5f, 0f, 4f);
        BuildBikeStand("RaceBikeStand", standPosition, BikeBomAuthoring.FrameRace, BikeBomAuthoring.RaceMechanics);
        CreatePlayerRig(standPosition + new Vector3(0f, 0f, -3f));

        EditorSceneManager.SaveScene(scene, RaceScenePath);
    }

    private static void CreateGround(string name, Vector3 scale, Color color)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = name;
        ground.transform.localScale = scale;
        ground.GetComponent<Renderer>().sharedMaterial = MaterialFor(name + "Mat", color);
    }

    private static void CreateRoad(Vector3 position, Vector3 scale, Color color)
    {
        var road = GameObject.CreatePrimitive(PrimitiveType.Plane);
        road.name = "Road";
        road.transform.position = position;
        road.transform.localScale = scale;
        road.GetComponent<Renderer>().sharedMaterial = MaterialFor("RoadMat", color);
    }

    private static void CreatePerimeterMarkers(Vector3 roadScale, float spacing, Vector3 markerSize, Color color, string namePrefix, bool rectangular)
    {
        float halfX = roadScale.x * 10f * 0.5f + 1f;
        float halfZ = roadScale.z * 10f * 0.5f + 1f;
        var parent = new GameObject($"{namePrefix}s").transform;
        var mat = MaterialFor(namePrefix + "Mat", color);
        int index = 0;

        void Place(float x, float z)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = $"{namePrefix}_{index++}";
            marker.transform.SetParent(parent, true);
            marker.transform.position = new Vector3(x, markerSize.y * 0.5f, z);
            marker.transform.localScale = markerSize;
            marker.GetComponent<Renderer>().sharedMaterial = mat;
        }

        for (float x = -halfX; x <= halfX; x += spacing)
        {
            Place(x, -halfZ);
            Place(x, halfZ);
        }
        for (float z = -halfZ + spacing; z < halfZ; z += spacing)
        {
            Place(-halfX, z);
            Place(halfX, z);
        }
    }

    private static void CreateLight()
    {
        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.0f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void BuildBikeStand(string name, Vector3 position, PartDefinition frame, FrameMechanics mechanics)
    {
        var stand = new GameObject(name);
        stand.transform.position = position;

        var assembly = stand.AddComponent<BikeAssembly>();
        assembly.objectId = name;
        assembly.frameDefinition = frame;
        assembly.requiredParts = BikeBomAuthoring.SharedRequiredParts;
        assembly.mechanics = mechanics;

        var mountCollider = stand.AddComponent<BoxCollider>();
        mountCollider.isTrigger = true;
        mountCollider.size = new Vector3(1.2f, 1f, 0.5f);
        mountCollider.center = new Vector3(0f, 0.5f, 0f);

        var ride = stand.AddComponent<BikeRideController>();
        var camAnchor = new GameObject("RiderCameraAnchor").transform;
        camAnchor.SetParent(stand.transform, false);
        camAnchor.localPosition = new Vector3(0f, 1.1f, -0.35f);
        ride.cameraAnchor = camAnchor;

        var requiredParts = BikeBomAuthoring.SharedRequiredParts;
        var looseParent = new GameObject($"{name}_LooseParts").transform;
        looseParent.SetParent(stand.transform.parent, true);
        for (int i = 0; i < requiredParts.Length; i++)
        {
            var def = requiredParts[i];
            float angle = i * (360f / requiredParts.Length) * Mathf.Deg2Rad;
            var offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.6f;
            var instance = Object.Instantiate(def.prefab, position + offset + Vector3.up * 0.15f, Quaternion.identity, looseParent);
            instance.name = def.displayName;
            var loose = instance.AddComponent<LoosePart>();
            loose.definition = def;
        }
    }

    private static void CreatePlayerRig(Vector3 position)
    {
        var player = new GameObject("Player");
        player.transform.position = position;
        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        controller.radius = 0.35f;

        var cameraGo = new GameObject("PlayerCamera");
        cameraGo.transform.SetParent(player.transform, false);
        cameraGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        var cam = cameraGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cameraGo.AddComponent<AudioListener>();

        var holdPoint = new GameObject("HoldPoint");
        holdPoint.transform.SetParent(cameraGo.transform, false);
        holdPoint.transform.localPosition = new Vector3(0.3f, -0.2f, 0.6f);

        var firstPerson = player.AddComponent<FirstPersonController>();
        firstPerson.eye = cam;

        var interactor = player.AddComponent<PlayerInteractor>();
        interactor.eye = cam;
        interactor.firstPerson = firstPerson;
        interactor.holdPoint = holdPoint.transform;

        player.AddComponent<BikeDebugOverlay>();
    }

    private static Material MaterialFor(string name, Color color)
    {
        string path = $"Assets/Art/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            SetBaseColor(existing, color); // re-apply: earlier runs used the wrong shader property
            return existing;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader);
        SetBaseColor(mat, color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    /// <summary>URP shaders expose "_BaseColor", not the legacy "_Color" that Material.color assumes.</summary>
    private static void SetBaseColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
    }
}
