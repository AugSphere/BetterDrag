using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BetterDrag
{
    class CircularBuffer<T> : IEnumerable<T>
        where T : struct
    {
#if DEBUG
        int period;
#else
        readonly int period;
#endif
        readonly int capacity;
        readonly T[] buffer;
        private int insertionIndex;

        public CircularBuffer(ushort period, ushort? capacity = null)
        {
            this.period = period;
            this.capacity = capacity ?? period;
            this.buffer = new T[this.capacity];
        }

        public IEnumerator<T> GetEnumerator()
        {
            int startIdx = insertionIndex - period;
            for (int offset = 0; offset < period; ++offset)
                yield return this.buffer[Mod(startIdx + offset, period)];
        }

        public T Insert(T value)
        {
            T previous = this.buffer[insertionIndex % capacity];
            this.buffer[insertionIndex % period] = value;
            insertionIndex = ++insertionIndex % period;
            return previous;
        }

        public int Length
        {
            get { return this.period; }
        }

        public sealed override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append('[');
            for (int idx = 0; idx < capacity; ++idx)
            {
                stringBuilder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    "{0:F2}",
                    this.buffer[idx]
                );
                if (idx != capacity - 1)
                    stringBuilder.Append(", ");
            }
            stringBuilder.Append(']');
            return stringBuilder.ToString();
        }

#if DEBUG
        public T this[int idx]
        {
            get { return this.buffer[idx]; }
        }

        internal void SetPeriod(ushort period)
        {
            this.period = period;
        }
#endif

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static int Mod(int dividend, int divisor)
        {
            int remainder = dividend % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }
    }
}
