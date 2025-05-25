using System;
using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;
using WorldGenerator.Factory;
using WorldGenerator.Interface;
using WorldGenerator.Settings;

namespace WorldGenerator.Noise
{
    public class DepressionNoiseGenerator : INoiseGenerator
    {
        private FastNoiseLite _noise;
        private DepressionSettings _settings;
        private readonly INoiseGenerator _baseGenerator;

        public DepressionNoiseGenerator(DepressionSettings settings)
        {
            _settings = settings ?? _settings;
            // Создаем базовый генератор если он указан
            if (_settings.baseNoise != null)
            {
                _baseGenerator = NoiseFactory.CreateGenerator(_settings.baseNoise);
            }
            
            ConfigureNoise();
        }

        private void ConfigureNoise()
        {
            _noise = new FastNoiseLite(_settings.seed + 1000);
            _noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            _noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Manhattan);
            _noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance);
            _noise.SetFrequency(1f / _settings.depressionScale);
            _noise.SetFractalOctaves(_settings.octaves);
        }
        
        public float[,] GenerateNoiseMap(int width, int height)
        {
            float[,] baseMap;
            
            // Получаем базовую карту
            if (_baseGenerator != null)
            {
                baseMap = _baseGenerator.GenerateNoiseMap(width, height);
            }
            else
            {
                // Если нет базового генератора, создаем плоскую карту
                baseMap = new float[width, height];
            }
            
            // Применяем впадины к базовой карте
            return ApplyDepressionsToMap(baseMap, width, height);
        }

        // Основной метод для применения впадин к внешней карте
        public float[,] ApplyDepressionsToExternalMap(float[,] originalMap)
        {
            int width = originalMap.GetLength(0);
            int height = originalMap.GetLength(1);
            return ApplyDepressionsToMap(originalMap, width, height);
        }

        private float[,] ApplyDepressionsToMap(float[,] originalMap, int width, int height)
        {
            float[,] depressedMap = new float[width, height];
            
            // Копируем оригинальную карту
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                depressedMap[x, y] = originalMap[x, y];
            }
            
            // Применяем впадины 
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                // Генерация шума впадин (точно как в оригинале)
                float depressionValue = _noise.GetNoise(x, y);
                
                // Применение впадин (точно как в TestNoiseGenerator)
                if (depressionValue > _settings.threshold)
                {
                    Debug.Log($"Depression: {depressionValue}, Threshold: {_settings.threshold}");
                    // Инвертируем и усиливаем впадины (точно как в оригинале)
                    depressedMap[x, y] = -depressedMap[x, y] * _settings.strength * 2f;
                }
            }
            
            return depressedMap;
        }

        // Метод для получения только маски впадин
        public float[,] GetDepressionMask(int width, int height)
        {
            float[,] depressionMask = new float[width, height];
            
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float depressionValue = _noise.GetNoise(x, y);
                depressionMask[x, y] = depressionValue > _settings.threshold ? depressionValue : 0f;
            }
            
            return depressionMask;
        }

        // Метод для применения впадин с определенной силой в определенной точке
        public float ApplyDepressionToPoint(float originalValue, int x, int y)
        {
            float depressionValue = _noise.GetNoise(x, y);
            
            // Применение впадин (точно как в TestNoiseGenerator)
            if (depressionValue > _settings.threshold)
            {
                Debug.Log($"Depression: {depressionValue}, Threshold: {_settings.threshold}");
                return -originalValue * _settings.strength * 2f;
            }
            
            return originalValue;
        }

        
        public void UpdateNoiseMap(object settings)
        {
            if (settings is DepressionSettings newSettings)
            {
                _settings = newSettings;
                ConfigureNoise();
                
                // Обновляем базовый генератор если нужно
                if (_baseGenerator != null && newSettings.baseNoise != null)
                {
                    _baseGenerator.UpdateNoiseMap(newSettings.baseNoise);
                }
            }
            else
            {
                throw new ArgumentException($"Settings not supported: {settings?.GetType().Name}");
            }
        }

        public NoiseSettings GetSettings() => _settings;
    }
}