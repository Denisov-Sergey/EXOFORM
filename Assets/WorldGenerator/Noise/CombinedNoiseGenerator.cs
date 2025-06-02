using System;
using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Interface;
using WorldGenerator.Settings;

namespace WorldGenerator.Noise
{
    public class CombinedNoiseGenerator : INoiseGenerator
    {
        private FastNoiseLite _noise;
        private CombinedNoiseSettings _settings;

        public CombinedNoiseGenerator(CombinedNoiseSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));;
            ConfigureNoise();
        }

        private void ConfigureNoise()
        {
            
            _noise = new FastNoiseLite(_settings.seed);
            _noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            _noise.SetFractalOctaves(_settings.octaves);
            _noise.SetFractalGain(_settings.persistence);
            _noise.SetFrequency(_settings.frequency);

        }
        
        public float[,] GenerateNoiseMap(int width, int height)
        {
            float[,] map = new float[width, height];
            
            for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * _settings.scale;
                float yCoord = (float)y / height * _settings.scale;
                
                
                // Основной шум
                float noiseValue = _noise.GetNoise(xCoord, yCoord);
                // noiseValue = Math.Abs(noiseValue - 0.5f) * 2f;
                // noiseValue = 1f - noiseValue;
                
                noiseValue = (float)Math.Pow(noiseValue, _settings.heightExponent);
                if (noiseValue > _settings.cutoffThreshold) noiseValue = 0f;
                
                map[x,y] = -noiseValue;
            }

            return map;
        }

        public void UpdateNoiseMap(object settings)
        {
            if (settings is CombinedNoiseSettings newSettings)
            {
                _settings = newSettings;
                ConfigureNoise();
            }
            else
            {
                throw new ArgumentException($"Settings not supported: {settings}");
            }
        }
    }
}