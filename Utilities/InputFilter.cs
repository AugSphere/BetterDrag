using UnityEngine;

namespace BetterDrag
{
    internal class InputFilter(Rigidbody rigidBody)
    {
        private readonly Rigidbody rigidBody = rigidBody;
        private readonly Vector3[] bodyVelocities = new Vector3[Hydrostatics.probeCount];
        private readonly InputStore bodyVelocityStore = new();
        private readonly InputStore waterVelocityStore = new();
        private readonly InputStore waterDisplacementStore = new();
        private const float velocityCutoff = 30f;
        private const float velocityCutoffSqr = velocityCutoff * velocityCutoff;
        private const float displacementCutoff = 15f;
        private const float displacementCutoffSqr = displacementCutoff * displacementCutoff;

        internal (
            Vector3[] bodyVelocities,
            Vector3[] queryVelocities,
            Vector3[] queryDisplacements
        ) GetLastValidInputs(
            bool dontUpdateVelocity,
            Vector3[] queryPoints,
            Vector3[] queryDisplacements,
            Vector3[] queryVelocities
        )
        {
            for (var idx = 0; idx < Hydrostatics.probeCount; ++idx)
                bodyVelocities[idx] = rigidBody.GetPointVelocity(queryPoints[idx]);

            var areInputsValid =
                !dontUpdateVelocity
                && !HasMagnitudeOutliers(bodyVelocities, velocityCutoffSqr)
                && !HasMagnitudeOutliers(queryVelocities, velocityCutoffSqr)
                && !HasMagnitudeOutliers(queryDisplacements, displacementCutoffSqr);

#if DEBUG
            BetterDragDebug.LogCSVBuffered([("valid_inputs", areInputsValid ? 1 : 0)]);
#endif

            if (areInputsValid)
            {
                bodyVelocityStore.SaveArray(bodyVelocities);
                waterVelocityStore.SaveArray(queryVelocities);
                waterDisplacementStore.SaveArray(queryDisplacements);
            }
            return (
                bodyVelocityStore.savedValues,
                waterVelocityStore.savedValues,
                waterDisplacementStore.savedValues
            );
        }

        private static bool HasMagnitudeOutliers(Vector3[] values, float outlierCutoffSqr)
        {
            for (int idx = 0; idx < Hydrostatics.probeCount; ++idx)
            {
                if (values[idx].sqrMagnitude > outlierCutoffSqr)
                    return true;
            }
            return false;
        }

        private class InputStore
        {
            internal readonly Vector3[] savedValues = new Vector3[Hydrostatics.probeCount];

            internal void SaveArray(Vector3[] values)
            {
                for (int idx = 0; idx < Hydrostatics.probeCount; ++idx)
                {
                    savedValues[idx] = values[idx];
                }
            }
        }
    }
}
