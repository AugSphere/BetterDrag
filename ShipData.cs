using System;
using Crest;
using UnityEngine;
using static BetterDrag.GeometryQueries;
#if DEBUG
using System.Collections.Generic;
#endif

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
        private float overflowOffset = 5f;
        private float centerOfMassHeight;
        private float draftOffset;
        private float keelOffset = 1f;
        private float lengthAtWaterline = 15f;
        private Vector3 keelPointPosition = Vector3.zero;
        private Vector3 bowPointPosition = Vector3.zero;
        private Vector3 sternPointPosition = Vector3.zero;
        private bool valuesSet;

#if DEBUG
        public DebugSphereRenderer keelRenderer = new(color: Color.red);
        public DebugSphereRenderer overflowRenderer = new(color: Color.blue);
        public DebugSphereRenderer bowRenderer = new(color: Color.green);
        public DebugSphereRenderer sternRenderer = new(color: Color.green);
        public List<(DebugSphereRenderer renderer, Vector3 position)> sideRenderers = [];

        public void DrawAll(Transform transform, bool drawHullPoints = false)
        {
            keelRenderer.DrawSphere(transform.TransformPoint(this.keelPointPosition));
            overflowRenderer.DrawSphere(transform.TransformPoint(overflowOffset * Vector3.up));
            bowRenderer.DrawSphere(transform.TransformPoint(this.bowPointPosition));
            sternRenderer.DrawSphere(transform.TransformPoint(this.sternPointPosition));
            if (drawHullPoints)
                hydrostatics.DrawHullPoints(transform);
        }
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
            float lengthAtWaterline
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
                this.lengthAtWaterline
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
                $"draftOffset={this.draftOffset}"
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

#if DEBUG
            BetterDragDebug.LogLinesBuffered(
                [
                    $"{rigidbody.name}: set keel height to {this.keelOffset}",
                    $"{rigidbody.name}: set draft offset to {this.draftOffset} from {hitInfo.collider.name}",
                ]
            );
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
#endif
        }

        internal static void CalculateOverflowOffset(WaveSplashZone splashZone)
        {
            var rigidbody = splashZone.GetComponentInParent<Rigidbody>();
            var shipData = ShipData.GetShipData(rigidbody.gameObject);
            var worldOverflowPoint =
                splashZone.transform.position
                + splashZone.transform.TransformDirection(Vector3.up) * splashZone.verticalOffset;
            var bodyOffset = rigidbody.transform.InverseTransformPoint(worldOverflowPoint).y;

            shipData.overflowOffset = Mathf.Min(shipData.overflowOffset, bodyOffset);

#if DEBUG
            BetterDragDebug.LogLineBuffered(
                $"{rigidbody.name}: set overflow offset to {shipData.overflowOffset}"
            );
#endif
        }
    }
}
