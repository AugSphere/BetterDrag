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

            var (viscousDrag, waveMakingDrag) = CalculateForwardDragForce(
                forwardVelocity,
                displacement,
                wettedArea,
                shipPerformanceData
            );

            var dragForceMagnitude = -Mathf.Sign(forwardVelocity) * (viscousDrag + waveMakingDrag);
            var clampedForceMagnitude = forceFilter.ClampValue(dragForceMagnitude, rigidbody);

#if DEBUG
            Debug.LogCSVBuffered(
                [
                    ("forward velocity, m/s", forwardVelocity),
                    ("draft, m", draft),
                    ("displacement, m^3", displacement),
                    ("area, m^2", wettedArea),
                    ("drag V, N", viscousDrag),
                    ("drag WM, N", waveMakingDrag),
                    ("drag total, N", dragForceMagnitude),
                    ("drag clamped, N", clampedForceMagnitude),
                ]
            );

            Debug.FlushBuffer(Debug.Mode.Line);
            Debug.FinishUpdate();
#endif

            return dragForceMagnitude;
        }

        static (float, float) CalculateForwardDragForce(
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

            var viscousDrag =
                Plugin.globalViscousDragMultiplier!.Value
                * performanceData.ViscousDragMultiplier
                * performanceData.CalculateViscousDragForce(
                    absVelocity,
                    lengthAtWaterline,
                    formFactor,
                    displacement,
                    wettedArea
                );
            var waveMakingDrag =
                Plugin.globalWaveMakingDragMultiplier!.Value
                * performanceData.WaveMakingDragMultiplier
                * performanceData.CalculateWaveMakingDragForce(
                    absVelocity,
                    lengthAtWaterline,
                    formFactor,
                    displacement,
                    wettedArea
                );

            return (viscousDrag, waveMakingDrag);
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
