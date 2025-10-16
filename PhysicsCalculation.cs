using Crest;
using UnityEngine;

namespace BetterDrag
{
    internal static class PhysicsCalculation
    {
        static readonly OutlierFilter forceFilter = new(
            "Force filter",
            rateLimit: 1.2f,
            noFilterCutoff: 5f
        );

        static readonly OutlierFilter velocityFilter = new(
            "Velocity filter",
            rateLimit: 1.1f,
            noFilterCutoff: 0.1f
        );

        public static float GetDragForceMagnitude(
            BoatProbes boatProbes,
            Rigidbody rigidbody,
            float forwardVelocity
        )
        {
            var shipPerformanceData = GetShipData(boatProbes);
            var displacement = GetDisplacement(boatProbes);
            var draft = GetDraft(boatProbes, rigidbody);
            var wettedArea =
                1.7f * shipPerformanceData.LengthAtWaterline * draft + displacement / draft;
            forwardVelocity = velocityFilter.ClampValue(forwardVelocity, rigidbody);

            var dragForceMagnitude =
                -Mathf.Sign(forwardVelocity)
                * CalculateForwardDragForce(
                    forwardVelocity,
                    displacement,
                    wettedArea,
                    shipPerformanceData
                );

            var clampedForceMagnitude = forceFilter.ClampValue(dragForceMagnitude, rigidbody);
#if DEBUG
            if (!Debug.executedOnce)
            {
                Debug.ClearDragModelBuffer();
                var (testVelocity, testDisplacement, testWettedArea) = (8.5f, 38, 190);
                Debug.LogBuffered(
                    $"Calling drag function with forwardVelocity:{10}, displacement: {testDisplacement}m^3, wetted area: {testWettedArea}m^2"
                );
                CalculateForwardDragForce(
                    testVelocity,
                    testDisplacement,
                    testWettedArea,
                    shipPerformanceData
                );
                Debug.FLushBuffer(withDragModel: true);
                Debug.executedOnce = true;
            }

            var logPhysics = Debug.IsAtPeriod || Mathf.Abs(dragForceMagnitude) > 100000;
            if (logPhysics)
                Debug.LogBuffered(
                    [
                        $"\n{shipPerformanceData}",
                        $"Draft: {draft}m",
                        $"Displacement: {displacement}m^3",
                        $"Wetted area: {wettedArea}m^2",
                        $"Modified drag force: {dragForceMagnitude}N",
                        $"Clamped drag force: {clampedForceMagnitude}N",
                        $"Forward velocity: {forwardVelocity}m/s",
                    ]
                );

            Debug.FLushBuffer(withDragModel: logPhysics);
            Debug.IncrementCounter();
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

        static FinalShipDragPerformanceData GetShipData(BoatProbes boatProbes)
        {
            GameObject ship = boatProbes.gameObject;
            return cache.GetValue(ship);
        }

        static readonly SampleHeightHelper sampleHeightHelper = new();
        static uint draftSampleCounter = 0;
        static float lastDraft = 1.0f;
        static readonly float draftSmoothing = 1f / 16f;

        static float GetDraft(BoatProbes boatProbes, Rigidbody rigidbody)
        {
            draftSampleCounter++;
            if (draftSampleCounter % Plugin.draftSamplingPeriod!.Value != 0)
                return lastDraft;

            return SampleDraft(boatProbes, rigidbody);
        }

        static float SampleDraft(BoatProbes boatProbes, Rigidbody rigidbody)
        {
            var downPoint = rigidbody.ClosestPointOnBounds(
                rigidbody.centerOfMass + 100 * Vector3.down
            );
            sampleHeightHelper.Init(
                downPoint,
                boatProbes.ObjectWidth,
                allowMultipleCallsPerFrame: false
            );
            sampleHeightHelper.Sample(out float o_height);
            var clampedDraft = Mathf.Clamp(o_height - downPoint.y, 0.1f, 20f);
            lastDraft = (1 - draftSmoothing) * lastDraft + draftSmoothing * clampedDraft;
            return lastDraft;
        }

        static float GetDisplacement(BoatProbes boatProbes)
        {
            float displacement = 0.0f;
            for (int idx = 0; idx < boatProbes.appliedBuoyancyForces.Length; idx++)
                displacement += boatProbes.appliedBuoyancyForces[idx];

            return Mathf.Clamp(displacement * 1e-4f, 0.1f, float.MaxValue);
        }
    }
}
