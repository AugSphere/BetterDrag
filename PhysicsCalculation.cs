using Crest;
using UnityEngine;

namespace BetterDrag
{
    internal static class PhysicsCalculation
    {
        static readonly float waterWeight = 1000f * Mathf.Abs(Physics.gravity.y);

        static float CalculateDragForce(
            float velocity,
            float displacement,
            float wettedArea,
            ShipData shipData,
            bool isLongitudinal
        )
        {
            var absVelocity = Mathf.Abs(velocity);
            var performanceData = shipData.dragData;
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

            if (!isLongitudinal)
                return viscousDrag * Plugin.globalOffAxisDragMultiplier!.Value;

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

            return viscousDrag + waveMakingDrag;
        }

        public static void UpdateForces(
            BoatProbes boatProbes,
            Rigidbody rigidBody,
            ShipData shipData,
            Vector3[] queryPoints,
            Vector3[] queryDisplacements,
            Vector3[] queryVelocities,
            float totalWeight
        )
        {
            var totalDisplacement = 0.0f;
            var wettedArea = 0.0f;
#if DEBUG
            float averageDraft = 0.0f;
            var totalFbDisplacement = 0.0f;
            var averageFbArea = 0.0f;
#endif

            var shipDataValues = shipData.GetValues(boatProbes);
            var lengthAtWaterline = shipData.dragData.LengthAtWaterline;
            var buoyancyMultiplier = shipData.dragData.BuoyancyMultiplier;

            float seaLevel = OceanRenderer.Instance.SeaLevel;
            Vector3 bodyForward = rigidBody.transform.forward;

            for (int idx = 0; idx < boatProbes._forcePoints.Length; ++idx)
            {
                float waterHeightSample = seaLevel + queryDisplacements[idx].y - queryPoints[idx].y;
                float draft = Mathf.Clamp(
                    waterHeightSample + shipDataValues.draftOffset,
                    0.001f,
                    30f
                );

                var fallbackDisplacement =
                    draft
                    * shipDataValues.baseBuoyancy
                    * boatProbes._forcePoints[idx]._weight
                    * shipDataValues.draftSpanRatio
                    / buoyancyMultiplier
                    / totalWeight;
                var fallbackWettedArea = 3f * lengthAtWaterline * draft / totalWeight;
                var (area, displacement) =
                    shipData.GetHydrostaticValues(idx, draft)
                    ?? (fallbackWettedArea, fallbackDisplacement);
                totalDisplacement += displacement;
                wettedArea += area;

                float buoyantForceMagnitude =
                    PhysicsCalculation.waterWeight
                    * displacement
                    * boatProbes._forceMultiplier
                    / shipDataValues.baseBuoyancy
                    * buoyancyMultiplier
                    * Plugin.globalBuoyancyMultiplier!.Value;

                Vector3 bodyPointVelocity = rigidBody.GetPointVelocity(queryPoints[idx]);
                Vector3 relativeVelocity = bodyPointVelocity - queryVelocities[idx];
                Vector3 forwardVelocity = Vector3.Project(relativeVelocity, bodyForward);
                Vector3 offAxisVelocity = relativeVelocity - forwardVelocity;

                float forwardDrag = CalculateDragForce(
                    forwardVelocity.magnitude,
                    displacement,
                    wettedArea,
                    shipData,
                    true
                );

                float offAxisDrag = CalculateDragForce(
                    offAxisVelocity.magnitude,
                    displacement,
                    wettedArea,
                    shipData,
                    false
                );

                Vector3 buoyantForce = Vector3.up * buoyantForceMagnitude;
                Vector3 dragForce =
                    -forwardVelocity.normalized * forwardDrag
                    - offAxisVelocity.normalized * offAxisDrag;

                rigidBody.AddForceAtPosition(buoyantForce + dragForce, queryPoints[idx]);
                boatProbes.appliedBuoyancyForces[idx] = buoyantForceMagnitude;

#if DEBUG
                averageDraft += draft / totalWeight;
                totalFbDisplacement += fallbackDisplacement;
                averageFbArea += fallbackWettedArea;
                shipData.buoyancyForceRenderers[idx].SetMagnitude(buoyantForceMagnitude / 1000f);
                shipData.dragForceRenderers[idx].SetDirection(dragForce.normalized);
                shipData.dragForceRenderers[idx].SetMagnitude(dragForce.magnitude / 100f);
#endif
            }

#if DEBUG
            BetterDragDebug.LogCSVBuffered(
                [
                    ("draft, m", averageDraft),
                    ("displacement, m^3", totalDisplacement),
                    ("displacementFb, m^3", totalFbDisplacement),
                    ("area, m^2", wettedArea),
                    ("areaFb, m^2", averageFbArea),
                    ("baseBuoyancy", shipDataValues.baseBuoyancy),
                    ("draftSpanRatio", shipDataValues.draftSpanRatio),
                ]
            );
#endif
        }
    }
}
