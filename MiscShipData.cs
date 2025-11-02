using System;
using Crest;
using UnityEngine;

namespace BetterDrag
{
    internal class MiscShipData(string name)
    {
        private static readonly Cache<MiscShipData> miscDataCache = new(
            "MiscShipData",
            (gameObject) => new(gameObject.name)
        );
        private static readonly Vector3 globalKeelOffset = 0.1f * Vector3.up;
        private static readonly float globalOverflowOffset = -0.2f;

        public string name = name;
        public float baseBuoyancy = 25f;
        public float overflowOffset = 10.0f;
        public float draftOffset = 0.0f;
#if DEBUG
        public Vector3 keelPointPosition = Vector3.zero;
        public DebugSphereRenderer keelRenderer = new(name, Color.red);
        public DebugSphereRenderer overflowRenderer = new(name, Color.blue, 0.5f, 0.05f);
#endif

        public static MiscShipData GetMiscShipData(GameObject gameObject)
        {
            return miscDataCache.GetValue(gameObject);
        }

        public override string ToString()
        {
            var name = nameof(MiscShipData);
            var fields = String.Join(
                ", ",
                $"baseBuoyancy={this.baseBuoyancy}",
                $"overflowOffset={this.overflowOffset}",
                $"draftOffset={this.draftOffset}"
            );
            return name + "(" + fields + ")";
        }

        internal static void CalculateDraftOffset(BoatProbes boatProbes, Rigidbody rigidbody)
        {
            var shipData = miscDataCache.GetValue(rigidbody.gameObject);
            var downPointWorld = rigidbody.transform.TransformPoint(Vector3.down * 100);
            Physics.Raycast(
                downPointWorld,
                rigidbody.position - downPointWorld,
                out var hitInfo,
                maxDistance: float.MaxValue,
                layerMask: 1 << 2
            );
            var keelPoint =
                rigidbody.transform.InverseTransformPoint(hitInfo.point) + globalKeelOffset;

            shipData.draftOffset = boatProbes._forcePoints[0]._offsetPosition.y - keelPoint.y;

#if DEBUG
            shipData.keelPointPosition = keelPoint;
            BetterDragDebug.LogLineBuffered(
                $"{rigidbody.name}: set draft offset to {shipData.draftOffset}"
            );
#endif
        }

        internal static void CalculateOverflowOffset(WaveSplashZone splashZone)
        {
            var rigidbody = splashZone.GetComponentInParent<Rigidbody>();
            var shipData = MiscShipData.GetMiscShipData(rigidbody.gameObject);
            var worldOverflowPoint =
                splashZone.transform.position
                + splashZone.transform.TransformDirection(Vector3.up) * splashZone.verticalOffset;
            var bodyOffset = rigidbody.transform.InverseTransformPoint(worldOverflowPoint).y;
            shipData.overflowOffset = Mathf.Min(
                shipData.overflowOffset,
                bodyOffset + globalOverflowOffset
            );
#if DEBUG
            BetterDragDebug.LogLineBuffered(
                $"{rigidbody.name}: set overflow offset to {shipData.overflowOffset}"
            );
#endif
        }
    }
}
