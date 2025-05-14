using UnityEngine;
using VoxelEngine.Core;
using VoxelEngine.Generation.Noise;


namespace VoxelEngine.Generation
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Настройки")]
        [SerializeField] private int _seed = 0;
        [SerializeField] private float _noiseScale = 20f;
        [SerializeField] private float _heightMultiplier = 3f;

        [Header("Горы")]
        [SerializeField] private float _warpScale = 10f;
        [SerializeField] private float _warpStrength = 5f;
        
        
        private DomainWarping _domainWarping;
        void Start() {
            _domainWarping = new DomainWarping(_seed);
            VoxelEngineManager.Instance.ChunkManager.OnChunkLoad += GenerateChunk;
        }

        private void GenerateChunk(Chunk chunk) {
            VoxelData[,,] data = new VoxelData[16, 16, 16];
        
            for (int x = 0; x < 16; x++) {
                for (int z = 0; z < 16; z++) {
                    // Генерация высот с использованием Domain Warping
                    // Vector2 warpedPos = _domainWarping.Warp(x, z);
                    // float height = Mathf.PerlinNoise(warpedPos.x / _noiseScale, warpedPos.y / _noiseScale) * _heightMultiplier;
                    // Vector2 warpedPos = _domainWarping.Warp(x, z);
                    float height = Mathf.PerlinNoise(x / _noiseScale, z / _noiseScale) * _heightMultiplier;
                
                    for (int y = 0; y < height; y++) {
                        data[x, y, z] = new VoxelData { Type = 1 }; // Камень
                    }
                }
            }
        
            chunk.SetVoxelData(data);
        }
    }
}