#if DEBUG
using System.Threading;

namespace BetterDrag
{
    internal static class DebugCounter
    {
        private static int counter = 0;
        private static readonly int period = 250;

        public static bool IsAtFirstFrame()
        {
            return counter == 0;
        }

        public static bool IsAtPeriod()
        {
            return counter % period == 0;
        }

        public static void Increment()
        {
            Interlocked.Increment(ref counter);
        }
    }
}
#endif
