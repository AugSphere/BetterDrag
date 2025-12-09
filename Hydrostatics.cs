using Crest;
using UnityEngine;
#if DEBUG
using System.Collections.Generic;
#endif

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
        readonly Vector3[,] hullPoints = new Vector3[
            heightSegmentCount + 1,
            lengthSegmentCount + 1
        ];
        readonly float[] displacements = new float[heightSegmentCount + 1];
        readonly float[] wettedAreas = new float[heightSegmentCount + 1];
        readonly float[] beamLengths = new float[lengthSegmentCount + 1];
        float lengthSegmentSize;
        float heightSegmentSize;
        bool isRayCast;
        bool isTableFilled;
        readonly string shipName = shipName;

#if DEBUG
        readonly List<DebugSphereRenderer> renderers = [];
#endif

        internal (float area, float displacement)? GetValues(float draft)
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
            if (heightSegmentFloor == heightSegmentCount)
            {
                return (
                    wettedAreas[heightSegmentCount] * 2f,
                    displacements[heightSegmentCount] * 2f
                );
            }
            var area = Mathf.Lerp(
                wettedAreas[heightSegmentFloor],
                wettedAreas[heightSegmentFloor + 1],
                heightSegmentFraction
            );
            var displacement = Mathf.Lerp(
                displacements[heightSegmentFloor],
                displacements[heightSegmentFloor + 1],
                heightSegmentFraction
            );
            return (area * 2f, displacement * 2f);
        }

        internal void UpdateProbePositions(
            BoatProbes boatProbes,
            Vector3 bowPoint,
            Vector3 sternPoint
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
                var beam = this.GetBeam(probeZ);
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
        }

        internal float? GetBeam(float rigidBodyZ)
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
            var lengthSegmentFloat =
                Mathf.Clamp01((rigidBodyZ - minLength) / (maxLength - minLength))
                * lengthSegmentCount;
            var lengthSegmentFloor = (int)lengthSegmentFloat;
            var lengthSegmentFraction = lengthSegmentFloat % 1f;
            if (lengthSegmentFloor == lengthSegmentCount)
            {
                return beamLengths[lengthSegmentCount] * 2f;
            }
            else
            {
                return Mathf.Lerp(
                        beamLengths[lengthSegmentFloor],
                        beamLengths[lengthSegmentFloor + 1],
                        lengthSegmentFraction
                    ) * 2f;
            }
        }

        internal void CastHullRays(
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
                    LayerMask.GetMask("OnlyPlayerCol+Paintable")
                )
            )
            {
                isRayCast = true;
                return;
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
                LayerMask.GetMask("Ignore Raycast")
            );
            isRayCast = hitsOnCapsule;
        }

        private bool CastHullRaysOnLayer(
            Rigidbody rigidbody,
            Vector3 bowPoint,
            Vector3 sternPoint,
            Vector3 keelPoint,
            int layerMask
        )
        {
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
                        beamLengths[lengthIdx] = Mathf.Max(beamLengths[lengthIdx], hitPoint.x);
#if DEBUG
                        renderers.Add(new(rigidbody, hitPoint, radius: 0.1f));
#endif
                    }
                    else
                    {
                        hullPoints[heightIdx, lengthIdx] = sentinelVector;
                    }
                }
            }
            heightSegmentSize = Mathf.Abs(maxHeight - minHeight) / heightSegmentCount;
            lengthSegmentSize = Mathf.Abs(maxLength - minLength) / lengthSegmentCount;
            return isGettingHits;
        }

        internal void BuildTables()
        {
            if (!this.isRayCast)
            {
#if DEBUG
                BetterDragDebug.LogLineBuffered(
                    "Trying to build hydrostatic tables before finding hull, aborting."
                );
#endif
                return;
            }

            displacements[0] = 0f;
            wettedAreas[0] = 0f;

            for (int heightIdx = 0; heightIdx < heightSegmentCount; ++heightIdx)
            {
                wettedAreas[heightIdx + 1] = wettedAreas[heightIdx];
                displacements[heightIdx + 1] = displacements[heightIdx];
                for (int lengthIdx = 0; lengthIdx < lengthSegmentCount; ++lengthIdx)
                {
                    var asternPointLow = hullPoints[heightIdx, lengthIdx];
                    var aheadPointLow = hullPoints[heightIdx, lengthIdx + 1];
                    var asternPointHigh = hullPoints[heightIdx + 1, lengthIdx];
                    var aheadPointHigh = hullPoints[heightIdx + 1, lengthIdx + 1];

                    this.ApplyTriangleContribution(
                        heightIdx,
                        asternPointLow,
                        aheadPointLow,
                        aheadPointHigh
                    );

                    this.ApplyTriangleContribution(
                        heightIdx,
                        asternPointLow,
                        aheadPointHigh,
                        asternPointHigh
                    );
                }
            }
            isTableFilled = true;
        }

        void ApplyTriangleContribution(int heightIdx, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            if (v1 == sentinelVector || v2 == sentinelVector || v3 == sentinelVector)
                return;

            var (area, displacement) = Numerics.GetTriangleContribution(v1, v2, v3);
            this.wettedAreas[heightIdx + 1] += area;
            this.displacements[heightIdx + 1] += displacement;
        }
    }
}
