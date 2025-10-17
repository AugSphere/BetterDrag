#if DEBUG
using HarmonyLib;
using System.Collections.Generic;

namespace BetterDrag
{
    internal static class Debug
    {
        public static bool executedOnce = false;
        private static uint counter = 1;
        private static readonly uint period = 250;
        private static readonly List<string> dragModelLog = [];

        public static void IncrementCounter()
        {
            counter++;
        }

        public static bool IsAtPeriod
        {
            get { return counter % period == 0; }
        }

        public static void LogBuffered(string line)
        {
            FileLog.LogBuffered(line);
        }

        public static void LogBuffered(List<string> lines)
        {
            FileLog.LogBuffered(lines);
        }

        public static void LogDragModelBuffered(List<string> lines)
        {
            dragModelLog.AddRange(lines);
        }

        public static void ClearDragModelBuffer()
        {
            dragModelLog.Clear();
        }

        public static void FLushBuffer(bool withDragModel = false)
        {
            if (withDragModel)
                FileLog.LogBuffered(dragModelLog);
            FileLog.FlushBuffer();
            dragModelLog.Clear();
        }
    }
}
#endif
