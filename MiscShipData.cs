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

        internal void CalculateDraftOffset(BoatProbes boatProbes, Rigidbody rigidbody)
        {
            var downPoint = rigidbody.transform.TransformPoint(Vector3.down * 100);

            float sqrDistance = float.MaxValue;
            Vector3 nearest = Vector3.zeroVector;

            foreach (
                var meshCollider in rigidbody.GetComponentsInChildren<MeshCollider>(
                    includeInactive: false
                )
            )
            {
                if (!meshCollider.name.ToLower().Contains("hull"))
                    continue;

                var meshTransform = meshCollider.transform;
                foreach (Vector3 vertex in meshCollider.sharedMesh.vertices)
                {
                    var worldVertex = meshTransform.TransformPoint(vertex);
                    var testDistance = (downPoint - worldVertex).sqrMagnitude;
                    if (testDistance < sqrDistance)
                    {
                        sqrDistance = testDistance;
                        nearest = worldVertex;
                    }
                }
            }
            nearest = rigidbody.transform.InverseTransformPoint(nearest);
            this.draftOffset = boatProbes._forcePoints[0]._offsetPosition.y - nearest.y;

#if DEBUG
            if (sqrDistance < float.MaxValue)
            {
                Debug.LogBuffered(
                    $"{boatProbes.gameObject.name}: set draft offset to {this.draftOffset}"
                );
            }
            else
            {
                Debug.LogBuffered(
                    $"{boatProbes.gameObject.name}: failed to calculate draft offset"
                );
            }
#endif
        }
    }
}
