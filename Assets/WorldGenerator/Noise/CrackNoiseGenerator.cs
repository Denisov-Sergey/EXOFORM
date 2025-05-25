using System;
using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;
using WorldGenerator.Factory;
using WorldGenerator.Interface;
using WorldGenerator.Settings;

namespace WorldGenerator.Noise
{
    public class CrackNoiseGenerator : INoiseGenerator
    {
        private FastNoiseLite _noise;
        private CrackSettings _settings;
        private readonly INoiseGenerator _baseGenerator;

        public CrackNoiseGenerator(CrackSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            
            // Создаем базовый генератор если он указан
            if (_settings.baseNoise != null)
            {
                _baseGenerator = NoiseFactory.CreateGenerator(_settings.baseNoise);
            }
            
            ConfigureNoise();
        }

        private void ConfigureNoise()
        {
            _noise = new FastNoiseLite(_settings.seed + 2000);
            _noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            _noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Manhattan);
            _noise.SetFrequency(1f / _settings.crackScale);
            _noise.SetFractalOctaves(3);
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
            
            // Применяем трещины к базовой карте
            return ApplyCracksToMap(baseMap, width, height);
        }
        
        // Основной метод для применения трещин к внешней карте
        public float[,] ApplyCracksToExternalMap(float[,] originalMap)
        {
            int width = originalMap.GetLength(0);
            int height = originalMap.GetLength(1);
            return ApplyCracksToMap(originalMap, width, height);
        }

        private float[,] ApplyCracksToMap(float[,] originalMap, int width, int height)
        {
            float[,] crackedMap = new float[width, height];
            
            // Копируем оригинальную карту
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                crackedMap[x, y] = originalMap[x, y];
            }
            
            // Применяем трещины (точно как в TestNoiseGenerator)
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                // Генерируем шум трещин с удвоенной частотой (точно как в оригинале)
                float crackValue = _noise.GetNoise(x * 1.5f, y * 1.5f);
                
                // Применяем резкость (точно как в TestNoiseGenerator)
                crackValue = Mathf.Pow(Mathf.Abs(crackValue), _settings.crackSharpness);
                
                // Применяем трещины если значение превышает порог
                if (crackValue > _settings.crackThreshold)
                {
                    crackedMap[x, y] -= _settings.crackStrength * crackValue;
                }
            }
            
            return crackedMap;
        }
        
        // Метод для получения только маски трещин
        public float[,] GetCrackMask(int width, int height)
        {
            float[,] crackMask = new float[width, height];
            
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float crackValue = _noise.GetNoise(x * 1.5f, y * 1.5f);
                crackValue = Mathf.Pow(Mathf.Abs(crackValue), _settings.crackSharpness);
                
                crackMask[x, y] = crackValue > _settings.crackThreshold ? crackValue : 0f;
            }
            
            return crackMask;
        }
        // Метод для применения трещин с определенной силой в определенной точке
        public float ApplyCrackToPoint(float originalValue, int x, int y)
        {
            float crackValue = _noise.GetNoise(x * 1.5f, y * 1.5f);
            crackValue = Mathf.Pow(Mathf.Abs(crackValue), _settings.crackSharpness);
            
            if (crackValue > _settings.crackThreshold)
            {
                return originalValue - _settings.crackStrength * crackValue;
            }
            
            return originalValue;
        }

        public void UpdateNoiseMap(object settings)
        {
            if (settings is CrackSettings newSettings)
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