using UnityEngine;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "GlobalNoiseSettings", menuName = "Noise/Global Settings")]
    public class GlobalNoiseSettings : NoiseSettings
    {
        [Header("Global Post-Processing")]
        [Tooltip("Глобальная резкость (как в TestNoiseGenerator)")]
        [Range(1, 5)] public float globalSharpness = 5f;
    
        [Tooltip("Глобальное квантование")]
        [Range(0, 1)] public float globalQuantizeSteps = 0.1f;
    
        [Header("Debug")]
        [Tooltip("Включить логирование впадин")]
        public bool enableDepressionLogging = true;
    }

}