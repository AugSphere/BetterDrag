using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Crest;
using HarmonyLib;
using UnityEngine;

namespace BetterDrag
{
    [HarmonyPatch]
    static class BoatProbesFixedUpdateDragPatch
    {
        static readonly MethodInfo m_AddForceAtPosition = AccessTools.Method(
            typeof(Rigidbody),
            nameof(Rigidbody.AddForceAtPosition),
            [typeof(Vector3), typeof(Vector3), typeof(ForceMode)]
        );

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
        static IEnumerable<CodeInstruction> DropOriginalLongitudinalDrag(
            IEnumerable<CodeInstruction> instructions
        )
        {
            var addForceCount = 0;
            var enumerator = instructions.GetEnumerator();

            enumerator.MoveNext();
            while (addForceCount < 2)
            {
                if (enumerator.Current.Calls(m_AddForceAtPosition))
                    addForceCount++;
                yield return enumerator.Current;
                enumerator.MoveNext();
            }

            while (addForceCount < 3)
            {
                if (enumerator.Current.Calls(m_AddForceAtPosition))
                    addForceCount++;
                enumerator.MoveNext();
            }

            while (true)
            {
                yield return enumerator.Current;
                if (!enumerator.MoveNext())
                    break;
            }
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Component), nameof(Component.transform), MethodType.Getter)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        static Transform ComponentBaseTransform(BoatProbes _) => null!;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatProbes), "FixedUpdateDrag")]
        static void AddCustomLongitudinalDrag(
            Vector3 waterSurfaceVel,
            BoatProbes __instance,
            Rigidbody ____rb,
            float ____forceHeightOffset
        )
        {
            Profiler.RestartClock();

            var transform = ComponentBaseTransform(__instance);
            Profiler.Profile("transform");

            Vector3 velocityVector = ____rb.velocity - waterSurfaceVel;
            Vector3 dragPositionVector = ____rb.position + ____forceHeightOffset * Vector3.up;
            var forwardVector = transform.forward;
            var forwardVelocity = Vector3.Dot(forwardVector, velocityVector);
            Profiler.Profile("velocity");

            var signedDragForceMagnitude = PhysicsCalculation.GetDragForceMagnitude(
                __instance,
                ____rb,
                forwardVelocity
            );
            Profiler.Profile("GetDragForceMagnitude");

            var dragForceVector = forwardVector * signedDragForceMagnitude;
            ____rb.AddForceAtPosition(dragForceVector, dragPositionVector, ForceMode.Force);
            Profiler.Profile("AddForceAtPosition");

            Profiler.LogDurations();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BoatProbes), "Start")]
        static void AddVelocityGuard(BoatProbes __instance, Rigidbody ____rb)
        {
            __instance.StartCoroutine(VelocityGuard(____rb));
        }

        static readonly OutlierFilter boatVelocityFilterX = new("Velocity x filter");
        static readonly OutlierFilter boatVelocityFilterY = new("Velocity y filter");
        static readonly OutlierFilter boatVelocityFilterZ = new("Velocity z filter");

        static IEnumerator VelocityGuard(Rigidbody body)
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();
                if (body.IsSleeping() || body.isKinematic)
                    continue;
                var velocity = body.velocity;
                var x = boatVelocityFilterX.ClampValue(velocity.x, body);
                var y = boatVelocityFilterY.ClampValue(velocity.y, body);
                var z = boatVelocityFilterZ.ClampValue(velocity.z, body);
                body.velocity = new(x, y, z);
            }
        }
    }
}
