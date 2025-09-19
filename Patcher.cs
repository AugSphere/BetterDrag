using System.Collections.Generic;
using System.Linq;
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

        static readonly SampleHeightHelper sampleHeightHelper = new();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
        static void AddCustomLongitudinalDrag(
            Vector3 waterSurfaceVel,
            ref BoatProbes __instance,
            ref Rigidbody ____rb,
            float ____forceHeightOffset
        )
        {
            var transform = ComponentBaseTransform(__instance);
            var shipPerformanceData = ShipDragDataStore.GetPerformanceData(__instance.gameObject);

            Vector3 velocityVector = ____rb.velocity - waterSurfaceVel;
            Vector3 dragPositionVector = ____rb.position + ____forceHeightOffset * Vector3.up;
            var forwardVector = transform.forward;
            var forwardVelocity = Vector3.Dot(forwardVector, velocityVector);

            var displacement = Mathf.Clamp(
                __instance.appliedBuoyancyForces.Sum() * 1e-4f,
                0.1f,
                float.MaxValue
            );
            var draft = GetDraft(ref __instance, ref ____rb);
            var wettedArea =
                1.7f * shipPerformanceData.LengthAtWaterline * draft + displacement / draft;

            var dragForceMagnitude = CalculateForwardDragForce(
                forwardVelocity,
                displacement,
                wettedArea,
                shipPerformanceData
            );

            ____rb.AddForceAtPosition(
                -forwardVector * Mathf.Sign(forwardVelocity) * dragForceMagnitude,
                dragPositionVector,
                ForceMode.Force
            );

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
                FileLog.Log($"Draft: {draft}m");
                FileLog.Log($"Displacement: {displacement}m^3");
                FileLog.Log($"Wetted area: {wettedArea}m^2");
                FileLog.Log($"Final modified drag force: {dragForceMagnitude}N");
                FileLog.Log($"Forward velocity: {forwardVelocity}m/s\n");
            }
            DebugCounter.Increment();
#endif
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
            var totalDragForce =
                Plugin.globalViscousDragMultiplier!.Value
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
            return totalDragForce;
        }

        private static float GetDraft(ref BoatProbes instance, ref Rigidbody rb)
        {
            var downPoint = rb.ClosestPointOnBounds(rb.centerOfMass + 100 * Vector3.down);
            sampleHeightHelper.Init(
                downPoint,
                instance.ObjectWidth,
                allowMultipleCallsPerFrame: true
            );
            sampleHeightHelper.Sample(out float o_height);
            return Mathf.Clamp(o_height - downPoint.y, 0.1f, float.MaxValue);
        }
    }
}
