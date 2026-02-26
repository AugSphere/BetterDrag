using Crest;
using UnityEngine;

namespace BetterDrag
{
    internal class Hydrostatics
    {
        const uint lengthSegmentCount = 100;
        const uint heightSegmentCount = 50;
        const uint probeLengthPositions = 6;
        readonly float minHeight;
        const float maxHeight = 10f;
        readonly float minLength;
        readonly float maxLength;
        static readonly Vector3 sentinelVector = Vector3.zero + 128f * Vector3.up;
        readonly float[,] displacements = new float[probeLengthPositions, heightSegmentCount + 1];
        readonly float[,] wettedAreas = new float[probeLengthPositions, heightSegmentCount + 1];
        readonly bool isTableFilled;
        readonly string shipName;

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

        internal Hydrostatics(
            string shipName,
            Rigidbody rigidbody,
            BoatProbes boatProbes,
            Vector3 bowPointPosition,
            Vector3 sternPointPosition,
            Vector3 keelPointPosition
        )
        {
            this.shipName = shipName;
            this.minHeight = keelPointPosition.y;
            this.minLength = 1.3f * sternPointPosition.z;
            this.maxLength = 1.3f * bowPointPosition.z;
            if (!CastHullRays(rigidbody, out var hullPoints, out var beamWidths))
            {
#if DEBUG
                BetterDragDebug.LogLineBuffered(
                    "Failed to cast rays to the hull, falling back to default hydrostatics."
                );
#endif
                return;
            }
            UpdateProbePositions(boatProbes, bowPointPosition, sternPointPosition, beamWidths);
            BuildTables(boatProbes, hullPoints);
            isTableFilled = true;
        }

        internal (float area, float displacement)? GetValues(int probeIdx, float draft)
        {
            if (!isTableFilled)
            {
#if DEBUG

                BetterDragDebug.LogLineBuffered(
                    "Trying to get a value from hydrostatic tables before they are built."
                );
#endif
                return null;
            }
            var heightSegmentFloat =
                Mathf.Clamp01(draft / Hydrostatics.maxHeight) * heightSegmentCount;
            var heightSegmentFloor = (int)heightSegmentFloat;
            var heightSegmentFraction = heightSegmentFloat % 1f;
            var halfProbeIdx = probeIdx / 2;
            if (heightSegmentFloor == heightSegmentCount)
            {
                return (
                    wettedAreas[halfProbeIdx, heightSegmentCount],
                    displacements[halfProbeIdx, heightSegmentCount]
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
            Vector3[] newPositions = new Vector3[boatProbes._forcePoints.Length];
            for (var lengthIdx = 0; lengthIdx < probeLengthPositions; ++lengthIdx)
            {
                var probeZ =
                    (maxProbeZ - minProbeZ) / (float)(probeLengthPositions - 1) * lengthIdx
                    + minProbeZ;
                var beam = this.GetBeam(beamWidths, probeZ);
                newPositions[lengthIdx * 2] = new(-beam / 2.5f, 0f, probeZ);
                newPositions[lengthIdx * 2 + 1] = new(beam / 2.5f, 0f, probeZ);
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
        }

        internal float GetBeam(float[] beamWidths, float rigidBodyZ)
        {
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
                    $"{shipName}: no hits on hull, falling back to embark."
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
            hullPoints = new Vector3[heightSegmentCount + 1, lengthSegmentCount + 1];
            beamWidths = new float[lengthSegmentCount + 1];
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
