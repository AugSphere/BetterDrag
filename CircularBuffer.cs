using System;
using UnityEngine;
#if DEBUG
using System.Globalization;
using System.Text;
#endif

namespace BetterDrag
{
    struct CircularBuffer
    {
        public const int maxCapacity = 16;
        private VectorBuffer buffer;
        private int insertionIndex;
#if DEBUG
        int period;
#else
        readonly int period;
#endif

        internal CircularBuffer(ushort period)
        {
            this.period = period;
        }

        public Vector3 Insert(Vector3 value)
        {
            var vectorSpan = buffer.AsVectors;
            Vector3 previous = vectorSpan[insertionIndex];
            vectorSpan[insertionIndex] = value;
            insertionIndex = (insertionIndex + 1) % period;
            return previous;
        }

        public readonly int Length
        {
            get { return period; }
        }

        private unsafe struct VectorBuffer
        {
            fixed float componentBuffer[maxCapacity * 3];

            internal Span<Vector3> AsVectors
            {
                get
                {
                    fixed (float* pointer = componentBuffer)
                    {
                        return new Span<Vector3>(pointer, maxCapacity);
                    }
                }
            }
        }

#if DEBUG
        public VectorEnumerator GetEnumerator()
        {
            int startIdx = insertionIndex - period;
            var vectorSpan = buffer.AsVectors;
            return new(vectorSpan, period, startIdx);
        }

        internal void SetPeriod(ushort period)
        {
            this.period = period;
        }

        public ref struct VectorEnumerator
        {
            private readonly Span<Vector3> span;
            private readonly int startIdx;
            private readonly int period;
            private int offset;

            public VectorEnumerator(Span<Vector3> span, int period, int startIdx)
            {
                this.span = span;
                this.startIdx = startIdx;
                this.period = period;
                offset = -1;
            }

            public bool MoveNext()
            {
                var next = offset + 1;
                if (next < period)
                {
                    offset = next;
                    return true;
                }
                return false;
            }

            public readonly ref Vector3 Current => ref span[Mod(startIdx + offset, period)];

            private static int Mod(int dividend, int divisor)
            {
                int remainder = dividend % divisor;
                return remainder < 0 ? remainder + divisor : remainder;
            }
        }

        public override readonly string ToString()
        {
            var vectorSpan = buffer.AsVectors;
            var stringBuilder = new StringBuilder();
            stringBuilder.Append('[');
            for (int idx = 0; idx < maxCapacity; ++idx)
            {
                stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0:F2}", vectorSpan[idx]);
                if (idx != maxCapacity - 1)
                    stringBuilder.Append(", ");
            }
            stringBuilder.Append(']');
            return stringBuilder.ToString();
        }
#endif
    }
}
