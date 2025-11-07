using UnityEngine;

namespace BetterDrag
{
    internal class Hydrostatics
    {
        const uint lengthSegmentCount = 100;
        const uint heightSegmentCount = 50;
        static readonly float maxHeight = 10f;
        readonly Vector3[,] hullPoints = new Vector3[
            heightSegmentCount + 1,
            lengthSegmentCount + 1
        ];
        readonly float[] displacements = new float[heightSegmentCount + 1];
        readonly float[] wettedAreas = new float[heightSegmentCount + 1];
        float lengthSegmentSize;
        float hegithSegmentSize;
        bool isRayCast = false;
        bool isTableFilled = false;

#if DEBUG
        readonly DebugSphereRenderer[,] renderers = new DebugSphereRenderer[
            heightSegmentCount + 1,
            lengthSegmentCount + 1
        ];
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
            var minHeight = keelPoint.y;
            var maxHeight = Hydrostatics.maxHeight;
            var minLength = 1.3f * sternPoint.z;
            var maxLength = 1.3f * bowPoint.z;

            for (int heightIdx = 0; heightIdx < heightSegmentCount + 1; heightIdx++)
            {
                float heightFraction = (float)heightIdx / heightSegmentCount;
                var heightCoordinate = Mathf.Lerp(minHeight, maxHeight, heightFraction);

                for (int lengthIdx = 0; lengthIdx < lengthSegmentCount + 1; lengthIdx++)
                {
                    float lengthFraction = (float)lengthIdx / lengthSegmentCount;
                    var lengthCoordinate = Mathf.Lerp(minLength, maxLength, lengthFraction);

                    Vector3 castTarget = new(0, heightCoordinate, lengthCoordinate);
                    Vector3 castOrigin = castTarget + Vector3.right * 50f;
                    var isHit = GeometryQueries.SphereCastToHull(
                        castOrigin,
                        castTarget,
                        rigidbody,
                        out var hitInfo,
                        layerMask: LayerMask.GetMask("OnlyPlayerCol+Paintable")
                    );
                    if (isHit)
                    {
                        var hitPoint = rigidbody.transform.InverseTransformPoint(hitInfo.point);
                        hullPoints[heightIdx, lengthIdx] = hitPoint;
                    }
                    else
                    {
                        hullPoints[heightIdx, lengthIdx] = castTarget;
                    }
#if DEBUG
                    renderers[heightIdx, lengthIdx] = new(radius: 0.1f);
#endif
                }
            }
            hegithSegmentSize = Mathf.Abs(maxHeight - minHeight) / heightSegmentCount;
            lengthSegmentSize = Mathf.Abs(maxLength - minLength) / lengthSegmentCount;
            this.isRayCast = true;
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

            for (int heightIdx = 0; heightIdx < heightSegmentCount; heightIdx++)
            {
                wettedAreas[heightIdx + 1] = wettedAreas[heightIdx];
                displacements[heightIdx + 1] = displacements[heightIdx];
                for (int lengthIdx = 0; lengthIdx < lengthSegmentCount; lengthIdx++)
                {
                    var asternPointLow = hullPoints[heightIdx, lengthIdx];
                    var aheadPointLow = hullPoints[heightIdx, lengthIdx + 1];
                    var asternPointHigh = hullPoints[heightIdx + 1, lengthIdx];
                    var aheadPointHigh = hullPoints[heightIdx + 1, lengthIdx + 1];

                    var (lowArea, lowDisplacement) = Numerics.GetTriangleContribution(
                        (
                            new UnityVector3(asternPointLow),
                            new UnityVector3(aheadPointLow),
                            new UnityVector3(aheadPointHigh)
                        )
                    );
                    var (highArea, highDisplacement) = Numerics.GetTriangleContribution(
                        (
                            new UnityVector3(asternPointLow),
                            new UnityVector3(aheadPointHigh),
                            new UnityVector3(asternPointHigh)
                        )
                    );
                    this.wettedAreas[heightIdx + 1] += lowArea + highArea;
                    this.displacements[heightIdx + 1] += lowDisplacement + highDisplacement;
                }
            }
            isTableFilled = true;
        }

#if DEBUG
        internal void DrawHullPoints(Transform transform)
        {
            for (int heightIdx = 0; heightIdx < heightSegmentCount; heightIdx++)
            {
                for (int lengthIdx = 0; lengthIdx < lengthSegmentCount; lengthIdx++)
                {
                    var hullPoint = hullPoints[heightIdx, lengthIdx];
                    if (hullPoint.x == 0)
                        continue;
                    var worldPoint = transform.TransformPoint(hullPoint);
                    renderers[heightIdx, lengthIdx].DrawSphere(worldPoint);
                }
            }
        }
#endif
    }
}
