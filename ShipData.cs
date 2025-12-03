using System;
using Crest;
using UnityEngine;
using static BetterDrag.GeometryQueries;

namespace BetterDrag
{
    internal class ShipData(GameObject shipGameObject)
    {
        private static readonly Cache<ShipData> dataCache = new(
            "ShipData",
            static (gameObject) => new(gameObject)
        );

        public readonly string shipName = shipGameObject.name;
        public readonly ShipDragPerformanceData dragData = ShipDragConfigManager.GetPerformanceData(
            shipGameObject
        );
        private readonly Hydrostatics hydrostatics = new(shipGameObject.name);
        private float baseBuoyancy = 25f;
        private float overflowOffset = 10f;
        private float centerOfMassHeight;
        private float draftOffset;
        private float keelOffset = 1f;
        private float lengthAtWaterline = 15f;
        private float draftSpanRatio;
        private Vector3 keelPointPosition;
        private Vector3 bowPointPosition;
        private Vector3 sternPointPosition;
        private bool valuesSet;

#if DEBUG
        public DebugSphereRenderer? keelRenderer;
        public DebugSphereRenderer? overflowRenderer;
        public DebugSphereRenderer? bowRenderer;
        public DebugSphereRenderer? sternRenderer;
#endif

        public static ShipData GetShipData(GameObject shipGameObject)
        {
            return dataCache.GetValue(shipGameObject);
        }

        public (
            float baseBuoyancy,
            float overflowOffset,
            float draftOffset,
            float keelDepth,
            float lengthAtWaterline,
            float draftSpanRatio
        ) GetValues(BoatProbes boatProbes, Rigidbody rigidbody)
        {
            if (!this.valuesSet)
            {
                this.CalculateDraftOffset(boatProbes, rigidbody);
                this.CalculateLWL(rigidbody);
                this.hydrostatics.CastHullRays(
                    rigidbody,
                    this.bowPointPosition,
                    this.sternPointPosition,
                    this.keelPointPosition
                );
                this.hydrostatics.BuildTables();
                valuesSet = true;
            }
            return (
                this.baseBuoyancy,
                this.overflowOffset,
                this.draftOffset,
                this.keelOffset,
                this.lengthAtWaterline,
                this.draftSpanRatio
            );
        }

        internal (float area, float displacement)? GetHydrostaticValues(float draft)
        {
            return this.hydrostatics.GetValues(draft);
        }

        internal void SetCenterOfMassHeight(float centerOfMassHeight)
        {
            this.centerOfMassHeight = centerOfMassHeight;
        }

        internal void SetBaseBuoyancy(float baseBuoyancy)
        {
            this.baseBuoyancy = baseBuoyancy;
        }

        public sealed override string ToString()
        {
            var name = nameof(ShipData);
            var fields = String.Join(
                ", ",
                $"valueName={this.shipName}",
                $"baseBuoyancy={this.baseBuoyancy}",
                $"overflowOffset={this.overflowOffset}",
                $"draftOffset={this.draftOffset}",
                $"keelOffset={this.keelOffset}",
                $"lengthAtWaterline={this.lengthAtWaterline}",
                $"draftSpanRatio={this.draftSpanRatio}"
            );
            return name + "(" + fields + ")";
        }

        private void CalculateDraftOffset(BoatProbes boatProbes, Rigidbody rigidbody)
        {
            var originPoint = Vector3.down * GeometryQueries.defaultOriginOffset;
            var targetPoint = Vector3.zero;

            if (!GetFirstHullHit(originPoint, targetPoint, rigidbody, out var hitInfo))
            {
#if DEBUG
                BetterDragDebug.LogLineBuffered($"{rigidbody.name}: keel cast failed");
#endif
                return;
            }
            var keelPoint = rigidbody.transform.InverseTransformPoint(hitInfo.point);

            var draftOffset =
                boatProbes._forcePoints[0]._offsetPosition.y
                + this.centerOfMassHeight
                - keelPoint.y;
            this.draftOffset = Mathf.Clamp(draftOffset, -1f, 15f);
            this.keelOffset = Mathf.Clamp(-keelPoint.y, 0, 20f);
            this.keelPointPosition = keelPoint;
            var originalDraftSpan = keelOffset - draftOffset + this.overflowOffset;
            var fullDraftSpan = keelOffset + this.overflowOffset;
            this.draftSpanRatio = originalDraftSpan / fullDraftSpan;

#if DEBUG
            BetterDragDebug.LogLinesBuffered(
                [
                    $"{rigidbody.name}: set keel height to {this.keelOffset}",
                    $"{rigidbody.name}: set draft offset to {this.draftOffset} from {hitInfo.collider.name}",
                ]
            );
            this.keelRenderer = new(rigidbody, keelPoint, Color.red);
#endif
        }

        private void CalculateLWL(Rigidbody rigidbody)
        {
            var fullSpan = this.keelOffset + this.overflowOffset;
            var lwlHeight = -this.keelOffset + 0.5f * fullSpan;
            var transform = rigidbody.transform;
            var bowOriginPoint = Vector3.forward * GeometryQueries.defaultOriginOffset;
            var sternOriginPoint = Vector3.back * GeometryQueries.defaultOriginOffset;
            var targetPoint = Vector3.up * lwlHeight;

            var isBowHit = GetFirstHullHit(bowOriginPoint, targetPoint, rigidbody, out var bowHit);
            var isSternHit = GetFirstHullHit(
                sternOriginPoint,
                targetPoint,
                rigidbody,
                out var sternHit
            );

            if (!isBowHit || !isSternHit)
            {
#if DEBUG
                BetterDragDebug.LogLineBuffered($"{rigidbody.name}: LWL cast failed");
#endif
                return;
            }
            var bowPointPosition = transform.InverseTransformPoint(bowHit.point);
            var sternPointPosition = transform.InverseTransformPoint(sternHit.point);
            this.lengthAtWaterline = (bowPointPosition - sternPointPosition).magnitude;
            this.bowPointPosition = bowPointPosition;
            this.sternPointPosition = sternPointPosition;
#if DEBUG

            BetterDragDebug.LogLineBuffered(
                $"{rigidbody.name}: calculated LWL {this.lengthAtWaterline}"
            );
            this.bowRenderer = new(rigidbody, bowPointPosition, Color.green);
            this.sternRenderer = new(rigidbody, sternPointPosition, Color.green);
#endif
        }

        internal void CalculateOverflowOffset(Rigidbody rigidbody, WaveSplashZone splashZone)
        {
            var worldOverflowPoint =
                splashZone.transform.position
                + splashZone.transform.TransformDirection(Vector3.up) * splashZone.verticalOffset;
            var bodyOffset = rigidbody.transform.InverseTransformPoint(worldOverflowPoint).y;

            this.overflowOffset = Mathf.Min(this.overflowOffset, bodyOffset);

#if DEBUG
            BetterDragDebug.LogLineBuffered(
                $"{rigidbody.name}: set overflow offset to {this.overflowOffset}"
            );
            this.overflowRenderer = new(rigidbody, new(0, bodyOffset, 0), Color.blue);
#endif
        }
    }
}
