using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "VoronoiSettings", menuName = "Noise/Voronoi Settings")]
    public class VoronoiSettings : NoiseSettings
    {
        public bool enabled = true;
        
        public FastNoiseLite.CellularDistanceFunction cellularDistanceFunction = FastNoiseLite.CellularDistanceFunction.Euclidean;
        
        public FastNoiseLite.CellularReturnType cellularReturnType = FastNoiseLite.CellularReturnType.Distance;
        
        [Range(0, 1)] public float jitter = 0.75f;
    }
}