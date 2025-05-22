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
            _settings = settings ?? _settings;
            // _baseGenerator = NoiseFactory.CreateGenerator(settings.ba); 

            ConfigureNoise();
        }
        
        private void ConfigureNoise()
        {
            _noise = new FastNoiseLite(_settings.seed);
            
            _noise.SetNoiseType(_settings.noiseType);
            _noise.SetCellularDistanceFunction(_settings.cellularDistanceFunction);
            _noise.SetCellularReturnType(_settings.cellularReturnType);
            _noise.SetFractalOctaves(_settings.octaves);
            _noise.SetFractalGain(_settings.persistence);
        }
        
        public float[,] GenerateNoiseMap(int width, int height)
        {
            // Декоративный генератор (модифицируют существующий шум)
            float[,] baseMap = _baseGenerator.GenerateNoiseMap(width, height);

            for(int x = 0; x < width; x++)
            for(int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * _settings.scale;
                float yCoord = (float)y / height * _settings.scale;
                
                // Apply domain warping
                float warpX = _noise.GetNoise(
                    xCoord + _settings.offsetX, 
                    yCoord + _settings.offsetY
                ) * _settings.strength;

                float warpY = _noise.GetNoise(
                    xCoord - _settings.offsetX, 
                    yCoord - _settings.offsetY
                ) * _settings.strength;

                baseMap[x,y] = _baseGenerator.GenerateNoiseMap(
                    Mathf.RoundToInt(xCoord + warpX), 
                    Mathf.RoundToInt(yCoord + warpY)
                )[x,y];
            }

            return baseMap;
        }

        public void UpdateNoiseMap(object settings)
        {
            if (settings is DomainWarpSettings newSettings)
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
