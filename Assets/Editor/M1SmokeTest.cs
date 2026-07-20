using System;
using MyDutchBike.Bike;
using MyDutchBike.Parts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Edit-mode behavioral check for the M1 slice: drives BikeAssembly's actual install/tighten/remove
/// logic end to end (no scene clicking involved). Asserts the bike is incomplete before, complete after
/// a proper layered build, that the layering gates hold (can't hang a child on an untight parent; can't
/// turn a parent's fasteners while a child is attached), and that the whole bike strips back off in
/// reverse leaving only the frame. Doesn't require Play mode — Awake() doesn't fire on scenes opened in
/// edit mode, so BikeAssembly.Initialize() is called explicitly.
/// </summary>
public static class M1SmokeTest
{
    [MenuItem("Tools/Project Bootstrap/Run M1 Smoke Test")]
    public static void Run()
    {
        try
        {
            BikeBomAuthoring.Run();
            GreyboxSceneAuthoring.Run();

            ValidateScene("Assets/Scenes/City.unity", "CityBikeStand");
            ValidateScene("Assets/Scenes/RaceTrack.unity", "RaceBikeStand");

            Debug.Log("M1SmokeTest: ALL PASS");
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogError($"M1SmokeTest: FAILED - {e.Message}\n{e.StackTrace}");
            EditorApplication.Exit(1);
        }
    }

    private static void ValidateScene(string scenePath, string standName)
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var standGo = GameObject.Find(standName);
        if (standGo == null)
            throw new Exception($"{standName} not found in {scenePath}");

        var assembly = standGo.GetComponent<BikeAssembly>();
        if (assembly == null)
            throw new Exception($"{standName} has no BikeAssembly");

        assembly.Initialize();

        if (assembly.IsFullyAssembledAndSecured())
            throw new Exception($"{standName}: expected INCOMPLETE before any parts are installed");

        // --- Attach gate: a child can't go on a parent whose fasteners aren't fully seated. ---
        if (!assembly.TryInstallPart(BikeBomAuthoring.Fork, "frame.head"))
            throw new Exception("could not install the fork on the frame");
        if (assembly.TryInstallPart(BikeBomAuthoring.WheelFront, "fork.dropout"))
            throw new Exception($"{standName}: attach gate failed — mounted a wheel on an un-tightened fork");
        Secure(assembly, BikeBomAuthoring.Fork.id);

        // --- Fastener lock: with a child attached, the parent's fasteners must be un-turnable. ---
        InstallAndSecure(assembly, BikeBomAuthoring.WheelFront, "fork.dropout");
        var forkState = assembly.State.Find(BikeBomAuthoring.Fork.id);
        string forkNut = forkState.fasteners[0].fastenerSlotId;
        float before = assembly.GetFastenerTightness(BikeBomAuthoring.Fork.id, forkNut);
        assembly.SetFastenerTightness(BikeBomAuthoring.Fork.id, forkNut, -0.5f);
        if (assembly.GetFastenerTightness(BikeBomAuthoring.Fork.id, forkNut) < before - 1e-4f)
            throw new Exception($"{standName}: fastener lock failed — loosened a fork nut while the wheel was attached");

        // --- Finish the layered build (each parent is secured before its children go on). ---
        InstallAndSecure(assembly, BikeBomAuthoring.WheelRear, "frame.dropout");
        InstallAndSecure(assembly, BikeBomAuthoring.Crankset, "frame.bb");
        InstallAndSecure(assembly, BikeBomAuthoring.TireFront, "wheel_front.rim");
        InstallAndSecure(assembly, BikeBomAuthoring.TireRear, "wheel_rear.rim");
        InstallAndSecure(assembly, BikeBomAuthoring.Handlebar, "fork.stem");
        InstallAndSecure(assembly, BikeBomAuthoring.Seat, "frame.post");
        InstallAndSecure(assembly, BikeBomAuthoring.Brakes, "fork.brake");
        InstallAndSecure(assembly, BikeBomAuthoring.Pedals, "crankset.pedal");
        InstallAndSecure(assembly, BikeBomAuthoring.Chain, "crankset.chain");

        if (!assembly.IsFullyAssembledAndSecured())
            throw new Exception($"{standName}: expected COMPLETE after the full layered build");

        // --- Reverse strip: everything comes back off (leaves first), leaving only the frame. ---
        string[] stripOrder =
        {
            "bike.chain", "bike.pedals", "bike.seat", "bike.brakes", "bike.handlebar",
            "bike.tire_front", "bike.tire_rear", "bike.wheel_front", "bike.wheel_rear",
            "bike.crankset", "bike.fork",
        };
        foreach (var id in stripOrder)
            LoosenAndRemove(assembly, id, standName);

        int remaining = assembly.State.parts.Count;
        if (remaining != 1)
            throw new Exception($"{standName}: expected only the frame left after stripping, but {remaining} parts remain");

        Debug.Log($"M1SmokeTest: {scenePath} OK ({standName} builds, gates hold, and strips back to the frame)");
    }

    private static void InstallAndSecure(BikeAssembly assembly, PartDefinition def, string socketId)
    {
        if (!assembly.TryInstallPart(def, socketId))
            throw new Exception($"Failed to install {def.id} on socket '{socketId}'");
        Secure(assembly, def.id);
    }

    /// <summary>Fully tightens every fastener on an already-installed part (routing points included, in
    /// order — chain_front before chain_rear).</summary>
    private static void Secure(BikeAssembly assembly, string defId)
    {
        var state = assembly.State.Find(defId);
        foreach (var fastener in state.fasteners)
            assembly.SetFastenerTightness(defId, fastener.fastenerSlotId, 1f);
    }

    /// <summary>Loosens every fastener (in reverse, so ordered routing points come undone last-first) then
    /// removes the part, asserting it actually detaches.</summary>
    private static void LoosenAndRemove(BikeAssembly assembly, string defId, string standName)
    {
        var state = assembly.State.Find(defId);
        if (state == null)
            throw new Exception($"{standName}: {defId} was not installed to strip");
        for (int i = state.fasteners.Count - 1; i >= 0; i--)
            assembly.SetFastenerTightness(defId, state.fasteners[i].fastenerSlotId, -1f);

        if (!assembly.CanRemove(defId, out string why))
            throw new Exception($"{standName}: expected {defId} removable after loosening, but: {why}");
        if (assembly.TryRemovePart(defId) == null)
            throw new Exception($"{standName}: TryRemovePart({defId}) returned null (didn't detach)");
    }
}
