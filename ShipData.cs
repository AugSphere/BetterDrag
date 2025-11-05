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
            (gameObject) => new(gameObject)
        );

        public readonly string shipName = shipGameObject.name;
        public readonly FinalShipDragPerformanceData dragData =
            ShipDragDataStore.GetPerformanceData(shipGameObject);
        private float baseBuoyancy = 25f;
        private float overflowOffset = 5f;
        private float centerOfMassHeight = 0f;
        private float draftOffset = 0f;
        private float keelOffset = 1f;
        private float lengthAtWaterline = 15f;
        private bool valuesSet = false;

#if DEBUG
        public Vector3 keelPointPosition = Vector3.zero;
        public Vector3 bowPointPosition = Vector3.zero;
        public Vector3 sternPointPosition = Vector3.zero;

        public DebugSphereRenderer keelRenderer = new(shipGameObject.name, Color.red);
        public DebugSphereRenderer overflowRenderer = new(
            shipGameObject.name,
            Color.blue,
            0.5f,
            0.05f
        );
        public DebugSphereRenderer bowRenderer = new(shipGameObject.name, Color.green);
        public DebugSphereRenderer sternRenderer = new(shipGameObject.name, Color.green);

        public void DrawAll(Transform transform)
        {
            keelRenderer.DrawSphere(transform.TransformPoint(this.keelPointPosition));
            overflowRenderer.DrawSphere(transform.TransformPoint(overflowOffset * Vector3.up));
            bowRenderer.DrawSphere(transform.TransformPoint(this.bowPointPosition));
            sternRenderer.DrawSphere(transform.TransformPoint(this.sternPointPosition));
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

        public void SetCenterOfMassHeight(float centerOfMassHeight)
        {
            this.centerOfMassHeight = centerOfMassHeight;
        }

        public void SetBaseBuoyancy(float baseBuoyancy)
        {
            this.baseBuoyancy = baseBuoyancy;
        }

        public override string ToString()
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

            if (!SphereCastToHull(originPoint, targetPoint, rigidbody, out var hitInfo))
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

#if DEBUG
            this.keelPointPosition = keelPoint;
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

            var isBowHit = SphereCastToHull(bowOriginPoint, targetPoint, rigidbody, out var bowHit);
            var isSternHit = SphereCastToHull(
                sternOriginPoint,
                targetPoint,
                rigidbody,
                out var sternHit
            );

            if (!isBowHit || !isSternHit)
            {
#if DEBUG
                BetterDragDebug.LogLineBuffered($"{rigidbody.name}: LWL raycast failed");
#endif
                return;
            }
            var bowPointPosition = transform.InverseTransformPoint(bowHit.point);
            var sternPointPosition = transform.InverseTransformPoint(sternHit.point);
            this.lengthAtWaterline = (bowPointPosition - sternPointPosition).magnitude;
#if DEBUG
            this.bowPointPosition = bowPointPosition;
            this.sternPointPosition = sternPointPosition;
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
