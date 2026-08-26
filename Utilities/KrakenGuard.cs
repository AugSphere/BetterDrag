using System.Collections;
using Crest;
using UnityEngine;

namespace BetterDrag.Utilities;

using Hydrostatics = Hydrostatics.Hydrostatics;

internal sealed class KrakenGuard(Rigidbody rigidBody)
{
    private bool _wasSleeping;

    internal void ApplySleepFixes(Vector3[] queryPoints, Vector3[] queryDisplacements)
    {
        if (GameState.sleeping)
        {
            if (!_wasSleeping)
            {
                Sleep.instance.StartCoroutine(OnSleep(rigidBody, queryPoints, queryDisplacements));
            }
            _wasSleeping = true;
            return;
        }
        if (!_wasSleeping)
        {
            return;
        }
        var velocity = rigidBody.velocity;
        var currentEuler = rigidBody.rotation.eulerAngles;
        rigidBody.isKinematic = true;
        UnfreezeItems(rigidBody);
        SetSailCollisions(rigidBody, true);
        MoveShipToWaterSurface(rigidBody, queryPoints, queryDisplacements, velocity, currentEuler);
        rigidBody.isKinematic = false;
        rigidBody.velocity = velocity;
        rigidBody.angularVelocity = Vector3.zero;
        _wasSleeping = false;
    }

    private static IEnumerator OnSleep(
        Rigidbody rigidBody,
        Vector3[] queryPoints,
        Vector3[] queryDisplacements
    )
    {
        yield return new WaitForSeconds(3f);
        var velocity = rigidBody.velocity;
        var currentEuler = rigidBody.rotation.eulerAngles;
        rigidBody.isKinematic = true;
        FreezeItems(rigidBody);
        SetSailCollisions(rigidBody, false);
        MoveShipToWaterSurface(rigidBody, queryPoints, queryDisplacements, velocity, currentEuler);
        rigidBody.isKinematic = false;
        rigidBody.velocity = velocity;
        rigidBody.angularVelocity = Vector3.zero;
    }

    private static void MoveShipToWaterSurface(
        Rigidbody rigidBody,
        Vector3[] queryPoints,
        Vector3[] queryDisplacements,
        Vector3 velocity,
        Vector3 currentEuler
    )
    {
        float seaLevel = OceanRenderer.Instance.SeaLevel;
        uint midProbeIdx = Hydrostatics.ProbeCount / 2;
        float waterHeightSample =
            seaLevel + queryDisplacements[midProbeIdx].y - queryPoints[midProbeIdx].y;
        rigidBody.position += Vector3.up * waterHeightSample;
        rigidBody.rotation = Quaternion.Euler(0, currentEuler.y, currentEuler.z);
#if DEBUG
        BetterDragDebug.LogLineBuffered($"{rigidBody.name}: moved to water surface");
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
