using Crest;
using UnityEngine;

namespace BetterDrag
{
    internal class Hydrostatics(string shipName)
    {
        const uint lengthSegmentCount = 100;
        const uint heightSegmentCount = 50;
        const uint probeLengthPositions = 6;
        const float maxHeight = 10f;
        float minLength;
        float maxLength;
        static readonly Vector3 sentinelVector = Vector3.zero + 128f * Vector3.up;
        readonly float[,] displacements = new float[probeLengthPositions, heightSegmentCount + 1];
        readonly float[,] wettedAreas = new float[probeLengthPositions, heightSegmentCount + 1];
        bool isRayCast;
        bool isProbeUpdated;
        bool isTableFilled;
        readonly string shipName = shipName;

#if DEBUG
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

        internal (float area, float displacement)? GetValues(int probeIdx, float draft)
        {
            if (!isTableFilled)
            {
#if DEBUG

                BetterDragDebug.LogLineBuffered(
                    "Trying to get a value from hydrostatic tables before they are built."
                );
                return null;
#endif
            }
            var heightSegmentFloat =
                Mathf.Clamp01(draft / Hydrostatics.maxHeight) * heightSegmentCount;
            var heightSegmentFloor = (int)heightSegmentFloat;
            var heightSegmentFraction = heightSegmentFloat % 1f;
            var halfProbeIdx = probeIdx / 2;
            if (heightSegmentFloor == heightSegmentCount)
            {
                return (
                    wettedAreas[halfProbeIdx, heightSegmentCount] * 2f,
                    displacements[halfProbeIdx, heightSegmentCount] * 2f
                );
            }
            var area = Mathf.Lerp(
                wettedAreas[halfProbeIdx, heightSegmentFloor],
                wettedAreas[halfProbeIdx, heightSegmentFloor + 1],
                heightSegmentFraction
            );
            var displacement = Mathf.Lerp(
                displacements[halfProbeIdx, heightSegmentFloor],
                displacements[halfProbeIdx, heightSegmentFloor + 1],
                heightSegmentFraction
            );
            return (area * 2f, displacement * 2f);
        }

        internal void UpdateProbePositions(
            BoatProbes boatProbes,
            Vector3 bowPoint,
            Vector3 sternPoint,
            float[] beamWidths
        )
        {
            if (!this.isRayCast)
            {
#if DEBUG
                BetterDragDebug.LogLineBuffered(
                    "Trying to update probes before finding hull, aborting."
                );
#endif
                return;
            }

            var maxProbeZ = bowPoint.z * 0.9f;
            var minProbeZ = sternPoint.z * 0.9f;
            Vector3[] newPositions = new Vector3[boatProbes._forcePoints.Length];
            for (var lengthIdx = 0; lengthIdx < probeLengthPositions; ++lengthIdx)
            {
                var probeZ =
                    (maxProbeZ - minProbeZ) / (float)(probeLengthPositions - 1) * lengthIdx
                    + minProbeZ;
                var beam = this.GetBeam(beamWidths, probeZ);
                if (beam is null)
                {
#if DEBUG
                    BetterDragDebug.LogLineBuffered(
                        "Hull beam value query failed, keeping default probes."
                    );
#endif
                    return;
                }
                newPositions[lengthIdx * 2] = new(-beam.Value / 2.5f, 0f, probeZ);
                newPositions[lengthIdx * 2 + 1] = new(beam.Value / 2.5f, 0f, probeZ);
            }
            for (
                var forcePointIdx = 0;
                forcePointIdx < boatProbes._forcePoints.Length;
                ++forcePointIdx
            )
            {
                boatProbes._forcePoints[forcePointIdx]._offsetPosition = newPositions[
                    forcePointIdx
                ];
            }
            isProbeUpdated = true;
        }

        internal float? GetBeam(float[] beamWidths, float rigidBodyZ)
        {
            if (!this.isRayCast)
            {
#if DEBUG
                BetterDragDebug.LogLineBuffered(
                    "Trying to get beam values before finding hull, aborting."
                );
#endif
                return null;
            }
            var lengthSegmentFloat =
                Mathf.Clamp01((rigidBodyZ - minLength) / (maxLength - minLength))
                * lengthSegmentCount;
            var lengthSegmentFloor = (int)lengthSegmentFloat;
            var lengthSegmentFraction = lengthSegmentFloat % 1f;
            if (lengthSegmentFloor == lengthSegmentCount)
            {
                return beamWidths[lengthSegmentCount] * 2f;
            }
            else
            {
                return Mathf.Lerp(
                        beamWidths[lengthSegmentFloor],
                        beamWidths[lengthSegmentFloor + 1],
                        lengthSegmentFraction
                    ) * 2f;
            }
        }

        internal (Vector3[,] hullPoints, float[] beamWidths) CastHullRays(
            Rigidbody rigidbody,
            Vector3 bowPoint,
            Vector3 sternPoint,
            Vector3 keelPoint
        )
        {
            if (
                CastHullRaysOnLayer(
                    rigidbody,
                    bowPoint,
                    sternPoint,
                    keelPoint,
                    LayerMask.GetMask("OnlyPlayerCol+Paintable"),
                    out var hullPoints,
                    out var beamWidths
                )
            )
            {
                isRayCast = true;
                return (hullPoints, beamWidths);
            }

#if DEBUG
            BetterDragDebug.LogLineBuffered(
                $"{shipName}: no hits on hull, falling back to capsule."
            );
#endif
            var hitsOnCapsule = CastHullRaysOnLayer(
                rigidbody,
                bowPoint,
                sternPoint,
                keelPoint,
                LayerMask.GetMask("Ignore Raycast"),
                out hullPoints,
                out beamWidths
            );
            isRayCast = hitsOnCapsule;
            return (hullPoints, beamWidths);
        }

        private bool CastHullRaysOnLayer(
            Rigidbody rigidbody,
            Vector3 bowPoint,
            Vector3 sternPoint,
            Vector3 keelPoint,
            int layerMask,
            out Vector3[,] hullPoints,
            out float[] beamWidths
        )
        {
            hullPoints = new Vector3[heightSegmentCount + 1, lengthSegmentCount + 1];
            beamWidths = new float[lengthSegmentCount + 1];
            var minHeight = keelPoint.y;
            var maxHeight = Hydrostatics.maxHeight;
            minLength = 1.3f * sternPoint.z;
            maxLength = 1.3f * bowPoint.z;
            var isGettingHits = false;

            for (int heightIdx = 0; heightIdx < heightSegmentCount + 1; ++heightIdx)
            {
                float heightFraction = (float)heightIdx / heightSegmentCount;
                var heightCoordinate = Mathf.Lerp(minHeight, maxHeight, heightFraction);

                for (int lengthIdx = 0; lengthIdx < lengthSegmentCount + 1; ++lengthIdx)
                {
                    float lengthFraction = (float)lengthIdx / lengthSegmentCount;
                    var lengthCoordinate = Mathf.Lerp(minLength, maxLength, lengthFraction);

                    Vector3 castTarget = new(0, heightCoordinate, lengthCoordinate);
                    Vector3 castOrigin =
                        castTarget + Vector3.right * GeometryQueries.defaultOriginOffset;
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
#if DEBUG
                        renderers[heightIdx, lengthIdx] = new(rigidbody, hitPoint, radius: 0.1f);
#endif
                    }
                    else
                    {
                        hullPoints[heightIdx, lengthIdx] = sentinelVector;
                    }
                }
            }
            return isGettingHits;
        }

        internal void BuildTables(BoatProbes boatProbes, Vector3[,] hullPoints)
        {
            if (!this.isProbeUpdated)
            {
#if DEBUG
                BetterDragDebug.LogLineBuffered(
                    "Trying to build hydrostatic tables before updating boat probes, aborting."
                );
#endif
                return;
            }

            for (int heightIdx = 0; heightIdx < heightSegmentCount; ++heightIdx)
            {
                for (int halfProbeIdx = 0; halfProbeIdx < probeLengthPositions; ++halfProbeIdx)
                {
                    wettedAreas[halfProbeIdx, heightIdx + 1] = wettedAreas[halfProbeIdx, heightIdx];
                    displacements[halfProbeIdx, heightIdx + 1] = displacements[
                        halfProbeIdx,
                        heightIdx
                    ];
                }
                for (int lengthIdx = 0; lengthIdx < lengthSegmentCount; ++lengthIdx)
                {
                    var asternPointLow = hullPoints[heightIdx, lengthIdx];
                    var aheadPointLow = hullPoints[heightIdx, lengthIdx + 1];
                    var asternPointHigh = hullPoints[heightIdx + 1, lengthIdx];
                    var aheadPointHigh = hullPoints[heightIdx + 1, lengthIdx + 1];
                    var halfProbeIdx =
                        FindNearestProbe(boatProbes._forcePoints, asternPointLow) / 2;
#if DEBUG
                    if (renderers[heightIdx, lengthIdx] is not null)
                        renderers[heightIdx, lengthIdx]!.SetColor(colorList[halfProbeIdx]);
#endif

                    this.ApplyTriangleContribution(
                        halfProbeIdx,
                        heightIdx,
                        asternPointLow,
                        aheadPointLow,
                        aheadPointHigh
                    );

                    this.ApplyTriangleContribution(
                        halfProbeIdx,
                        heightIdx,
                        asternPointLow,
                        aheadPointHigh,
                        asternPointHigh
                    );
                }
            }
            isTableFilled = true;
        }

        static int FindNearestProbe(FloaterForcePoints[] forcePoints, Vector3 position)
        {
            var minDistanceSq = float.MaxValue;
            var minIdx = 0;
            for (var idx = 0; idx < forcePoints.Length; ++idx)
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

        void ApplyTriangleContribution(
            int halfProbeIdx,
            int heightIdx,
            Vector3 v1,
            Vector3 v2,
            Vector3 v3
        )
        {
            if (v1 == sentinelVector || v2 == sentinelVector || v3 == sentinelVector)
                return;

            var (area, displacement) = Numerics.GetTriangleContribution(v1, v2, v3);
            this.wettedAreas[halfProbeIdx, heightIdx + 1] += area;
            this.displacements[halfProbeIdx, heightIdx + 1] += displacement;
        }
    }
}
