using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "VoronoiSettings", menuName = "Noise/Voronoi Settings")]
    public class VoronoiSettings : NoiseSettings
    {
        public bool enabled = true;
        
        [Tooltip("Резкость скал")]
        [Range(1, 5)] public float sharpness = 5f;
        
        public FastNoiseLite.CellularDistanceFunction cellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.Euclidean;
        
        public FastNoiseLite.CellularReturnType cellularReturnType = FastNoiseLite.CellularReturnType.Distance;
        
        [Range(0, 1)] public float jitter = 0.75f;
        
        
        [Tooltip("Квантование высот для ступенчатых уступов")]
        [Range(0, 1)] public float quantizeSteps = 0.1f;
        
    }
}