using System;
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
            ShipData shipData,
            float forwardVelocity,
            float draft,
            float displacement
        )
        {
            var wettedArea =
                1.7f * shipData.dragData.LengthAtWaterline * draft + displacement / draft;
            var (areaHT, displacementHT) = shipData.GetHydrostaticValues(draft) ?? (0f, 0f);
            forwardVelocity = velocityFilter.ClampValue(forwardVelocity, rigidbody);

            var (viscousDrag, waveMakingDrag) = CalculateForwardDragForce(
                forwardVelocity,
                displacement,
                wettedArea,
                shipData.dragData
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
                    ("displacementHT, m^3", displacementHT),
                    ("areaHT, m^2", areaHT),
                    ("drag V, N", viscousDrag),
                    ("drag WM, N", waveMakingDrag),
                    ("drag total, N", dragForceMagnitude),
                    ("drag clamped, N", clampedForceMagnitude),
                ]
            );

            shipData.DrawAll(rigidbody.transform, drawHullPoints: true, drawSidePoints: true);
            BetterDragDebug.FlushBuffer(BetterDragDebug.Mode.Line);
            BetterDragDebug.FinishUpdate();
#endif

            return dragForceMagnitude;
        }

        static (float, float) CalculateForwardDragForce(
            float forwardVelocity,
            float displacement,
            float wettedArea,
            ShipDragPerformanceData performanceData
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

        public static void UpdateBuoyancy(
            BoatProbes boatProbes,
            Rigidbody rigidbody,
            ShipData shipData,
            Vector3[] queryResultDisps,
            Vector3[] queryPoints,
            float totalWeight,
            out float displacement,
            out float averageDraft
        )
        {
            float weight = 1000f * Mathf.Abs(Physics.gravity.y);
            averageDraft = 0.0f;
            displacement = 0.0f;

            var (baseBuoyancy, overflowOffset, draftOffset, keelDepth, _) = shipData.GetValues(
                boatProbes,
                rigidbody
            );

            float seaLevel = OceanRenderer.Instance.SeaLevel;
            float originalSpan = overflowOffset - boatProbes._forcePoints[0]._offsetPosition.y;
            float fullSpan = 0.8f * (overflowOffset + keelDepth);
            float spanRatio = originalSpan / fullSpan;

            for (int idx = 0; idx < boatProbes._forcePoints.Length; idx++)
            {
                float waterHeightSample = seaLevel + queryResultDisps[idx].y - queryPoints[idx].y;
                float draft = Mathf.Clamp(waterHeightSample + draftOffset, 0.001f, 20f);
                float displacementScale = CalculateDisplacementScale(
                    draft,
                    fullSpan,
                    shipData.dragData
                );
                averageDraft += draft;
                float scaledDraft =
                    draft
                    * spanRatio
                    * displacementScale
                    * boatProbes._forcePoints[idx]._weight
                    / totalWeight;
                displacement += scaledDraft * baseBuoyancy;
                float force = weight * boatProbes._forceMultiplier * scaledDraft;
                rigidbody.AddForceAtPosition(Vector3.up * force, queryPoints[idx]);
                boatProbes.appliedBuoyancyForces[idx] = force;
            }
            averageDraft /= totalWeight;
        }

        private static float CalculateDisplacementScale(
            float draft,
            float fullSpan,
            ShipDragPerformanceData shipDragPerformanceData
        )
        {
            var relativeToOverflow = draft / fullSpan;
            if (relativeToOverflow > 1)
                return 1f;
            var factor = Mathf.Clamp(2.5f - 4.8f * shipDragPerformanceData.FormFactor, 1f, 3f);
            return Mathf.Pow(relativeToOverflow, factor);
        }
    }
}
