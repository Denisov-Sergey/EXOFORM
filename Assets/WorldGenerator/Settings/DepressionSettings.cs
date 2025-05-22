using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "DepressionSettings", menuName = "Noise/Depression Settings")]
    public class DepressionSettings  : NoiseSettings
    {
        [Tooltip("Сила впадин (0 = нет впадин, 1 = максимальные)")]
        [Range(0, 1)] public float strength = 0.55f;
        
        [Tooltip("Масштаб шума для впадин")]
        public float scale = 15f;
        
        [Tooltip("Порог активации впадин")]
        [Range(-1, 1)] public float threshold = -0.3f;
    }
}