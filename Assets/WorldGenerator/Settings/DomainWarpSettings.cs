using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "DomainWarpSettings", menuName = "Noise/DomainWarp Settings")]
    public class DomainWarpSettings : NoiseSettings
    {
        [Tooltip("Включить смещение для вторичного шума")]
        public bool enabled = true;
                
        [Range(1, 8)] [Tooltip("Количество октав. Увеличивает детализацию шума.")]
        public int octaves = 5;
        
        [Range(0.1f, 1f)] [Tooltip("Влияние каждой октавы. Меньше = сглаженный шум.")]
        public float persistence = 0.7f;
        
        [Tooltip("Смещение для вторичного шума по X")]
        public float offsetX = 150f;
        
        [Tooltip("Смещение для вторичного шума по Y")]
        public float offsetY = 150f;
        
        [Tooltip("Сила искажения координат")]
        public float strength = 100f;
        
        public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.Perlin;
       
        public FastNoiseLite.CellularDistanceFunction cellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.Euclidean;
        
        public FastNoiseLite.CellularReturnType cellularReturnType = FastNoiseLite.CellularReturnType.Distance;
    }
}