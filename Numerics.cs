using System;
using UnityEngine;

namespace BetterDrag
{
    internal static class Numerics
    {
        internal static (float area, float displacement) GetTriangleContribution(
            Vector3 v1,
            Vector3 v2,
            Vector3 v3
        )
        {
            var side1 = v2 - v1;
            var side2 = v3 - v1;
            var cross = Vector3.Cross(side1, side2);
            var area = cross.magnitude / 2f;
            var averageBeam = Math.Abs((v1.x + v2.x + v3.x) / 3f);
            var baseArea = Math.Abs(cross.x) / 2f;
            var displacement = averageBeam * baseArea;
            return (area, displacement);
        }
    }
}
