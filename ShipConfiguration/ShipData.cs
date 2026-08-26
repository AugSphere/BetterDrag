using BetterDrag.Utilities;
using Crest;
using UnityEngine;
using static BetterDrag.Hydrostatics.GeometryQueries;
#if DEBUG
using System.Collections.Generic;
#endif

namespace BetterDrag.ShipConfiguration;

using Hydrostatics = Hydrostatics.Hydrostatics;

internal sealed class ShipData(GameObject shipGameObject)
{
    private static readonly Cache<ShipData> DataCache = new(
        "ShipData",
        static (gameObject) => new(gameObject)
    );

    private readonly string _shipName = shipGameObject.name;
    private readonly Rigidbody _rigidBody = shipGameObject.GetComponent<Rigidbody>();
    internal readonly ShipDragPerformanceData DragData = ShipDragConfigManager.GetPerformanceData(
        shipGameObject
    );
    internal readonly ModEnableCheck ModEnableCheck = new(shipGameObject);
    internal readonly InputFilter InputFilter = new(shipGameObject.GetComponent<Rigidbody>());
    internal readonly Vector3[] RawForces = new Vector3[Hydrostatics.ProbeCount];
    internal readonly UnstickUpdateVelocity UnstickUpdateVelocity = new();
    internal readonly KrakenGuard KrakenGuard = new(shipGameObject.GetComponent<Rigidbody>());
    private Hydrostatics? _hydrostatics;
    private float _baseBuoyancy = 25f;
    private float _overflowOffset = 10f;
    private float _centerOfMassHeight;
    private float _draftOffset;
    private float _keelOffset = 1f;
    private float _lengthAtWaterline = 15f;
    private float _draftSpanRatio;
    private float _unloadedMass;
    private Vector3 _keelPointPosition;
    private Vector3 _bowPointPosition;
    private Vector3 _sternPointPosition;
    private bool _valuesSet;

#if DEBUG
    public DebugSphereRenderer? KeelRenderer;
    public DebugSphereRenderer? OverflowRenderer;
    public DebugSphereRenderer? BowRenderer;
    public DebugSphereRenderer? SternRenderer;
    public DebugSphereRenderer? ComRenderer;
    public DebugSphereRenderer? RbComRenderer;
    public List<DebugVectorRenderer> BuoyancyForceRenderers = [];
    public List<DebugVectorRenderer> DragForceRenderers = [];
    public List<DebugVectorRenderer> WaterVelocityRenderers = [];
    public List<DebugVectorRenderer> RelativeVelocityRenderers = [];
    public List<DebugVectorRenderer> OutputForceRenderers = [];
#endif

    public static ShipData GetShipData(GameObject shipGameObject)
    {
        return DataCache.GetValue(shipGameObject);
    }

    public (
        float baseBuoyancy,
        float overflowOffset,
        float draftOffset,
        float keelDepth,
        float lengthAtWaterline,
        float draftSpanRatio,
        float unloadedMass
    ) GetValues(BoatProbes boatProbes)
    {
        UnstickUpdateVelocity.Update(boatProbes);
        if (!_valuesSet)
        {
            CalculateDraftOffset(boatProbes);
            CalculateLWL();
            _hydrostatics = new(
                _shipName,
                _rigidBody,
                boatProbes,
                _bowPointPosition,
                _sternPointPosition,
                _keelPointPosition
            );
#if DEBUG
            SetupVectorRenderers(boatProbes);
            BetterDragDebug.LogLineBuffered($"{_shipName}: ship data filled");
#endif
            _valuesSet = true;
        }
        return (
            _baseBuoyancy,
            _overflowOffset,
            _draftOffset,
            _keelOffset,
            _lengthAtWaterline,
            _draftSpanRatio,
            _unloadedMass
        );
    }

    internal (float area, float displacement)? GetHydrostaticValues(int probeIdx, float draft)
    {
        return _hydrostatics?.GetValues(probeIdx, draft);
    }

    internal void SetCenterOfMass(Vector3 centerOfMass)
    {
        _centerOfMassHeight = centerOfMass.y;
#if DEBUG
        ComRenderer = new(_rigidBody, centerOfMass, Color.cyan, 0.6f);
        RbComRenderer = new(_rigidBody, Vector3.zero, Color.white, 2f, 0.4f, relativeToCoM: true);
#endif
    }

    internal void SetBaseBuoyancy(float baseBuoyancy)
    {
        _baseBuoyancy = baseBuoyancy;
    }

    internal void SetUnloadedMass(float mass)
    {
        _unloadedMass = mass;
    }

    public sealed override string ToString()
    {
        var name = nameof(ShipData);
        var fields = string.Join(
            ", ",
            $"valueName={_shipName}",
            $"baseBuoyancy={_baseBuoyancy}",
            $"overflowOffset={_overflowOffset}",
            $"draftOffset={_draftOffset}",
            $"keelOffset={_keelOffset}",
            $"lengthAtWaterline={_lengthAtWaterline}",
            $"draftSpanRatio={_draftSpanRatio}"
        );
        return name + "(" + fields + ")";
    }

    private void CalculateDraftOffset(BoatProbes boatProbes)
    {
        var originPoint = Vector3.down * DefaultOriginOffset;
        var targetPoint = Vector3.zero;

        if (!GetFirstHullHit(originPoint, targetPoint, _rigidBody, out var hitInfo))
        {
#if DEBUG
            BetterDragDebug.LogLineBuffered($"{_rigidBody.name}: keel cast failed");
#endif
            _draftSpanRatio = 1;
            return;
        }
        var keelPoint = _rigidBody.transform.InverseTransformPoint(hitInfo.point);

        var draftOffset =
            boatProbes._forcePoints[0]._offsetPosition.y + _centerOfMassHeight - keelPoint.y;
        _draftOffset = Mathf.Clamp(draftOffset, -1f, 15f);
        _keelOffset = Mathf.Clamp(-keelPoint.y, 0, 20f);
        _keelPointPosition = keelPoint;
        var originalDraftSpan = _keelOffset - draftOffset + _overflowOffset;
        var fullDraftSpan = _keelOffset + _overflowOffset;
        _draftSpanRatio = originalDraftSpan / fullDraftSpan;

#if DEBUG
        BetterDragDebug.LogLinesBuffered([
            $"{_rigidBody.name}: set keel height to {_keelOffset}",
            $"{_rigidBody.name}: set draft offset to {_draftOffset} from {hitInfo.collider.name}",
        ]);
        KeelRenderer = new(_rigidBody, keelPoint, Color.red);
#endif
    }

    private void CalculateLWL()
    {
        var fullSpan = _keelOffset + _overflowOffset;
        var lwlHeight = -_keelOffset + (0.5f * fullSpan);
        var transform = _rigidBody.transform;
        var bowOriginPoint = Vector3.forward * DefaultOriginOffset;
        var sternOriginPoint = Vector3.back * DefaultOriginOffset;
        var targetPoint = Vector3.up * lwlHeight;

        var isBowHit = GetFirstHullHit(bowOriginPoint, targetPoint, _rigidBody, out var bowHit);
        var isSternHit = GetFirstHullHit(
            sternOriginPoint,
            targetPoint,
            _rigidBody,
            out var sternHit
        );

        if (!isBowHit || !isSternHit)
        {
#if DEBUG
            BetterDragDebug.LogLineBuffered($"{_rigidBody.name}: LWL cast failed");
#endif
            return;
        }
        var bowPointPosition = transform.InverseTransformPoint(bowHit.point);
        var sternPointPosition = transform.InverseTransformPoint(sternHit.point);
        _lengthAtWaterline = (bowPointPosition - sternPointPosition).magnitude;
        _bowPointPosition = bowPointPosition;
        _sternPointPosition = sternPointPosition;
#if DEBUG

        BetterDragDebug.LogLineBuffered($"{_rigidBody.name}: calculated LWL {_lengthAtWaterline}");
        BowRenderer = new(_rigidBody, bowPointPosition, Color.green);
        SternRenderer = new(_rigidBody, sternPointPosition, Color.green);
#endif
    }

    internal void CalculateOverflowOffset(WaveSplashZone splashZone)
    {
        var worldOverflowPoint =
            splashZone.transform.position
            + (splashZone.transform.TransformDirection(Vector3.up) * splashZone.verticalOffset);
        var bodyOffset = _rigidBody.transform.InverseTransformPoint(worldOverflowPoint).y;

        _overflowOffset = Mathf.Min(_overflowOffset, bodyOffset);

#if DEBUG
        BetterDragDebug.LogLineBuffered(
            $"{_rigidBody.name}: set overflow offset to {_overflowOffset}"
        );
        OverflowRenderer = new(_rigidBody, new(0, bodyOffset, 0), Color.blue);
#endif
    }

#if DEBUG
    private void SetupVectorRenderers(BoatProbes boatProbes)
    {
        for (int idx = 0; idx < boatProbes._forcePoints.Length; ++idx)
        {
            var position =
                boatProbes._forcePoints[idx]._offsetPosition
                + new Vector3(0f, _centerOfMassHeight, 0f);
            BuoyancyForceRenderers.Add(new(_rigidBody, position, Vector3.up, Color.blue));
            DragForceRenderers.Add(new(_rigidBody, position, Vector3.zero, Color.red));
            WaterVelocityRenderers.Add(new(_rigidBody, position, Vector3.zero, Color.green));
            RelativeVelocityRenderers.Add(new(_rigidBody, position, Vector3.zero, Color.magenta));
            OutputForceRenderers.Add(new(_rigidBody, position, Vector3.zero, Color.white));
        }
    }
#endif
}
