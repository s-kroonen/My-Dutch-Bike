using System;
using MyDutchBike.Bike;
using MyDutchBike.Parts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Edit-mode behavioral check for the M1 slice: drives BikeAssembly's actual install/tighten
/// logic end to end (no scene clicking involved) and asserts the bike reports incomplete before,
/// and complete after. Doesn't require Play mode — MonoBehaviour.Awake() doesn't fire on scenes
/// opened in edit mode, so BikeAssembly.Initialize() is called explicitly instead.
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

        InstallAndSecure(assembly, BikeBomAuthoring.Fork, "frame.head");
        InstallAndSecure(assembly, BikeBomAuthoring.WheelRear, "frame.dropout");
        InstallAndSecure(assembly, BikeBomAuthoring.Crankset, "frame.bb");
        InstallAndSecure(assembly, BikeBomAuthoring.WheelFront, "fork.dropout");
        InstallAndSecure(assembly, BikeBomAuthoring.TireFront, "wheel_front.rim");
        InstallAndSecure(assembly, BikeBomAuthoring.TireRear, "wheel_rear.rim");
        InstallAndSecure(assembly, BikeBomAuthoring.Handlebar, "fork.stem");
        InstallAndSecure(assembly, BikeBomAuthoring.Seat, "frame.post");
        InstallAndSecure(assembly, BikeBomAuthoring.Brakes, "fork.brake");
        InstallAndSecure(assembly, BikeBomAuthoring.Pedals, "crankset.pedal");
        InstallAndSecure(assembly, BikeBomAuthoring.Chain, "crankset.chain");

        if (!assembly.IsFullyAssembledAndSecured())
            throw new Exception($"{standName}: expected COMPLETE after installing + securing every required part");

        Debug.Log($"M1SmokeTest: {scenePath} OK ({standName} assembles + secures correctly)");
    }

    private static void InstallAndSecure(BikeAssembly assembly, PartDefinition def, string socketId)
    {
        if (!assembly.TryInstallPart(def, socketId))
            throw new Exception($"Failed to install {def.id} on socket '{socketId}'");

        var state = assembly.State.Find(def.id);
        foreach (var fastener in state.fasteners)
            assembly.SetFastenerTightness(def.id, fastener.fastenerSlotId, 1f);
    }
}
