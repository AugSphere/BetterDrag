using System;
using UnityEngine;

namespace BetterDrag
{
    internal static class GeometryQueries
    {
        internal const float defaultRadius = 0.1f;
        internal const float defaultOriginOffset = 100f;
        static readonly int layerMask = LayerMask.GetMask(
            "Ignore Raycast", // embark and interior layer
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
            var targetPointWorld = rigidbody.transform.TransformPoint(targetPointBody);
            var originPointWorld = rigidbody.transform.TransformPoint(originPointBody);

            var allHits = Physics.SphereCastAll(
                originPointWorld,
                radius ?? GeometryQueries.defaultRadius,
                targetPointWorld - originPointWorld,
                maxDistance: maxDistance ?? GeometryQueries.defaultOriginOffset,
                layerMask: layerMask ?? GeometryQueries.layerMask
            );

            return GetFirstMatchingHit(
                allHits,
                rigidbody.gameObject,
                out hitInfo,
                IsCleanableColliderOfShip,
                IsInnerEmbarkColliderOfShip,
                IsInteriorTriggerColliderOfShip
            );
        }

        static bool GetFirstMatchingHit(
            RaycastHit[] allHits,
            GameObject shipObject,
            out RaycastHit hitInfo,
            params Func<Collider, GameObject, bool>[] filters
        )
        {
            foreach (var filter in filters)
            {
                if (
                    GetFirstHitWithFilter(
                        allHits,
                        (hit) => filter(hit.collider, shipObject),
                        out hitInfo
                    )
                )
                    return true;
            }
            hitInfo = new();
            return false;
        }

        static bool GetFirstHitWithFilter(
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

        static bool IsCleanableColliderOfShip(Collider collider, GameObject shipObject)
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

        static bool IsInnerEmbarkColliderOfShip(Collider collider, GameObject shipObject)
        {
            if (!typeof(MeshCollider).IsInstanceOfType(collider))
                return false;
            if (collider.attachedRigidbody is null)
                return false;
            if (!collider.name.Equals("embark_col (this)", StringComparison.OrdinalIgnoreCase))
                return false;
            return ReferenceEquals(collider.attachedRigidbody.gameObject, shipObject);
        }

        static bool IsInteriorTriggerColliderOfShip(Collider collider, GameObject shipObject)
        {
            if (!typeof(MeshCollider).IsInstanceOfType(collider))
                return false;
            if (collider.attachedRigidbody is null)
                return false;
            if (!collider.name.Contains("interior"))
                return false;
            return ReferenceEquals(collider.attachedRigidbody.gameObject, shipObject);
        }
    }
}
