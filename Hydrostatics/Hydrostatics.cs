using BetterDrag.Utilities;
using Crest;
using UnityEngine;

namespace BetterDrag.Hydrostatics;

internal sealed class Hydrostatics
{
    private const uint LengthSegmentCount = 100;
    private const uint HeightSegmentCount = 50;
    public const uint ProbeLengthPositions = 6;
    public const uint ProbeCount = ProbeLengthPositions * 2;
    private readonly float _minHeight;
    private const float MaxHeight = 10f;
    private readonly float _minLength;
    private readonly float _maxLength;
    private static readonly Vector3 SentinelVector = Vector3.zero + (128f * Vector3.up);
    private readonly float[,] _displacements = new float[
        ProbeLengthPositions,
        HeightSegmentCount + 1
    ];
    private readonly float[,] _wettedAreas = new float[
        ProbeLengthPositions,
        HeightSegmentCount + 1
    ];
    private readonly bool _isTableFilled;
    private readonly string _shipName;

#if DEBUG && DRAW_HULL
    static readonly Color[] colorList =
    [
        Color.white,
        Color.gray,
        Color.black,
        Color.blue,
        Color.red,
        Color.green,
    ];
    readonly DebugSphereRenderer?[,] renderers = new DebugSphereRenderer?[
        heightSegmentCount + 1,
        lengthSegmentCount + 1
    ];
#endif

    internal Hydrostatics(
        string shipName,
        Rigidbody rigidbody,
        BoatProbes boatProbes,
        Vector3 bowPointPosition,
        Vector3 sternPointPosition,
        Vector3 keelPointPosition
    )
    {
        _shipName = shipName;
        _minHeight = keelPointPosition.y;
        _minLength = 1.3f * sternPointPosition.z;
        _maxLength = 1.3f * bowPointPosition.z;
        if (!CastHullRays(rigidbody, out var hullPoints, out var beamWidths))
        {
#if DEBUG
            BetterDragDebug.LogLineBuffered(
                $"{shipName}: failed to cast rays to the hull, falling back to default hydrostatics."
            );
#endif
            return;
        }
        UpdateProbePositions(boatProbes, bowPointPosition, sternPointPosition, beamWidths);
        BuildTables(boatProbes, hullPoints);
        _isTableFilled = true;
    }

    internal (float area, float displacement)? GetValues(int probeIdx, float draft)
    {
        if (!_isTableFilled)
        {
            return null;
        }

        var heightSegmentFloat = Mathf.Clamp01(draft / MaxHeight) * HeightSegmentCount;
        var heightSegmentFloor = (int)heightSegmentFloat;
        var heightSegmentFraction = heightSegmentFloat % 1f;
        var halfProbeIdx = probeIdx / 2;
        if (heightSegmentFloor == HeightSegmentCount)
        {
            return (
                _wettedAreas[halfProbeIdx, HeightSegmentCount],
                _displacements[halfProbeIdx, HeightSegmentCount]
            );
        }
        var area = Mathf.Lerp(
            _wettedAreas[halfProbeIdx, heightSegmentFloor],
            _wettedAreas[halfProbeIdx, heightSegmentFloor + 1],
            heightSegmentFraction
        );
        var displacement = Mathf.Lerp(
            _displacements[halfProbeIdx, heightSegmentFloor],
            _displacements[halfProbeIdx, heightSegmentFloor + 1],
            heightSegmentFraction
        );
        return (area, displacement);
    }

    internal void UpdateProbePositions(
        BoatProbes boatProbes,
        Vector3 bowPoint,
        Vector3 sternPoint,
        float[] beamWidths
    )
    {
        var maxProbeZ = bowPoint.z * 0.9f;
        var minProbeZ = sternPoint.z * 0.9f;
        Vector3[] newPositions = new Vector3[ProbeCount];
        for (var lengthIdx = 0; lengthIdx < ProbeLengthPositions; ++lengthIdx)
        {
            var probeZ =
                ((maxProbeZ - minProbeZ) / (ProbeLengthPositions - 1) * lengthIdx) + minProbeZ;
            var beam = GetBeam(beamWidths, probeZ);
            newPositions[lengthIdx * 2] = new(-beam / 2.5f, 0f, probeZ);
            newPositions[(lengthIdx * 2) + 1] = new(beam / 2.5f, 0f, probeZ);
        }
        for (var forcePointIdx = 0; forcePointIdx < ProbeCount; ++forcePointIdx)
        {
            boatProbes._forcePoints[forcePointIdx]._offsetPosition = newPositions[forcePointIdx];
        }
    }

    internal float GetBeam(float[] beamWidths, float rigidBodyZ)
    {
        var lengthSegmentFloat =
            Mathf.Clamp01((rigidBodyZ - _minLength) / (_maxLength - _minLength))
            * LengthSegmentCount;
        var lengthSegmentFloor = (int)lengthSegmentFloat;
        var lengthSegmentFraction = lengthSegmentFloat % 1f;
        return lengthSegmentFloor == LengthSegmentCount
            ? beamWidths[LengthSegmentCount] * 2f
            : Mathf.Lerp(
                beamWidths[lengthSegmentFloor],
                beamWidths[lengthSegmentFloor + 1],
                lengthSegmentFraction
            ) * 2f;
    }

    internal bool CastHullRays(
        Rigidbody rigidbody,
        out Vector3[,] hullPoints,
        out float[] beamWidths
    )
    {
        if (
            CastHullRaysOnLayer(
                rigidbody,
                LayerMask.GetMask("OnlyPlayerCol+Paintable"),
                out hullPoints,
                out beamWidths
            )
        )
        {
            return true;
        }
        else
        {
#if DEBUG
            BetterDragDebug.LogLineBuffered(
                $"{_shipName}: no hits on hull, falling back to embark."
            );
#endif
            return CastHullRaysOnLayer(
                rigidbody,
                LayerMask.GetMask("Ignore Raycast"),
                out hullPoints,
                out beamWidths
            );
        }
    }

    private bool CastHullRaysOnLayer(
        Rigidbody rigidbody,
        int layerMask,
        out Vector3[,] hullPoints,
        out float[] beamWidths
    )
    {
        hullPoints = new Vector3[HeightSegmentCount + 1, LengthSegmentCount + 1];
        beamWidths = new float[LengthSegmentCount + 1];
        var isGettingHits = false;

        for (int heightIdx = 0; heightIdx < HeightSegmentCount + 1; ++heightIdx)
        {
            float heightFraction = (float)heightIdx / HeightSegmentCount;
            var heightCoordinate = Mathf.Lerp(_minHeight, MaxHeight, heightFraction);

            for (int lengthIdx = 0; lengthIdx < LengthSegmentCount + 1; ++lengthIdx)
            {
                float lengthFraction = (float)lengthIdx / LengthSegmentCount;
                var lengthCoordinate = Mathf.Lerp(_minLength, _maxLength, lengthFraction);

                Vector3 castTarget = new(0, heightCoordinate, lengthCoordinate);
                Vector3 castOrigin =
                    castTarget + (Vector3.right * GeometryQueries.DefaultOriginOffset);
                var isHit = GeometryQueries.GetFirstHullHit(
                    castOrigin,
                    castTarget,
                    rigidbody,
                    out var hitInfo,
                    layerMask: layerMask
                );
                if (isHit)
                {
                    isGettingHits = true;
                    var hitPoint = rigidbody.transform.InverseTransformPoint(hitInfo.point);
                    hullPoints[heightIdx, lengthIdx] = hitPoint;
                    beamWidths[lengthIdx] = Mathf.Max(beamWidths[lengthIdx], hitPoint.x);
#if DEBUG && DRAW_HULL
                    renderers[heightIdx, lengthIdx] = new(rigidbody, hitPoint, radius: 0.1f);
#endif
                }
                else
                {
                    hullPoints[heightIdx, lengthIdx] = SentinelVector;
                }
            }
        }
        return isGettingHits;
    }

    internal void BuildTables(BoatProbes boatProbes, Vector3[,] hullPoints)
    {
        for (int heightIdx = 0; heightIdx < HeightSegmentCount; ++heightIdx)
        {
            for (int halfProbeIdx = 0; halfProbeIdx < ProbeLengthPositions; ++halfProbeIdx)
            {
                _wettedAreas[halfProbeIdx, heightIdx + 1] = _wettedAreas[halfProbeIdx, heightIdx];
                _displacements[halfProbeIdx, heightIdx + 1] = _displacements[
                    halfProbeIdx,
                    heightIdx
                ];
            }
            for (int lengthIdx = 0; lengthIdx < LengthSegmentCount; ++lengthIdx)
            {
                var asternPointLow = hullPoints[heightIdx, lengthIdx];
                var aheadPointLow = hullPoints[heightIdx, lengthIdx + 1];
                var asternPointHigh = hullPoints[heightIdx + 1, lengthIdx];
                var aheadPointHigh = hullPoints[heightIdx + 1, lengthIdx + 1];
                var halfProbeIdx = FindNearestProbe(boatProbes._forcePoints, asternPointLow) / 2;
#if DEBUG && DRAW_HULL
                if (renderers[heightIdx, lengthIdx] is not null)
                    renderers[heightIdx, lengthIdx]!.SetColor(colorList[halfProbeIdx]);
#endif

                ApplyTriangleContribution(
                    halfProbeIdx,
                    heightIdx,
                    asternPointLow,
                    aheadPointLow,
                    aheadPointHigh
                );

                ApplyTriangleContribution(
                    halfProbeIdx,
                    heightIdx,
                    asternPointLow,
                    aheadPointHigh,
                    asternPointHigh
                );
            }
        }
    }

    private static int FindNearestProbe(FloaterForcePoints[] forcePoints, Vector3 position)
    {
        var minDistanceSq = float.MaxValue;
        var minIdx = 0;
        for (var idx = 0; idx < ProbeCount; ++idx)
        {
            var distanceSq = (forcePoints[idx]._offsetPosition - position).sqrMagnitude;
            if (distanceSq < minDistanceSq)
            {
                minDistanceSq = distanceSq;
                minIdx = idx;
            }
        }
        return minIdx;
    }

    private void ApplyTriangleContribution(
        int halfProbeIdx,
        int heightIdx,
        Vector3 v1,
        Vector3 v2,
        Vector3 v3
    )
    {
        if (v1 == SentinelVector || v2 == SentinelVector || v3 == SentinelVector)
        {
            return;
        }

        var (area, displacement) = Numerics.GetTriangleContribution(v1, v2, v3);
        _wettedAreas[halfProbeIdx, heightIdx + 1] += area;
        _displacements[halfProbeIdx, heightIdx + 1] += displacement;
    }
}
