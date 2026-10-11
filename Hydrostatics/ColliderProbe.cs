using System;
using System.Collections.Generic;
using BetterDrag.Utilities;
using UnityEngine;

namespace BetterDrag.Hydrostatics;

internal sealed class ColldierProbe
{
    private Vector3 _lastProbeTarget;
    private readonly List<DebugSphereRenderer> _colliderPoints = [];

    internal void Probe(Rigidbody rigidbody)
    {
        Vector3 newProbe = new(0, Plugin.ColliderProbeY!.Value, Plugin.ColliderProbeZ!.Value);
        if (newProbe == _lastProbeTarget)
        {
            return;
        }

        _lastProbeTarget = newProbe;
        _colliderPoints.Clear();

        Vector3 castOrigin = newProbe + (Vector3.right * GeometryQueries.DefaultOriginOffset);

        var targetPointWorld = rigidbody.transform.TransformPoint(newProbe);
        var originPointWorld = rigidbody.transform.TransformPoint(castOrigin);

        var allHits = UnityEngine.Physics.SphereCastAll(
            originPointWorld,
            GeometryQueries.DefaultRadius,
            targetPointWorld - originPointWorld,
            GeometryQueries.DefaultOriginOffset,
            layerMask: -1
        );

        int idx = 0;
        foreach (var hitInfo in allHits)
        {
            if (
                hitInfo.collider.name.Equals(
                    "embark_col (this)",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                continue;
            }

            var hitPoint = rigidbody.transform.InverseTransformPoint(hitInfo.point);
            var colorIdx = idx % BetterDragDebug.ColorList.Length;
            var color = BetterDragDebug.ColorList[colorIdx];
            _colliderPoints.Add(new(rigidbody, hitPoint, radius: 0.1f, color: color));
            BetterDragDebug.LogLineBuffered(
                $"{rigidbody.name} {idx}: {hitInfo.collider.name} {hitInfo.collider.GetType().FullName} layer {hitInfo.collider.gameObject.layer}, {color}"
            );
            ++idx;
        }
    }
}
