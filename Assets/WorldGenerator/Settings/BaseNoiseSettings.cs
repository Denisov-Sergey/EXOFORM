using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [System.Serializable]
    public class BaseNoiseSettings : NoiseSettings
    {
        
        [Tooltip("Масштаб шума. Меньше = более детализировано.")]
        public float scale = 250f;
        
        [Range(1, 8)] [Tooltip("Количество октав. Увеличивает детализацию шума.")]
        public int octaves = 5;
        
        [Range(0.1f, 1f)] [Tooltip("Влияние каждой октавы. Меньше = сглаженный шум.")]
        public float persistence = 0.7f;
        
        public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.Perlin;
    }
}