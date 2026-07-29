using System;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace BetterDrag.Utilities
{
    internal class Cache<T>(string name, Func<GameObject, T> createValueCallback)
        where T : class
    {
        private readonly ConditionalWeakTable<GameObject, T> _cache = new();
        private readonly ConditionalWeakTable<
            GameObject,
            T
        >.CreateValueCallback _createValueCallback = new(createValueCallback);
        private (GameObject key, T value)? _lastAccessed;
        private readonly string _name = name;

        public T GetValue(GameObject key)
        {
            if (ReferenceEquals(_lastAccessed?.key, key))
            {
                return _lastAccessed.Value.value;
            }

#if DEBUG && VERBOSE
            Debug.LogBuffered($"{name}: L1 cache miss for {key.name}");
#endif
            T value = _cache.GetValue(key, _createValueCallback);
            _lastAccessed = (key, value);
            return value;
        }

        public void SetValue(GameObject key, T value)
        {
            _cache.Remove(key);
            _cache.Add(key, value);
#if DEBUG && VERBOSE
            Debug.LogBuffered($"{name}: set {value} for {key.name}");
#endif
        }
    }
}