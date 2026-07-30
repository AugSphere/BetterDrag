#if DEBUG
using BetterDrag.Utilities;
#endif

using UnityEngine;

namespace BetterDrag.Physics;

internal static class DragModel
{
    private const float TuningTotalDragMult = 350.0f;
    private const float TuningRelativeWaveMakingDragMult = 0.25f;
    private static readonly float G = Mathf.Abs(UnityEngine.Physics.gravity.y);

    private const float TuningViscousDragMult = TuningTotalDragMult;
    private const float TuningWaveMakingDragMult =
        TuningTotalDragMult * TuningRelativeWaveMakingDragMult;

    public static float CalculateWaveMakingDragForce(
        float absVelocity,
        float lengthAtWaterline,
        float formFactor,
        float displacement,
        float wettedArea
    )
    {
        float froudeNumber = absVelocity / Mathf.Sqrt(lengthAtWaterline * G);
#if DEBUG
        BetterDragDebug.LogCSVBuffered([("Fr", froudeNumber)]);
#endif
        if (absVelocity < 1e-4)
        {
            return 0f;
        }

        float force = 0f;
        if (froudeNumber < 0.5801)
        {
            float forceScaling = (Mathf.Exp(froudeNumber * 10f) - 1f) / 500f;
            float forceOscillation = 2f + Mathf.Cos(2f * Mathf.PI / froudeNumber);
            force = forceScaling * forceOscillation;
        }
        else
        {
            force = 1.2106f;
        }

        force *= displacement * TuningWaveMakingDragMult;
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
        float force = 0.0f;

        if (reynoldsNumber < 0.01)
        {
            return force;
        }

        float speedOrder = Mathf.Log10(reynoldsNumber) - 2.0f;
        float coefficient = 0.075f / (speedOrder * speedOrder);
        force = coefficient * wettedArea * (1.0f + formFactor) * absVelocity * absVelocity;
        force *= TuningViscousDragMult;

        return force;
    }
}