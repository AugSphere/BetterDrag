using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("BetterDrag")]

namespace BetterDrag
{
    internal static class Numerics
    {
        internal static (float area, float displacement) GetTriangleContribution<T>(
            (Vector3Wrapper<T>, Vector3Wrapper<T>, Vector3Wrapper<T>) vertices
        )
            where T : struct
        {
            var side1 = vertices.Item2 - vertices.Item1;
            var side2 = vertices.Item3 - vertices.Item1;
            var cross = Vector3Wrapper<T>.Cross(side1, side2);
            var area = cross.magnitude / 2f;
            var averageBeam = Abs((vertices.Item1.x + vertices.Item2.x + vertices.Item3.x) / 3f);
            var baseArea = Abs(cross.x) / 2f;
            var prismVolume = averageBeam * baseArea;
            return (area, prismVolume);
        }

        static float Abs(float a)
        {
            return a < 0 ? -a : a;
        }
    }
}
