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
            float forwardVelocity,
            float baseBuoyancy
        )
        {
            var shipPerformanceData = GetShipDragPerformanceData(boatProbes);
            var displacement = GetDisplacement(boatProbes, baseBuoyancy);
            var draft = DraftSampler.GetAverageDraft(boatProbes, rigidbody);
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
                var (testVelocity, testDisplacement, testWettedArea) = (9f, 38, 250);
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
            var lengthAtWaterline =
                Plugin.globalShipLengthMultiplier!.Value * performanceData.LengthAtWaterline;
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

        static readonly Cache<FinalShipDragPerformanceData> shipDragPerformanceCache = new(
            "Ship performance",
            (ship) => ShipDragDataStore.GetPerformanceData(ship)
        );

        static FinalShipDragPerformanceData GetShipDragPerformanceData(BoatProbes boatProbes)
        {
            GameObject ship = boatProbes.gameObject;
            return shipDragPerformanceCache.GetValue(ship);
        }

        static float GetDisplacement(BoatProbes boatProbes, float baseBuoyancy)
        {
            float displacement = 0.0f;
            for (int idx = 0; idx < boatProbes.appliedBuoyancyForces.Length; idx++)
                displacement += boatProbes.appliedBuoyancyForces[idx];

            var unmodifiedForce = displacement * 1e-4f / boatProbes._forceMultiplier * baseBuoyancy;
            return Mathf.Clamp(unmodifiedForce, 0.1f, float.MaxValue);
        }
    }
}
