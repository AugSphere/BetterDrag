using UnityEngine;
#if DEBUG
using HarmonyLib;
#endif

namespace BetterDrag
{
    internal class OutlierFilter(string name)
    {
        static readonly uint sampleCount = 16;
        static readonly float rateLimit = 1.3f;
        readonly string name = name;
        readonly Cache<MemoryBuffer> cache = new(name);

        public float ClampValue(float value, Rigidbody rb)
        {
            var buffer = this.cache.Get(rb.gameObject, () => new());
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

            if (isShrinkingToZero || isNearExtreme || extreme == 0f || Mathf.Abs(clampedValue) < 1)
            {
                buffer.Insert(value);
                return value;
            }
            else
            {
#if DEBUG
                FileLog.Log(
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

            public float this[int index]
            {
                get { return this.buffer[index]; }
            }

            public void Insert(float value)
            {
                this.buffer[insertionIndex++ % sampleCount] = value;
            }

            public override string ToString()
            {
                return "[" + string.Join(", ", this.buffer) + "]";
            }
        }
    }
}
