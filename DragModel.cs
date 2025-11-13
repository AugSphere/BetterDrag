using UnityEngine;

namespace BetterDrag
{
    internal static class DragModel
    {
        private const float tuningTotalDragMult = 300.0f;
        private const float tuningRelativeWaveMakingDragMult = 0.3f;
        private static float g = Mathf.Abs(Physics.gravity.y);

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
            float froudeNumber = absVelocity / Mathf.Sqrt(lengthAtWaterline * g);
            float force;

#if DEBUG
            BetterDragDebug.LogCSVBuffered([("Fr", froudeNumber)]);
#endif
            if (absVelocity < 1e-4)
                return 0f;

            float forceScaling = (Mathf.Exp(froudeNumber * 10f) - 1f) / 500f;
            float forceOscillation = 2f + Mathf.Cos(2f * Mathf.PI / froudeNumber);
            force = forceScaling * forceOscillation;

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
