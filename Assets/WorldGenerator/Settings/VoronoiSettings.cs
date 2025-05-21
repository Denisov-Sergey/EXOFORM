using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [System.Serializable]
    public class VoronoiSettings : NoiseSettings
    {
        public bool enabled = true;
        
        public FastNoiseLite.CellularDistanceFunction distanceFunction = FastNoiseLite.CellularDistanceFunction.Euclidean;
        
        public FastNoiseLite.CellularReturnType returnType = FastNoiseLite.CellularReturnType.Distance;
        
        [Range(0, 1)] public float jitter = 0.75f;
    }
}