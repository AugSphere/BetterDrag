using Crest;
using HarmonyLib;
using UnityEngine;

namespace BetterDrag
{
    [HarmonyPatch]
    static class BoatProbesFixedUpdateDragPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
        static bool IsUnpatchedDragUsed() => false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateBuoyancy")]
        static bool IsUnpatchedBuoyancyUsed() => false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
        static void AddCustomPhysics(
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

            var areVelocitiesValid =
                !__instance.dontUpdateVelocity
                && !shipData.bodyVelocityFilter.IsOutlier(____rb.velocity.magnitude)
                && !shipData.waterVelocityFilter.IsAnyMagnitudeOutlier(____queryResultVels);

            PhysicsCalculation.UpdateForces(
                __instance,
                ____rb,
                shipData,
                ____queryPoints,
                ____queryResultDisps,
                areVelocitiesValid ? ____queryResultVels : shipData.lastValidWaterVelocities,
                ____totalWeight
            );
            Profiler.Profile("UpdateForces");

            if (areVelocitiesValid)
                shipData.lastValidWaterVelocities = (Vector3[])____queryResultVels.Clone();

            Profiler.LogDurations();

#if DEBUG
            BetterDragDebug.FlushBuffer(BetterDragDebug.Mode.CSV);
            BetterDragDebug.FinishUpdate();
#endif
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatMass), nameof(BoatMass.UpdateMass))]
        static void UpdateMass(Rigidbody ___body, float ___selfMass, float ___partsMass)
        {
            var shipData = ShipData.GetShipData(___body.gameObject);
            ___body.mass +=
                (___selfMass + ___partsMass)
                * (Plugin.globalMassMultiplier!.Value * shipData.dragData.MassMultiplier - 1f);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatProbes), "Start")]
        static void BoatProbesStart(BoatProbes __instance, Vector3 ____centerOfMass)
        {
            var shipData = ShipData.GetShipData(__instance.gameObject);
            shipData.SetCenterOfMass(____centerOfMass);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatDamage), "Start")]
        static void BoatDamageStart(BoatDamage __instance, float ___baseBuoyancy)
        {
            __instance.waterDrag = 0f;
            var shipData = ShipData.GetShipData(__instance.gameObject);
            shipData.SetBaseBuoyancy(___baseBuoyancy);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaveSplashZone), "Start")]
        static void WaveSplashZoneStart(WaveSplashZone __instance)
        {
            var rigidbody = __instance.GetComponentInParent<Rigidbody>();
            var shipData = ShipData.GetShipData(rigidbody.gameObject);
            shipData.CalculateOverflowOffset(__instance);
        }
    }
}
