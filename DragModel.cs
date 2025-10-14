using UnityEngine;
#if DEBUG
using HarmonyLib;
#endif

namespace BetterDrag
{
    internal static class DragModel
    {
        private static readonly float tuningTotalDragMult = 300.0f;
        private static readonly float tuningRelativeWaveMakingDragMult = 0.65f;

        private static readonly float tuningViscousDragMult = tuningTotalDragMult;
        private static readonly float tuningWaveMakingDragMult =
            tuningTotalDragMult * tuningRelativeWaveMakingDragMult;

        public static float CalculateWaveMakingDragForce(
            float absVelocity,
            float lengthAtWaterline,
            float formFactor,
            float displacement,
            float wettedArea
        )
        {
            float froudeNumber = absVelocity / Mathf.Sqrt(lengthAtWaterline * 10.0f);
            float force;

            if (froudeNumber <= 0.1476)
            {
                force = 0.02179f / 0.1476f * froudeNumber;
            }
            else
            {
                float forceScaling = 1.0f + 1.4f / (Mathf.Exp(18.0f - 40.0f * froudeNumber) + 1.0f);
                float forceOscillation =
                    Mathf.Pow(froudeNumber, 2)
                    * (
                        2.0f
                        - Mathf.Sqrt(froudeNumber)
                            * Mathf.Cos(1.0f / Mathf.Pow(froudeNumber, 2) - 1.9f)
                    );
                force = forceScaling * forceOscillation;
            }
            force *= displacement * tuningWaveMakingDragMult;

#if DEBUG && VERBOSE
            if (DebugCounter.IsAtPeriod())
            {
                FileLog.Log($"Froude number: {froudeNumber}");
                FileLog.Log($"Unmodified WM resistance: {force}");
            }
#endif

            return force;
        }

        public static float CalculateViscousDragForce(
            float absVelocity,
            float lengthAtWaterline,
            float formFactor,
            float displacement,
            float wettedArea
        )
        {
            float reynoldsNumber = absVelocity * lengthAtWaterline * 1e6f;
            float force;

            if (reynoldsNumber < 0.01)
            {
                force = 0.0f;
            }
            else
            {
                float coefficient = 0.075f / Mathf.Pow(Mathf.Log10(reynoldsNumber) - 2.0f, 2);
                force = coefficient * wettedArea * (1.0f + formFactor) * Mathf.Pow(absVelocity, 2);
                force *= tuningViscousDragMult;
            }

#if DEBUG && VERBOSE
            if (DebugCounter.IsAtPeriod())
            {
                FileLog.Log($"\nReynolds number: {reynoldsNumber:E2}");
                FileLog.Log($"Unmodified viscous resistance: {force}");
            }
#endif

            return force;
        }
    }
}
