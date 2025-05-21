using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [System.Serializable]
    public class CrackSettings  : NoiseSettings
    {
        public bool enabled = true;
        
        public float scale = 10f;
        
        [Range(0, 1)] public float strength = 0.3f;
        
        [Range(0, 1)] public float threshold = 0.6f;
        
        [Range(1, 5)] public float sharpness = 2f;
    }
}