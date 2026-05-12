using System;
using UnityEngine;

namespace BetterDrag
{
    internal class ModEnableCheck(GameObject shipGameObject)
    {
        static readonly string[] disableForShipList = ["BOAT CUTTER (212)"];
        private readonly bool isEnabledForShip = IsEnabledForShip(shipGameObject);

        internal bool IsModEnabled()
        {
            if (!isEnabledForShip)
                return false;

            return true;
        }

        private static bool IsEnabledForShip(GameObject shipGameObject)
        {
            var normalizedName = ShipDragConfigManager.GetNormalizedShipName(shipGameObject);
            foreach (var disableForShip in disableForShipList)
                if (string.Equals(disableForShip, normalizedName, StringComparison.Ordinal))
                    return false;
            return true;
        }
    }
}
