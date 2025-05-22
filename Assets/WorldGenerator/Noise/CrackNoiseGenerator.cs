using System;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Interface;
using WorldGenerator.Settings;

namespace WorldGenerator.Noise
{
    public class CrackNoiseGenerator : INoiseGenerator
    {
        private FastNoiseLite _noise;
        private CrackSettings _settings;

        public CrackNoiseGenerator(CrackSettings settings)
        {
            _settings = settings ?? _settings;
            ConfigureNoise();
        }

        private void ConfigureNoise()
        {
            _noise = new FastNoiseLite();
        }
        
        public float[,] GenerateNoiseMap(int width, int height)
        {
            float[,] map = new float[height, width];
            // ... генерация ...
            return map;
        }

        public void UpdateNoiseMap(object settings)
        {
            if (settings is CrackSettings newSettings)
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