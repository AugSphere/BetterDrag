using System.Globalization;
using System.Text;
using UnityEngine;

namespace BetterDrag
{
    internal class OutlierFilter(
        string filterName,
        string shipName,
        float rateLimit,
        float noFilterCutoff
    )
    {
        const uint sampleCount = 16;
        readonly float rateLimit = rateLimit;
        readonly float noFilterCutoff = noFilterCutoff;
        readonly MemoryBuffer buffer = new();
        readonly string filterName = filterName;
        readonly string shipName = shipName;

        public bool IsOutlier(float value)
        {
            return CheckOutlierWithBuffer(value, this.buffer);
        }

        bool CheckOutlierWithBuffer(float value, MemoryBuffer buffer)
        {
            float min = float.MaxValue,
                max = float.MinValue;
            for (int idx = 0; idx < sampleCount; ++idx)
            {
                var sample = buffer[idx];
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

        class MemoryBuffer
        {
            readonly float[] buffer = new float[sampleCount];
            private uint insertionIndex;

            public float this[int idx]
            {
                get { return this.buffer[idx]; }
            }

            public void Insert(float value)
            {
                this.buffer[insertionIndex++ % sampleCount] = value;
            }

            public sealed override string ToString()
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append('[');
                for (int idx = 0; idx < sampleCount; ++idx)
                {
                    stringBuilder.AppendFormat(
                        CultureInfo.InvariantCulture,
                        "{0:F2}",
                        this.buffer[idx]
                    );
                    if (idx != sampleCount - 1)
                        stringBuilder.Append(", ");
                }
                stringBuilder.Append(']');
                return stringBuilder.ToString();
            }
        }
    }
}
