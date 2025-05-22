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
                    return new BaseNoiseGenerator(baseNoiseSettings);
                
                case DomainWarpSettings domainWarpSettings:
                    return new DomainWarpNoiseGenerator(domainWarpSettings);

                case VoronoiSettings voronoiSettings:
                    return new VoronoiNoiseGenerator(voronoiSettings);
                
                case DepressionSettings depressionSettings:
                    return new DepressionNoiseGenerator(depressionSettings);
                
                case CrackSettings crackSettings:
                    return new CrackNoiseGenerator(crackSettings);
                
                
                default:
                    throw new ArgumentException($"Settings not supported: {settings}");
            }
            return null;
        }
    }
}