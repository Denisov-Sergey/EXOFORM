using System;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Interface;
using WorldGenerator.Settings;

namespace WorldGenerator.Noise
{
    public class FractalNoiseGenerator : INoiseGenerator
    {
        private FastNoiseLite _noise;
        private BaseNoiseSettings _settings;

        public FractalNoiseGenerator(BaseNoiseSettings settings)
        {
            settings = settings ?? new BaseNoiseSettings();
            ConfigureNoise();
        }

        private void ConfigureNoise()
        {
            _noise = new FastNoiseLite(_settings.seed);
            _noise.SetNoiseType(_settings.noiseType);
            _noise.SetFrequency(1f / _settings.scale);
        }
        
        public float[,] GenerateNoiseMap(int width, int height)
        {
            float[,] map = new float[height, width];
            // ... генерация ...
            return map;
        }

        public void UpdateNoiseMap(object settings)
        {
            if (settings is BaseNoiseSettings newSettings)
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