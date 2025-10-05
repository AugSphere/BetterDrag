using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Crest;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Scripting;

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
            float ____forceHeightOffset
        )
        {
            Profiler.RestartClock();

            GarbageCollector.SetMode(GarbageCollector.Mode.Disabled);
            Profiler.Profile("GarbageCollector.SetMode(Disabled)");

            var transform = ComponentBaseTransform(__instance);
            Profiler.Profile("transform");

            var shipPerformanceData = ShipDragDataStore.GetPerformanceData(__instance.gameObject);
            Profiler.Profile("shipPerformanceData");

            Vector3 velocityVector = ____rb.velocity - waterSurfaceVel;
            Vector3 dragPositionVector = ____rb.position + ____forceHeightOffset * Vector3.up;
            var forwardVector = transform.forward;
            var forwardVelocity = Vector3.Dot(forwardVector, velocityVector);
            Profiler.Profile("forwardVelocity");

            var displacement = GetDisplacement(__instance);
            Profiler.Profile("GetDisplacement");

            var draft = GetDraft(__instance, ____rb);
            Profiler.Profile("draft");

            var wettedArea =
                1.7f * shipPerformanceData.LengthAtWaterline * draft + displacement / draft;
            Profiler.Profile("wettedArea");

            var dragForceMagnitude = CalculateForwardDragForce(
                forwardVelocity,
                displacement,
                wettedArea,
                shipPerformanceData
            );
            Profiler.Profile("dragForceMagnitude");

            var dragForceVector = -forwardVector * Mathf.Sign(forwardVelocity) * dragForceMagnitude;
            ____rb.AddForceAtPosition(dragForceVector, dragPositionVector, ForceMode.Force);
            Profiler.Profile("AddForceAtPosition");

            GarbageCollector.SetMode(GarbageCollector.Mode.Enabled);
            Profiler.Profile("GarbageCollector.SetMode(Enabled)");
#if DEBUG
            if (DebugCounter.IsAtFirstFrame())
            {
                var (testVelocity, testDisplacement, testWettedArea) = (8.5f, 38, 190);
                FileLog.Log(
                    $"\nCalling drag function with forwardVelocity:{10}, displacement: {testDisplacement}m^3, wetted area: {testWettedArea}m^2"
                );
                CalculateForwardDragForce(
                    testVelocity,
                    testDisplacement,
                    testWettedArea,
                    shipPerformanceData
                );
                FileLog.Log("\n");
            }

            if (DebugCounter.IsAtPeriod())
            {
                FileLog.Log($"{shipPerformanceData}");
                FileLog.Log($"Draft: {draft}m");
                FileLog.Log($"Displacement: {displacement}m^3");
                FileLog.Log($"Wetted area: {wettedArea}m^2");
                FileLog.Log($"Final modified drag force: {dragForceMagnitude}N");
                FileLog.Log($"Forward velocity: {forwardVelocity}m/s\n");
            }
            DebugCounter.Increment();
#endif
            Profiler.LogDurations();
        }

        private static float CalculateForwardDragForce(
            float forwardVelocity,
            float displacement,
            float wettedArea,
            FinalShipDragPerformanceData performanceData
        )
        {
            var absVelocity = Mathf.Abs(forwardVelocity);
            var lengthAtWaterline = performanceData.LengthAtWaterline;
            var formFactor = performanceData.FormFactor;

            return Plugin.globalViscousDragMultiplier!.Value
                    * performanceData.ViscousDragMultiplier
                    * performanceData.CalculateViscousDragForce(
                        absVelocity,
                        lengthAtWaterline,
                        formFactor,
                        displacement,
                        wettedArea
                    )
                + Plugin.globalWaveMakingDragMultiplier!.Value
                    * performanceData.WaveMakingDragMultiplier
                    * performanceData.CalculateWaveMakingDragForce(
                        absVelocity,
                        lengthAtWaterline,
                        formFactor,
                        displacement,
                        wettedArea
                    );
        }

        static readonly SampleHeightHelper sampleHeightHelper = new();
        static uint draftSampleCounter = 0;
        static float lastDraft = 1.0f;

        private static float GetDraft(BoatProbes instance, Rigidbody rb)
        {
            draftSampleCounter++;
            if (draftSampleCounter % Plugin.draftSamplingPeriod!.Value != 0)
                return lastDraft;

            return SampleDraft(instance, rb);
        }

        private static float GetDisplacement(BoatProbes instance)
        {
            float displacement = 0.0f;
            for (int idx = 0; idx < instance.appliedBuoyancyForces.Length; idx++)
                displacement += instance.appliedBuoyancyForces[idx];

            return Mathf.Clamp(displacement * 1e-4f, 0.1f, float.MaxValue);
        }

        private static float SampleDraft(BoatProbes instance, Rigidbody rb)
        {
            var downPoint = rb.ClosestPointOnBounds(rb.centerOfMass + 100 * Vector3.down);
            sampleHeightHelper.Init(
                downPoint,
                instance.ObjectWidth,
                allowMultipleCallsPerFrame: false
            );
            sampleHeightHelper.Sample(out float o_height);
            lastDraft = Mathf.Clamp(o_height - downPoint.y, 0.1f, float.MaxValue);
            return lastDraft;
        }
    }
}
