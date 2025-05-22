using System;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Interface;
using WorldGenerator.Settings;

namespace WorldGenerator.Noise
{
    public class VoronoiNoiseGenerator : INoiseGenerator
    {
        private FastNoiseLite _noise;
        private VoronoiSettings _settings;

        public VoronoiNoiseGenerator(VoronoiSettings settings)
        {
            _settings = settings ?? _settings;
            ConfigureNoise();
        }

        private void ConfigureNoise()
        {
            _noise = new FastNoiseLite(_settings.seed);
            
            _noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            _noise.SetCellularDistanceFunction(_settings.cellularDistanceFunction);
            _noise.SetCellularReturnType(_settings.cellularReturnType);
            _noise.SetCellularJitter(_settings.jitter);
            _noise.SetFrequency(1f / _settings.scale);
        }
        
        public float[,] GenerateNoiseMap(int width, int height)
        {
            float[,] map = new float[width, height];
            
            for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * _settings.scale;
                float yCoord = (float)y / height * _settings.scale;
                
                map[x,y] = _noise.GetNoise(xCoord, yCoord);
            }

            return map;
        }

        public void UpdateNoiseMap(object settings)
        {
            if (settings is VoronoiSettings newSettings)
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