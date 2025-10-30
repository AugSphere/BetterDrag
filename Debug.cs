#if DEBUG
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace BetterDrag
{
    internal static class Debug
    {
        private static bool isOnFirstRun = true;
        private static uint counter = 1;
        private static readonly List<string> textBuffer = [];
        private static readonly List<float> valuesBuffer = [];

        internal enum Mode
        {
            Line,
            CSV,
        }

        public static void FinishUpdate()
        {
            counter++;
            FileLog.SetBuffer([]);
            textBuffer.Clear();
            valuesBuffer.Clear();
        }

        public static bool IsAtPeriod
        {
            get { return counter % Plugin.debugPrintPeriod!.Value == 0; }
        }

        public static void LogBuffered(string line)
        {
            FileLog.LogBuffered(line);
        }

        public static void LogBuffered(List<string> lines)
        {
            FileLog.LogBuffered(lines);
        }

        public static void LogCSVBuffered(List<(string, float)> entries)
        {
            foreach (var (text, value) in entries)
            {
                textBuffer.Add(text);
                valuesBuffer.Add(value);
            }
        }

        public static void FlushBuffer(Mode mode)
        {
            switch (mode)
            {
                case Mode.Line:
                    FileLog.FlushBuffer();
                    break;
                case Mode.CSV:
                    FlushCSVBuffer();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private static void FlushCSVBuffer()
        {
            if (isOnFirstRun)
            {
                FileLog.Log(textBuffer.Join(delimiter: ";"));
                isOnFirstRun = false;
            }
            FileLog.Log(valuesBuffer.Join((n) => n.ToString(), delimiter: ";"));
            textBuffer.Clear();
            valuesBuffer.Clear();
        }
    }
}
#endif
