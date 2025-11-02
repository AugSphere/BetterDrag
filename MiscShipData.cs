using System;
using Crest;
using UnityEngine;

namespace BetterDrag
{
    internal class MiscShipData(string shipName)
    {
        private static readonly Cache<MiscShipData> miscDataCache = new(
            "MiscShipData",
            (gameObject) => new(gameObject.name)
        );
        private static readonly Vector3 globalKeelOffset = 0.1f * Vector3.up;
        private static readonly float globalOverflowOffset = -0.2f;

        public readonly string shipName = shipName;
        public ValueWithDefault baseBuoyancy = new(shipName, nameof(baseBuoyancy), 25f);
        public ValueWithDefault overflowOffset = new(shipName, nameof(overflowOffset), 10f);
        public ValueWithDefault draftOffset = new(shipName, nameof(draftOffset), 0f);

        internal struct ValueWithDefault(string shipName, string valueName, float defaultValue)
        {
            readonly string shipName = shipName;
            readonly string valueName = valueName;
            readonly float defaultValue = defaultValue;
            float? currentValue = null;

            public float Value
            {
                readonly get
                {
#if DEBUG
                    if (currentValue is null)
                    {
                        BetterDragDebug.LogLineBuffered(
                            $"{shipName}: attempting to access {valueName} before it is set, using {defaultValue}"
                        );
                    }
#endif
                    return currentValue ?? defaultValue;
                }
                set { currentValue = value; }
            }

            public readonly bool IsSet()
            {
                return currentValue is not null;
            }
        }

#if DEBUG
        public Vector3 keelPointPosition = Vector3.zero;
        public DebugSphereRenderer keelRenderer = new(shipName, Color.red);
        public DebugSphereRenderer overflowRenderer = new(shipName, Color.blue, 0.5f, 0.05f);
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
                $"valueName={this.shipName}",
                $"baseBuoyancy={this.baseBuoyancy.Value}",
                $"overflowOffset={this.overflowOffset.Value}",
                $"draftOffset={this.draftOffset.Value}"
            );
            return name + "(" + fields + ")";
        }

        internal static void CalculateDraftOffset(
            BoatProbes boatProbes,
            Rigidbody rigidbody,
            Vector3 centerOfMass
        )
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

            shipData.draftOffset.Value =
                boatProbes._forcePoints[0]._offsetPosition.y + centerOfMass.y - keelPoint.y;

#if DEBUG
            shipData.keelPointPosition = keelPoint;
            BetterDragDebug.LogLineBuffered(
                $"{rigidbody.name}: set draft offset to {shipData.draftOffset.Value} from {hitInfo.collider.name}"
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
            if (!shipData.overflowOffset.IsSet())
                shipData.overflowOffset.Value = bodyOffset + globalOverflowOffset;
            else
            {
                shipData.overflowOffset.Value = Mathf.Min(
                    shipData.overflowOffset.Value,
                    bodyOffset + globalOverflowOffset
                );
            }

#if DEBUG
            BetterDragDebug.LogLineBuffered(
                $"{rigidbody.name}: set overflow offset to {shipData.overflowOffset.Value}"
            );
#endif
        }
    }
}
