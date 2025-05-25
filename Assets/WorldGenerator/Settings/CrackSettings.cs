using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "CrackSettings", menuName = "Noise/Crack Settings")]
    public class CrackSettings  : NoiseSettings
    {
        [Tooltip("Масштаб шума трещин")]
        public float crackScale = 10f;
        
        [Tooltip("Сила трещин")]
        [Range(0, 1)] public float crackStrength = 0.1f;
        
        [Tooltip("Порог для трещин")]
        [Range(0, 1)] public float crackThreshold = 0.5f;
        
        [Tooltip("Резкость трещин")]
        [Range(1, 5)] public float crackSharpness = 5f;
        
        [Header("Base Noise Reference")]
        [Tooltip("Базовый шум для применения трещин")]
        public NoiseSettings baseNoise;

        public override int GetHashCode()
        {
            return System.HashCode.Combine(
                base.GetHashCode(),
                crackScale, crackStrength, 
                crackThreshold, crackSharpness,
                baseNoise?.GetHashCode() ?? 0
            );
        }
    }
}