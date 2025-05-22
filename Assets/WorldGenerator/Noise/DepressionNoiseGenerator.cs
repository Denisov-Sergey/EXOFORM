using System;
using VoxelEngine.Generation.Noise;
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
            // _baseGenerator = NoiseFactory.CreateGenerator(_settings.baseNoise);
            ConfigureNoise();
        }

        private void ConfigureNoise()
        {
            _noise = new FastNoiseLite(_settings.seed);
        }
        
        public float[,] GenerateNoiseMap(int width, int height)
        {
            float[,] map = new float[height, width];
            // ... генерация ...
            return map;
        }

        public void UpdateNoiseMap(object settings)
        {
            if (settings is DepressionSettings newSettings)
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