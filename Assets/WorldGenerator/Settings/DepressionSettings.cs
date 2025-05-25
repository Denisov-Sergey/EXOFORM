using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "DepressionSettings", menuName = "Noise/Depression Settings")]
    public class DepressionSettings  : NoiseSettings
    {
        [Header("Depression Settings")]
        [Tooltip("Сила впадин (0 = нет впадин, 1 = максимальные)")]
        [Range(0, 1)] public float strength = 0.55f;
        
        [Tooltip("Масштаб шума для впадин")]
        public float depressionScale = 15f;
        
        [Tooltip("Порог активации впадин")]
        [Range(-1, 1)] public float threshold = -0.3f;
        
        [Header("Noise Configuration")]
        [Tooltip("Количество октав для детализации впадин")]
        [Range(1, 8)] public int octaves = 5;
        
        [Header("Base Noise Reference")]
        [Tooltip("Базовый шум для применения впадин")]
        public NoiseSettings baseNoise;

        public override int GetHashCode()
        {
            return System.HashCode.Combine(
                base.GetHashCode(),
                strength, depressionScale, threshold, octaves,
                baseNoise?.GetHashCode() ?? 0
            );
        }
    }
}