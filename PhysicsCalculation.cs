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
            Rigidbody rigidbody,
            ShipData shipData,
            float forwardVelocity,
            float displacement,
            float wettedArea
        )
        {
            var clampedVelocity = velocityFilter.ClampValue(forwardVelocity, rigidbody);

            var (viscousDrag, waveMakingDrag) = CalculateForwardDragForce(
                clampedVelocity,
                displacement,
                wettedArea,
                shipData.dragData
            );

            var dragForceMagnitude = -Mathf.Sign(clampedVelocity) * (viscousDrag + waveMakingDrag);
            var clampedForceMagnitude = forceFilter.ClampValue(dragForceMagnitude, rigidbody);

#if DEBUG
            BetterDragDebug.LogCSVBuffered(
                [
                    ("clamped velocity, m/s", clampedVelocity),
                    ("drag V, N", viscousDrag),
                    ("drag WM, N", waveMakingDrag),
                    ("drag total, N", dragForceMagnitude),
                    ("drag clamped, N", clampedForceMagnitude),
                ]
            );
#endif

            return clampedForceMagnitude;
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
            out float totalDisplacement,
            out float wettedArea
        )
        {
            float weight = 1000f * Mathf.Abs(Physics.gravity.y);
            totalDisplacement = 0.0f;
            wettedArea = 0.0f;
#if DEBUG
            float averageDraft = 0.0f;
            var totalFbDispalcement = 0.0f;
            var averageFbArea = 0.0f;
#endif

            var (baseBuoyancy, overflowOffset, draftOffset, keelDepth, _) = shipData.GetValues(
                boatProbes,
                rigidbody
            );
            var lengthAtWaterline = shipData.dragData.LengthAtWaterline;
            var displacementCorrection = Mathf.Clamp(25f - 0.8f * lengthAtWaterline, 5f, 25f);

            float seaLevel = OceanRenderer.Instance.SeaLevel;

            for (int idx = 0; idx < boatProbes._forcePoints.Length; ++idx)
            {
                float waterHeightSample = seaLevel + queryResultDisps[idx].y - queryPoints[idx].y;
                float draft = Mathf.Clamp(waterHeightSample + draftOffset, 0.001f, 30f);

                var fallbackDisplacement =
                    draft
                    * baseBuoyancy
                    * boatProbes._forcePoints[idx]._weight
                    * displacementCorrection;
                var fallbackWettedArea = 4f * lengthAtWaterline * draft;
                var (area, displacement) =
                    shipData.GetHydrostaticValues(draft)
                    ?? (fallbackWettedArea, fallbackDisplacement);
                displacement /= totalWeight;
                totalDisplacement += displacement;
                wettedArea += area / totalWeight;
#if DEBUG
                averageDraft += draft / totalWeight;
                totalFbDispalcement += fallbackDisplacement / totalWeight;
                averageFbArea += fallbackWettedArea / totalWeight;
#endif

                float force =
                    weight
                    * displacement
                    * boatProbes._forceMultiplier
                    / baseBuoyancy
                    / displacementCorrection
                    * Plugin.globalBuoyancyMultiplier!.Value;
                rigidbody.AddForceAtPosition(Vector3.up * force, queryPoints[idx]);
                boatProbes.appliedBuoyancyForces[idx] = force;
            }

#if DEBUG
            BetterDragDebug.LogCSVBuffered(
                [
                    ("draft, m", averageDraft),
                    ("displacement, m^3", totalDisplacement),
                    ("displacementFb, m^3", totalFbDispalcement),
                    ("area, m^2", wettedArea),
                    ("areaFb, m^2", averageFbArea),
                ]
            );
#endif
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
