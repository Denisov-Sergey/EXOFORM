using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "CrackSettings", menuName = "Noise/Crack Settings")]
    public class CrackSettings  : NoiseSettings
    {
        [Tooltip("Включить мелкие трещины")]
        public bool enabled = true;
        
        [Tooltip("Масштаб шума трещин")]
        public float scale = 10f;
        
        [Tooltip("Сила трещин")]
        [Range(0, 1)] public float strength = 0.1f;
        
        [Tooltip("Порог для трещин")]
        [Range(0, 1)] public float threshold = 0.5f;
        
        [Tooltip("Резкость трещин")]
        [Range(1, 5)] public float sharpness = 5f;
    }
}