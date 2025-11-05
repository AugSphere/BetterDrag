using System.Collections.Generic;
using System.Reflection;
using Crest;
using HarmonyLib;
using UnityEngine;

namespace BetterDrag
{
    [HarmonyPatch]
    static class BoatProbesFixedUpdateDragPatch
    {
        static readonly MethodInfo m_AddForceAtPosition = AccessTools.Method(
            typeof(Rigidbody),
            nameof(Rigidbody.AddForceAtPosition),
            [typeof(Vector3), typeof(Vector3), typeof(ForceMode)]
        );

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
        static IEnumerable<CodeInstruction> DropOriginalLongitudinalDrag(
            IEnumerable<CodeInstruction> instructions
        )
        {
            var addForceCount = 0;
            var enumerator = instructions.GetEnumerator();

            enumerator.MoveNext();
            while (addForceCount < 2)
            {
                if (enumerator.Current.Calls(m_AddForceAtPosition))
                    addForceCount++;
                yield return enumerator.Current;
                enumerator.MoveNext();
            }

            while (addForceCount < 3)
            {
                if (enumerator.Current.Calls(m_AddForceAtPosition))
                    addForceCount++;
                enumerator.MoveNext();
            }

            while (true)
            {
                yield return enumerator.Current;
                if (!enumerator.MoveNext())
                    break;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateBuoyancy")]
        static bool DropOriginalBuoyancy() => false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
        static void AddCustomPhysics(
            Vector3 waterSurfaceVel,
            BoatProbes __instance,
            Rigidbody ____rb,
            float ____forceHeightOffset,
            Vector3 ___lastVel,
            Vector3[] ____queryResultDisps,
            Vector3[] ____queryPoints,
            float ____totalWeight
        )
        {
            Profiler.RestartClock();

            Vector3 bodyVelocity = __instance.dontUpdateVelocity ? ___lastVel : ____rb.velocity;
            Vector3 velocityVector = bodyVelocity - waterSurfaceVel;
            Vector3 dragPositionVector = ____rb.position + ____forceHeightOffset * Vector3.up;
            var forwardVector = ____rb.transform.forward;
            var forwardVelocity = Vector3.Dot(forwardVector, velocityVector);
            Profiler.Profile("velocity");

            var shipData = ShipData.GetShipData(__instance.gameObject);
            Profiler.Profile("GetShipData");

            PhysicsCalculation.UpdateBuoyancy(
                __instance,
                ____rb,
                shipData,
                ____queryResultDisps,
                ____queryPoints,
                ____totalWeight,
                out var displacement,
                out var draft
            );
            Profiler.Profile("UpdateBuoyancy");

            var signedDragForceMagnitude = PhysicsCalculation.GetDragForceMagnitude(
                __instance,
                ____rb,
                shipData,
                forwardVelocity,
                draft,
                displacement
            );
            Profiler.Profile("GetDragForceMagnitude");

            var dragForceVector = forwardVector * signedDragForceMagnitude;
            ____rb.AddForceAtPosition(dragForceVector, dragPositionVector, ForceMode.Force);
            Profiler.Profile("AddForceAtPosition");

            Profiler.LogDurations();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatProbes), "Start")]
        static void BoatProbesStart(BoatProbes __instance, Vector3 ____centerOfMass)
        {
            var boatData = ShipData.GetShipData(__instance.gameObject);
            boatData.SetCenterOfMassHeight(____centerOfMass.y);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatDamage), "Start")]
        static void BoatDamageStart(BoatDamage __instance, float ___baseBuoyancy)
        {
            __instance.waterDrag = 0f;
            var boatData = ShipData.GetShipData(__instance.gameObject);
            boatData.SetBaseBuoyancy(___baseBuoyancy);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WaveSplashZone), "Start")]
        static void WaveSplashZoneStart(WaveSplashZone __instance)
        {
            ShipData.CalculateOverflowOffset(__instance);
        }
    }
}
