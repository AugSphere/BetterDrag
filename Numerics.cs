using System;

namespace BetterDrag
{
    internal static class Numerics
    {
        internal static (float area, float displacement) GetTriangleContribution<T>(
            (IVector3<T>, IVector3<T>, IVector3<T>) vertices
        )
            where T : struct
        {
            var side1 = vertices.Item2.Subtract(vertices.Item1);
            var side2 = vertices.Item3.Subtract(vertices.Item1);
            var cross = side1.Cross(side2);
            var area = cross.Magnitude / 2f;
            var averageBeam = Math.Abs(
                (vertices.Item1.X + vertices.Item2.X + vertices.Item3.X) / 3f
            );
            var baseArea = Math.Abs(cross.X) / 2f;
            var displacement = averageBeam * baseArea;
            return (area, displacement);
        }
    }
}
