using System;
using WorldGenerator.Abstract;
using WorldGenerator.Interface;
using WorldGenerator.Noise;
using WorldGenerator.Settings;

namespace WorldGenerator.Factory
{
    public static class NoiseFactory
    {
        public static INoiseGenerator CreateGenerator(NoiseSettings settings)
        {
            switch (settings)
            {
                case BaseNoiseSettings baseNoiseSettings:
                    return new FractalNoiseGenerator(baseNoiseSettings);
                
                default:
                    throw new ArgumentException($"Settings not supported: {settings}");
            }
            return null;
        }
    }
}