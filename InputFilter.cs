using UnityEngine;

namespace BetterDrag
{
    internal class InputFilter(Rigidbody rigidBody)
    {
        private const float smoothingFactor = 0.25f;
        private readonly Rigidbody rigidBody = rigidBody;

        private readonly Vector3[] smoothedBodyVelocities = new Vector3[
            Hydrostatics.probeLengthPositions * 2
        ];
        private readonly Vector3[] smoothedQueryVelocities = new Vector3[
            Hydrostatics.probeLengthPositions * 2
        ];
        private readonly Vector3[] smoothedQueryDisplacements = new Vector3[
            Hydrostatics.probeLengthPositions * 2
        ];

        private readonly OutlierFilter bodyVelocityFilter = new(
            "body velocity filter",
            rigidBody.gameObject.name,
            rateLimit: 1.2f,
            noFilterCutoff: 0.1f
        );
        private readonly OutlierFilter queryVelocityFilter = new(
            "query velocity filter",
            rigidBody.gameObject.name,
            rateLimit: 1.2f,
            noFilterCutoff: 0.1f
        );
        private readonly OutlierFilter queryDisplacementFilter = new(
            "query displacement filter",
            rigidBody.gameObject.name,
            rateLimit: 1.2f,
            noFilterCutoff: 0.1f
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
                && !bodyVelocityFilter.IsOutlier(rigidBody.velocity.magnitude)
                && !queryVelocityFilter.IsAnyMagnitudeOutlier(queryVelocities)
                && !queryDisplacementFilter.IsAnyMagnitudeOutlier(queryDisplacements);

#if DEBUG
            BetterDragDebug.LogCSVBuffered([("valid inputs", areInputsValid ? 1 : 0)]);
#endif

            if (areInputsValid)
            {
                for (var idx = 0; idx < Hydrostatics.probeLengthPositions * 2; ++idx)
                {
                    SmoothValue(smoothedQueryVelocities, idx, queryVelocities[idx]);
                    SmoothValue(
                        smoothedBodyVelocities,
                        idx,
                        rigidBody.GetPointVelocity(queryPoints[idx])
                    );
                    SmoothValue(smoothedQueryDisplacements, idx, queryDisplacements[idx]);
                }
            }
            return (smoothedBodyVelocities, smoothedQueryVelocities, smoothedQueryDisplacements);
        }

        private static void SmoothValue(Vector3[] smoothedValues, int idx, Vector3 value)
        {
            smoothedValues[idx] *= (1f - smoothingFactor);
            smoothedValues[idx] += smoothingFactor * value;
        }
    }
}
