using UnityEngine;
using VoxelEngine.Core;
using VoxelEngine.Generation.Noise;

namespace VoxelEngine.Generation
{
    /// <summary>
    /// Основной генератор воксельного мира, объединяющий различные алгоритмы генерации ландшафта
    /// </summary>
    public class WorldGenerator : MonoBehaviour
    {
        [Header("Базовые настройки")]
        [Tooltip("Сид для генерации мира (влияет на все элементы ландшафта)")]
        [SerializeField] private int _seed = 12345;
        
        [Tooltip("Масштаб базового рельефа (чем больше значение - тем более плавный ландшафт)")]
        [SerializeField] private float _terrainScale = 80f;
        
        [Tooltip("Базовая высота мирового уровня")]
        [SerializeField] private float _baseHeight = 0f;

        [Header("Горный рельеф")]
        [Tooltip("Частота горных образований (меньше значение = более крупные горы)")]
        [SerializeField] private float _mountainFrequency = 0.3f;
        
        [Tooltip("Высота горных пиков")]
        [SerializeField] private float _mountainAmplitude = 5f;

        [Header("Речные русла")]
        [Tooltip("Частота извилистости рек")]
        [SerializeField] private float _riverFrequency = 0.2f;
        
        [Tooltip("Глубина речных каньонов")]
        [SerializeField] private float _riverDepth = 3f;

        [Header("Вулканические зоны")]
        [Tooltip("Частота вулканических образований")]
        [SerializeField] private float _volcanoFrequency = 0.1f;
        
        [Tooltip("Высота вулканических конусов")]
        [SerializeField] private float _volcanoHeight = 5f;

        private NoiseGenerator _noise;

        /// <summary>
        /// Инициализация генератора и подписка на события менеджера чанков
        /// </summary>
        void Start()
        {
            _noise = new NoiseGenerator(_seed);
            VoxelEngineManager.Instance.ChunkManager.OnChunkLoad += GenerateChunk;
        }

        /// <summary>
        /// Основной метод генерации данных для чанка
        /// </summary>
        /// <param name="chunk">Чанк для заполнения данными</param>
        private void GenerateChunk(Chunk chunk)
        {
            Debug.Log($"chunk.Size: {chunk.Size}");
            Debug.Log($"chunk.Size: {chunk.VoxelSize}");
            
            VoxelData[,,] data = new VoxelData[chunk.Size, chunk.Size, chunk.Size];
            
            // Учитываем размер вокселя при расчёте мировой позиции чанка
            Vector3 chunkWorldPos = new Vector3(
                chunk.ChunkPosition.x * (chunk.Size * chunk.VoxelSize),
                chunk.ChunkPosition.y * (chunk.Size * chunk.VoxelSize),
                chunk.ChunkPosition.z * (chunk.Size * chunk.VoxelSize)
            );

            for (int x = 0; x < chunk.Size; x++)
            {
                float globalX = chunkWorldPos.x + x * chunk.VoxelSize;
                for (int z = 0; z < chunk.Size; z++)
                {
                    float globalZ = chunkWorldPos.z + z * chunk.VoxelSize;
                    
                    // Расчет базовой высоты с шумом Перлина
                    float height = _baseHeight + 
                        _noise.GetPerlin(
                            globalX / _terrainScale ,
                            globalZ / _terrainScale 
                        ) * 2f;
                    
                    // Наложение различных типов рельефа
                    height += CalculateMountain(globalX, globalZ);
                    height -= CalculateRiver(globalX, globalZ);
                    height += CalculateVolcano(globalX, globalZ);

                    // Заполнение вертикальных слоев
                    for (int y = 0; y < chunk.Size; y++)
                    {
                        float globalY = chunkWorldPos.y + y * chunk.VoxelSize;
                        data[x, y, z] = GetVoxelType(globalY, height);
                    }
                }
            }

            chunk.SetVoxelData(data);
        }

        /// <summary>
        /// Рассчитывает высоту горного рельефа
        /// </summary>
        private float CalculateMountain(float x, float z)
        {
            return _noise.GetRidged(x * _mountainFrequency, z * _mountainFrequency) * _mountainAmplitude;
        }

        /// <summary>
        /// Рассчитывает глубину речного русла
        /// </summary>
        private float CalculateRiver(float x, float z)
        {
            float river = Mathf.Abs(_noise.GetSimplex(x * _riverFrequency, z * _riverFrequency));
            return river * _riverDepth;
        }

        /// <summary>
        /// Генерирует вулканические образования
        /// </summary>
        private float CalculateVolcano(float x, float z)
        {
            float noise = _noise.GetPerlin(x * _volcanoFrequency, z * _volcanoFrequency);
            return noise > 0.8f ? _volcanoHeight : 0;
        }

        /// <summary>
        /// Определяет тип вокселя на основе высоты
        /// </summary>
        /// <param name="y">Глобальная Y-координата</param>
        /// <param name="height">Рассчитанная высота поверхности</param>
        /// <returns>Данные вокселя</returns>
        /// <remarks>
        /// Сейчас реализована базовая логика:
        /// - Воздух выше поверхности
        /// - Камень ниже поверхности
        /// Раскомментируйте строки для многослойной генерации
        /// </remarks>
        private VoxelData GetVoxelType(float y, float height)
        {
            if (y > height) return new VoxelData { Type = VoxelType.Air };
            
            // Автоматическое определение слоев (пример)
            float depth = height - y;
            
            // if (depth < 1f) return new VoxelData { Type = VoxelType.Grass };
            // if (depth < 3f) return new VoxelData { Type = VoxelType.Dirt };
            
            return new VoxelData { Type = VoxelType.Stone };
        }
    }
}