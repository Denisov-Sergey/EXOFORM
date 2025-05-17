using UnityEngine;
using VoxelEngine.Core;
using VoxelEngine.Generation.Noise;

namespace VoxelEngine.Generation
{
    /// <summary>
    /// Улучшенный генератор мира с поддержкой Domain Warping и оптимизированным кэшированием
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Базовые настройки")]
        [SerializeField] private int _seed = 12345;
        [SerializeField] private float _terrainScale = 80f;
        [SerializeField] private float _baseHeight = 0f;

        [Header("Горный рельеф")]
        [SerializeField] private float _mountainFrequency = 0.3f;
        [SerializeField] private float _mountainAmplitude = 5f;
        [SerializeField][Range(0, 1)] private float _rockSteepnessThreshold = 0.6f;

        [Header("Речные русла")]
        [SerializeField] private float _riverFrequency = 0.2f;
        [SerializeField] private float _riverDepth = 3f;

        [Header("Вулканические зоны")]
        [SerializeField] private float _volcanoFrequency = 0.1f;
        [SerializeField] private float _volcanoHeight = 5f;

        [Header("Биомы и заражение")]
        [SerializeField] private float _biomeScale = 200f;
        [SerializeField] private float _corruptionScale = 350f;
        [SerializeField][Range(0, 1)] private float _corruptionThreshold = 0.85f;

        [Header("Domain Warping")]
        [SerializeField] private float _warpStrength = 30f;
        [SerializeField] private float _warpScale = 0.05f;

        private NoiseGenerator _noise;
        private DomainWarping _domainWarping;
        private TerrainData[,] _terrainCache;

        // Структура для кэширования данных поверхности
        private struct TerrainData
        {
            public float height;
            public BiomeType biome;
            public float steepness;
            public float corruption;
            public Vector2 warpedPos;
        }

        void Start()
        {
            InitializeNoiseSystems();
            VoxelEngineManager.Instance.ChunkManager.OnChunkLoad += GenerateChunk;
        }

        /// <summary>
        /// Инициализация шумовых систем с раздельными сидами
        /// </summary>
        private void InitializeNoiseSystems()
        {
            _noise = new NoiseGenerator(_seed);
            _domainWarping = new DomainWarping(_seed + 1, _warpStrength); // Разные сиды для разделения паттернов
        }

        private void GenerateChunk(Chunk chunk)
        {
            Vector3 chunkWorldPos = CalculateChunkWorldPosition(chunk);
            PrecalculateTerrainData(chunk, chunkWorldPos);
            
            VoxelData[,,] data = new VoxelData[chunk.Size, chunk.Size, chunk.Size];
            
            for (int x = 0; x < chunk.Size; x++)
            {
                for (int z = 0; z < chunk.Size; z++)
                {
                    var terrain = _terrainCache[x, z];
                    for (int y = 0; y < chunk.Size; y++)
                    {
                        float globalY = chunkWorldPos.y + y * chunk.VoxelSize;
                        data[x, y, z] = CalculateVoxel(
                            terrain,
                            globalY,
                            chunkWorldPos.x + x * chunk.VoxelSize,
                            chunkWorldPos.z + z * chunk.VoxelSize
                        );
                    }
                }
            }
            chunk.SetVoxelData(data);
        }

        /// <summary>
        /// Предварительный расчет всех данных поверхности с применением Domain Warping
        /// </summary>
        private void PrecalculateTerrainData(Chunk chunk, Vector3 chunkWorldPos)
        {
            _terrainCache = new TerrainData[chunk.Size, chunk.Size];
            
            for (int x = 0; x < chunk.Size; x++)
            {
                float globalX = chunkWorldPos.x + x * chunk.VoxelSize;
                for (int z = 0; z < chunk.Size; z++)
                {
                    float globalZ = chunkWorldPos.z + z * chunk.VoxelSize;
                    
                    // Применяем Domain Warping
                    Vector2 warped = _domainWarping.Warp(
                        globalX * _warpScale, 
                        globalZ * _warpScale
                    );

                    _terrainCache[x, z] = new TerrainData
                    {
                        warpedPos = warped,
                        height = CalculateTotalHeight(warped.x, warped.y),
                        biome = GetBiome(warped.x, warped.y),
                        steepness = CalculateSteepness(warped.x, warped.y),
                        corruption = _noise.GetSimplex(
                            warped.x / _corruptionScale, 
                            warped.y / _corruptionScale
                        )
                    };
                }
            }
        }

        /// <summary>
        /// Комбинированный расчет высоты с использованием искаженных координат
        /// </summary>
        private float CalculateTotalHeight(float warpedX, float warpedZ)
        {
            float height = _baseHeight + _noise.GetPerlin(warpedX / _terrainScale, warpedZ / _terrainScale) * 2f;
            height += _noise.GetRidged(warpedX * _mountainFrequency, warpedZ * _mountainFrequency) * _mountainAmplitude;
            height -= Mathf.Abs(_noise.GetSimplex(warpedX * _riverFrequency, warpedZ * _riverFrequency)) * _riverDepth;
            height += _noise.GetPerlin(warpedX * _volcanoFrequency, warpedZ * _volcanoFrequency) > 0.8f ? _volcanoHeight : 0;
            return height;
        }
        
        private BiomeType GetBiome(float x, float z)
        {
            float noise = _noise.GetPerlin(x / _biomeScale, z / _biomeScale);
            return noise switch
            {
                < -0.33f => BiomeType.Wasteland,
                < 0.33f => BiomeType.Mountains,
                _ => BiomeType.InfectedCity
            };
        }

        private float CalculateSteepness(float x, float z)
        {
            float dx = _noise.GetPerlin(x + 0.1f, z) - _noise.GetPerlin(x - 0.1f, z);
            float dz = _noise.GetPerlin(x, z + 0.1f) - _noise.GetPerlin(x, z - 0.1f);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private VoxelData CalculateVoxel(TerrainData terrain, float globalY, float x, float z)
        {
            if (globalY > terrain.height) 
                return new VoxelData { Type = VoxelType.Air };

            Debug.Log($"terrain.corruption: {terrain.corruption}");
            Debug.Log($"_corruptionThreshold: {_corruptionThreshold}");
            
            // if (terrain.corruption > _corruptionThreshold)
                // return new VoxelData { Type = VoxelType.CorruptedBiomass, Color = GetColor(VoxelType.CorruptedBiomass) };

            return terrain.biome switch
            {
                BiomeType.Mountains => CalculateMountainVoxel(terrain.steepness),
                BiomeType.Wasteland => CalculateWastelandVoxel(globalY, terrain.height, terrain.steepness),
                BiomeType.InfectedCity => CalculateCityVoxel(globalY, terrain.height),
                _ => new VoxelData { Type = VoxelType.Stone, Color = GetColor(VoxelType.Stone) }
            };
        }

        private VoxelData CalculateMountainVoxel(float steepness)
        {
            var type = steepness > _rockSteepnessThreshold ? 
                VoxelType.Stone : 
                VoxelType.VolcanicRock;
            
            return new VoxelData { Type = type, Color = GetColor(type) };
        }

        private VoxelData CalculateWastelandVoxel(float y, float height, float steepness)
        {
            var type = steepness > 0.4f ? 
                VoxelType.Stone : 
                y > height - 1f ? VoxelType.CrackedDirt : VoxelType.Dirt;
            
            return new VoxelData { Type = type, Color = GetColor(type) };
        }

        private VoxelData CalculateCityVoxel(float y, float height)
        {
            var type = y > height - 2f ? 
                VoxelType.MetalDebris : 
                VoxelType.RustedMetal;
            
            return new VoxelData { Type = type, Color = GetColor(type) };
        }

        private Color32 GetColor(VoxelType type) => type switch
        {
            VoxelType.CrackedDirt => new Color32(100, 70, 50, 255),
            VoxelType.VolcanicRock => new Color32(80, 80, 80, 255),
            VoxelType.CorruptedBiomass => new Color32(160, 30, 200, 255),
            VoxelType.MetalDebris => new Color32(150, 150, 150, 255),
            VoxelType.RustedMetal => new Color32(180, 100, 50, 255),
            _ => Color.magenta
        };

        private Vector3 CalculateChunkWorldPosition(Chunk chunk) => new Vector3(
            chunk.ChunkPosition.x * (chunk.Size * chunk.VoxelSize),
            chunk.ChunkPosition.y * (chunk.Size * chunk.VoxelSize),
            chunk.ChunkPosition.z * (chunk.Size * chunk.VoxelSize)
        );
    }
}