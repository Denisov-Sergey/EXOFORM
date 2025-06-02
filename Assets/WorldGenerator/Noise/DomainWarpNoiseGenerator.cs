using System;
using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;
using WorldGenerator.Factory;
using WorldGenerator.Interface;
using WorldGenerator.Settings;

namespace WorldGenerator.Noise
{
    public class DomainWarpNoiseGenerator : INoiseGenerator
    {
        private FastNoiseLite _noise;
        private DomainWarpSettings _settings;
        private readonly INoiseGenerator _baseGenerator;
        public DomainWarpNoiseGenerator(DomainWarpSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));;
            // _baseGenerator = NoiseFactory.CreateGenerator(settings.ba); 
            if (_settings.baseNoise != null)
            {
                _baseGenerator = NoiseFactory.CreateGenerator(_settings.baseNoise);
            }

            ConfigureNoise();
        }
        
        private void ConfigureNoise()
        {
            _noise = new FastNoiseLite(_settings.seed + 500);
            
            _noise.SetNoiseType(_settings.noiseType);
            _noise.SetCellularDistanceFunction(_settings.cellularDistanceFunction);
            _noise.SetCellularReturnType(_settings.cellularReturnType);
            _noise.SetFractalOctaves(_settings.octaves);
            _noise.SetFractalGain(_settings.persistence);
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
                // Если нет базового генератора, создаем простую карту Perlin
                baseMap = GenerateSimplePerlinMap(width, height);
            }
            
            // Применяем Domain Warping к базовой карте
            return ApplyWarpingToExternalMap(baseMap);
        }
        
        private float[,] GenerateSimplePerlinMap(int width, int height)
        {
            float[,] map = new float[width, height];
            var noise = new FastNoiseLite(_settings.seed);
            noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * _settings.scale;
                float yCoord = (float)y / height * _settings.scale;
                map[x, y] = noise.GetNoise(xCoord, yCoord);
            }
            
            return map;
        }
        
        // Основной метод для применения warping к внешней карте
        public float[,] ApplyWarpingToExternalMap(float[,] originalMap)
        {
            int width = originalMap.GetLength(0);
            int height = originalMap.GetLength(1);
            float[,] warpedMap = new float[width, height];
            
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                // Координаты в пространстве шума
                float xCoord = (float)x / width * _settings.scale;
                float yCoord = (float)y / height * _settings.scale;
                
                // Применяем искажение координат (точно как в TestNoiseGenerator)
                ApplyDomainWarpingToCoordinates(ref xCoord, ref yCoord);
                
                // Преобразуем искаженные координаты обратно в пиксели карты
                float warpedPixelX = (xCoord / _settings.scale) * width;
                float warpedPixelY = (yCoord / _settings.scale) * height;
                
                // Используем билинейную интерполяцию для сглаживания
                warpedMap[x, y] = SampleMapBilinear(originalMap, warpedPixelX, warpedPixelY, width, height);
            }
            
            return warpedMap;
        }

        // Метод для искажения координат (точно как в TestNoiseGenerator)
        public void ApplyDomainWarpingToCoordinates(ref float xCoord, ref float yCoord)
        {
            // Точно такая же логика как в TestNoiseGenerator
            float warpX = _noise.GetNoise(
                xCoord + _settings.offsetX, 
                yCoord + _settings.offsetY
            ) * _settings.strength;

            float warpY = _noise.GetNoise(
                xCoord - _settings.offsetX, 
                yCoord - _settings.offsetY
            ) * _settings.strength;

            xCoord += warpX;
            yCoord += warpY;
        }
        private float SampleMapBilinear(float[,] map, float x, float y, int width, int height)
        {
            // Обрезаем координаты до границ карты
            x = Mathf.Clamp(x, 0, width - 1.001f);
            y = Mathf.Clamp(y, 0, height - 1.001f);
            
            // Целые и дробные части
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            int x1 = Mathf.Min(x0 + 1, width - 1);
            int y1 = Mathf.Min(y0 + 1, height - 1);
            
            float fx = x - x0;
            float fy = y - y0;
            
            // Билинейная интерполяция
            float top = Mathf.Lerp(map[x0, y0], map[x1, y0], fx);
            float bottom = Mathf.Lerp(map[x0, y1], map[x1, y1], fx);
            
            return Mathf.Lerp(top, bottom, fy);
        }

        public void UpdateNoiseMap(object settings)
        {
            if (settings is DomainWarpSettings newSettings)
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
        
        // Методы для отладки (получение полей искажения)
        public float[,] GetWarpFieldX(int width, int height)
        {
            float[,] warpField = new float[width, height];
            
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * _settings.scale;
                float yCoord = (float)y / height * _settings.scale;
                
                warpField[x, y] = _noise.GetNoise(
                    xCoord + _settings.offsetX, 
                    yCoord + _settings.offsetY
                ) * _settings.strength;
            }
            
            return warpField;
        }
        
        public float[,] GetWarpFieldY(int width, int height)
        {
            float[,] warpField = new float[width, height];
            
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * _settings.scale;
                float yCoord = (float)y / height * _settings.scale;
                
                warpField[x, y] = _noise.GetNoise(
                    xCoord - _settings.offsetX, 
                    yCoord - _settings.offsetY
                ) * _settings.strength;
            }
            
            return warpField;
        }
    }
}
