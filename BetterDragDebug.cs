#if DEBUG
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterDrag
{
    internal static class BetterDragDebug
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

        public static void LogLineBuffered(string line)
        {
            FileLog.LogBuffered(line);
        }

        public static void LogLinesBuffered(List<string> lines)
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

    internal class DebugSphereRenderer
    {
        private static readonly Vector3[] s_UnitSphere = MakeUnitSphere(16);
        private readonly GameObject gameObject;
        private readonly float debugLineSize;
        private readonly Color color;
        private readonly LineRenderer lineRenderer;

        internal DebugSphereRenderer(string name, Color color, float debugLineSize = 0.1f)
        {
            this.gameObject = new GameObject(nameof(DebugSphereRenderer) + "(" + name + ")");
            this.debugLineSize = debugLineSize;
            this.color = color;
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        public void DrawSphere(Vector3 position, float radius = 1f)
        {
            lineRenderer.startColor = this.color;
            lineRenderer.endColor = this.color;
            lineRenderer.startWidth = this.debugLineSize;
            lineRenderer.endWidth = this.debugLineSize;
            lineRenderer.positionCount = s_UnitSphere.Length;

            Vector3[] vertices = new Vector3[s_UnitSphere.Length];
            for (int idx = 0; idx < s_UnitSphere.Length; idx++)
            {
                vertices[idx] = position + radius * s_UnitSphere[idx];
            }
            lineRenderer.SetPositions(vertices);
        }

        private static Vector3[] MakeUnitSphere(int len)
        {
            Debug.Assert(len > 2);
            var vertices = new Vector3[len * 3];
            for (int i = 0; i < len; i++)
            {
                var f = i / (float)len;
                float c = Mathf.Cos(f * (float)(Math.PI * 2.0));
                float s = Mathf.Sin(f * (float)(Math.PI * 2.0));
                vertices[0 * len + i] = new(c, s, 0);
                vertices[1 * len + i] = new(0, c, s);
                vertices[2 * len + i] = new(s, 0, c);
            }
            return vertices;
        }
    }
}
#endif
