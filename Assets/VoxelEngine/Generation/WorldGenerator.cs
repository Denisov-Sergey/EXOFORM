using UnityEngine;
using VoxelEngine.Core;
using VoxelEngine.Generation.Noise;

namespace VoxelEngine.Generation
{
    /// <summary>
    /// Продвинутый генератор процедурного мира с поддержкой различных биомов,
    /// Domain Warping и динамическим кэшированием данных
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Базовые настройки")]
        [Tooltip("Базовый сид для генерации мира (меняйте для совершенно другого ландшафта)")]
        [SerializeField] private int _seed = 12345;
        
        [Tooltip("Общий масштаб террейна (меньше значение = более крупные детали)")]
        [SerializeField] private float _terrainScale = 40f;
        
        [Tooltip("Базовая высота уровня моря")]
        [SerializeField] private float _baseHeight = 0f;

        [Header("Горный рельеф")]
        [Tooltip("Частота горных хребтов (выше = больше горных образований)")]
        [SerializeField] private float _mountainFrequency = 3f;
        
        [Tooltip("Высота горных пиков")]
        [SerializeField] private float _mountainAmplitude = 8f;
        
        [Tooltip("Порог крутизны для перехода с земли на камень (0-1)")]
        [SerializeField][Range(0, 1)] private float _rockSteepnessThreshold = 0.6f;

        [Header("Речные русла")]
        [Tooltip("Частота речных изгибов (выше = более извилистые реки)")]
        [SerializeField] private float _riverFrequency = 0.5f;
        
        [Tooltip("Глубина вымывания речных русел")]
        [SerializeField] private float _riverDepth = 7f;

        [Header("Вулканические зоны")]
        [Tooltip("Частота вулканических образований")]
        [SerializeField] private float _volcanoFrequency = 0.1f;
        
        [Tooltip("Высота вулканических конусов")]
        [SerializeField] private float _volcanoHeight = 9f;

        [Header("Биомы и заражение")]
        [Tooltip("Масштаб зон биомов (больше значение = более крупные биомы)")]
        [SerializeField] private float _biomeScale = 30;
        
        [Tooltip("Масштаб зараженных зон")]
        [SerializeField] private float _corruptionScale = 350f;
        
        [Tooltip("Порог активации заражения (0-1)")]
        [SerializeField][Range(0, 1)] private float _corruptionThreshold = 0.85f;

        [Header("Domain Warping")]
        [Tooltip("Сила искажения ландшафта")]
        [SerializeField] private float _warpStrength = 50f;
        
        [Tooltip("Масштаб эффекта искажения")]
        [SerializeField] private float _warpScale = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool _showHeightGizmos = true;
        [SerializeField] [Range(1, 16)] private int _debugGridStep = 4;
        
        // Шумовые генераторы и системы искажения
        private NoiseGenerator _noise;
        private DomainWarping _domainWarping;
        
        // Кэш предрассчитанных данных поверхности
        private TerrainData[,] _terrainCache;

        /// <summary>
        /// Структура для хранения предрассчитанных данных поверхности
        /// </summary>
        private struct TerrainData
        {
            public float height;        // Общая высота точки
            public BiomeType biome;     // Тип биома
            public float steepness;      // Крутизна склона (0-1)
            public float corruption;     // Уровень заражения (0-1)
            public Vector2 warpedPos;   // Искаженные координаты
        }

        /// <summary>
        /// Типы биомов
        /// </summary>
        private enum BiomeType
        {
            Wasteland,     // Пустоши с трещинами
            Mountains,    // Горные массивы
            InfectedCity   // Зараженные городские руины
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
            _domainWarping = new DomainWarping(_seed + 1, _warpStrength);
        }

        /// <summary>
        /// Основной метод генерации чанка
        /// </summary>
        /// <param name="chunk">Чанк для генерации</param>
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
        /// Предрасчет всех данных поверхности для чанка
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
                    
                    // Применение Domain Warping к координатам
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
        /// Расчет общей высоты с комбинацией разных типов шума
        /// </summary>
        private float CalculateTotalHeight(float warpedX, float warpedZ)
        {
            BiomeType biome = GetBiome(warpedX, warpedZ);
            
            // Базовый шум ландшафта
            float baseNoise = _noise.GetPerlin(warpedX / _terrainScale, warpedZ / _terrainScale) * 2f;
            
            float mountainNoise = Mathf.PerlinNoise(
                warpedX * _mountainFrequency, 
                warpedZ * _mountainFrequency
            ) * _mountainAmplitude;
            mountainNoise = Mathf.Clamp(mountainNoise, 0, 10f);

            
            float riverNoise = Mathf.Abs(_noise.GetSimplex(
                warpedX * _riverFrequency, 
                warpedZ * _riverFrequency
            )) * _riverDepth * 2f;
            
            float volcanoNoise = _noise.GetRidged(
                warpedX * _volcanoFrequency * 0.5f, 
                warpedZ * _volcanoFrequency * 0.5f
            );
            float volcanoAdd = volcanoNoise > 0.7f ? _volcanoHeight : 0;

            float height = _baseHeight;
    
            
            Debug.Log($"Height components:\n" +
                      $"biome: {biome}\n" +
                      $"Base: {baseNoise}\n" +
                      $"Mountains: {mountainNoise}\n" +
                      $"Rivers: {-riverNoise}\n" +
                      $"Volcano: {volcanoAdd}\n" +
                      $"TOTAL: {height}");
            
            switch(biome)
            {
                case BiomeType.Mountains:
                    height += baseNoise * 0.5f + mountainNoise * 1.5f;
                    break;
            
                case BiomeType.Wasteland:
                    height += baseNoise * 1.2f - riverNoise * 1.5f;
                    break;
            
                case BiomeType.InfectedCity:
                    height += baseNoise * 0.8f + volcanoAdd * 2f;
                    break;
            }

            Debug.Log($"Height: {height}");
            
            return height;
        }
        
        /// <summary>
        /// Определение биома по координатам
        /// </summary>
        private BiomeType GetBiome(float x, float z)
        {
            float noise = _noise.GetPerlin(x / _biomeScale, z / _biomeScale);
            return noise switch
            {
                < -0.5f => BiomeType.Wasteland,    // 35% - Пустоши с реками
                < 0.5f => BiomeType.Mountains,     // 50% - Горы
                _ => BiomeType.InfectedCity        // 15% - Вулканические зоны
            };
        }

        /// <summary>
        /// Расчет крутизны склона через градиент шума
        /// </summary>
        private float CalculateSteepness(float x, float z)
        {
            float scale = 0.05f;
            float dx = _noise.GetPerlin(x + scale, z) - _noise.GetPerlin(x - scale, z);
            float dz = _noise.GetPerlin(x, z + scale) - _noise.GetPerlin(x, z - scale);
            return Mathf.Clamp(Mathf.Sqrt(dx * dx + dz * dz) * 10f, 0f, 1f);
        }

        /// <summary>
        /// Основная логика определения типа вокселя
        /// </summary>
        private VoxelData CalculateVoxel(TerrainData terrain, float globalY, float x, float z)
        {
            // Воздух выше поверхности
            if (globalY > terrain.height) 
                return new VoxelData { Type = VoxelType.Air };

            // Применение заражения
            // if (terrain.corruption > _corruptionThreshold)
                // return new VoxelData { 
                    // Type = VoxelType.CorruptedBiomass, 
                    // Color = GetColor(VoxelType.CorruptedBiomass) 
                // };

            // Распределение по биомам
            return terrain.biome switch
            {
                BiomeType.Mountains => CalculateMountainVoxel(terrain.steepness),
                BiomeType.Wasteland => CalculateWastelandVoxel(globalY, terrain.height, terrain.steepness),
                BiomeType.InfectedCity => CalculateCityVoxel(globalY, terrain.height),
                _ => new VoxelData { Type = VoxelType.Stone, Color = GetColor(VoxelType.Stone) }
            };
        }

        /// <summary>
        /// Генерация горных вокселей
        /// </summary>
        private VoxelData CalculateMountainVoxel(float steepness)
        {
            var type = steepness > _rockSteepnessThreshold ? 
                VoxelType.Stone : 
                VoxelType.VolcanicRock;
            
            return new VoxelData { Type = type, Color = GetColor(type) };
        }

        /// <summary>
        /// Генерация вокселей пустошей
        /// </summary>
        private VoxelData CalculateWastelandVoxel(float y, float height, float steepness)
        {
            var type = steepness > 0.4f ? 
                VoxelType.Stone : 
                y > height - 1f ? VoxelType.CrackedDirt : VoxelType.Dirt;
            
            return new VoxelData { Type = type, Color = GetColor(type) };
        }

        /// <summary>
        /// Генерация городских руин
        /// </summary>
        private VoxelData CalculateCityVoxel(float y, float height)
        {
            var type = y > height - 2f ? 
                VoxelType.MetalDebris : 
                VoxelType.RustedMetal;
            
            return new VoxelData { Type = type, Color = GetColor(type) };
        }

        /// <summary>
        /// Цветовая палитра вокселей
        /// </summary>
        private Color32 GetColor(VoxelType type) => type switch
        {
            VoxelType.Dirt => new Color32(120, 85, 60, 255),
            VoxelType.CrackedDirt => new Color32(100, 70, 50, 255),
            VoxelType.Stone => new Color32(130, 130, 130, 255),
            VoxelType.VolcanicRock => new Color32(80, 80, 80, 255),
            VoxelType.CorruptedBiomass => new Color32(160, 30, 200, 255),
            VoxelType.MetalDebris => new Color32(150, 150, 150, 255),
            VoxelType.RustedMetal => new Color32(180, 100, 50, 255),
            _ => Color.magenta // Индикатор ошибки
        };

        /// <summary>
        /// Конвертация позиции чанка в мировые координаты
        /// </summary>
        private Vector3 CalculateChunkWorldPosition(Chunk chunk) => new Vector3(
            chunk.ChunkPosition.x * (chunk.Size * chunk.VoxelSize),
            chunk.ChunkPosition.y * (chunk.Size * chunk.VoxelSize),
            chunk.ChunkPosition.z * (chunk.Size * chunk.VoxelSize)
        );
        
        private void OnDrawGizmos()
        {
            if (!_showHeightGizmos || _terrainCache == null) return;

            for (int x = 0; x < _terrainCache.GetLength(0); x += _debugGridStep)
            {
                for (int z = 0; z < _terrainCache.GetLength(1); z += _debugGridStep)
                {
                    var data = _terrainCache[x, z];
                    Gizmos.color = Color.Lerp(Color.blue, Color.red, data.height / 50f);
                    Gizmos.DrawCube(
                        new Vector3(x, data.height, z),
                        Vector3.one * 0.3f
                    );
                }
            }
        }
    }
}