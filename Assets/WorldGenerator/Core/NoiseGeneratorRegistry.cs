using System;
using System.Collections.Generic;
using UnityEngine;
using WorldGenerator.Abstract;
using WorldGenerator.Interface;
using WorldGenerator.Factory;
using WorldGenerator.Settings;

namespace WorldGenerator.Core
{
    /// <summary>
    /// Управляет созданием, кэшированием и жизненным циклом генераторов шума.
    /// Использует паттерн Registry для централизованного управления генераторами.
    /// </summary>
    public class NoiseGeneratorRegistry
    {
        // Словарь для хранения созданных генераторов по типу настроек
        private readonly Dictionary<Type, INoiseGenerator> _generators = new();
        private readonly NoiseSettingsManager _settingsManager;

        public NoiseGeneratorRegistry(NoiseSettingsManager settingsManager)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
        }

        /// <summary>
        /// Создает или обновляет генераторы на основе текущих настроек.
        /// Очищает кэш генераторов при изменении настроек.
        /// </summary>
        public void UpdateGenerators()
        {
            _generators.Clear();

            try
            {
                CreateBaseNoiseGenerator();
                CreateVoronoiGenerator();
                CreateCombinedNoiseGenerator();
                CreateCrackGenerator();
                CreateDepressionGenerator();
                CreateDomainWarpGenerator();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create generators: {e.Message}");
                throw;
            }
        }

        /// <summary>
        /// Получает генератор по типу настроек.
        /// </summary>
        /// <typeparam name="T">Тип настроек генератора</typeparam>
        /// <returns>Генератор или null, если не найден</returns>
        public INoiseGenerator GetGenerator<T>() where T : NoiseSettings
        {
            return _generators.ContainsKey(typeof(T)) ? _generators[typeof(T)] : null;
        }

        /// <summary>
        /// Проверяет наличие генератора для указанного типа настроек.
        /// </summary>
        public bool HasGenerator<T>() where T : NoiseSettings
        {
            return _generators.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Очищает все созданные генераторы и освобождает ресурсы.
        /// </summary>
        public void ClearGenerators()
        {
            _generators.Clear();
        }

        #region Private Generator Creation Methods

        private void CreateBaseNoiseGenerator()
        {
            if (_settingsManager.UseBaseNoise && _settingsManager.BaseNoiseSettings != null)
            {
                _generators[typeof(BaseNoiseSettings)] = 
                    NoiseFactory.CreateGenerator(_settingsManager.BaseNoiseSettings);
            }
        }

        private void CreateVoronoiGenerator()
        {
            if (_settingsManager.UseVoronoiNoise && _settingsManager.VoronoiSettings != null)
            {
                _generators[typeof(VoronoiSettings)] = 
                    NoiseFactory.CreateGenerator(_settingsManager.VoronoiSettings);
            }
        }

        private void CreateCombinedNoiseGenerator()
        {
            if (_settingsManager.UseCombinedNoise && _settingsManager.CombinedNoiseSettings != null)
            {
                // ОТЛАДКА: Проверяем настройки
                // var settings = _settingsManager.CombinedNoiseSettings;
                // Debug.Log($"Creating CombinedNoiseGenerator with:");
                // Debug.Log($"  - Seed: {settings.seed}");
                // Debug.Log($"  - Scale: {settings.scale}");
                // Debug.Log($"  - Octaves: {settings.octaves}");
                // Debug.Log($"  - Persistence: {settings.persistence}");
                
                _generators[typeof(CombinedNoiseSettings)] = 
                    NoiseFactory.CreateGenerator(_settingsManager.CombinedNoiseSettings);
            }
        }

        private void CreateCrackGenerator()
        {
            if (_settingsManager.UseCracks && _settingsManager.CrackSettings != null)
            {
                _generators[typeof(CrackSettings)] = 
                    NoiseFactory.CreateGenerator(_settingsManager.CrackSettings);
            }
        }

        private void CreateDepressionGenerator()
        {
            if (_settingsManager.UseDepressions && _settingsManager.DepressionSettings != null)
            {
                _generators[typeof(DepressionSettings)] = 
                    NoiseFactory.CreateGenerator(_settingsManager.DepressionSettings);
            }
        }

        private void CreateDomainWarpGenerator()
        {
            if (_settingsManager.UseDomainWarp && _settingsManager.WarpSettings != null)
            {
                _generators[typeof(DomainWarpSettings)] = 
                    NoiseFactory.CreateGenerator(_settingsManager.WarpSettings);
            }
        }

        #endregion
    }
}
