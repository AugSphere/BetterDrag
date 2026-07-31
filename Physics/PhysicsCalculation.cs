using BetterDrag.ShipConfiguration;
using Crest;
using UnityEngine;
#if DEBUG
using BetterDrag.Utilities;
using System.Collections.Generic;
#endif

namespace BetterDrag.Physics;

internal static class PhysicsCalculation
{
    private static readonly float WaterWeight = 1000f * Mathf.Abs(UnityEngine.Physics.gravity.y);

    private static float CalculateDragForce(
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
            Plugin.GlobalShipLengthMultiplier!.Value
            * performanceData.LengthMultiplier
            * lengthAtWaterline;
        var formFactor = performanceData.FormFactor;

        var viscousDrag =
            Plugin.GlobalViscousDragMultiplier!.Value
            * performanceData.ViscousDragMultiplier
            * performanceData.CalculateViscousDragForce(
                absVelocity,
                finalLengthAtWaterline,
                formFactor,
                displacement,
                wettedArea
            );

        if (!isLongitudinal)
        {
            return viscousDrag * Plugin.GlobalOffAxisDragMultiplier!.Value;
        }

        var waveMakingDrag =
            Plugin.GlobalWaveMakingDragMultiplier!.Value
            * performanceData.WaveMakingDragMultiplier
            * performanceData.CalculateWaveMakingDragForce(
                absVelocity,
                finalLengthAtWaterline,
                formFactor,
                displacement,
                wettedArea
            );

#if DEBUG
        BetterDragDebug.LogCSVBuffered([
            ($"drag_vs_p{probeIdx}", viscousDrag),
            ($"drag_wm_p{probeIdx}", waveMakingDrag),
        ]);
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
        shipData.KrakenGuard.FixAfterSleep(rigidBody, queryPoints, queryDisplacements);
        if (!shipData.ModEnableCheck.IsModEnabled())
        {
            return;
        }

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
        var buoyancyMultiplier = shipData.DragData.BuoyancyMultiplier;

        float seaLevel = OceanRenderer.Instance.SeaLevel;
        Vector3 bodyForward = rigidBody.transform.forward;

        for (int idx = 0; idx < boatProbes._forcePoints.Length; ++idx)
        {
            float waterHeightSample = seaLevel + queryDisplacements[idx].y - queryPoints[idx].y;
            float draft = Mathf.Clamp(waterHeightSample + shipDataValues.draftOffset, 0.001f, 30f);

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
                WaterWeight
                * displacement
                * boatProbes._forceMultiplier
                / shipDataValues.baseBuoyancy
                * buoyancyMultiplier
                * Plugin.GlobalBuoyancyMultiplier!.Value;

            Vector3 bodyPointVelocity = bodyVelocities[idx];
            Vector3 relativeVelocity = bodyPointVelocity - queryVelocities[idx];
            Vector3 forwardVelocity = Vector3.Project(relativeVelocity, bodyForward);
            Vector3 offAxisVelocity = relativeVelocity - forwardVelocity;

            float forwardDrag = CalculateDragForce(
                forwardVelocity.magnitude,
                displacement,
                area,
                lengthAtWaterline,
                shipData.DragData,
                true,
                idx
            );

            float offAxisDrag = CalculateDragForce(
                offAxisVelocity.magnitude,
                displacement,
                area,
                lengthAtWaterline,
                shipData.DragData,
                false,
                idx
            );

            Vector3 buoyantForce = Vector3.up * buoyantForceMagnitude;
            Vector3 dragForce =
                (-forwardVelocity.normalized * forwardDrag)
                - (offAxisVelocity.normalized * offAxisDrag);

            shipData.RawForces[idx] = buoyantForce + dragForce;
            boatProbes.appliedBuoyancyForces[idx] = buoyantForceMagnitude;
#if DEBUG
            averageDraft += draft / totalWeight;
            totalFbDisplacement += fallbackDisplacement;
            totalFbWettedArea += fallbackWettedArea;
            csvItems.Add(($"draft_p{idx}", draft));
            BetterDragDebug.LogVectorComponents(csvItems, "v_rb", idx, bodyPointVelocity);
            BetterDragDebug.LogVectorComponents(csvItems, "v_w", idx, queryVelocities[idx]);
            BetterDragDebug.LogVectorComponents(csvItems, "v_rel", idx, relativeVelocity);
            BetterDragDebug.LogVectorComponents(csvItems, "v_fw", idx, forwardVelocity);
            BetterDragDebug.LogVectorComponents(csvItems, "v_oa", idx, offAxisVelocity);
            BetterDragDebug.LogVectorComponents(csvItems, "drag", idx, dragForce);
            csvItems.Add(($"displacement_p{idx}", displacement));
            csvItems.Add(($"area_p{idx}", area));
            csvItems.Add(($"drag_fw_p{idx}", forwardDrag));
            csvItems.Add(($"drag_oa_p{idx}", offAxisDrag));
            shipData.BuoyancyForceRenderers[idx].SetMagnitude(buoyantForceMagnitude / 1000f);
            shipData.DragForceRenderers[idx].SetDirection(dragForce.normalized);
            shipData.DragForceRenderers[idx].SetMagnitude(dragForce.magnitude / 1000f);
            shipData.WaterVelocityRenderers[idx].SetDirection(queryVelocities[idx].normalized);
            shipData.WaterVelocityRenderers[idx].SetMagnitude(queryVelocities[idx].magnitude);
            shipData.RelativeVelocityRenderers[idx].SetDirection(relativeVelocity.normalized);
            shipData.RelativeVelocityRenderers[idx].SetMagnitude(relativeVelocity.magnitude);
#endif
        }

        var forces = shipData.OutputFilter.FilterForces(shipData.RawForces);

        for (int idx = 0; idx < boatProbes._forcePoints.Length; ++idx)
        {
            rigidBody.AddForceAtPosition(forces[idx], queryPoints[idx]);
#if DEBUG
            shipData.OutputForceRenderers[idx].SetDirection(forces[idx].normalized);
            shipData.OutputForceRenderers[idx].SetMagnitude(forces[idx].magnitude / 1000f);
#endif
        }

#if DEBUG
        BetterDragDebug.LogCSVBuffered([
            ("draft_avg", averageDraft),
            ("displacement", totalDisplacement),
            ("displacement_fb", totalFbDisplacement),
            ("area", totalWettedArea),
            ("area_fb", totalFbWettedArea),
        ]);
        BetterDragDebug.LogCSVBuffered(csvItems);
#endif
    }
}
