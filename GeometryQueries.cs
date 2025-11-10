using System;
using UnityEngine;

namespace BetterDrag
{
    internal static class GeometryQueries
    {
        internal const float defaultRadius = 0.1f;
        internal const float defaultOriginOffset = 100f;
        static readonly int layerMask = LayerMask.GetMask(
            "Ignore Raycast", // CapsuleCollider layer
            "OnlyPlayerCol+Paintable" // hull player collider layer
        );

        internal static bool GetFirstHullHit(
            Vector3 originPointBody,
            Vector3 targetPointBody,
            Rigidbody rigidbody,
            out RaycastHit hitInfo,
            float? radius = null,
            float? maxDistance = null,
            int? layerMask = null
        )
        {
            var allHits = SphereCastToHull(
                originPointBody,
                targetPointBody,
                rigidbody,
                radius,
                maxDistance,
                layerMask
            );
            return FilterHullColliders(allHits, rigidbody.gameObject, out hitInfo);
        }

        internal static RaycastHit[] SphereCastToHull(
            Vector3 originPointBody,
            Vector3 targetPointBody,
            Rigidbody rigidbody,
            float? radius = null,
            float? maxDistance = null,
            int? layerMask = null
        )
        {
            var targetPointWorld = rigidbody.transform.TransformPoint(targetPointBody);
            var originPointWorld = rigidbody.transform.TransformPoint(originPointBody);

            return Physics.SphereCastAll(
                originPointWorld,
                radius ?? GeometryQueries.defaultRadius,
                targetPointWorld - originPointWorld,
                maxDistance: maxDistance ?? GeometryQueries.defaultOriginOffset,
                layerMask: layerMask ?? GeometryQueries.layerMask
            );
        }

        internal static bool FilterHullColliders(
            RaycastHit[] hits,
            GameObject shipObject,
            out RaycastHit hitInfo
        )
        {
            return FilterFirstHitsInOrder(
                hits,
                out hitInfo,
                (hit) => IsClenableColliderOfShip(hit.collider, shipObject),
                (hit) => IsCapsuleColliderOfShip(hit.collider, shipObject)
            );
        }

        static bool FilterFirstHitsInOrder(
            RaycastHit[] hits,
            out RaycastHit hitInfo,
            params Func<RaycastHit, bool>[] filters
        )
        {
            hitInfo = default;
            foreach (var filter in filters)
            {
                if (GetFirstHit(hits, filter, out hitInfo))
                    return true;
            }
            return false;
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
#if DEBUG && VERBOSE
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

        static bool IsMeshColliderOfShip(Collider collider, GameObject shipObject)
        {
            if (!typeof(MeshCollider).IsInstanceOfType(collider))
                return false;
            return ReferenceEquals(collider.attachedRigidbody.gameObject, shipObject);
        }
    }
}
