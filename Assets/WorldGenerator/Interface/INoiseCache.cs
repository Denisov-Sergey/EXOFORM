namespace WorldGenerator.Interface
{
    public interface INoiseCache
    {
        bool TryGetNoise(string key, out float[,] noiseMap);
        void CacheNoise(string key, float[,] noiseMap, bool isPersistent = false);
        void ClearCache();
        void SetCacheLimit(int limit);
    }
}