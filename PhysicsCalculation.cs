using Crest;
using UnityEngine;
#if DEBUG
using System.Collections.Generic;
#endif

namespace BetterDrag
{
    internal static class PhysicsCalculation
    {
        static readonly float waterWeight = 1000f * Mathf.Abs(Physics.gravity.y);

        static float CalculateDragForce(
            float velocity,
            float displacement,
            float wettedArea,
            float lengthAtWaterline,
            ShipDragPerformanceData performanceData,
            bool isLongitudinal,
            int probeIdx
        )
        {
            var absVelocity = Mathf.Abs(velocity);
            var finalLengthAtWaterline =
                Plugin.globalShipLengthMultiplier!.Value
                * performanceData.LengthMultiplier
                * lengthAtWaterline;
            var formFactor = performanceData.FormFactor;

            var viscousDrag =
                Plugin.globalViscousDragMultiplier!.Value
                * performanceData.ViscousDragMultiplier
                * performanceData.CalculateViscousDragForce(
                    absVelocity,
                    finalLengthAtWaterline,
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
                    finalLengthAtWaterline,
                    formFactor,
                    displacement,
                    wettedArea
                );

#if DEBUG
            BetterDragDebug.LogCSVBuffered(
                [($"drag_vs_p{probeIdx}", viscousDrag), ($"drag_wm_p{probeIdx}", waveMakingDrag)]
            );
#endif
            return viscousDrag + waveMakingDrag;
        }

        public static void UpdateForces(
            BoatProbes boatProbes,
            Rigidbody rigidBody,
            ShipData shipData,
            Vector3[] queryPoints,
            Vector3[] queryDisplacements,
            Vector3[] queryVelocities,
            Vector3[] bodyVelocities,
            float totalWeight
        )
        {
            var totalDisplacement = 0.0f;
            var totalWettedArea = 0.0f;
#if DEBUG
            float averageDraft = 0.0f;
            var totalFbDisplacement = 0.0f;
            var totalFbWettedArea = 0.0f;
            List<(string, float)> csvItems = [];
#endif

            var shipDataValues = shipData.GetValues(boatProbes);
            var lengthAtWaterline = shipDataValues.lengthAtWaterline;
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
                totalWettedArea += area;

                float buoyantForceMagnitude =
                    PhysicsCalculation.waterWeight
                    * displacement
                    * boatProbes._forceMultiplier
                    / shipDataValues.baseBuoyancy
                    * buoyancyMultiplier
                    * Plugin.globalBuoyancyMultiplier!.Value;

                Vector3 bodyPointVelocity = bodyVelocities[idx];
                Vector3 relativeVelocity = bodyPointVelocity - queryVelocities[idx];
                Vector3 forwardVelocity = Vector3.Project(relativeVelocity, bodyForward);
                Vector3 offAxisVelocity = relativeVelocity - forwardVelocity;

                float forwardDrag = CalculateDragForce(
                    forwardVelocity.magnitude,
                    displacement,
                    area,
                    lengthAtWaterline,
                    shipData.dragData,
                    true,
                    idx
                );

                float offAxisDrag = CalculateDragForce(
                    offAxisVelocity.magnitude,
                    displacement,
                    area,
                    lengthAtWaterline,
                    shipData.dragData,
                    false,
                    idx
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
                totalFbWettedArea += fallbackWettedArea;
                csvItems.Add(($"draft_p{idx}", draft));
                csvItems.Add(($"velocity_rb_p{idx}", bodyPointVelocity.magnitude));
                csvItems.Add(($"velocity_w_p{idx}", queryVelocities[idx].magnitude));
                csvItems.Add(($"velocity_r_p{idx}", relativeVelocity.magnitude));
                csvItems.Add(($"velocity_fw_p{idx}", forwardVelocity.magnitude));
                csvItems.Add(($"velocity_tr_p{idx}", offAxisVelocity.magnitude));
                csvItems.Add(($"displacement_p{idx}", displacement));
                csvItems.Add(($"area_p{idx}", area));
                csvItems.Add(($"drag_fw_p{idx}", forwardDrag));
                csvItems.Add(($"drag_tr_p{idx}", offAxisDrag));
                shipData.buoyancyForceRenderers[idx].SetMagnitude(buoyantForceMagnitude / 1000f);
                shipData.dragForceRenderers[idx].SetDirection(dragForce.normalized);
                shipData.dragForceRenderers[idx].SetMagnitude(dragForce.magnitude / 1000f);
                shipData.waterVelocityRenders[idx].SetDirection(queryVelocities[idx].normalized);
                shipData.waterVelocityRenders[idx].SetMagnitude(queryVelocities[idx].magnitude);
                shipData.relativeVelocityRenders[idx].SetDirection(relativeVelocity.normalized);
                shipData.relativeVelocityRenders[idx].SetMagnitude(relativeVelocity.magnitude);
#endif
            }

#if DEBUG
            BetterDragDebug.LogCSVBuffered(
                [
                    ("draft_avg", averageDraft),
                    ("displacement", totalDisplacement),
                    ("displacement_fb", totalFbDisplacement),
                    ("area", totalWettedArea),
                    ("area_fb", totalFbWettedArea),
                ]
            );
            BetterDragDebug.LogCSVBuffered(csvItems);
#endif
        }
    }
}
