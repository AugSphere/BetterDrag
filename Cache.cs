using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BetterDrag
{
    internal class Cache<TValue>(string name, Func<GameObject, TValue> createValueCallback)
        where TValue : class
    {
        private readonly ConditionalWeakTable<GameObject, TValue> cache = new();
        private readonly ConditionalWeakTable<
            GameObject,
            TValue
        >.CreateValueCallback createValueCallback = new(createValueCallback);
        private (GameObject key, TValue value)? lastAccessed;
#pragma warning disable CA1823
        private readonly string name = name;
#pragma warning restore CA1823

        public TValue GetValue(GameObject key)
        {
            TValue value;
            if (object.ReferenceEquals(lastAccessed?.key, key))
            {
                return lastAccessed.Value.value;
            }

#if DEBUG && VERBOSE
            Debug.LogBuffered($"{name}: L1 cache miss for {key.name}");
#endif
            value = cache.GetValue(key, createValueCallback);
            lastAccessed = (key, value);
            return value;
        }

        public void SetValue(GameObject key, TValue value)
        {
            cache.Remove(key);
            cache.Add(key, value);
#if DEBUG && VERBOSE
            Debug.LogBuffered($"{name}: set {value} for {key.name}");
#endif
        }
    }
}
