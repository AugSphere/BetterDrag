using System;
using System.Runtime.CompilerServices;
using UnityEngine;
#if DEBUG
using HarmonyLib;
#endif

namespace BetterDrag
{
    internal class Cache<TValue>(string name)
        where TValue : class
    {
        private readonly ConditionalWeakTable<GameObject, TValue> cache = new();
        private (GameObject key, TValue value)? lastAccessed;
        private readonly string name = name;

        public TValue Get(GameObject key, Func<TValue> constructor)
        {
            TValue value;
            if (object.ReferenceEquals(lastAccessed?.key, key))
            {
                return lastAccessed.Value.value;
            }

#if DEBUG
            FileLog.Log($"{name}: L1 cache miss for {key.name}");
#endif
            cache.TryGetValue(key, out var cacheValue);
            if (cacheValue is null)
            {
#if DEBUG
                FileLog.Log($"{name}: L2 cache miss entry for {key.name}");
#endif
                value = constructor();
                cache.Add(key, value);
            }
            else
            {
                value = cacheValue;
            }
            lastAccessed = (key, value);

            return value;
        }
    }
}
