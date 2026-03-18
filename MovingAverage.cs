using UnityEngine;

namespace BetterDrag
{
    internal struct MovingAverage
    {
        public const int defaultPeriod = 5;

        private CircularBuffer buffer = new(defaultPeriod);
#if !DEBUG
        private Vector3 previousMean;
#endif

        public MovingAverage() { }

        internal Vector3 Process(Vector3 value)
        {
#if DEBUG
            buffer.SetPeriod(Plugin.debugSmoothingPeriod!.Value);
            buffer.Insert(value);
            Vector3 sum = Vector3.zero;
            foreach (var sample in buffer)
                sum += sample;
            return sum / buffer.Length;
#else
            var popped = buffer.Insert(value);
            previousMean += (value - popped) / buffer.Length;
            return previousMean;
#endif
        }
    }
}
