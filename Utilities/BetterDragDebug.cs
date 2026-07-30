#if DEBUG
using System;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetterDrag.Utilities;

internal static class BetterDragDebug
{
    private static bool s_isOnFirstRun = true;
    private static uint s_counter = 1;
    private static readonly Dictionary<string, float> CsvBuffer = [];

    internal enum Mode
    {
        Line,
        CSV,
    }

    public static void FinishUpdate()
    {
        ++s_counter;
        FileLog.SetBuffer([]);
        CsvBuffer.Clear();
    }

    public static bool IsAtPeriod => s_counter % Plugin.DebugPrintPeriod!.Value == 0;

    public static void LogLineBuffered(string line)
    {
        FileLog.LogBuffered(line);
    }

    public static void LogLinesBuffered(List<string> lines)
    {
        FileLog.LogBuffered(lines);
    }

    public static void LogCSVBuffered(IEnumerable<(string, float)> entries)
    {
        foreach (var (text, value) in entries)
        {
            CsvBuffer[text] = value;
        }
    }

    public static void LogVectorComponents(
        List<(string, float)> csvItems,
        string prefix,
        int idx,
        Vector3 vector
    )
    {
        csvItems.Add(($"{prefix}_x_p{idx}", vector.x));
        csvItems.Add(($"{prefix}_y_p{idx}", vector.y));
        csvItems.Add(($"{prefix}_z_p{idx}", vector.z));
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
        if (s_isOnFirstRun)
        {
            FileLog.Log(CsvBuffer.Keys.Join(delimiter: ";"));
            s_isOnFirstRun = false;
        }
        FileLog.Log(
            CsvBuffer.Values.Join((n) => n.ToString(CultureInfo.InvariantCulture), delimiter: ";")
        );
        CsvBuffer.Clear();
    }
}

internal sealed class DebugSphereRenderer
{
    private static readonly Vector3[] UnitSphere = MakeUnitSphere(16);
    private readonly GameObject _gameObject;
    private readonly LineRenderer _lineRenderer;
    private readonly PositionUpdater _positionUpdater;

    internal DebugSphereRenderer(
        Rigidbody rigidbody,
        Vector3 origin,
        Color? color = null,
        float radius = 0.5f,
        float debugLineSize = 0.1f,
        bool relativeToCoM = false
    )
    {
        _gameObject = new GameObject(nameof(DebugSphereRenderer) + "(" + rigidbody.name + ")");
        _lineRenderer = _gameObject.AddComponent<LineRenderer>();
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = color ?? Color.magenta;
        _lineRenderer.endColor = color ?? Color.magenta;
        _lineRenderer.startWidth = debugLineSize;
        _lineRenderer.endWidth = debugLineSize;
        _lineRenderer.positionCount = UnitSphere.Length;
        _positionUpdater = _gameObject.AddComponent<PositionUpdater>();
        _positionUpdater.LineRenderer = _lineRenderer;
        _positionUpdater.Radius = radius;
        _positionUpdater.Origin = origin;
        _positionUpdater.Rigidbody = rigidbody;
        _positionUpdater.RelativeToCoM = relativeToCoM;
    }

    internal void SetColor(Color color)
    {
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
    }

    private sealed class PositionUpdater : MonoBehaviour
    {
        public Rigidbody? Rigidbody;
        public float Radius;
        public LineRenderer? LineRenderer;
        public Vector3 Origin;
        public bool RelativeToCoM;

        internal void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        internal void Update()
        {
            if (Rigidbody is null)
            {
                return;
            }

            var originInWorld = Rigidbody.transform.TransformPoint(
                Origin + ((RelativeToCoM ? 1f : 0f) * Rigidbody.centerOfMass)
            );
            SetSpherePositions(originInWorld);
        }

        private void SetSpherePositions(Vector3 originInWorld)
        {
            if (LineRenderer is null)
            {
                return;
            }

            Vector3[] vertices = new Vector3[UnitSphere.Length];
            for (int idx = 0; idx < UnitSphere.Length; ++idx)
            {
                vertices[idx] = originInWorld + (Radius * UnitSphere[idx]);
            }
            LineRenderer.SetPositions(vertices);
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
            vertices[(0 * len) + i] = new(c, s, 0);
            vertices[(1 * len) + i] = new(0, c, s);
            vertices[(2 * len) + i] = new(s, 0, c);
        }
        return vertices;
    }
}

internal sealed class DebugVectorRenderer
{
    private readonly GLLineRenderer _glRenderer;

    internal DebugVectorRenderer(
        Rigidbody rigidBody,
        Vector3 localOrigin,
        Vector3 worldDirection,
        Color color
    )
    {
        _glRenderer = Camera.main.gameObject.AddComponent<GLLineRenderer>();
        _glRenderer.RigidBody = rigidBody;
        _glRenderer.LocalOrigin = localOrigin;
        _glRenderer.WorldDirection = worldDirection;
        _glRenderer.Color = color;
    }

    internal void SetMagnitude(float magnitude)
    {
        _glRenderer.Magnitude = magnitude;
    }

    internal void SetDirection(Vector3 worldDirection)
    {
        _glRenderer.WorldDirection = worldDirection;
    }

    private sealed class GLLineRenderer : MonoBehaviour
    {
        public Rigidbody? RigidBody;
        public Material? LineMaterial;
        public Vector3 LocalOrigin;
        public Vector3 WorldDirection;
        public Color Color;
        public float Magnitude;

        internal void OnPostRender()
        {
            CreateLineMaterial();
            if (RigidBody is null || LineMaterial is null)
            {
                return;
            }

            _ = LineMaterial.SetPass(0);

            GL.PushMatrix();

            var originInWorld = RigidBody.transform.TransformPoint(LocalOrigin);

            GL.Begin(GL.LINES);
            GL.Color(Color);
            GL.Vertex(originInWorld);
            GL.Vertex(originInWorld + (WorldDirection * Magnitude));
            GL.End();

            GL.PopMatrix();
        }

        private void CreateLineMaterial()
        {
            if (LineMaterial is not null)
            {
                return;
            }

            LineMaterial = new(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave,
            };

            LineMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
        }
    }
}
#endif
