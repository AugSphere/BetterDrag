using System;
using Crest;
using HarmonyLib;
using UnityEngine;

namespace BetterDrag
{
    [HarmonyPatch]
    static class BoatProbesFixedUpdateDragPatch
    {
        static readonly string[] disableForShipList = ["BOAT CUTTER (212)"];

        static bool IsModDisabled(Rigidbody rigidBody)
        {
            var normalizedName = Utilities.GetNormalizedShipName(rigidBody.gameObject);
            foreach (var disableForShip in disableForShipList)
                if (string.Equals(disableForShip, normalizedName, StringComparison.Ordinal))
                    return true;
            return GameState.sleeping && !Plugin.enableDuringSleep!.Value;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
        static bool IsUnpatchedDragUsed(Rigidbody ____rb) => IsModDisabled(____rb);

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateBuoyancy")]
        static bool IsUnpatchedBuoyancyUsed(Rigidbody ____rb) => IsModDisabled(____rb);

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
            if (IsModDisabled(____rb))
                return;

            Profiler.RestartClock();

            var shipData = ShipData.GetShipData(__instance.gameObject);
            Profiler.Profile("GetShipData");

            var (bodyVelocities, queryVelocities, queryDisplacements) =
                shipData.inputFilter.GetLastValidInputs(
                    __instance.dontUpdateVelocity,
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
