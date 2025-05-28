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
        
        [Header("Mountain Shape")]
        [Tooltip("Степень для контроля крутизны гор. 1.0 = естественные склоны, больше = более острые и крутые горы")]
        [Range(1f, 5f)] public int heightExponent = 3;
        
        [Tooltip("Частота генерации шума. Больше значение = меньше горы, но чаще. Меньше значение = крупнее горы, но реже.")]
        [Range(0.01f, 10f)] public float frequency = 0.01f;
        [Tooltip("Порог отсечения для создания плоских областей. Все значения выше этого порога становятся плоскими")]
        [Range(-1f, 1f)] public float cutoffThreshold = -0.7f;
        
        [Header("Post-processing")]
        [Range(1, 5)] public float sharpness = 5f;
        [Range(0, 1)] public float quantizeSteps = 0.1f;

        /// <summary>
        /// Вычисляет хеш-код для определения изменений настроек.
        /// Используется системой кэширования для избежания лишних пересчетов.
        /// </summary>
        /// <returns>Уникальный хеш-код, основанный на всех параметрах настроек</returns>
        public override int GetHashCode()
        {
            return System.HashCode.Combine(
                base.GetHashCode(),
                octaves, persistence, sharpness, quantizeSteps,
                frequency,heightExponent,cutoffThreshold
            );
        }
    }
}