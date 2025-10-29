using Crest;
using UnityEngine;

namespace BetterDrag
{
    internal static class DraftSampler
    {
        static readonly Cache<SamplerImpl> draftSamplers = new("Draft samplers", (_) => new());

        internal static float GetAverageDraft(BoatProbes boatProbes, Rigidbody rigidbody)
        {
            var sampler = draftSamplers.GetValue(rigidbody.gameObject);
            var averageDraft = sampler.GetAverageDraft(boatProbes, rigidbody);
            return averageDraft;
        }

        private class SamplerImpl
        {
            private static readonly int sampleNumber = 3;
            private static readonly Vector3[] directions =
            [
                200 * Vector3.forward,
                Vector3.zero,
                200 * Vector3.back,
            ];
            private readonly Vector3[] queryPositions = new Vector3[sampleNumber];
            private readonly Vector3[] queryResults = new Vector3[sampleNumber];
            private uint draftSampleCounter = 0;
            private float lastDraft = 1.0f;

            internal float GetAverageDraft(BoatProbes boatProbes, Rigidbody rigidbody)
            {
                draftSampleCounter++;
                if (draftSampleCounter % Plugin.draftSamplingPeriod!.Value != 0)
                    return lastDraft;

                SetQueryPositions(rigidbody);

                float averageDraft = 0f;
                if (SampleDraft(out float seaLevel))
                {
                    for (int i = 0; i < sampleNumber; i++)
                    {
                        averageDraft += queryResults[i].y - queryPositions[i].y;
                    }
                    averageDraft = averageDraft / sampleNumber + seaLevel;
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

            private void SetQueryPositions(Rigidbody rigidbody)
            {
                var center = rigidbody.position;
                var down = 100 * rigidbody.transform.InverseTransformDirection(Vector3.down);
                for (int i = 0; i < sampleNumber; i++)
                {
                    queryPositions[i] = rigidbody.ClosestPointOnBounds(
                        center + down + directions[i]
                    );
                }
            }

            private bool SampleDraft(out float o_seaLevel)
            {
                o_seaLevel = OceanRenderer.Instance?.SeaLevel ?? 0f;
                ICollProvider? collProvider = OceanRenderer.Instance?.CollisionProvider;
                if (collProvider is null)
                {
                    return false;
                }

                int queryStatus = collProvider.Query(
                    this.GetHashCode(),
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
}
