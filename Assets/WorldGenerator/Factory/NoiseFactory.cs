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
            return settings switch
            {
                BaseNoiseSettings baseNoiseSettings => new BaseNoiseGenerator(baseNoiseSettings),
                VoronoiSettings voronoiSettings => new VoronoiNoiseGenerator(voronoiSettings),
                CombinedNoiseSettings combinedNoiseSettings => new CombinedNoiseGenerator(combinedNoiseSettings),
                
                DomainWarpSettings domainWarpSettings => new DomainWarpNoiseGenerator(domainWarpSettings),
                
                DepressionSettings depressionSettings => new DepressionNoiseGenerator(depressionSettings),
                CrackSettings crackSettings => new CrackNoiseGenerator(crackSettings),
                _ => throw new ArgumentException($"Settings not supported: {settings?.GetType().Name}")
            };
        }
        
        // Дополнительный метод для типобезопасного создания
        public static T CreateGenerator<T>(NoiseSettings settings) where T : class, INoiseGenerator
        {
            var generator = CreateGenerator(settings);
            return generator as T ?? throw new InvalidCastException($"Cannot cast to {typeof(T).Name}");
        }
    }
}
