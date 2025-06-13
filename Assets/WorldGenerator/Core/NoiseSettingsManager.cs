using System;
using UnityEngine;
using WorldGenerator.Settings;

namespace WorldGenerator.Core
{
    /// <summary>
    /// Управляет настройками генерации шума и их валидацией.
    /// Отвечает за кэширование хэшей настроек для оптимизации.
    /// </summary>
    public class NoiseSettingsManager
    {
        // Настройки различных типов шума
        public BaseNoiseSettings BaseNoiseSettings { get; set; }
        public VoronoiSettings VoronoiSettings { get; set; }
        public CombinedNoiseSettings CombinedNoiseSettings { get; set; }
        public CrackSettings CrackSettings { get; set; }
        public DepressionSettings DepressionSettings { get; set; }
        public DomainWarpSettings WarpSettings { get; set; }
        public MeshSettings MeshSettings { get; set; }
        public GlobalNoiseSettings GlobalSettings { get; set; }

        // Флаги активации различных типов шума
        public bool UseBaseNoise { get; set; }
        public bool UseVoronoiNoise { get; set; }
        public bool UseCombinedNoise { get; set; } = true;
        public bool UseCracks { get; set; } = true;
        public bool UseDepressions { get; set; } = true;
        public bool UseDomainWarp { get; set; } = true;
        public bool UseHeightTextures { get; set; } = true;
        public bool UseUVChecker { get; set; } = true;
        public bool AutoUpdateTexturesOnly { get; set; } = true;
        public bool AutoUpdateMeshOnNoiseChange { get; set; } = true;

        private int _cachedSettingsHash;

        /// <summary>
        /// Вычисляет хэш текущих настроек для определения необходимости регенерации.
        /// </summary>
        /// <returns>Хэш всех активных настроек</returns>
        public int CalculateSettingsHash()
        {
            var hash = 0;

            // Хэшируем только активные настройки
            if (UseBaseNoise && BaseNoiseSettings != null)
                hash ^= BaseNoiseSettings.GetHashCode();
            if (UseVoronoiNoise && VoronoiSettings != null)
                hash ^= VoronoiSettings.GetHashCode();
            if (UseCombinedNoise && CombinedNoiseSettings != null)
                hash ^= CombinedNoiseSettings.GetHashCode();
            if (UseCracks && CrackSettings != null)
                hash ^= CrackSettings.GetHashCode();
            if (UseDepressions && DepressionSettings != null)
                hash ^= DepressionSettings.GetHashCode();
            if (UseDomainWarp && WarpSettings != null)
                hash ^= WarpSettings.GetHashCode();
            if (MeshSettings != null)
                hash ^= MeshSettings.GetHashCode();

            return hash;
        }

        /// <summary>
        /// Проверяет, изменились ли настройки с последней проверки.
        /// </summary>
        /// <returns>True, если настройки изменились</returns>
        public bool HasSettingsChanged()
        {
            var currentHash = CalculateSettingsHash();
            var hasChanged = currentHash != _cachedSettingsHash;
            _cachedSettingsHash = currentHash;
            return hasChanged;
        }

        /// <summary>
        /// Получает текущее значение резкости из активных настроек.
        /// </summary>
        public float GetCurrentSharpness()
        {
            if (UseVoronoiNoise && VoronoiSettings != null)
                return VoronoiSettings.sharpness;
            if (UseCombinedNoise && CombinedNoiseSettings != null)
                return CombinedNoiseSettings.sharpness;
            if (GlobalSettings != null)
                return GlobalSettings.globalSharpness;

            return 5f; // Значение по умолчанию
        }

        /// <summary>
        /// Получает текущее значение квантования из активных настроек.
        /// </summary>
        public float GetCurrentQuantizeSteps()
        {
            if (UseVoronoiNoise && VoronoiSettings != null)
                return VoronoiSettings.quantizeSteps;
            if (UseCombinedNoise && CombinedNoiseSettings != null)
                return CombinedNoiseSettings.quantizeSteps;
            if (GlobalSettings != null)
                return GlobalSettings.globalQuantizeSteps;

            return 0.1f; // Значение по умолчанию
        }

        /// <summary>
        /// Валидирует корректность настроек перед генерацией.
        /// </summary>
        /// <returns>True, если настройки корректны</returns>
        public bool ValidateSettings()
        {
            if (MeshSettings == null)
            {
                Debug.LogError("Mesh settings not assigned!");
                return false;
            }

            // Проверяем, что хотя бы один тип шума активен
            if (!UseBaseNoise && !UseVoronoiNoise && !UseCombinedNoise)
            {
                Debug.LogWarning("No base noise type is enabled!");
            }

            return true;
        }
    }
}
