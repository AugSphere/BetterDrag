using Crest;
using UnityEngine;
#if DEBUG
using HarmonyLib;
#endif

namespace BetterDrag
{
    internal static class PhysicsCalculation
    {
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

            var dragForceMagnitude = CalculateForwardDragForce(
                forwardVelocity,
                displacement,
                wettedArea,
                shipPerformanceData
            );

            var clampedForceMagnitude = ClampForce(dragForceMagnitude);
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
                FileLog.Log($"Modified drag force: {dragForceMagnitude}N");
                FileLog.Log($"Clamped drag force: {clampedForceMagnitude}N");
                FileLog.Log($"Forward velocity: {forwardVelocity}m/s\n");
            }
            DebugCounter.Increment();
#endif
            return clampedForceMagnitude;
        }

        static readonly float defaultForce = 1.0f;
        static readonly uint forceSampleCount = 10;
        static readonly float[] forceSamples = new float[forceSampleCount];
        static uint forceSampleIdx = 0;

        static float ClampForce(float dragForceMagnitude)
        {
            if (forceSampleIdx < forceSampleCount)
            {
                forceSamples[forceSampleIdx++] = dragForceMagnitude;
                return defaultForce;
            }

            float average = 0,
                min = 0,
                max = 0;
            for (int idx = 0; idx < forceSampleCount; idx++)
            {
                var sample = forceSamples[idx];
                min = Mathf.Min(min, sample);
                max = Mathf.Max(max, sample);
                average += sample;
            }
            average /= forceSampleCount;
            var span = max - min;

            var bufferIdx = forceSampleIdx++ % forceSampleCount;

            if (Mathf.Abs(dragForceMagnitude - average) < span * 2f)
            {
                forceSamples[bufferIdx] = dragForceMagnitude;
                return dragForceMagnitude;
            }
            else
            {
#if DEBUG
                FileLog.Log(
                    $"Force of {dragForceMagnitude} inconsistent with samples {string.Join(", ", forceSamples)}"
                );
#endif
                forceSamples[bufferIdx] = 0.2f * dragForceMagnitude + 0.8f * average;
                return average;
            }
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

        static (GameObject ship, FinalShipDragPerformanceData data)? cachedShipData;

        static FinalShipDragPerformanceData GetShipData(BoatProbes instance)
        {
            FinalShipDragPerformanceData shipPerformanceData;
            GameObject ship = instance.gameObject;
            if (Object.ReferenceEquals(cachedShipData?.ship, ship))
            {
                shipPerformanceData = cachedShipData.Value.data;
            }
            else
            {
                shipPerformanceData = ShipDragDataStore.GetPerformanceData(ship);
                cachedShipData = (ship, shipPerformanceData);
#if DEBUG
                FileLog.Log($"Cache miss for {ship.name}");
#endif
            }
            return shipPerformanceData;
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
