using UnityEngine;

namespace VoxelEngine.Generation.Noise
{
    public class DomainWarping
    {
        private NoiseGenerator _noise;
        private float _warpStrength;

        public DomainWarping(int seed, float warpStrength = 50f)
        {
            _noise = new NoiseGenerator(seed);
            _warpStrength = warpStrength;
        }

        public Vector2 Warp(float x, float z)
        {
            float warpX = _noise.GetPerlin(x * 0.1f, z * 0.1f) * _warpStrength;
            float warpZ = _noise.GetPerlin(x * 0.1f + 1000, z * 0.1f + 1000) * _warpStrength;
            return new Vector2(x + warpX, z + warpZ);
        }
    }
}