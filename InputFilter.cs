using UnityEngine;

namespace BetterDrag
{
    internal class InputFilter(Rigidbody rigidBody)
    {
        private readonly Rigidbody rigidBody = rigidBody;

        private readonly VectorArrayFilter bodyVelocityFilter = new(
            "body velocity",
            rigidBody,
            0.25f
        );
        private readonly VectorArrayFilter queryVelocityFilter = new(
            "water velocity",
            rigidBody,
            0.03125f
        );
        private readonly VectorArrayFilter queryDisplacementFilter = new(
            "displacement",
            rigidBody,
            0.25f
        );

        internal (
            Vector3[] smoothedBodyVelocities,
            Vector3[] smoothedQueryVelocities,
            Vector3[] smoothedQueryDisplacements
        ) GetSmoothedQueries(
            bool dontUpdateVelocity,
            Vector3[] queryPoints,
            Vector3[] queryDisplacements,
            Vector3[] queryVelocities
        )
        {
            var areInputsValid =
                !dontUpdateVelocity
                && !bodyVelocityFilter.magnitudeFilter.IsOutlier(rigidBody.velocity.magnitude)
                && !queryVelocityFilter.magnitudeFilter.IsAnyMagnitudeOutlier(queryVelocities)
                && !queryDisplacementFilter.magnitudeFilter.IsAnyMagnitudeOutlier(
                    queryDisplacements
                );

#if DEBUG
            BetterDragDebug.LogCSVBuffered([("valid_inputs", areInputsValid ? 1 : 0)]);
#endif

            if (areInputsValid)
            {
                for (var idx = 0; idx < Hydrostatics.probeLengthPositions * 2; ++idx)
                {
                    bodyVelocityFilter.UpdateValueAtIndex(
                        rigidBody.GetPointVelocity(queryPoints[idx]),
                        idx
                    );
                    queryVelocityFilter.UpdateValueAtIndex(queryVelocities[idx], idx);
                    queryDisplacementFilter.UpdateValueAtIndex(queryDisplacements[idx], idx);
                }
            }
            return (
                bodyVelocityFilter.smoothedValues,
                queryVelocityFilter.smoothedValues,
                queryDisplacementFilter.smoothedValues
            );
        }

        private class VectorArrayFilter(string name, Rigidbody rigidBody, float smoothingFactor)
        {
            public Vector3[] smoothedValues = new Vector3[Hydrostatics.probeLengthPositions * 2];
            public readonly OutlierFilter magnitudeFilter = new(
                $"{name} filter",
                rigidBody.gameObject.name,
                rateLimit: 1.2f,
                noFilterCutoff: 0.1f
            );
            private readonly float smoothingFactor = smoothingFactor;

            public void UpdateValueAtIndex(Vector3 value, int idx)
            {
                smoothedValues[idx] *= (1f - smoothingFactor);
                smoothedValues[idx] += smoothingFactor * value;
            }
        }
    }
}
