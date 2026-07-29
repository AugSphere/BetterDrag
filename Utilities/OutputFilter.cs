using UnityEngine;

namespace BetterDrag.Utilities
{
    internal class OutputFilter
    {
        private readonly ArrayFilter _forcesFilter = new();

        internal Vector3[] FilterForces(Vector3[] rawForces)
        {
            if (!Plugin.EnableForceSmoothing!.Value || GameState.sleeping)
            {
                return rawForces;
            }

            _forcesFilter.ProcessArray(rawForces);
            return _forcesFilter.FilteredValues;
        }

        private class ArrayFilter
        {
            private const int WindowSize = 5;
            private const float Weight = 1f / WindowSize;
            internal readonly Vector3[] FilteredValues = new Vector3[Hydrostatics.Hydrostatics.ProbeCount];
            private readonly Vector3[,] _memory = new Vector3[WindowSize, Hydrostatics.Hydrostatics.ProbeCount];
            private int _memoryIdx;

            internal void ProcessArray(Vector3[] values)
            {
                for (int idx = 0; idx < Hydrostatics.Hydrostatics.ProbeCount; ++idx)
                {
                    FilteredValues[idx] += Weight * (values[idx] - _memory[_memoryIdx, idx]);
                    _memory[_memoryIdx, idx] = values[idx];
                }
                _memoryIdx = (_memoryIdx + 1) % WindowSize;
            }
        }
    }
}