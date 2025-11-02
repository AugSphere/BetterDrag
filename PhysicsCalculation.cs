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
            var shipPerformanceData = GetShipDragPerformanceData(boatProbes);
            var miscShipData = MiscShipData.GetMiscShipData(boatProbes.gameObject);
            var (displacement, draft) = GetDisplacementAndDraft(boatProbes, miscShipData);
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
            BetterDragDebug.LogCSVBuffered(
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

            miscShipData.keelRenderer.DrawSphere(
                rigidbody.transform.TransformPoint(miscShipData.keelPointPosition)
            );
            miscShipData.overflowRenderer.DrawSphere(
                rigidbody.transform.TransformPoint(miscShipData.overflowOffset * Vector3.up)
            );
            BetterDragDebug.FlushBuffer(BetterDragDebug.Mode.Line);
            BetterDragDebug.FinishUpdate();
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

        static (float, float) GetDisplacementAndDraft(
            BoatProbes boatProbes,
            MiscShipData miscShipData
        )
        {
            float weight = 1000f * Mathf.Abs(Physics.gravity.y);
            float force = 0.0f;
            for (int idx = 0; idx < boatProbes.appliedBuoyancyForces.Length; idx++)
            {
                force += boatProbes.appliedBuoyancyForces[idx];
            }

            var draftSum = force / weight / boatProbes._forceMultiplier;
            var dispalcement = draftSum * miscShipData.baseBuoyancy;
            var draft = draftSum + miscShipData.draftOffset;
            var clampedDraft = Mathf.Clamp(draft, 0.01f, 20);
            var clampedDisplacement = Mathf.Clamp(dispalcement, 0.01f, float.MaxValue);
            return (clampedDisplacement, clampedDraft);
        }
    }
}
