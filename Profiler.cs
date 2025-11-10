using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine.Assertions;

namespace BetterDrag
{
    internal static class Profiler
    {
        private static readonly Stopwatch clock = new();
        private static long lastTick;

        private static readonly List<string> names = [];
#if PROFILE
        private static readonly List<long> durations = [];
#endif

        private static bool isOnFirstRun = true;

        static Profiler()
        {
#if PROFILE
            clock.Start();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RestartClock()
        {
#if !PROFILE
            return;
#else
            lastTick = clock.ElapsedTicks;
            durations.Clear();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Profile(string name)
        {
#if !PROFILE
            return;
#else
            var duration = GetTicksSinceLast();
            if (isOnFirstRun)
                names.Add(name);
            durations.Add(duration);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LogDurations()
        {
#if !PROFILE
            return;
#else
            Assert.IsTrue(names.Count == durations.Count);
            PrintProfilingHeaderOnce();
            FileLog.Log(durations.Join((n) => n.ToString(), delimiter: ";"));
#endif
        }

        private static void PrintProfilingHeaderOnce()
        {
            if (!isOnFirstRun)
                return;
            FileLog.Log($"Performance clock frequency {Stopwatch.Frequency}");
            FileLog.Log(names.Join(delimiter: ";"));
            isOnFirstRun = false;
        }

        private static long GetTicksSinceLast()
        {
            var currentTick = clock.ElapsedTicks;
            var duration = currentTick - lastTick;
            lastTick = currentTick;
            return duration;
        }
    }
}
