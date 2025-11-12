using UnityEngine;

namespace BetterDrag
{
    internal static class DragModel
    {
        private const float tuningTotalDragMult = 500.0f;
        private const float tuningRelativeWaveMakingDragMult = 1f;

        private const float tuningViscousDragMult = tuningTotalDragMult;
        private const float tuningWaveMakingDragMult =
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

#if DEBUG
            BetterDragDebug.LogCSVBuffered([("Fr", froudeNumber)]);
#endif

            if (froudeNumber <= 0.001f)
            {
                force = froudeNumber * froudeNumber;
            }
            else
            {
                froudeNumber += 0.04f;
                float froudeSquared = froudeNumber * froudeNumber;
                float forceScaling = 0.5f + 4f / (Mathf.Exp(8f - 20f * froudeNumber) + 1f);
                float forceOscillation = 2f - Mathf.Cos(1.0f / froudeSquared);
                force = froudeSquared * forceScaling * forceOscillation;
            }
            force *= displacement * tuningWaveMakingDragMult;
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
                float speedOrder = Mathf.Log10(reynoldsNumber) - 2.0f;
                float coefficient = 0.075f / (speedOrder * speedOrder);
                force = coefficient * wettedArea * (1.0f + formFactor) * absVelocity * absVelocity;
                force *= tuningViscousDragMult;
            }

            return force;
        }
    }
}
