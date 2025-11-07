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
        bool raysCast = false;

#if DEBUG
        readonly DebugSphereRenderer[,] renderers = new DebugSphereRenderer[
            heightSegmentCount,
            lengthSegmentCount
        ];
#endif

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
            this.raysCast = true;
        }

        void BuildTables()
        {
            if (!this.raysCast)
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

                    Vector3[] lowTriangleVertices = [asternPointLow, aheadPointLow, aheadPointHigh];
                    Vector3[] highTriangleVertices =
                    [
                        asternPointLow,
                        aheadPointHigh,
                        asternPointHigh,
                    ];

                    var (lowArea, lowDisplacement) = Numerics.GetTriangleContribution(
                        ((Vector3Wrapper<Vector3>)asternPointLow, aheadPointLow, aheadPointHigh)
                    );
                    var (highArea, highDisplacement) = Numerics.GetTriangleContribution(
                        ((Vector3Wrapper<Vector3>)asternPointLow, aheadPointHigh, asternPointHigh)
                    );
                    this.wettedAreas[heightIdx + 1] += lowArea + highArea;
                    this.displacements[heightIdx + 1] += lowDisplacement + highDisplacement;
                }
            }
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
