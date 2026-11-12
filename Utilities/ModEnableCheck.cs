using System;
using UnityEngine;

namespace BetterDrag.Utilities;

internal sealed class ModEnableCheck(GameObject shipGameObject)
{
    private static readonly string[] DisableForShipList = ["BOAT CUTTER (212)", "DNG Cutter"];
    private readonly bool _isEnabledForShip = IsEnabledForShip(shipGameObject);
    private readonly BoatDamage _boatDamage = shipGameObject.GetComponent<BoatDamage>();

    internal bool IsModEnabled()
    {
        return _isEnabledForShip && (!GameState.sleeping || IsEnabledWhileSleeping());
    }

    private static bool IsEnabledForShip(GameObject shipGameObject)
    {
        var normalizedName = Utilities.GetNormalizedShipName(shipGameObject);
#if DEBUG
        BetterDragDebug.LogLineBuffered(
            $"{shipGameObject.name}: checking DisableForShipList for {normalizedName}"
        );
#endif
        foreach (var disableForShip in DisableForShipList)
        {
            if (string.Equals(disableForShip, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
#if DEBUG
                BetterDragDebug.LogLineBuffered($"{normalizedName}: is in DisableForShipList");
#endif
                return false;
            }
        }

        return true;
    }

    private bool IsEnabledWhileSleeping()
    {
        return Plugin.EnableDuringSleep!.Value && !Patcher.BoatIsMoored(_boatDamage);
    }
}
