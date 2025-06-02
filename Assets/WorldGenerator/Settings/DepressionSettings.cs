using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "DepressionSettings", menuName = "Noise/Depression Settings")]
    public class DepressionSettings : NoiseSettings
    {
        [Header("Main Depression Properties")]
        [Tooltip("Сила впадин (0 = нет впадин, 1 = максимальные)")]
        [Range(0f, 1f)] public float strength = 0.3f;
        
        [Tooltip("Масштаб шума для впадин")]
        [Min(1f)] public float depressionScale = 17f;
        
        [Tooltip("Порог активации впадин")]
        [Range(0f, 1f)] public float threshold = 0.3f;
        
        [Tooltip("Плавность переходов впадин")]
        [Range(0.1f, 5f)] public float smoothness = 2f;
        
        [Header("Shape Configuration")]
        [Tooltip("Тип формы впадин")]
        public DepressionShape shapeType = DepressionShape.Hybrid;

        [Tooltip("Соотношение сторон для овальных впадин (1 = круг)")]
        [Range(0.1f, 3f)] public float aspectRatio = 1f;

        [Tooltip("Поворот формы в градусах")]
        [Range(0f, 360f)] public float rotation = 0f;

        [Header("Noise Details")]
        [Tooltip("Количество октав для детализации впадин")]
        [Range(1, 6)] public int octaves = 3; // Уменьшено с 5 до 3 для производительности
        
        [Header("Variation")]
        [Tooltip("Случайная вариация глубины")]
        [Range(0f, 1f)] public float depthVariation = 0.3f;
        
        [Header("Base Noise Reference")]
        [Tooltip("Базовый шум для применения впадин")]
        public NoiseSettings baseNoise;

        public enum DepressionShape
        {
            Circular,      // Круглые впадины
            Oval,          // Овальные впадины  
            Manhattan,     // Квадратные впадины
            Hybrid         // Смешанная форма
        }

        // Валидация в редакторе
        private void OnValidate()
        {
            // Ограничиваем минимальные значения
            depressionScale = Mathf.Max(1f, depressionScale);
            strength = Mathf.Clamp01(strength);
            threshold = Mathf.Clamp01(threshold);
            smoothness = Mathf.Max(0.1f, smoothness);
            octaves = Mathf.Clamp(octaves, 1, 6);
            depthVariation = Mathf.Clamp01(depthVariation);
            aspectRatio = Mathf.Clamp(aspectRatio, 0.1f, 3f);
            rotation = Mathf.Repeat(rotation, 360f);
        }

        // Исправленный GetHashCode с всеми полями
        public override int GetHashCode()
        {
            var hash = new System.HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(strength);
            hash.Add(depressionScale);
            hash.Add(threshold);
            hash.Add(smoothness);
            hash.Add((int)shapeType);
            hash.Add(aspectRatio);
            hash.Add(rotation);
            hash.Add(octaves);
            hash.Add(depthVariation);
            hash.Add(baseNoise?.GetHashCode() ?? 0);
            return hash.ToHashCode();
        }

        // Методы для быстрого доступа к предустановкам
        [ContextMenu("Preset: Soft Depressions")]
        public void SetSoftPreset()
        {
            strength = 0.2f;
            smoothness = 3f;
            threshold = 0.4f;
            shapeType = DepressionShape.Circular;
        }

        [ContextMenu("Preset: Sharp Craters")]
        public void SetSharpPreset()
        {
            strength = 0.5f;
            smoothness = 1f;
            threshold = 0.6f;
            shapeType = DepressionShape.Manhattan;
        }

        [ContextMenu("Preset: Natural Valleys")]
        public void SetValleyPreset()
        {
            strength = 0.3f;
            smoothness = 2f;
            threshold = 0.3f;
            shapeType = DepressionShape.Oval;
            aspectRatio = 2.5f;
            depthVariation = 0.4f;
        }
    }
}
