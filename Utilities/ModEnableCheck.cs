using System;
using UnityEngine;

namespace BetterDrag.Utilities;

internal sealed class ModEnableCheck(GameObject shipGameObject)
{
    private static readonly string[] DisableForShipList = ["BOAT CUTTER (212)"];
    private readonly bool _isEnabledForShip = IsEnabledForShip(shipGameObject);

    internal bool IsModEnabled()
    {
        return _isEnabledForShip && (!GameState.sleeping || IsEnabledWhileSleeping());
    }

    private static bool IsEnabledForShip(GameObject shipGameObject)
    {
        var normalizedName = Utilities.GetNormalizedShipName(shipGameObject);
        foreach (var disableForShip in DisableForShipList)
        {
            if (string.Equals(disableForShip, normalizedName, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsEnabledWhileSleeping()
    {
        return Plugin.EnableDuringSleep!.Value && !Patcher.CurrentBoatIsMoored(Sleep.instance);
    }
}
