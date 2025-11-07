using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BetterDrag
{
    /// <summary>
    /// Tests for hydrostatic table creation.
    /// </summary>
    [TestClass]
    public class NumericsTests
    {
        /// <summary>
        /// Test hull triangle contribution calculation.
        /// </summary>
        /// <param name="translation">Vector by which the triangle is translated.</param>
        /// <param name="rotationAxis">Axis around which the triangle is rotated.</param>
        /// <param name="rotationAngle">Angle by which the triangle is rotated.</param>
        /// <param name="expectedArea">Expected surface area of the triangle.</param>
        /// <param name="expectedDisplacement">Expected volume between the triangle and the YZ plane.</param>
        [DataRow(new float[] { 0f, 0f, 0f }, new float[] { 0f, 0f, 1f }, 0f, 0.866f, 1 / 6f)]
        [DataRow(new float[] { 0f, 1f, 0f }, new float[] { 0f, 0f, 1f }, 0f, 0.866f, 1 / 6f)]
        [DataRow(new float[] { 0f, 1f, 1f }, new float[] { 0f, 0f, 1f }, 0f, 0.866f, 1 / 6f)]
        [DataRow(new float[] { 1f, 1f, 1f }, new float[] { 0f, 0f, 1f }, 0f, 0.866f, 0.5f + 1 / 6f)]
        [DataRow(
            new float[] { 1f, 1f, 1f },
            new float[] { 0f, 0f, 1f },
            -(float)Math.PI / 2f,
            0.866f,
            0.5f + 1 / 6f
        )]
        [DataRow(
            new float[] { 1f, 1f, 1f },
            new float[] { 0f, 0f, 1f },
            (float)Math.PI / 2f,
            0.866f,
            2 / 6f
        )]
        [DataRow(
            new float[] { 1f, 0f, 0f },
            new float[] { 0f, 0f, 1f },
            (float)Math.PI / 2f,
            0.866f,
            2 / 6f
        )]
        [DataRow(new float[] { 0f, 0f, 0f }, new float[] { 1f, 0f, 0f }, 1f, 0.866f, 1 / 6f)]
        [TestMethod]
        public void GetTriangleContribution_WithSimplex(
            float[] translation,
            float[] rotationAxis,
            float rotationAngle,
            float expectedArea,
            float expectedDisplacement
        )
        {
            var tV = new Vector3(translation[0], translation[1], translation[2]);
            var rV = new Vector3(rotationAxis[0], rotationAxis[1], rotationAxis[2]);
            rV = rV / rV.Length() * (float)Math.Sin(rotationAngle / 2d);
            Quaternion quaternion = new(rV, (float)Math.Cos(rotationAngle / 2d));
            var vertices = (
                Vector3.Transform(Vector3.UnitX, quaternion) + tV,
                Vector3.Transform(Vector3.UnitY, quaternion) + tV,
                Vector3.Transform(Vector3.UnitZ, quaternion) + tV
            );

            var (area, displacement) = Numerics.GetTriangleContribution<Vector3>(vertices);

            Assert.AreEqual(expectedArea, area, 0.01);
            Assert.AreEqual(expectedDisplacement, displacement, 0.01);
        }
    }
}
