using UnityEngine;


namespace VoxelEngine.Generation.Noise
{
    public class DomainWarping
    {
        private float _seed;

        public DomainWarping(float seed) {
            _seed = seed;
        }

        public Vector2 Warp(float x, float z) {
            float warpX = Mathf.PerlinNoise((x + _seed + 1000) / 10f, (z + _seed) / 10f) * 5f;
            float warpZ = Mathf.PerlinNoise((x + _seed) / 10f, (z + _seed + 1000) / 10f) * 5f;
            return new Vector2(x + warpX, z + warpZ);
        }
    }
}
