using System;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Interface;
using WorldGenerator.Settings;

namespace WorldGenerator.Noise
{
    public class BaseNoiseGenerator : INoiseGenerator
    {
        private FastNoiseLite _noise;
        private BaseNoiseSettings _settings;

        public BaseNoiseGenerator(BaseNoiseSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));;
            ConfigureNoise();
        }

        private void ConfigureNoise()
        {
            _noise = new FastNoiseLite(_settings.seed);
         
            _noise.SetNoiseType(_settings.noiseType);
            _noise.SetFrequency(1f / _settings.scale);
            _noise.SetFractalOctaves(_settings.octaves);
            _noise.SetFractalGain(_settings.persistence);
        }
        
        public float[,] GenerateNoiseMap(int width, int height)
        {
            float[,] map = new float[width, height];
            
            for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * _settings.scale;
                float yCoord = (float)y / height * _settings.scale;
                
                map[x,y] = -_noise.GetNoise(xCoord, yCoord);
            }

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