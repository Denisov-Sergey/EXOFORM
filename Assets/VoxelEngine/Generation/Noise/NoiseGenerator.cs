using UnityEngine;

namespace VoxelEngine.Generation.Noise
{
    public class NoiseGenerator
    {
        private FastNoiseLite _noise;
        
        public NoiseGenerator(int seed)
        {
            _noise = new FastNoiseLite(seed);
            _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        }

        public float GetPerlin(float x, float z)
        {
            return _noise.GetNoise(x, z);
        }

        public float GetSimplex(float x, float z)
        {
            _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            return _noise.GetNoise(x, z);
        }

        public float GetRidged(float x, float z)
        {
            _noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
    
            _noise.SetFractalType(FastNoiseLite.FractalType.Ridged);
            _noise.SetFractalOctaves(6);  // Количество октав
            _noise.SetFractalGain(0.5f); // Усиление для сглаживания переходов
    
            return _noise.GetNoise(x, z);
        }
    }
}