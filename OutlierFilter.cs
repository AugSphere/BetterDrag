using System.Text;
using UnityEngine;

namespace BetterDrag
{
    internal class OutlierFilter(string name, float rateLimit, float noFilterCutoff)
    {
        static readonly uint sampleCount = 16;
        readonly float rateLimit = rateLimit;
        readonly string name = name;
        readonly float noFilterCutoff = noFilterCutoff;
        readonly Cache<MemoryBuffer> cache = new(name, (_) => new());

        public float ClampValue(float value, Rigidbody rigidbody)
        {
            var buffer = this.cache.GetValue(rigidbody.gameObject);
            return ClampValueWithBuffer(value, buffer);
        }

        float ClampValueWithBuffer(float value, MemoryBuffer buffer)
        {
            float min = float.MaxValue,
                max = float.MinValue;
            for (int idx = 0; idx < sampleCount; idx++)
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
                return value;
            }
            else
            {
#if DEBUG && VERBOSE
                Debug.LogBuffered(
                    $"{this.name}: clipped {value, 10:F02} to {clampedValue, 10:F02}; samples: {buffer}"
                );
#endif
                buffer.Insert(clampedValue);
                return clampedValue;
            }
        }

        class MemoryBuffer
        {
            readonly float[] buffer = new float[sampleCount];
            private uint insertionIndex = 0;

            public float this[int idx]
            {
                get { return this.buffer[idx]; }
            }

            public void Insert(float value)
            {
                this.buffer[insertionIndex++ % sampleCount] = value;
            }

            public override string ToString()
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("[");
                for (int idx = 0; idx < sampleCount; idx++)
                {
                    stringBuilder.AppendFormat("{0:F2}", this.buffer[idx]);
                    if (idx != sampleCount - 1)
                        stringBuilder.Append(", ");
                }
                stringBuilder.Append("]");
                return stringBuilder.ToString();
            }
        }
    }
}
