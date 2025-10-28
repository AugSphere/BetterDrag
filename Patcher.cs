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

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Component), nameof(Component.transform), MethodType.Getter)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static Transform ComponentBaseTransform(BoatProbes _) => null!;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
        static void AddCustomLongitudinalDrag(
            Vector3 waterSurfaceVel,
            BoatProbes __instance,
            Rigidbody ____rb,
            float ____forceHeightOffset,
            Vector3 ___lastVel
        )
        {
            Profiler.RestartClock();

            var transform = ComponentBaseTransform(__instance);
            Profiler.Profile("transform");

            Vector3 bodyVelocity = __instance.dontUpdateVelocity ? ___lastVel : ____rb.velocity;
            Vector3 velocityVector = bodyVelocity - waterSurfaceVel;
            Vector3 dragPositionVector = ____rb.position + ____forceHeightOffset * Vector3.up;
            var forwardVector = transform.forward;
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

            var sailForce = sailForceCache.GetValue(____rb.gameObject);
            if (Debug.IsAtPeriod)
            {
                Debug.LogBuffered(
                    [
                        $"Sail force: {sailForce.force}N",
                        $"Wind force {sailForce.totalWindForce}",
                        $"Sail power: {sailForce.power}",
                    ]
                );
            }
            sailForce.force = 0;
            sailForce.totalWindForce = 0;
            sailForce.power = 0;

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

        class SailForce
        {
            public float force = 0f;
            public float totalWindForce = 0f;
            public float power = 0f;
        }

        static readonly Cache<SailForce> sailForceCache = new("Sail cache", (_) => new());

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Sail), "FixedUpdate")]
        static void AddSailPower(
            Sail __instance,
            float ___unamplifiedForwardForce,
            float ___totalWindForce
        )
        {
            var sailForce = sailForceCache.GetValue(__instance.shipRigidbody.gameObject);
            sailForce.force += GetForwardSailForce(
                __instance,
                ___unamplifiedForwardForce,
                out var power
            );
            sailForce.totalWindForce += ___totalWindForce;
            sailForce.power += power;
        }

        static float GetForwardSailForce(Sail sail, float unamplifiedForwardForce, out float power)
        {
            power = sail.GetRealSailPower();
            if (sail.category == SailCategory.junk)
            {
                power *= 0.75f;
            }
            if (sail.category == SailCategory.gaff)
            {
                power *= 0.85f;
            }
            return unamplifiedForwardForce * power * 50f;
        }
    }
}
