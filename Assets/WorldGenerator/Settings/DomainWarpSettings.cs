using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [System.Serializable]
    public class DomainWarpSettings : NoiseSettings
    {
        public bool enabled = true;
        
        public float offsetX = 150f;
        
        public float offsetY = 150f;
        
        public float strength = 100f;
        
        public FastNoiseLite.NoiseType noiseType = FastNoiseLite.NoiseType.Perlin;
    }
}