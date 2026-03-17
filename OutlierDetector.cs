using UnityEngine;

namespace BetterDrag
{
    internal partial class OutlierDetector(
        string name,
        string shipName,
        float rateLimit = 1.2f,
        float noFilterCutoff = 0.1f
    )
    {
        const int sampleCount = 16;
        readonly float rateLimit = rateLimit;
        readonly float noFilterCutoff = noFilterCutoff;
        readonly CircularBuffer<float> buffer = new(sampleCount);
        readonly string name = name;
        readonly string shipName = shipName;

        public bool IsOutlier(float value)
        {
            return CheckOutlierWithBuffer(value, this.buffer);
        }

        public bool IsAnyMagnitudeOutlier(Vector3[] values)
        {
            float max = values[0].magnitude;
            for (var idx = 1; idx < values.Length; ++idx)
            {
                var magnitude = values[idx].magnitude;
                if (magnitude > max)
                    max = magnitude;
            }
            return CheckOutlierWithBuffer(max, this.buffer);
        }

        bool CheckOutlierWithBuffer(float value, CircularBuffer<float> buffer)
        {
            float min = float.MaxValue,
                max = float.MinValue;
            foreach (var sample in buffer)
            {
                min = Mathf.Min(min, sample);
                max = Mathf.Max(max, sample);
            }
            var extreme = Mathf.Sign(value) > 0 ? max : min;
            var isSameSign = Mathf.Sign(value) == Mathf.Sign(extreme);
            var absExtreme = Mathf.Abs(extreme);
            var absValue = Mathf.Abs(value);

            var isNearExtreme = Mathf.Abs(value - extreme) <= (rateLimit - 1f) * absExtreme;
            var isShrinkingToZero = isSameSign && absValue <= absExtreme;

            var clampedValue = isSameSign
                ? extreme * rateLimit
                : Mathf.Sign(value) * Mathf.Min(0.5f * rateLimit * absExtreme, absValue);

            if (
                isShrinkingToZero
                || isNearExtreme
                || extreme == 0f
                || Mathf.Abs(value) < noFilterCutoff
            )
            {
                buffer.Insert(value);
                return false;
            }
            else
            {
#if DEBUG && VERBOSE
                BetterDragDebug.LogLineBuffered(
                    $"{this.shipName}: {filterName} outlier {value, 10:F02} clamped to {clampedValue, 10:F02}; samples: {buffer}"
                );
#endif
                buffer.Insert(clampedValue);
                return true;
            }
        }
    }
}
