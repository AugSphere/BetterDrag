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
        public float overflowOffset = 0.0f;
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
            var instance = miscDataCache.GetValue(rigidbody.gameObject);
            var downPointWorld = rigidbody.transform.TransformPoint(Vector3.down * 100);
            var keelPointWorld = rigidbody.ClosestPointOnBounds(downPointWorld);
            var keelPoint = rigidbody.transform.InverseTransformPoint(keelPointWorld);

            instance.draftOffset = boatProbes._forcePoints[0]._offsetPosition.y - keelPoint.y;

#if DEBUG
            Debug.LogBuffered($"{rigidbody.name}: set draft offset to {instance.draftOffset}");
#endif
        }
    }
}
