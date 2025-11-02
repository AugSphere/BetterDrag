using System;
using Crest;
using UnityEngine;

namespace BetterDrag
{
    internal class MiscShipData
    {
        private static readonly Cache<MiscShipData> miscDataCache = new(
            "MiscShipData",
            (_) => new()
        );

        public float baseBuoyancy = 25f;
        public float overflowOffset = 10.0f;
        public float draftOffset = 0.0f;

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
            var keelPointWorld = rigidbody.ClosestPointOnBounds(downPointWorld);
            var keelPoint = rigidbody.transform.InverseTransformPoint(keelPointWorld);

            shipData.draftOffset = boatProbes._forcePoints[0]._offsetPosition.y - keelPoint.y;

#if DEBUG
            Debug.LogBuffered($"{rigidbody.name}: set draftOffset to {shipData.draftOffset}");
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
            shipData.overflowOffset = Mathf.Min(shipData.overflowOffset, bodyOffset);
#if DEBUG
            Debug.LogBuffered($"{rigidbody.name}: set overflowOffset to {shipData.overflowOffset}");
#endif
        }
    }
}
