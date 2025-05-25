using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "CombinedNoiseSettings", menuName = "Noise/CombinedNoise Settings")]
    public class CombinedNoiseSettings  : NoiseSettings
    {
        
        [Header("Fractal Settings")]
        [Range(1, 8)] public int octaves = 5;
        [Range(0.1f, 1f)] public float persistence = 0.7f;
        
        [Header("Post-processing")]
        [Range(1, 5)] public float sharpness = 5f;
        [Range(0, 1)] public float quantizeSteps = 0.1f;

        public override int GetHashCode()
        {
            return System.HashCode.Combine(
                base.GetHashCode(),
                octaves, persistence, sharpness, quantizeSteps
            );
        }
    }
}