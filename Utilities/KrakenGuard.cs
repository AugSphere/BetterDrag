using Crest;
using UnityEngine;

namespace BetterDrag.Utilities;

using Hydrostatics = Hydrostatics.Hydrostatics;

internal sealed class KrakenGuard
{
    private bool _wasSleeping;

    internal void ApplySleepFixes(
        Rigidbody rigidBody,
        Vector3[] queryPoints,
        Vector3[] queryDisplacements
    )
    {
        if (GameState.sleeping)
        {
            if (!_wasSleeping)
            {
                FreezeItems(rigidBody);
                SetSailCollisions(rigidBody, false);
            }
            _wasSleeping = true;
            return;
        }
        if (!_wasSleeping)
        {
            return;
        }
        MoveShipToWaterSurface(rigidBody, queryPoints, queryDisplacements);
        UnfreezeItems(rigidBody);
        SetSailCollisions(rigidBody, true);
        _wasSleeping = false;
    }

    private static void MoveShipToWaterSurface(
        Rigidbody rigidBody,
        Vector3[] queryPoints,
        Vector3[] queryDisplacements
    )
    {
        float seaLevel = OceanRenderer.Instance.SeaLevel;
        uint midProbeIdx = Hydrostatics.ProbeCount / 2;
        float waterHeightSample =
            seaLevel + queryDisplacements[midProbeIdx].y - queryPoints[midProbeIdx].y;
        rigidBody.position += Vector3.up * waterHeightSample;
#if DEBUG
        BetterDragDebug.LogLineBuffered($"{rigidBody.name}: set vertical position after sleep");
#endif
    }

    private static void SetSailCollisions(Rigidbody rigidBody, bool value)
    {
        var sails = rigidBody.gameObject.GetComponentsInChildren<Sail>();
        foreach (var sail in sails)
        {
            var sailRigidBody = sail.GetComponent<Rigidbody>();
            sailRigidBody.detectCollisions = value;
        }
#if DEBUG
        BetterDragDebug.LogLineBuffered(
            $"{rigidBody.name}: set {sails.Length} sail colliders to {value}"
        );
#endif
    }

    private static void FreezeItems(Rigidbody rigidBody)
    {
        var shipItems = rigidBody.gameObject.GetComponentsInChildren<ShipItem>();
        foreach (var shipItem in shipItems)
        {
            shipItem.FreezeItem();
        }
#if DEBUG
        BetterDragDebug.LogLineBuffered($"{rigidBody.name}: froze {shipItems.Length} items");
#endif
    }

    private static void UnfreezeItems(Rigidbody rigidBody)
    {
        var shipItems = rigidBody.gameObject.GetComponentsInChildren<ShipItem>();
        foreach (var shipItem in shipItems)
        {
            UnfreezeItem(shipItem);
        }
#if DEBUG
        BetterDragDebug.LogLineBuffered($"{rigidBody.name}: unfroze {shipItems.Length} items");
#endif
    }

    private static void UnfreezeItem(ShipItem item)
    {
        var itemRigidbody = item.GetItemRigidbody();
        itemRigidbody.gameObject.SetActive(value: true);
        item.GetComponent<Collider>().enabled = true;
    }
}
