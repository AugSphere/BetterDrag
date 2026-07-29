using Crest;

using UnityEngine;

namespace BetterDrag.Utilities
{
    internal class InputFilter(Rigidbody rigidBody)
    {
        private readonly Rigidbody _rigidBody = rigidBody;
        private readonly Vector3[] _bodyVelocities = new Vector3[Hydrostatics.Hydrostatics.ProbeCount];
        private readonly InputStore _bodyVelocityStore = new();
        private readonly InputStore _waterVelocityStore = new();
        private readonly UnstickUpdateVelocity _unstickUpdateVelocity = new();
        private const float VelocityCutoff = 30f;
        private const float VelocityCutoffSqr = VelocityCutoff * VelocityCutoff;
        private const float DisplacementCutoff = 15f;
        private const float DisplacementCutoffSqr = DisplacementCutoff * DisplacementCutoff;

        internal (
            Vector3[] bodyVelocities,
            Vector3[] queryVelocities,
            Vector3[] queryDisplacements
        ) GetLastValidInputs(
            BoatProbes boatProbes,
            Vector3[] queryPoints,
            Vector3[] queryDisplacements,
            Vector3[] queryVelocities
        )
        {
            _unstickUpdateVelocity.Update(boatProbes);

            for (var idx = 0; idx < Hydrostatics.Hydrostatics.ProbeCount; ++idx)
            {
                _bodyVelocities[idx] = _rigidBody.GetPointVelocity(queryPoints[idx]);
            }

            var areInputsValid =
                !boatProbes.dontUpdateVelocity
                && !HasMagnitudeOutliers(_bodyVelocities, VelocityCutoffSqr)
                && !HasMagnitudeOutliers(queryVelocities, VelocityCutoffSqr)
                && !HasMagnitudeOutliers(queryDisplacements, DisplacementCutoffSqr);

#if DEBUG
            BetterDragDebug.LogCSVBuffered([("update_v", !boatProbes.dontUpdateVelocity ? 1 : 0)]);
            BetterDragDebug.LogCSVBuffered([("valid_inputs", areInputsValid ? 1 : 0)]);
#endif

            if (areInputsValid)
            {
                _bodyVelocityStore.SaveArray(_bodyVelocities);
                _waterVelocityStore.SaveArray(queryVelocities);
            }
            return (
                _bodyVelocityStore.SavedValues,
                _waterVelocityStore.SavedValues,
                queryDisplacements
            );
        }

        private static bool HasMagnitudeOutliers(Vector3[] values, float outlierCutoffSqr)
        {
            for (int idx = 0; idx < Hydrostatics.Hydrostatics.ProbeCount; ++idx)
            {
                if (values[idx].sqrMagnitude > outlierCutoffSqr)
                {
                    return true;
                }
            }
            return false;
        }

        private class InputStore
        {
            internal readonly Vector3[] SavedValues = new Vector3[Hydrostatics.Hydrostatics.ProbeCount];

            internal void SaveArray(Vector3[] values)
            {
                for (int idx = 0; idx < Hydrostatics.Hydrostatics.ProbeCount; ++idx)
                {
                    SavedValues[idx] = values[idx];
                }
            }
        }
    }
}