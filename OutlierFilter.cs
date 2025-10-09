using UnityEngine;
#if DEBUG
using HarmonyLib;
#endif

namespace BetterDrag
{
    internal static class OutlierFilter
    {
        static readonly uint sampleCount = 16;
        static readonly Cache<MemoryBuffer> cache = new("Velocity buffer");

        public static float ClampValue(float value, Rigidbody rb)
        {
            var buffer = cache.Get(rb.gameObject, () => new());
            return ClampVelocityForBuffer(value, buffer);
        }

        static float ClampVelocityForBuffer(float value, MemoryBuffer buffer)
        {
            float average = 0,
                min = float.MaxValue,
                max = float.MinValue;
            for (int idx = 0; idx < sampleCount; idx++)
            {
                var sample = buffer[idx];
                min = Mathf.Min(min, sample);
                max = Mathf.Max(max, sample);
                average += sample;
            }
            average /= sampleCount;
            var span = max - min;

            if (Mathf.Abs(value - average) < 3f * span)
            {
                buffer.Insert(value);
                return value;
            }
            else if (
                Mathf.Sign(value) == Mathf.Sign(average)
                && Mathf.Abs(value) < Mathf.Abs(average)
            )
            {
#if DEBUG
                FileLog.Log($"Value {value} magnitude rapidly dropping relative to {buffer}");
#endif
                buffer.Insert(value);
                return value;
            }
            else
            {
#if DEBUG
                FileLog.Log($"Value {value} inconsistent with samples {buffer}");
#endif
                buffer.Insert(0.25f * value + 0.75f * average);
                return average;
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
