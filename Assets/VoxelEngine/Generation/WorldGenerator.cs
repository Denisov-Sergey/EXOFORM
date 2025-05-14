using UnityEngine;
using VoxelEngine.Core;
using VoxelEngine.Generation.Noise;

namespace VoxelEngine.Generation
{
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Base Settings")]
        [SerializeField] private int _seed = 12345;
        [SerializeField] private float _terrainScale = 80f;
        [SerializeField] private float _baseHeight = 0f;

        [Header("Mountains")]
        [SerializeField] private float _mountainFrequency = 0.3f;
        [SerializeField] private float _mountainAmplitude = 5f;

        [Header("Rivers")]
        [SerializeField] private float _riverFrequency = 0.2f;
        [SerializeField] private float _riverDepth = 3f;

        [Header("Volcanoes")]
        [SerializeField] private float _volcanoFrequency = 0.1f;
        [SerializeField] private float _volcanoHeight = 5f;

        private NoiseGenerator _noise;

        void Start()
        {
            _noise = new NoiseGenerator(_seed);
            VoxelEngineManager.Instance.ChunkManager.OnChunkLoad += GenerateChunk;
        }

        private void GenerateChunk(Chunk chunk)
        {
            VoxelData[,,] data = new VoxelData[16, 16, 16];
            Vector3Int chunkWorldPos = chunk.ChunkPosition * 16;

            for (int x = 0; x < 16; x++)
            {
                float globalX = chunkWorldPos.x + x;
                for (int z = 0; z < 16; z++)
                {
                    float globalZ = chunkWorldPos.z + z;
                    
                    // Базовая высота
                    float height = _baseHeight + 
                        _noise.GetPerlin(globalX / _terrainScale, globalZ / _terrainScale) * 2f;
                    
                    // Добавляем элементы ландшафта
                    height += CalculateMountain(globalX, globalZ);
                    height -= CalculateRiver(globalX, globalZ);
                    height += CalculateVolcano(globalX, globalZ);

                    for (int y = 0; y < 16; y++)
                    {
                        int globalY = chunkWorldPos.y + y;
                        data[x, y, z] = GetVoxelType(globalY, height);
                    }
                }
            }

            chunk.SetVoxelData(data);
        }

        private float CalculateMountain(float x, float z)
        {
            return _noise.GetRidged(x * _mountainFrequency, z * _mountainFrequency) * _mountainAmplitude;
        }

        private float CalculateRiver(float x, float z)
        {
            float river = Mathf.Abs(_noise.GetSimplex(x * _riverFrequency, z * _riverFrequency));
            return river * _riverDepth;
        }

        private float CalculateVolcano(float x, float z)
        {
            float noise = _noise.GetPerlin(x * _volcanoFrequency, z * _volcanoFrequency);
            return noise > 0.8f ? _volcanoHeight : 0;
        }

        private VoxelData GetVoxelType(float y, float height)
        {
            if (y > height) return new VoxelData { Type = VoxelType.Air };
            
            // Автоматическое определение слоев
            float depth = height - y;
            
            // if (depth < 1f) return new VoxelData { Type = VoxelType.Grass };
            // if (depth < 3f) return new VoxelData { Type = VoxelType.Dirt };
            
            return new VoxelData { Type = VoxelType.Stone };
        }
    }
}