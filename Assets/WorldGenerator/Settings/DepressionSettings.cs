using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [System.Serializable]
    public class DepressionSettings  : NoiseSettings
    {
        [Range(0, 1)] public float strength = 0.55f;
        
        public float scale = 15f;
        
        [Range(-1, 1)] public float threshold = -0.3f;
    }
}