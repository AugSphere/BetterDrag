using UnityEngine;

namespace BetterDrag
{
    internal class OutputFilter
    {
        private readonly ArrayFilter forcesFilter = new();

        internal Vector3[] FilterForces(Vector3[] rawForces)
        {
            if (!Plugin.enableForceSmoothing!.Value || GameState.sleeping)
                return rawForces;
            forcesFilter.ProcessArray(rawForces);
            return forcesFilter.filteredValues;
        }

        private class ArrayFilter
        {
            const int windowSize = 5;
            const float weight = 1f / (float)windowSize;
            internal readonly Vector3[] filteredValues = new Vector3[Hydrostatics.probeCount];
            private readonly Vector3[,] memory = new Vector3[windowSize, Hydrostatics.probeCount];
            private int memoryIdx;

            internal void ProcessArray(Vector3[] values)
            {
                for (int idx = 0; idx < Hydrostatics.probeCount; ++idx)
                {
                    filteredValues[idx] += weight * (values[idx] - memory[memoryIdx, idx]);
                    memory[memoryIdx, idx] = values[idx];
                }
                memoryIdx = (memoryIdx + 1) % windowSize;
            }
        }
    }
}
