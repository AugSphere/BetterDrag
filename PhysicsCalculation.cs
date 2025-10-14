using Crest;
using UnityEngine;
#if DEBUG
using HarmonyLib;
#endif

namespace BetterDrag
{
    internal static class PhysicsCalculation
    {
        static readonly OutlierFilter forceFilter = new("Force filter", 1f);

        public static float GetDragForceMagnitude(
            BoatProbes instance,
            Rigidbody rb,
            float forwardVelocity
        )
        {
            var shipPerformanceData = GetShipData(instance);
            var displacement = GetDisplacement(instance);
            var draft = GetDraft(instance, rb);
            var wettedArea =
                1.7f * shipPerformanceData.LengthAtWaterline * draft + displacement / draft;

            var dragForceMagnitude =
                -Mathf.Sign(forwardVelocity)
                * CalculateForwardDragForce(
                    forwardVelocity,
                    displacement,
                    wettedArea,
                    shipPerformanceData
                );

            var clampedForceMagnitude = forceFilter.ClampValue(dragForceMagnitude, rb);
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

            if (DebugCounter.IsAtPeriod() || Mathf.Abs(dragForceMagnitude) > 100000f)
            {
                FileLog.Log($"{shipPerformanceData}");
                FileLog.Log($"Draft: {draft}m");
                FileLog.Log($"Displacement: {displacement}m^3");
                FileLog.Log($"Wetted area: {wettedArea}m^2");
                FileLog.Log($"Modified drag force: {dragForceMagnitude}N");
                FileLog.Log($"Clamped drag force: {clampedForceMagnitude}N");
                FileLog.Log($"Forward velocity: {forwardVelocity}m/s\n");
            }
            DebugCounter.Increment();
#endif
            return clampedForceMagnitude;
        }

        static float CalculateForwardDragForce(
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

        static readonly Cache<FinalShipDragPerformanceData> cache = new(
            "Ship performance",
            (ship) => ShipDragDataStore.GetPerformanceData(ship)
        );

        static FinalShipDragPerformanceData GetShipData(BoatProbes instance)
        {
            GameObject ship = instance.gameObject;
            return cache.GetValue(ship);
        }

        static readonly SampleHeightHelper sampleHeightHelper = new();
        static uint draftSampleCounter = 0;
        static float lastDraft = 1.0f;

        static float GetDraft(BoatProbes instance, Rigidbody rb)
        {
            draftSampleCounter++;
            if (draftSampleCounter % Plugin.draftSamplingPeriod!.Value != 0)
                return lastDraft;

            return SampleDraft(instance, rb);
        }

        static float SampleDraft(BoatProbes instance, Rigidbody rb)
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

        static float GetDisplacement(BoatProbes instance)
        {
            float displacement = 0.0f;
            for (int idx = 0; idx < instance.appliedBuoyancyForces.Length; idx++)
                displacement += instance.appliedBuoyancyForces[idx];

            return Mathf.Clamp(displacement * 1e-4f, 0.1f, float.MaxValue);
        }
    }
}
