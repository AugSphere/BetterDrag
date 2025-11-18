using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BetterDrag
{
    internal class Cache<T>(string name, Func<GameObject, T> createValueCallback)
        where T : class
    {
        private readonly ConditionalWeakTable<GameObject, T> cache = new();
        private readonly ConditionalWeakTable<
            GameObject,
            T
        >.CreateValueCallback createValueCallback = new(createValueCallback);
        private (GameObject key, T value)? lastAccessed;
        private readonly string name = name;

        public T GetValue(GameObject key)
        {
            if (object.ReferenceEquals(lastAccessed?.key, key))
            {
                return lastAccessed.Value.value;
            }

#if DEBUG && VERBOSE
            Debug.LogBuffered($"{name}: L1 cache miss for {key.name}");
#endif
            T value = cache.GetValue(key, createValueCallback);
            lastAccessed = (key, value);
            return value;
        }

        public void SetValue(GameObject key, T value)
        {
            cache.Remove(key);
            cache.Add(key, value);
#if DEBUG && VERBOSE
            Debug.LogBuffered($"{name}: set {value} for {key.name}");
#endif
        }
    }
}
