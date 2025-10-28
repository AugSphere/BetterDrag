using UnityEngine;

namespace BetterDrag
{
    internal static class DragModel
    {
        private static readonly float tuningTotalDragMult = 800.0f;
        private static readonly float tuningRelativeWaveMakingDragMult = 1.25f;

        private static readonly float tuningViscousDragMult = tuningTotalDragMult;
        private static readonly float tuningWaveMakingDragMult =
            tuningTotalDragMult * tuningRelativeWaveMakingDragMult;

#if DEBUG
        private static float smoothedViscousDragForce = 0f;
        private static float smoothedWavemakingDragForce = 0f;
#endif

        public static float CalculateWaveMakingDragForce(
            float absVelocity,
            float lengthAtWaterline,
            float formFactor,
            float displacement,
            float wettedArea
        )
        {
            float froudeNumber = absVelocity / Mathf.Sqrt(lengthAtWaterline * 10.0f);
            float froudeSquared = froudeNumber * froudeNumber;
            float force;

            if (froudeNumber <= 0.001f)
            {
                force = froudeSquared;
            }
            else
            {
                float forceScaling = 0.5f + 2.5f / (Mathf.Exp(6f - 20f * froudeNumber) + 1f);
                float forceOscillation = 2f - Mathf.Cos(1.0f / froudeSquared);
                force = froudeSquared * forceScaling * forceOscillation;
            }
            force *= displacement * tuningWaveMakingDragMult;

#if DEBUG
            float smoothing = 1f / (1 << Plugin.debugForceSmoothing!.Value);
            smoothedWavemakingDragForce =
                (1 - smoothing) * smoothedWavemakingDragForce + smoothing * force;
            Debug.LogDragModelBuffered(
                [
                    $"Froude number: {froudeNumber}",
                    $"Unmodified WM resistance: {force}",
                    $"Smoothed WM resistance: {smoothedWavemakingDragForce}",
                ]
            );
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
                float speedOrder = Mathf.Log10(reynoldsNumber) - 2.0f;
                float coefficient = 0.075f / (speedOrder * speedOrder);
                force = coefficient * wettedArea * (1.0f + formFactor) * absVelocity * absVelocity;
                force *= tuningViscousDragMult;
            }

#if DEBUG
            float smoothing = 1f / (1 << Plugin.debugForceSmoothing!.Value);
            smoothedViscousDragForce =
                (1 - smoothing) * smoothedViscousDragForce + smoothing * force;
            Debug.LogDragModelBuffered(
                [
                    $"Reynolds number: {reynoldsNumber:E2}",
                    $"Unmodified viscous resistance: {force}",
                    $"Smoothed viscous resistance: {smoothedViscousDragForce}",
                ]
            );
#endif

            return force;
        }
    }
}
