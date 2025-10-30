using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
        static void AddCustomLongitudinalDrag(
            Vector3 waterSurfaceVel,
            BoatProbes __instance,
            Rigidbody ____rb,
            float ____forceHeightOffset,
            Vector3 ___lastVel,
            Vector3[] ____queryPoints
        )
        {
            Profiler.RestartClock();

            Vector3 bodyVelocity = __instance.dontUpdateVelocity ? ___lastVel : ____rb.velocity;
            Vector3 velocityVector = bodyVelocity - waterSurfaceVel;
            Vector3 dragPositionVector = ____rb.position + ____forceHeightOffset * Vector3.up;
            var forwardVector = ____rb.transform.forward;
            var forwardVelocity = Vector3.Dot(forwardVector, velocityVector);
            Profiler.Profile("velocity");

            var baseBuoyancy = baseBuoyancyCache.GetValue(__instance.gameObject).Item1;
            Profiler.Profile("baseBuoyancy");

            var signedDragForceMagnitude = PhysicsCalculation.GetDragForceMagnitude(
                __instance,
                ____rb,
                forwardVelocity,
                baseBuoyancy
            );
            Profiler.Profile("GetDragForceMagnitude");

            var dragForceVector = forwardVector * signedDragForceMagnitude;
            ____rb.AddForceAtPosition(dragForceVector, dragPositionVector, ForceMode.Force);
            Profiler.Profile("AddForceAtPosition");

            Profiler.LogDurations();
        }

        static readonly Cache<System.Tuple<float>> baseBuoyancyCache = new(
            "Base buoyancy",
            (_) => new(25f)
        );

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatDamage), "Start")]
        static void BoatDamageStart(BoatDamage __instance, float ___baseBuoyancy)
        {
            __instance.waterDrag = 0f;
            baseBuoyancyCache.SetValue(__instance.gameObject, new(___baseBuoyancy));
        }
    }
}
