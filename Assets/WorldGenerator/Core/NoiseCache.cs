using System;
using System.Collections.Generic;
using UnityEngine;
using WorldGenerator.Interface;

namespace WorldGenerator.Core
{
    public class NoiseCache : INoiseCache
    {
        private readonly Dictionary<string, CachedNoiseData> _cache = new();
        private readonly Queue<string> _accessOrder = new();
        private int _maxCacheSize = 20;
        
        [System.Serializable]
        private class CachedNoiseData
        {
            public float[,] noiseMap;
            public DateTime cachedTime;
            public int accessCount;
            public bool isPersistent; // Для важных карт которые не удаляем
        }

        public bool TryGetNoise(string key, out float[,] noiseMap)
        {
            noiseMap = null;
            
            if (!_cache.TryGetValue(key, out var cachedData))
                return false;

            // Проверяем актуальность кэша (можно добавить TTL)
            var age = DateTime.Now - cachedData.cachedTime;
            if (age.TotalMinutes > 30 && !cachedData.isPersistent) // Кэш устарел
            {
                _cache.Remove(key);
                return false;
            }

            cachedData.accessCount++;
            noiseMap = cachedData.noiseMap;
            
            // Обновляем порядок доступа для LRU
            UpdateAccessOrder(key);
            
            return true;
        }

        public void CacheNoise(string key, float[,] noiseMap, bool isPersistent = false)
        {
            // Проверяем лимит кэша
            if (_cache.Count >= _maxCacheSize)
            {
                EvictLeastRecentlyUsed();
            }

            var cachedData = new CachedNoiseData
            {
                noiseMap = CloneNoiseMap(noiseMap), // Клонируем для безопасности
                cachedTime = DateTime.Now,
                accessCount = 1,
                isPersistent = isPersistent
            };

            _cache[key] = cachedData;
            UpdateAccessOrder(key);
            
            Debug.Log($"Cached noise map: {key} (Cache size: {_cache.Count})");
        }

        private void UpdateAccessOrder(string key)
        {
            // Удаляем из очереди если есть и добавляем в конец
            var temp = new Queue<string>();
            while (_accessOrder.Count > 0)
            {
                var item = _accessOrder.Dequeue();
                if (item != key)
                    temp.Enqueue(item);
            }
            
            while (temp.Count > 0)
                _accessOrder.Enqueue(temp.Dequeue());
                
            _accessOrder.Enqueue(key);
        }

        private void EvictLeastRecentlyUsed()
        {
            while (_accessOrder.Count > 0)
            {
                var oldestKey = _accessOrder.Dequeue();
                if (_cache.TryGetValue(oldestKey, out var data) && !data.isPersistent)
                {
                    _cache.Remove(oldestKey);
                    Debug.Log($"Evicted from cache: {oldestKey}");
                    break;
                }
            }
        }

        private float[,] CloneNoiseMap(float[,] original)
        {
            int width = original.GetLength(0);
            int height = original.GetLength(1);
            var clone = new float[width, height];
            
            Array.Copy(original, clone, original.Length);
            return clone;
        }

        public void ClearCache() => _cache.Clear();
        public void SetCacheLimit(int limit) => _maxCacheSize = limit;
        
        // Статистика для отладки
        public void PrintCacheStats()
        {
            Debug.Log($"Cache Stats: {_cache.Count}/{_maxCacheSize} entries");
            foreach (var kvp in _cache)
            {
                var data = kvp.Value;
                Debug.Log($"  {kvp.Key}: accessed {data.accessCount} times, age: {(DateTime.Now - data.cachedTime).TotalMinutes:F1} min");
            }
        }
    }
}