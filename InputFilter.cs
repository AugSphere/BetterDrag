using UnityEngine;

namespace BetterDrag
{
    internal class InputFilter(Rigidbody rigidBody)
    {
        private readonly Rigidbody rigidBody = rigidBody;
        private readonly Vector3[] bodyVelocities = new Vector3[Hydrostatics.probeCount];
        private readonly VectorArrayFilter bodyVelocityFilter = new();
        private readonly VectorArrayFilter waterVelocityFilter = new();
        private readonly VectorArrayFilter waterDisplacementFilter = new();

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
            for (var idx = 0; idx < Hydrostatics.probeCount; ++idx)
                bodyVelocities[idx] = rigidBody.GetPointVelocity(queryPoints[idx]);

            var areInputsValid = !dontUpdateVelocity;

#if DEBUG
            BetterDragDebug.LogCSVBuffered([("valid_inputs", areInputsValid ? 1 : 0)]);
#endif

            if (areInputsValid)
            {
                bodyVelocityFilter.ProcessArray(bodyVelocities);
                waterVelocityFilter.ProcessArray(queryVelocities);
                waterDisplacementFilter.ProcessArray(queryDisplacements);
            }
            return (
                bodyVelocityFilter.filteredValues,
                waterVelocityFilter.filteredValues,
                waterDisplacementFilter.filteredValues
            );
        }

        private class VectorArrayFilter
        {
            const float expSmoothing = 1f;
            const float expSmoothingComplement = 1f - expSmoothing;
            private readonly MovingAverage[] movingAverage;
            internal readonly Vector3[] filteredValues;

            internal VectorArrayFilter()
            {
                filteredValues = new Vector3[Hydrostatics.probeCount];
                movingAverage = new MovingAverage[Hydrostatics.probeCount];
                for (int idx = 0; idx < Hydrostatics.probeCount; ++idx)
                    movingAverage[idx] = new();
            }

            internal void ProcessArray(Vector3[] values)
            {
                for (int idx = 0; idx < Hydrostatics.probeCount; ++idx)
                {
                    filteredValues[idx] =
                        expSmoothing * movingAverage[idx].Process(values[idx])
                        + expSmoothingComplement * filteredValues[idx];
                }
            }
        }
    }
}
