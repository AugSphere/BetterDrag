using Crest;
using UnityEngine;

namespace BetterDrag.Utilities;

using Hydrostatics = Hydrostatics.Hydrostatics;

internal sealed class KrakenGuard
{
    private bool _wasSleeping;

    internal void FixAfterSleep(
        Rigidbody rigidBody,
        Vector3[] queryPoints,
        Vector3[] queryDisplacements
    )
    {
        if (GameState.sleeping)
        {
            _wasSleeping = true;
            return;
        }
        if (!_wasSleeping)
        {
            return;
        }
        float seaLevel = OceanRenderer.Instance.SeaLevel;
        uint midProbeIdx = Hydrostatics.ProbeCount / 2;
        float waterHeightSample =
            seaLevel + queryDisplacements[midProbeIdx].y - queryPoints[midProbeIdx].y;
        rigidBody.position += Vector3.up * waterHeightSample;
        _wasSleeping = false;
#if DEBUG
        BetterDragDebug.LogLineBuffered($"{rigidBody.name}: set vertical position after sleep");
#endif
    }
}
