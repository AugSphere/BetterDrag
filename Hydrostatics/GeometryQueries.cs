using System;

using UnityEngine;

namespace BetterDrag.Hydrostatics
{
    internal static class GeometryQueries
    {
        internal const float DefaultRadius = 0.1f;
        internal const float DefaultOriginOffset = 100f;
        private static readonly int LayerMask = UnityEngine.LayerMask.GetMask(
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

            var allHits = UnityEngine.Physics.SphereCastAll(
                originPointWorld,
                radius ?? DefaultRadius,
                targetPointWorld - originPointWorld,
                maxDistance: maxDistance ?? DefaultOriginOffset,
                layerMask: layerMask ?? LayerMask
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

        private static bool GetFirstMatchingHit(
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
                {
                    return true;
                }
            }
            hitInfo = new();
            return false;
        }

        private static bool GetFirstHitWithFilter(
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
                {
                    continue;
                }

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

        private static bool IsCleanableColliderOfShip(Collider collider, GameObject shipObject)
        {
            var cleanable = collider.gameObject.GetComponent<CleanableObjectCollider>();
            if (cleanable is null || cleanable.parentCleanable is null)
            {
                return false;
            }

            var transform = cleanable.parentCleanable.transform;
            while (transform is not null)
            {
                if (ReferenceEquals(transform.gameObject, shipObject))
                {
                    return true;
                }

                transform = transform.parent;
            }
            return false;
        }

        private static bool IsInnerEmbarkColliderOfShip(Collider collider, GameObject shipObject)
        {
            return typeof(MeshCollider).IsInstanceOfType(collider)
                && collider.attachedRigidbody is not null
                && collider.name.Equals("embark_col (this)", StringComparison.OrdinalIgnoreCase)
                && ReferenceEquals(collider.attachedRigidbody.gameObject, shipObject);
        }

        private static bool IsInteriorTriggerColliderOfShip(Collider collider, GameObject shipObject)
        {
            return typeof(MeshCollider).IsInstanceOfType(collider)
                && collider.attachedRigidbody is not null
                && collider.name.Contains("interior")
                && ReferenceEquals(collider.attachedRigidbody.gameObject, shipObject);
        }
    }
}