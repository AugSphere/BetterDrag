using Crest;
using UnityEngine;

namespace BetterDrag
{
    internal static class DraftSampler
    {
        private static readonly Vector3[] queryPositions = new Vector3[3];
        private static readonly Vector3[] queryResults = new Vector3[3];
        private static readonly int hashCode = typeof(DraftSampler).GetHashCode();
        private static uint draftSampleCounter = 0;
        private static float lastDraft = 1.0f;
        private static readonly Vector3[] directions =
        [
            100 * (Vector3.down + Vector3.forward),
            100 * Vector3.down,
            100 * (Vector3.down + Vector3.back),
        ];

        internal static float GetAverageDraft(BoatProbes boatProbes, Rigidbody rigidbody)
        {
            draftSampleCounter++;
            if (draftSampleCounter % Plugin.draftSamplingPeriod!.Value != 0)
                return lastDraft;

            SetQueryPositions(rigidbody);

            float averageDraft = 0f;
            var queryHash = boatProbes.GetHashCode() ^ hashCode;
            if (SampleDraft(queryHash, out float seaLevel))
            {
                for (int i = 0; i < 3; i++)
                {
                    averageDraft += queryResults[i].y - queryPositions[i].y;
                }
                averageDraft = averageDraft / 3f + seaLevel;
#if DEBUG
                Debug.LogBuffered($"Sampled average draft {averageDraft}");
#endif
            }
            else
            {
                averageDraft = lastDraft;
#if DEBUG
                Debug.LogBuffered($"Failed to sample draft, using last value {averageDraft}");
#endif
            }

            lastDraft = Mathf.Clamp(averageDraft, 0.1f, 20f);
            return lastDraft;
        }

        private static void SetQueryPositions(Rigidbody rigidbody)
        {
            var center = rigidbody.centerOfMass;
            queryPositions[0] = rigidbody.ClosestPointOnBounds(center + directions[0]);
            queryPositions[1] = rigidbody.ClosestPointOnBounds(center + directions[1]);
            queryPositions[2] = rigidbody.ClosestPointOnBounds(center + directions[2]);
        }

        private static bool SampleDraft(int queryHash, out float o_seaLevel)
        {
            o_seaLevel = OceanRenderer.Instance?.SeaLevel ?? 0f;
            ICollProvider? collProvider = OceanRenderer.Instance?.CollisionProvider;
            if (collProvider is null)
            {
                return false;
            }

            int queryStatus = collProvider.Query(
                queryHash,
                0f,
                queryPositions,
                queryResults,
                null,
                null
            );
            if (!collProvider.RetrieveSucceeded(queryStatus))
            {
                return false;
            }

            return true;
        }
    }
}
