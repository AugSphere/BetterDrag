using BetterDrag.Physics;
using BetterDrag.ShipConfiguration;
using BetterDrag.Utilities;
using Crest;
using HarmonyLib;
using UnityEngine;

namespace BetterDrag;

[HarmonyPatch]
internal static class BoatProbesFixedUpdateDragPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
    internal static bool IsUnpatchedDragUsed(BoatProbes __instance)
    {
        return !ShipData.GetShipData(__instance.gameObject).ModEnableCheck.IsModEnabled();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BoatProbes), "FixedUpdateBuoyancy")]
    internal static bool IsUnpatchedBuoyancyUsed(BoatProbes __instance)
    {
        return !ShipData.GetShipData(__instance.gameObject).ModEnableCheck.IsModEnabled();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
    internal static void AddCustomPhysics(
        BoatProbes __instance,
        Rigidbody ____rb,
        Vector3[] ____queryPoints,
        Vector3[] ____queryResultDisps,
        Vector3[] ____queryResultVels,
        float ____totalWeight
    )
    {
        Profiler.RestartClock();

        var shipData = ShipData.GetShipData(__instance.gameObject);
        Profiler.Profile("GetShipData");

        var (bodyVelocities, queryVelocities, queryDisplacements) =
            shipData.InputFilter.GetLastValidInputs(
                __instance,
                ____queryPoints,
                ____queryResultDisps,
                ____queryResultVels
            );

        PhysicsCalculation.UpdateForces(
            __instance,
            ____rb,
            shipData,
            ____queryPoints,
            queryDisplacements,
            queryVelocities,
            bodyVelocities,
            ____totalWeight
        );
        Profiler.Profile("UpdateForces");
        Profiler.LogDurations();

#if DEBUG
        BetterDragDebug.FlushBuffer(BetterDragDebug.Mode.Line);
        BetterDragDebug.FinishUpdate();
#endif
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BoatMass), nameof(BoatMass.UpdateMass))]
    internal static void UpdateMass(Rigidbody ___body, float ___selfMass, float ___partsMass)
    {
        var shipData = ShipData.GetShipData(___body.gameObject);
        if (!shipData.ModEnableCheck.IsModEnabled())
        {
            return;
        }

        ___body.mass +=
            (___selfMass + ___partsMass)
            * ((Plugin.GlobalMassMultiplier!.Value * shipData.DragData.MassMultiplier) - 1f);
        shipData.SetUnloadedMass(___body.mass);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BoatProbes), "Start")]
    internal static void BoatProbesStart(BoatProbes __instance, Vector3 ____centerOfMass)
    {
        var shipData = ShipData.GetShipData(__instance.gameObject);
        shipData.SetCenterOfMass(____centerOfMass);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BoatDamage), "Start")]
    internal static void BoatDamageStart(BoatDamage __instance, float ___baseBuoyancy)
    {
        __instance.waterDrag = 0f;
        var shipData = ShipData.GetShipData(__instance.gameObject);
        shipData.SetBaseBuoyancy(___baseBuoyancy);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(WaveSplashZone), "Start")]
    internal static void WaveSplashZoneStart(WaveSplashZone __instance)
    {
        var rigidbody = __instance.GetComponentInParent<Rigidbody>();
        var shipData = ShipData.GetShipData(rigidbody.gameObject);
        shipData.CalculateOverflowOffset(__instance);
    }
}
