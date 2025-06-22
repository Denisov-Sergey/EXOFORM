using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PandemicWars.Scripts.Map
{
    /// <summary>
    /// Простой генератор города только с корутинами
    /// </summary>
    public class CityGenerator : MonoBehaviour
    {
        [Header("Grid Settings")] [Tooltip("Ширина сетки города")]
        public int gridWidth = 50;

        [Tooltip("Высота сетки города")] public int gridHeight = 50;

        [Tooltip("Размер одной клетки")] public float tileSize = 5f;

        [Header("Generation Settings")] [Range(0.05f, 0.5f)] [Tooltip("Плотность дорог")]
        public float roadDensity = 0.05f;

        [Range(3, 30)] [Tooltip("Длина дорожного сегмента")]
        public int roadLength = 15;

        [Range(0.05f, 0.3f)] [Tooltip("Плотность зданий")]
        public float buildingDensity = 0.15f;

        [Range(0.1f, 0.8f)] [Tooltip("Плотность растительности")]
        public float vegetationDensity = 0.4f;

        [Header("Animation")] [Range(0.01f, 1f)] [Tooltip("Скорость анимации")]
        public float animationSpeed = 0.1f;

        [Header("Prefabs")] [Tooltip("Префаб травы")]
        public GameObject grassPrefab;

        [Tooltip("Префаб дороги")] public GameObject roadPrefab;

        [Tooltip("Префабы с компонентом PrefabSettings")]
        public List<GameObject> prefabsWithSettings = new List<GameObject>();

        [Header("Controls")] [Tooltip("Генерировать город")] [SerializeField]
        private bool _generateCity;

        [Tooltip("Очистить город")] [SerializeField]
        private bool _clearCity;

        [Header("Debug")] [Tooltip("Показывать Gizmos")]
        public bool showGizmos = true;

        // Компоненты системы
        private CityGrid cityGrid;
        private RoadGenerator roadGenerator;
        private ObjectPlacer objectPlacer;
        private VegetationPlacer vegetationPlacer;
        private TileSpawner tileSpawner;

        private bool isGenerating = false;

        void Start()
        {
            try
            {
                InitializeComponents();
                if (!isGenerating)
                {
                    StartCoroutine(GenerateCity());
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Ошибка при инициализации: {e.Message}");
            }
        }

        void InitializeComponents()
        {
            if (cityGrid == null)
                cityGrid = new CityGrid(gridWidth, gridHeight, tileSize);

            if (roadGenerator == null)
                roadGenerator = new RoadGenerator(cityGrid);

            if (objectPlacer == null)
                objectPlacer = new ObjectPlacer(cityGrid, prefabsWithSettings, this);

            if (vegetationPlacer == null)
                vegetationPlacer = new VegetationPlacer(cityGrid, prefabsWithSettings, this);

            if (tileSpawner == null)
                tileSpawner = new TileSpawner(cityGrid, transform);
        }

        /// <summary>
        /// Основной метод генерации города
        /// </summary>
        private IEnumerator GenerateCity()
        {
            if (isGenerating) yield break;
            isGenerating = true;

            Debug.Log("🌱 Начинаем генерацию города...");

            // Этап 1: Инициализация
            Debug.Log("🟩 Этап 1: Создание базы (трава)");
            cityGrid.Initialize();
            yield return StartCoroutine(tileSpawner.SpawnAllTiles(grassPrefab, roadPrefab, prefabsWithSettings,
                animationSpeed));
            yield return new WaitForSeconds(animationSpeed * 2);

            // Этап 2: Дороги
            Debug.Log("🛣️ Этап 2: Генерация дорог");
            yield return StartCoroutine(roadGenerator.GenerateRoads(roadDensity, roadLength, animationSpeed));
            yield return StartCoroutine(tileSpawner.UpdateChangedTiles(grassPrefab, roadPrefab, prefabsWithSettings,
                animationSpeed));
            yield return new WaitForSeconds(animationSpeed * 2);

            // Этап 3: Объекты с настройками
            Debug.Log("🏢 Этап 3: Размещение объектов");
            yield return StartCoroutine(objectPlacer.PlaceObjects(buildingDensity, animationSpeed));
            yield return StartCoroutine(tileSpawner.UpdateChangedTiles(grassPrefab, roadPrefab, prefabsWithSettings,
                animationSpeed));
            yield return new WaitForSeconds(animationSpeed * 2);

            // Этап 4: Растительность
            Debug.Log("🌳 Этап 4: Размещение растительности");
            yield return StartCoroutine(vegetationPlacer.PlaceVegetation(vegetationDensity, animationSpeed));
            yield return StartCoroutine(tileSpawner.UpdateChangedTiles(grassPrefab, roadPrefab, prefabsWithSettings,
                animationSpeed));

            Debug.Log("✅ Генерация завершена!");
            LogMapStatistics();
            isGenerating = false;
        }

        /// <summary>
        /// Выводит статистику по всей карте
        /// </summary>
        public void LogMapStatistics()
        {
            if (cityGrid?.Grid == null) return;

            int totalCells = cityGrid.Width * cityGrid.Height;
            var statistics = new Dictionary<TileType, int>();

            // Подсчитываем базовые тайлы
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    TileType tileType = cityGrid.Grid[x][y];
                    if (!statistics.ContainsKey(tileType))
                        statistics[tileType] = 0;
                    statistics[tileType]++;
                }
            }

            // Подсчитываем здания и растительность
            int totalBuildingCells = 0;
            int totalVegetationCells = 0;
            var buildingStats = new Dictionary<TileType, int>();
            var vegetationStats = new Dictionary<TileType, int>();

            foreach (var kvp in cityGrid.BuildingOccupancy)
            {
                TileType buildingType = kvp.Key;
                int cellCount = kvp.Value.Count;

                // Определяем, является ли тип растительностью
                if (IsVegetationType(buildingType))
                {
                    totalVegetationCells += cellCount;
                    vegetationStats[buildingType] = cellCount;
                }
                else
                {
                    totalBuildingCells += cellCount;
                    buildingStats[buildingType] = cellCount;
                }
            }

            // Выводим статистику
            Debug.Log("📊 === СТАТИСТИКА КАРТЫ ===");
            Debug.Log($"📏 Размер карты: {cityGrid.Width}x{cityGrid.Height} = {totalCells} клеток");

            // Базовые тайлы
            foreach (var kvp in statistics)
            {
                float percentage = (float)kvp.Value / totalCells * 100f;
                string emoji = kvp.Key switch
                {
                    TileType.Grass => "🟩",
                    TileType.RoadStraight => "🛤️",
                    _ => "⬜"
                };
                Debug.Log($"{emoji} {kvp.Key}: {kvp.Value} клеток ({percentage:F2}%)");
            }

            // Здания
            if (totalBuildingCells > 0)
            {
                float buildingPercentage = (float)totalBuildingCells / totalCells * 100f;
                Debug.Log($"🏢 Здания: {totalBuildingCells} клеток ({buildingPercentage:F2}%)");

                // Детализация по типам зданий
                foreach (var kvp in buildingStats)
                {
                    float percentage = (float)kvp.Value / totalCells * 100f;
                    string emoji = GetBuildingEmoji(kvp.Key);
                    Debug.Log($"  {emoji} {kvp.Key}: {kvp.Value} клеток ({percentage:F2}%)");
                }
            }

            // Растительность
            if (totalVegetationCells > 0)
            {
                float vegetationPercentage = (float)totalVegetationCells / totalCells * 100f;
                Debug.Log($"🌳 Растительность: {totalVegetationCells} клеток ({vegetationPercentage:F2}%)");

                // Детализация по типам растительности
                foreach (var kvp in vegetationStats)
                {
                    float percentage = (float)kvp.Value / totalCells * 100f;
                    string emoji = GetVegetationEmoji(kvp.Key);
                    Debug.Log($"  {emoji} {kvp.Key}: {kvp.Value} клеток ({percentage:F2}%)");
                }
            }

            Debug.Log("========================");
        }

        /// <summary>
        /// Проверяет, является ли тип растительностью
        /// </summary>
        private bool IsVegetationType(TileType tileType)
        {
            return tileType switch
            {
                TileType.Tree => true,
                TileType.TreeCluster => true,
                TileType.Bush => true,
                TileType.Flower => true,
                TileType.SmallPlant => true,
                TileType.Forest => true,
                TileType.Garden => true,
                _ => false
            };
        }

        /// <summary>
        /// Получает эмодзи для типа здания
        /// </summary>
        private string GetBuildingEmoji(TileType buildingType)
        {
            return buildingType switch
            {
                TileType.Building => "🏠",
                TileType.LargeBuilding => "🏢",
                TileType.Mall => "🏬",
                TileType.Factory => "🏭",
                TileType.Park => "🏞️",
                TileType.Special => "🏛️",
                _ => "🏗️"
            };
        }

        /// <summary>
        /// Получает эмодзи для типа растительности
        /// </summary>
        private string GetVegetationEmoji(TileType vegetationType)
        {
            return vegetationType switch
            {
                TileType.Tree => "🌲",
                TileType.TreeCluster => "🌳",
                TileType.Bush => "🌿",
                TileType.Flower => "🌸",
                TileType.SmallPlant => "🌱",
                TileType.Forest => "🌲🌲",
                TileType.Garden => "🌺",
                _ => "🌿"
            };
        }


        /// <summary>
        /// Очистка города
        /// </summary>
        public void ClearCity()
        {
            StopAllCoroutines();
            SafeClearAllTiles();
            isGenerating = false;

            if (cityGrid != null)
            {
                cityGrid.Initialize();
            }


            Debug.Log("🧹 Город очищен!");
        }

        void SafeClearAllTiles()
        {
            if (cityGrid?.SpawnedTiles == null) return;

            // 1. Удаляем базовые тайлы из массива
            for (int x = 0; x < cityGrid.Width; x++)
            {
                if (cityGrid.SpawnedTiles[x] != null)
                {
                    for (int y = 0; y < cityGrid.Height; y++)
                    {
                        if (cityGrid.SpawnedTiles[x][y] != null)
                        {
                            try
                            {
                                if (Application.isPlaying)
                                    Destroy(cityGrid.SpawnedTiles[x][y]);
                                else
                                    DestroyImmediate(cityGrid.SpawnedTiles[x][y]);

                                cityGrid.SpawnedTiles[x][y] = null;
                            }
                            catch (System.Exception e)
                            {
                                Debug.LogWarning($"Ошибка при удалении объекта в ({x},{y}): {e.Message}");
                                cityGrid.SpawnedTiles[x][y] = null;
                            }
                        }
                    }
                }
            }

            // 2. Удаляем все дочерние объекты (здания, растительность и прочее)
            if (transform.childCount > 0)
            {
                // Собираем всех детей в массив, чтобы избежать проблем с изменением коллекции во время итерации
                Transform[] children = new Transform[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                {
                    children[i] = transform.GetChild(i);
                }

                // Удаляем всех детей
                foreach (Transform child in children)
                {
                    if (child != null)
                    {
                        try
                        {
                            if (Application.isPlaying)
                                Destroy(child.gameObject);
                            else
                                DestroyImmediate(child.gameObject);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Ошибка при удалении дочернего объекта {child.name}: {e.Message}");
                        }
                    }
                }
            }

            // 3. Очищаем данные о занятости зданиями
            if (cityGrid?.BuildingOccupancy != null)
            {
                cityGrid.BuildingOccupancy.Clear();
            }
        }

        void OnValidate()
        {
            try
            {
                gridWidth = Mathf.Max(5, gridWidth);
                gridHeight = Mathf.Max(5, gridHeight);
                tileSize = Mathf.Max(0.1f, tileSize);
                roadDensity = Mathf.Clamp01(roadDensity);
                buildingDensity = Mathf.Clamp01(buildingDensity);
                vegetationDensity = Mathf.Clamp01(vegetationDensity);
                roadLength = Mathf.Max(1, roadLength);
                animationSpeed = Mathf.Max(0.01f, animationSpeed);

                if (!Application.isPlaying) return;

                if (_generateCity)
                {
                    _generateCity = false;
                    if (!isGenerating)
                    {
                        StartCoroutine(GenerateCity());
                    }
                }

                if (_clearCity)
                {
                    _clearCity = false;
                    ClearCity();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Ошибка в OnValidate: {e.Message}");
            }
        }

        void OnDrawGizmos()
        {
            if (!showGizmos || cityGrid?.Grid == null) return;

            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 pos = new Vector3(x * tileSize, 0.1f, y * tileSize);

                    TileType? buildingType = cityGrid.GetBuildingTypeAt(new Vector2Int(x, y));

                    if (buildingType.HasValue)
                    {
                        Gizmos.color = GetBuildingColor(buildingType.Value);
                    }
                    else
                    {
                        Gizmos.color = cityGrid.Grid[x][y] switch
                        {
                            TileType.Grass => new Color(0.2f, 0.8f, 0.2f, 0.7f),
                            TileType.RoadStraight => new Color(0.5f, 0.5f, 0.5f, 0.8f),
                            _ => new Color(1f, 1f, 1f, 0.3f)
                        };
                    }

                    Gizmos.DrawCube(pos, Vector3.one * tileSize * 0.8f);
                }
            }

            DrawBuildingOutlines();
        }

        void DrawBuildingOutlines()
        {
            if (cityGrid?.BuildingOccupancy == null) return;

            foreach (var kvp in cityGrid.BuildingOccupancy)
            {
                TileType buildingType = kvp.Key;
                List<Vector2Int> buildingCells = kvp.Value;

                if (buildingCells.Count == 0) continue;

                var buildingGroups = GroupConnectedCells(buildingCells);

                foreach (var buildingGroup in buildingGroups)
                {
                    DrawSingleBuildingOutline(buildingGroup, buildingType);
                }
            }
        }

        public List<List<Vector2Int>> GroupConnectedCells(List<Vector2Int> allCells)
        {
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            foreach (var cell in allCells)
            {
                if (visited.Contains(cell))
                    continue;

                List<Vector2Int> group = new List<Vector2Int>();
                Queue<Vector2Int> toCheck = new Queue<Vector2Int>();
                toCheck.Enqueue(cell);
                visited.Add(cell);

                while (toCheck.Count > 0)
                {
                    Vector2Int current = toCheck.Dequeue();
                    group.Add(current);

                    Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                    foreach (var dir in directions)
                    {
                        Vector2Int neighbor = current + dir;
                        if (allCells.Contains(neighbor) && !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            toCheck.Enqueue(neighbor);
                        }
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        void DrawSingleBuildingOutline(List<Vector2Int> buildingCells, TileType buildingType)
        {
            if (buildingCells.Count == 0) return;

            Vector2Int minCell = buildingCells[0];
            Vector2Int maxCell = buildingCells[0];

            foreach (var cell in buildingCells)
            {
                if (cell.x < minCell.x) minCell.x = cell.x;
                if (cell.y < minCell.y) minCell.y = cell.y;
                if (cell.x > maxCell.x) maxCell.x = cell.x;
                if (cell.y > maxCell.y) maxCell.y = cell.y;
            }

            Gizmos.color = GetBuildingOutlineColor(buildingType);

            Vector3 center = new Vector3(
                (minCell.x + maxCell.x + 1) * tileSize * 0.5f,
                0.2f,
                (minCell.y + maxCell.y + 1) * tileSize * 0.5f
            );

            Vector3 size = new Vector3(
                (maxCell.x - minCell.x + 1) * tileSize,
                0.1f,
                (maxCell.y - minCell.y + 1) * tileSize
            );

            Gizmos.DrawWireCube(center, size);

#if UNITY_EDITOR
            if (Selection.activeGameObject == gameObject)
            {
                UnityEditor.Handles.Label(
                    center + Vector3.up * 0.5f,
                    $"{buildingType}\n{maxCell.x - minCell.x + 1}x{maxCell.y - minCell.y + 1}"
                );
            }
#endif
        }

        Color GetBuildingColor(TileType buildingType)
        {
            return buildingType switch
            {
                TileType.Building => new Color(0.3f, 0.3f, 0.8f, 0.8f),
                TileType.LargeBuilding => new Color(0.5f, 0.2f, 0.8f, 0.8f),
                TileType.Mall => new Color(0.8f, 0.2f, 0.5f, 0.8f),
                TileType.Factory => new Color(0.8f, 0.5f, 0.2f, 0.8f),
                TileType.Park => new Color(0.2f, 0.8f, 0.4f, 0.8f),
                TileType.Special => new Color(0.8f, 0.8f, 0.2f, 0.8f),

                // Растительность
                TileType.Tree => new Color(0.1f, 0.6f, 0.1f, 0.9f), // Темно-зеленый
                TileType.TreeCluster => new Color(0.0f, 0.5f, 0.0f, 0.9f), // Еще темнее
                TileType.Bush => new Color(0.3f, 0.7f, 0.3f, 0.8f), // Светло-зеленый
                TileType.Flower => new Color(0.9f, 0.4f, 0.7f, 0.8f), // Розовый
                TileType.SmallPlant => new Color(0.4f, 0.8f, 0.2f, 0.7f), // Салатовый
                TileType.Forest => new Color(0.0f, 0.4f, 0.0f, 0.9f), // Очень темно-зеленый
                TileType.Garden => new Color(0.5f, 0.9f, 0.5f, 0.8f), // Яркий зеленый

                _ => new Color(0.3f, 0.3f, 0.8f, 0.8f)
            };
        }

        Color GetBuildingOutlineColor(TileType buildingType)
        {
            return buildingType switch
            {
                TileType.Building => Color.blue,
                TileType.LargeBuilding => Color.magenta,
                TileType.Mall => Color.red,
                TileType.Factory => new Color(1f, 0.5f, 0f),
                TileType.Park => Color.green,
                TileType.Special => Color.yellow,

                // Растительность
                TileType.Tree => new Color(0f, 0.8f, 0f),
                TileType.TreeCluster => new Color(0f, 0.6f, 0f),
                TileType.Bush => new Color(0.5f, 1f, 0.5f),
                TileType.Flower => new Color(1f, 0.5f, 0.8f),
                TileType.SmallPlant => new Color(0.7f, 1f, 0.3f),
                TileType.Forest => new Color(0f, 0.5f, 0f),
                TileType.Garden => new Color(0.3f, 1f, 0.3f),

                _ => Color.blue
            };
        }

        bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
        }
    }
}