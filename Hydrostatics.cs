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
        const float maxHeight = 10f;
        static readonly Vector3 sentinelVector = Vector3.zero + 128f * Vector3.up;
        readonly Vector3[,] hullPoints = new Vector3[
            heightSegmentCount + 1,
            lengthSegmentCount + 1
        ];
        readonly float[] displacements = new float[heightSegmentCount + 1];
        readonly float[] wettedAreas = new float[heightSegmentCount + 1];
        float lengthSegmentSize;
        float hegithSegmentSize;
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
                return (wettedAreas[heightSegmentCount], displacements[heightSegmentCount]);
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
            var minLength = 1.3f * sternPoint.z;
            var maxLength = 1.3f * bowPoint.z;
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
            hegithSegmentSize = Mathf.Abs(maxHeight - minHeight) / heightSegmentCount;
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
