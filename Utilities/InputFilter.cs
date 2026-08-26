using Crest;
using UnityEngine;

namespace BetterDrag.Utilities;

using Hydrostatics = Hydrostatics.Hydrostatics;

internal sealed class InputFilter(Rigidbody rigidBody)
{
    private readonly Rigidbody _rigidBody = rigidBody;
    private readonly Vector3[] _bodyVelocities = new Vector3[Hydrostatics.ProbeCount];
    private readonly InputStore _bodyVelocityStore = new();
    private readonly InputStore _waterVelocityStore = new();
    private readonly UnstickUpdateVelocity _unstickUpdateVelocity = new();

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

        for (var idx = 0; idx < Hydrostatics.ProbeCount; ++idx)
        {
            _bodyVelocities[idx] = _rigidBody.GetPointVelocity(queryPoints[idx]);
        }

        var areInputsValid = !boatProbes.dontUpdateVelocity;

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

    private sealed class InputStore
    {
        internal readonly Vector3[] SavedValues = new Vector3[Hydrostatics.ProbeCount];

        internal void SaveArray(Vector3[] values)
        {
            for (int idx = 0; idx < Hydrostatics.ProbeCount; ++idx)
            {
                SavedValues[idx] = values[idx];
            }
        }
    }
}
