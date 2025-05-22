using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "CombinedNoiseSettings", menuName = "Noise/CombinedNoise Settings")]
    public class CombinedNoiseSettings  : NoiseSettings
    {
        public NoiseSettings baseNoise;
        public NoiseSettings secondaryNoise;
        [Range(0f, 1f)] public float blendFactor = 0.5f;
    }
}