using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HarmonyLib;
#if PROFILE
using System.Globalization;
#endif

namespace BetterDrag.Utilities;

internal static class Profiler
{
    private static readonly Stopwatch Clock = new();
    private static long s_lastTick;

    private static readonly List<string> Names = [];
#if PROFILE
    private static readonly List<long> Durations = [];
#endif

    private static bool s_isOnFirstRun = true;

    static Profiler()
    {
#if PROFILE
        Clock.Start();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RestartClock()
    {
#if !PROFILE
        return;
#else
        s_lastTick = Clock.ElapsedTicks;
        Durations.Clear();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Profile(string name)
    {
#if !PROFILE
        return;
#else
        var duration = GetTicksSinceLast();
        if (s_isOnFirstRun)
        {
            Names.Add(name);
        }
        Durations.Add(duration);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogDurations()
    {
#if !PROFILE
        return;
#else
        UnityEngine.Debug.Assert(Names.Count == Durations.Count);
        PrintProfilingHeaderOnce();
        FileLog.Log(
            Durations.Join((n) => n.ToString(CultureInfo.InvariantCulture), delimiter: ";")
        );
#endif
    }

    private static void PrintProfilingHeaderOnce()
    {
        if (!s_isOnFirstRun)
        {
            return;
        }

        FileLog.Log($"Performance clock frequency {Stopwatch.Frequency}");
        FileLog.Log(Names.Join(delimiter: ";"));
        s_isOnFirstRun = false;
    }

    private static long GetTicksSinceLast()
    {
        var currentTick = Clock.ElapsedTicks;
        var duration = currentTick - s_lastTick;
        s_lastTick = currentTick;
        return duration;
    }
}
