using System;
using UnityEngine;

namespace BetterDrag
{
    internal static class GeometryQueries
    {
        internal static readonly float defaultRadius = 0.1f;
        internal static readonly float defaultOriginOffset = 100f;
        static readonly int layerMask = LayerMask.GetMask(
            "Ignore Raycast", // CapsuleCollider layer
            "OnlyPlayerCol+Paintable" // hull player collider layer
        );

        internal static bool SphereCastToHull(
            Vector3 originPointBody,
            Vector3 targetPointBody,
            Rigidbody rigidbody,
            out RaycastHit hitInfo,
            float? radius = null,
            float? maxDistance = null
        )
        {
            var targetPointWorld = rigidbody.transform.TransformPoint(targetPointBody);
            var originPointWorld = rigidbody.transform.TransformPoint(originPointBody);

            var allHits = Physics.SphereCastAll(
                originPointWorld,
                radius ?? GeometryQueries.defaultRadius,
                targetPointWorld - originPointWorld,
                maxDistance: maxDistance ?? GeometryQueries.defaultOriginOffset,
                layerMask: GeometryQueries.layerMask
            );

            var shipObject = rigidbody.gameObject;
            var isHullHit = GetFirstHit(
                allHits,
                (hit) => IsClenableColliderOfShip(hit.collider, shipObject),
                out hitInfo
            );
            if (isHullHit)
                return true;
            var isCapsuleHit = GetFirstHit(
                allHits,
                (hit) => IsCapsuleColliderOfShip(hit.collider, shipObject),
                out hitInfo
            );
            return isCapsuleHit;
        }

        static bool GetFirstHit(
            RaycastHit[] hits,
            Func<RaycastHit, bool> filter,
            out RaycastHit hitInfo
        )
        {
            bool isHit = false;
            float minDistance = float.MaxValue;
            hitInfo = default;
            foreach (var hit in hits)
            {
                if (!filter(hit))
                    continue;
                isHit = true;
                if (hit.distance < minDistance)
                {
                    minDistance = hit.distance;
                    hitInfo = hit;
                }
            }
#if DEBUG
            if (isHit)
            {
                BetterDragDebug.LogLineBuffered(
                    $"First hit on {hitInfo.collider.name} {hitInfo.collider.GetType().FullName} layer {hitInfo.collider.gameObject.layer}"
                );
            }
#endif
            return isHit;
        }

        static bool IsClenableColliderOfShip(Collider collider, GameObject shipObject)
        {
            var cleanable = collider.gameObject.GetComponent<CleanableObjectCollider>();
            if (cleanable is null)
                return false;
            var transform = cleanable.parentCleanable.transform;
            while (transform is not null)
            {
                if (ReferenceEquals(transform.gameObject, shipObject))
                    return true;
                transform = transform.parent;
            }
            return false;
        }

        static bool IsCapsuleColliderOfShip(Collider collider, GameObject shipObject)
        {
            if (!typeof(CapsuleCollider).IsInstanceOfType(collider))
                return false;
            return ReferenceEquals(collider.attachedRigidbody.gameObject, shipObject);
        }
    }
}
