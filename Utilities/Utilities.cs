using System;
using UnityEngine;

namespace BetterDrag
{
    internal static class Utilities
    {
        internal static string GetNormalizedShipName(GameObject ship)
        {
            return StripCloneSuffix(ship.name);
        }

        internal static string StripCloneSuffix(string shipName)
        {
            var strippedName = shipName;
            var suffix = "(Clone)";

            while (strippedName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                strippedName = strippedName.Substring(0, strippedName.Length - suffix.Length);
            }
            return strippedName;
        }
    }
}
