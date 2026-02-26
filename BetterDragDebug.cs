#if DEBUG
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            ++counter;
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

        public static void LogCSVBuffered((string, float)[] entries)
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
            FileLog.Log(
                valuesBuffer.Join((n) => n.ToString(CultureInfo.InvariantCulture), delimiter: ";")
            );
            textBuffer.Clear();
            valuesBuffer.Clear();
        }
    }

    internal class DebugSphereRenderer
    {
        private static readonly Vector3[] s_UnitSphere = MakeUnitSphere(16);
        private GameObject gameObject;
        private readonly LineRenderer lineRenderer;
        private readonly PositionUpdater positionUpdater;

        internal DebugSphereRenderer(
            Rigidbody rigidbody,
            Vector3 origin,
            UnityEngine.Color? color = null,
            float radius = 0.5f,
            float debugLineSize = 0.1f,
            bool relativeToCoM = false
        )
        {
            this.gameObject = new GameObject(
                nameof(DebugSphereRenderer) + "(" + rigidbody.name + ")"
            );
            lineRenderer = this.gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color ?? UnityEngine.Color.magenta;
            lineRenderer.endColor = color ?? UnityEngine.Color.magenta;
            lineRenderer.startWidth = debugLineSize;
            lineRenderer.endWidth = debugLineSize;
            lineRenderer.positionCount = s_UnitSphere.Length;
            positionUpdater = this.gameObject.AddComponent<PositionUpdater>();
            positionUpdater.lineRenderer = lineRenderer;
            positionUpdater.radius = radius;
            positionUpdater.origin = origin;
            positionUpdater.rigidbody = rigidbody;
            positionUpdater.relativeToCoM = relativeToCoM;
        }

        internal void SetColor(Color color)
        {
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }

        private class PositionUpdater : MonoBehaviour
        {
            public Rigidbody? rigidbody;
            public float radius;
            public LineRenderer? lineRenderer;
            public Vector3 origin;
            public bool relativeToCoM;

            void Awake()
            {
                DontDestroyOnLoad(gameObject);
            }

            void Update()
            {
                if (rigidbody is null)
                    return;
                var originInWorld = rigidbody.transform.TransformPoint(
                    origin + (relativeToCoM ? 1f : 0f) * rigidbody.centerOfMass
                );
                SetSpherePositions(originInWorld);
            }

            private void SetSpherePositions(Vector3 originInWorld)
            {
                if (lineRenderer is null)
                    return;
                Vector3[] vertices = new Vector3[s_UnitSphere.Length];
                for (int idx = 0; idx < s_UnitSphere.Length; ++idx)
                {
                    vertices[idx] = originInWorld + this.radius * s_UnitSphere[idx];
                }
                lineRenderer.SetPositions(vertices);
            }
        }

        private static Vector3[] MakeUnitSphere(int len)
        {
            Debug.Assert(len > 2);
            var vertices = new Vector3[len * 3];
            for (int i = 0; i < len; ++i)
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

    internal class DebugVectorRenderer
    {
        private GameObject gameObject;
        private readonly LineRenderer lineRenderer;
        private readonly PositionUpdater positionUpdater;

        internal DebugVectorRenderer(
            Rigidbody rigidbody,
            Vector3 origin,
            Vector3 direction,
            float? debugLineSize = null
        )
        {
            this.gameObject = new GameObject(
                nameof(DebugSphereRenderer) + "(" + rigidbody.name + ")"
            );
            lineRenderer = this.gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startWidth = debugLineSize ?? 0.25f;
            lineRenderer.endWidth = debugLineSize ?? 0.25f;
            lineRenderer.positionCount = 2;
            positionUpdater = this.gameObject.AddComponent<PositionUpdater>();
            positionUpdater.lineRenderer = lineRenderer;
            positionUpdater.rigidbody = rigidbody;
            positionUpdater.origin = origin;
            positionUpdater.direction = direction;
        }

        internal void SetMagnitude(float magnitude)
        {
            this.positionUpdater.magnitude = magnitude;
        }

        private class PositionUpdater : MonoBehaviour
        {
            public Rigidbody? rigidbody;
            public LineRenderer? lineRenderer;
            public Vector3 origin;
            public Vector3 direction;
            public float magnitude;

            void Awake()
            {
                DontDestroyOnLoad(gameObject);
            }

            void Update()
            {
                if (rigidbody is null || lineRenderer is null)
                    return;
                var originInWorld = rigidbody.transform.TransformPoint(origin);
                var directionInWorld = rigidbody.transform.TransformDirection(direction);
                var color = magnitude < 0 ? UnityEngine.Color.red : UnityEngine.Color.green;
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
                lineRenderer.SetPositions(
                    [originInWorld, originInWorld + directionInWorld * magnitude]
                );
            }
        }
    }
}
#endif
