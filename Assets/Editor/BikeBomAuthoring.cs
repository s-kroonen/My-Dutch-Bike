using System.Collections.Generic;
using MyDutchBike.Parts;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Authors the M1 bike bill of materials (docs/PART_SYSTEM_DESIGN.md §8): two frame archetypes
/// (city, race) plus the shared parts that install identically on either, as placeholder
/// primitive-mesh prefabs + PartDefinition assets. Idempotent — safe to re-run.
///
/// Dependency note: most parts only need their immediate socket-parent installed, and that's
/// already enforced by socket existence (a socket doesn't exist until the part providing it is
/// spawned) — so PartDefinition.dependencies is left empty for those. The one real cross-branch
/// dependency is the chain, which installs on the crankset but also needs the rear wheel on.
/// </summary>
public static class BikeBomAuthoring
{
    private const string PrefabDir = "Assets/Prefabs/Parts";
    private const string DataDir = "Assets/Data/Parts";
    private const string MechanicsDir = "Assets/Data/Mechanics";
    private const string MaterialDir = "Assets/Art/Materials";

    public static PartDefinition FrameCity { get; private set; }
    public static PartDefinition FrameRace { get; private set; }
    public static PartDefinition Fork { get; private set; }
    public static PartDefinition WheelFront { get; private set; }
    public static PartDefinition WheelRear { get; private set; }
    public static PartDefinition TireFront { get; private set; }
    public static PartDefinition TireRear { get; private set; }
    public static PartDefinition Crankset { get; private set; }
    public static PartDefinition Chain { get; private set; }
    public static PartDefinition Handlebar { get; private set; }
    public static PartDefinition Seat { get; private set; }
    public static PartDefinition Brakes { get; private set; }
    public static PartDefinition Pedals { get; private set; }

    public static FrameMechanics CityMechanics { get; private set; }
    public static FrameMechanics RaceMechanics { get; private set; }

    public static PartDefinition[] SharedRequiredParts => new[]
    {
        Fork, WheelFront, WheelRear, TireFront, TireRear, Crankset, Chain, Handlebar, Seat, Brakes, Pedals
    };

    [MenuItem("Tools/Project Bootstrap/Author Bike BOM")]
    public static void Run()
    {
        EnsureFolder("Assets/Prefabs", "Parts");
        EnsureFolder("Assets/Data", "Parts");
        EnsureFolder("Assets/Data", "Mechanics");
        EnsureFolder("Assets/Art", "Materials");

        var frameSockets = new[]
        {
            new SocketDefinition { id = "frame.head", acceptedCategory = PartCategory.Fork, localPosition = new Vector3(0.45f, 0.25f, 0f) },
            new SocketDefinition { id = "frame.bb", acceptedCategory = PartCategory.Crankset, localPosition = new Vector3(0f, 0f, 0f) },
            new SocketDefinition { id = "frame.dropout", acceptedCategory = PartCategory.Wheel, localPosition = new Vector3(-0.45f, 0f, 0f) },
            new SocketDefinition { id = "frame.post", acceptedCategory = PartCategory.Seat, localPosition = new Vector3(-0.15f, 0.5f, 0f) },
        };

        var cityFramePrefab = CreatePrimitivePrefab("Frame_City", PrimitiveType.Cube,
            new Vector3(0.9f, 0.5f, 0.08f), Vector3.zero, new Vector3(0f, 0.35f, 0f), new Color(0.55f, 0.35f, 0.2f));
        var raceFramePrefab = CreatePrimitivePrefab("Frame_Race", PrimitiveType.Cube,
            new Vector3(1.0f, 0.3f, 0.06f), Vector3.zero, new Vector3(0f, 0.3f, 0f), new Color(0.75f, 0.1f, 0.1f));

        FrameCity = CreatePart("bike.frame.city", "City Frame", PartCategory.Frame, PartCategory.None, cityFramePrefab, null, frameSockets, null);
        FrameRace = CreatePart("bike.frame.race", "Race Frame", PartCategory.Frame, PartCategory.None, raceFramePrefab, null, frameSockets, null);

        CityMechanics = CreateMechanics("bike.frame.city", topSpeed: 5f, accel: 2.5f, turnRate: 110f, stability: 1.3f);
        RaceMechanics = CreateMechanics("bike.frame.race", topSpeed: 9f, accel: 4f, turnRate: 70f, stability: 0.7f);

        var forkPrefab = CreatePrimitivePrefab("Fork", PrimitiveType.Cube,
            new Vector3(0.06f, 0.4f, 0.06f), Vector3.zero, new Vector3(0f, -0.2f, 0f), new Color(0.2f, 0.2f, 0.2f));
        var forkSockets = new[]
        {
            new SocketDefinition { id = "fork.dropout", acceptedCategory = PartCategory.Wheel, localPosition = new Vector3(0f, -0.35f, 0f) },
            new SocketDefinition { id = "fork.stem", acceptedCategory = PartCategory.Handlebar, localPosition = new Vector3(0f, 0.05f, 0f) },
            new SocketDefinition { id = "fork.brake", acceptedCategory = PartCategory.Brake, localPosition = new Vector3(0f, -0.15f, 0.08f) },
        };
        var forkFasteners = new[]
        {
            new FastenerSlot { id = "headset_nut_1", type = FastenerType.Nut, localPosition = new Vector3(0.02f, 0.15f, 0f) },
            new FastenerSlot { id = "headset_nut_2", type = FastenerType.Nut, localPosition = new Vector3(-0.02f, 0.15f, 0f) },
        };
        Fork = CreatePart("bike.fork", "Fork", PartCategory.Fork, PartCategory.Fork, forkPrefab, null, forkSockets, forkFasteners);

        var wheelPrefab = CreatePrimitivePrefab("Wheel", PrimitiveType.Cylinder,
            new Vector3(0.66f, 0.03f, 0.66f), new Vector3(0f, 0f, 90f), Vector3.zero, new Color(0.08f, 0.08f, 0.08f));
        var axleFasteners = new[]
        {
            new FastenerSlot { id = "axle_nut_1", type = FastenerType.Nut, localPosition = new Vector3(0f, 0.35f, 0f) },
            new FastenerSlot { id = "axle_nut_2", type = FastenerType.Nut, localPosition = new Vector3(0f, -0.35f, 0f) },
        };
        WheelFront = CreatePart("bike.wheel_front", "Front Wheel", PartCategory.Wheel, PartCategory.Wheel, wheelPrefab, null,
            new[] { new SocketDefinition { id = "wheel_front.rim", acceptedCategory = PartCategory.Tire, localPosition = Vector3.zero } }, axleFasteners);
        WheelRear = CreatePart("bike.wheel_rear", "Rear Wheel", PartCategory.Wheel, PartCategory.Wheel, wheelPrefab, null,
            new[] { new SocketDefinition { id = "wheel_rear.rim", acceptedCategory = PartCategory.Tire, localPosition = Vector3.zero } }, axleFasteners);

        var tirePrefab = CreatePrimitivePrefab("Tire", PrimitiveType.Cylinder,
            new Vector3(0.72f, 0.05f, 0.72f), new Vector3(0f, 0f, 90f), Vector3.zero, new Color(0.02f, 0.02f, 0.02f));
        TireFront = CreatePart("bike.tire_front", "Front Tire", PartCategory.Tire, PartCategory.Tire, tirePrefab, null, null, null);
        TireRear = CreatePart("bike.tire_rear", "Rear Tire", PartCategory.Tire, PartCategory.Tire, tirePrefab, null, null, null);

        var cranksetPrefab = CreatePrimitivePrefab("Crankset", PrimitiveType.Cylinder,
            new Vector3(0.22f, 0.02f, 0.22f), new Vector3(0f, 0f, 90f), Vector3.zero, new Color(0.6f, 0.6f, 0.65f));
        var cranksetSockets = new[]
        {
            new SocketDefinition { id = "crankset.pedal", acceptedCategory = PartCategory.Pedal, localPosition = new Vector3(0.15f, 0f, 0.15f) },
            new SocketDefinition { id = "crankset.chain", acceptedCategory = PartCategory.Chain, localPosition = new Vector3(-0.1f, -0.05f, 0f) },
        };
        var cranksetFasteners = new[]
        {
            new FastenerSlot { id = "crank_bolt_1", type = FastenerType.Bolt, localPosition = new Vector3(0.1f, 0f, 0f) },
            new FastenerSlot { id = "crank_bolt_2", type = FastenerType.Bolt, localPosition = new Vector3(-0.1f, 0f, 0f) },
        };
        Crankset = CreatePart("bike.crankset", "Crankset", PartCategory.Crankset, PartCategory.Crankset, cranksetPrefab, null, cranksetSockets, cranksetFasteners);

        var chainPrefab = CreatePrimitivePrefab("Chain", PrimitiveType.Cube,
            new Vector3(0.5f, 0.02f, 0.02f), Vector3.zero, new Vector3(-0.25f, 0f, 0f), new Color(0.15f, 0.15f, 0.15f));
        Chain = CreatePart("bike.chain", "Chain", PartCategory.Chain, PartCategory.Chain, chainPrefab, new[] { WheelRear }, null, null);

        var handlebarPrefab = CreatePrimitivePrefab("Handlebar", PrimitiveType.Cube,
            new Vector3(0.5f, 0.03f, 0.03f), Vector3.zero, Vector3.zero, new Color(0.15f, 0.15f, 0.15f));
        Handlebar = CreatePart("bike.handlebar", "Handlebar", PartCategory.Handlebar, PartCategory.Handlebar, handlebarPrefab, null, null,
            new[] { new FastenerSlot { id = "stem_bolt", type = FastenerType.Bolt, localPosition = Vector3.zero } });

        var seatPrefab = CreatePrimitivePrefab("Seat", PrimitiveType.Cube,
            new Vector3(0.22f, 0.06f, 0.12f), Vector3.zero, new Vector3(0f, 0.03f, 0f), new Color(0.1f, 0.08f, 0.06f));
        Seat = CreatePart("bike.seat", "Seat", PartCategory.Seat, PartCategory.Seat, seatPrefab, null, null,
            new[] { new FastenerSlot { id = "seat_clamp", type = FastenerType.Bolt, localPosition = Vector3.zero } });

        var brakesPrefab = CreatePrimitivePrefab("Brakes", PrimitiveType.Cube,
            new Vector3(0.05f, 0.1f, 0.03f), Vector3.zero, Vector3.zero, new Color(0.8f, 0.1f, 0.1f));
        Brakes = CreatePart("bike.brakes", "Brakes", PartCategory.Brake, PartCategory.Brake, brakesPrefab, null, null,
            new[]
            {
                new FastenerSlot { id = "caliper_bolt_1", type = FastenerType.Bolt, localPosition = new Vector3(0.02f, 0f, 0f) },
                new FastenerSlot { id = "caliper_bolt_2", type = FastenerType.Bolt, localPosition = new Vector3(-0.02f, 0f, 0f) },
            });

        var pedalsPrefab = CreatePrimitivePrefab("Pedals", PrimitiveType.Cube,
            new Vector3(0.3f, 0.02f, 0.08f), Vector3.zero, Vector3.zero, new Color(0.8f, 0.7f, 0.1f));
        Pedals = CreatePart("bike.pedals", "Pedals", PartCategory.Pedal, PartCategory.Pedal, pedalsPrefab, null, null,
            new[]
            {
                new FastenerSlot { id = "pedal_thread_1", type = FastenerType.Screw, localPosition = new Vector3(0.14f, 0f, 0f) },
                new FastenerSlot { id = "pedal_thread_2", type = FastenerType.Screw, localPosition = new Vector3(-0.14f, 0f, 0f) },
            });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("BikeBomAuthoring: done.");
    }

    private static PartDefinition CreatePart(string id, string displayName, PartCategory category, PartCategory installsOn,
        GameObject prefab, PartDefinition[] dependencies, SocketDefinition[] sockets, FastenerSlot[] fasteners)
    {
        string path = $"{DataDir}/{id}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<PartDefinition>(path);
        if (existing != null)
            return existing;

        var part = ScriptableObject.CreateInstance<PartDefinition>();
        part.id = id;
        part.displayName = displayName;
        part.category = category;
        part.installsOn = installsOn;
        part.prefab = prefab;
        part.dependencies = dependencies ?? new PartDefinition[0];
        part.sockets = sockets ?? new SocketDefinition[0];
        part.fasteners = fasteners ?? new FastenerSlot[0];

        AssetDatabase.CreateAsset(part, path);
        return part;
    }

    private static FrameMechanics CreateMechanics(string frameId, float topSpeed, float accel, float turnRate, float stability)
    {
        string path = $"{MechanicsDir}/{frameId}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<FrameMechanics>(path);
        if (existing != null)
            return existing;

        var mechanics = ScriptableObject.CreateInstance<FrameMechanics>();
        mechanics.frameId = frameId;
        mechanics.topSpeed = topSpeed;
        mechanics.acceleration = accel;
        mechanics.turnRate = turnRate;
        mechanics.stability = stability;

        AssetDatabase.CreateAsset(mechanics, path);
        return mechanics;
    }

    private static GameObject CreatePrimitivePrefab(string name, PrimitiveType type, Vector3 localScale,
        Vector3 localEulerAngles, Vector3 meshOffset, Color color)
    {
        string path = $"{PrefabDir}/{name}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing;

        var root = new GameObject(name);
        var visual = GameObject.CreatePrimitive(type);
        visual.name = "Visual";
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = meshOffset;
        visual.transform.localEulerAngles = localEulerAngles;
        visual.transform.localScale = localScale;
        visual.GetComponent<Renderer>().sharedMaterial = GetOrCreateMaterial(name + "Mat", color);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

    private static Material GetOrCreateMaterial(string name, Color color)
    {
        if (MaterialCache.TryGetValue(name, out var cached))
            return cached;

        string path = $"{MaterialDir}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            SetBaseColor(existing, color); // re-apply: earlier runs used the wrong shader property
            MaterialCache[name] = existing;
            return existing;
        }

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var mat = new Material(shader);
        SetBaseColor(mat, color);
        AssetDatabase.CreateAsset(mat, path);
        MaterialCache[name] = mat;
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

    private static void EnsureFolder(string parent, string name)
    {
        string path = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, name);
    }
}
