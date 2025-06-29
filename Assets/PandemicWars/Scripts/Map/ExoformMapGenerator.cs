using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PandemicWars.Scripts.Map
{
    /// <summary>
    /// Главный генератор города с улучшенной архитектурой и разделением префабов
    /// </summary>
    public class ExoformMapGenerator : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("🗺️ Zone Grid Settings")]
        [Tooltip("Ширина сетки")]
        public int gridWidth = 50;
        [Tooltip("Высота сетки")]
        public int gridHeight = 50;
        [Tooltip("Размер одной клетки")]
        public float tileSize = 5f;

        [Header("📊 Density Settings")]
        [Range(0.05f, 0.5f)] [Tooltip("Процент карты под дорогами (5-50%)")]
        public float roadDensity = 0.15f;
        
        [Range(0.05f, 0.4f)] [Tooltip("Процент свободной площади под зданиями (5-40%)")]
        public float buildingDensity = 0.20f;
        
        [Range(0.1f, 0.6f)] [Tooltip("Процент свободной площади под растительностью (10-60%)")]
        public float vegetationDensity = 0.40f;
        
        [Range(0.02f, 0.30f)] [Tooltip("Процент свободной площади под ресурсами (2-30%)")]
        public float resourceDensity = 0.08f;
        
        [Range(0.0f, 0.4f)] [Tooltip("Процент свободной площади под декорациями (0-40%)")]
        public float decorationDensity = 0.1f;

        [Header("🛣️ Road Objects Settings")]
        [Range(0.0f, 0.2f)] [Tooltip("Процент дорог с декоративными объектами (0-20%)")]
        public float roadObjectDensity = 0.10f;
        
        [Range(0.0f, 0.1f)] [Tooltip("Процент дорог с лутом (0-10%)")]
        public float lootDensity = 0.05f;

        [Header("🛤️ Road Generation")]
        [Range(3, 30)] [Tooltip("Длина дорожного сегмента")]
        public int roadLength = 15;
        [Tooltip("Использовать улучшенный генератор дорог")]
        public bool useImprovedRoadGenerator = true;
        [Tooltip("Настройки для улучшенного генератора дорог")]
        public ImprovedRoadGenerator.RoadSettings advancedRoadSettings = new ImprovedRoadGenerator.RoadSettings();

        [Header("📦 SupplyCache Settings")]
        [Tooltip("Минимальное количество лута на карте")]
        public int minLootCount = 5;
        [Tooltip("Максимальное количество лута на карте")]
        public int maxLootCount = 30;
        [Tooltip("Группировать лут (несколько ящиков рядом)")]
        public bool clusterLoot = true;
        [Range(1, 5)] [Tooltip("Размер группы лута")]
        public int lootClusterSize = 3;

        [Header("⚡ Performance")]
        [Range(0.01f, 1f)] [Tooltip("Скорость анимации")]
        public float animationSpeed = 0.1f;
        [Tooltip("Пакетное обновление визуала (улучшает производительность)")]
        public bool useBatchedVisualUpdates = true;

        [Header("🎯 Base Prefabs")]
        [Tooltip("Префаб травы")]
        public GameObject grassPrefab;
        [Tooltip("Префаб дороги")]
        public GameObject roadPrefab;

        [Header("🏗️ Prefab Configuration")]
        [Tooltip("Конфигурация префабов по категориям")]
        public PrefabConfiguration prefabConfig = new PrefabConfiguration();

        [Header("🔧 Legacy Support")]
        [Tooltip("Старый список префабов (для миграции)")]
        public List<GameObject> legacyPrefabsWithSettings = new List<GameObject>();

        [Header("🎮 Controls")]
        [SerializeField] private bool _generateCity;
        [SerializeField] private bool _clearCity;
        [SerializeField] private bool _validatePrefabs;
        [SerializeField] private bool _migrateLegacyPrefabs;

        [Header("🐛 Debug")]
        [Tooltip("Показывать Gizmos")]
        public bool showGizmos = true;
        [Tooltip("Подробные логи генерации")]
        public bool verboseLogging = false;

        #endregion

        #region Private Fields

        // Компоненты системы
        private CityGrid cityGrid;
        private RoadGenerator roadGenerator;
        private ImprovedRoadGenerator improvedRoadGenerator;
        private ObjectPlacer objectPlacer;
        private VegetationPlacer vegetationPlacer;
        private TileSpawner tileSpawner;
        private RoadObjectsPlacer roadObjectsPlacer;
        private DecorationPlacer decorationPlacer;
        private LootPlacer lootPlacer;
        private ResourcePlacer resourcePlacer;

        // Состояние
        private bool isGenerating = false;
        private GenerationStage currentStage = GenerationStage.None;

        private enum GenerationStage
        {
            None, Initialization, Roads, Buildings, Vegetation, 
            Resources, Loot, RoadObjects, Decorations, Completed
        }

        #endregion

        #region Unity Lifecycle

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
                Debug.LogError($"Ошибка при инициализации ExoformMapGenerator: {e.Message}\n{e.StackTrace}");
            }
        }

        void OnValidate()
        {
            try
            {
                ValidateAndClampValues();
                
                if (!Application.isPlaying) 
                {
                    CalculateExpectedCounts();
                    return;
                }

                HandleEditorControls();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Ошибка в OnValidate: {e.Message}");
            }
        }

        #endregion

        #region Initialization

        void InitializeComponents()
        {
            // Инициализация основной сетки
            cityGrid ??= new CityGrid(gridWidth, gridHeight, tileSize);

            // Получаем актуальный список префабов
            var allPrefabs = GetAllPrefabs();

            // Инициализация генераторов дорог
            if (useImprovedRoadGenerator)
            {
                improvedRoadGenerator ??= new ImprovedRoadGenerator(cityGrid, advancedRoadSettings);
            }
            else
            {
                roadGenerator ??= new RoadGenerator(cityGrid);
            }

            // Инициализация всех плейсеров
            objectPlacer ??= new ObjectPlacer(cityGrid, allPrefabs, this);
            vegetationPlacer ??= new VegetationPlacer(cityGrid, allPrefabs, this);
            resourcePlacer ??= new ResourcePlacer(cityGrid, allPrefabs, this, this);
            decorationPlacer ??= new DecorationPlacer(cityGrid, allPrefabs, this);
            roadObjectsPlacer ??= new RoadObjectsPlacer(cityGrid, allPrefabs, this);
            lootPlacer ??= new LootPlacer(cityGrid, allPrefabs, this, this);
            
            // Инициализация спавнера тайлов
            tileSpawner ??= new TileSpawner(cityGrid, transform);

            LogDebug("Все компоненты инициализированы");
        }

        /// <summary>
        /// Получить все префабы из конфигурации (с поддержкой legacy)
        /// </summary>
        private List<GameObject> GetAllPrefabs()
        {
            var allPrefabs = prefabConfig.GetAllPrefabs();
            
            // Добавляем legacy префабы если они есть
            if (legacyPrefabsWithSettings.Count > 0)
            {
                foreach (var legacyPrefab in legacyPrefabsWithSettings)
                {
                    if (legacyPrefab != null && !allPrefabs.Contains(legacyPrefab))
                    {
                        allPrefabs.Add(legacyPrefab);
                    }
                }
            }

            return allPrefabs;
        }

        void ValidateAndClampValues()
        {
            gridWidth = Mathf.Max(5, gridWidth);
            gridHeight = Mathf.Max(5, gridHeight);
            tileSize = Mathf.Max(0.1f, tileSize);
            roadLength = Mathf.Max(1, roadLength);
            animationSpeed = Mathf.Max(0.01f, animationSpeed);

            // Ограничения для процентов
            roadDensity = Mathf.Clamp(roadDensity, 0.05f, 0.5f);
            buildingDensity = Mathf.Clamp(buildingDensity, 0.05f, 0.4f);
            vegetationDensity = Mathf.Clamp(vegetationDensity, 0.1f, 0.6f);
            resourceDensity = Mathf.Clamp(resourceDensity, 0.02f, 0.30f);
            decorationDensity = Mathf.Clamp(decorationDensity, 0f, 0.4f);
            roadObjectDensity = Mathf.Clamp(roadObjectDensity, 0f, 0.2f);
            lootDensity = Mathf.Clamp(lootDensity, 0f, 0.1f);

            // Лимиты лута
            minLootCount = Mathf.Max(0, minLootCount);
            maxLootCount = Mathf.Max(minLootCount, maxLootCount);
            lootClusterSize = Mathf.Clamp(lootClusterSize, 1, 5);
        }

        void HandleEditorControls()
        {
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

            if (_validatePrefabs)
            {
                _validatePrefabs = false;
                ValidatePrefabConfiguration();
            }

            if (_migrateLegacyPrefabs)
            {
                _migrateLegacyPrefabs = false;
                MigrateLegacyPrefabs();
            }
        }

        #endregion

        #region Generation Pipeline

        private IEnumerator GenerateCity()
        {
            if (isGenerating) yield break;
            
            isGenerating = true;
            var startTime = Time.time;

            Debug.Log("🌱 Начинаем генерацию города...");
            CalculateExpectedCounts();

            // Выполняем все этапы генерации
            yield return StartCoroutine(ExecuteGenerationPipeline());

            // Завершение
            currentStage = GenerationStage.Completed;
            var generationTime = Time.time - startTime;
            
            Debug.Log($"\n✅ Генерация завершена за {generationTime:F2} секунд!");
            LogMapStatistics();
            
            isGenerating = false;
        }

        private IEnumerator ExecuteGenerationPipeline()
        {
            var steps = new[]
            {
                ("Создание базы (трава)", "🟩", (System.Func<IEnumerator>)(() => InitializeBaseLayer())),
                ("Генерация дорог", "🛣️", (System.Func<IEnumerator>)(() => GenerateRoads())),
                ("Размещение зданий", "🏢", (System.Func<IEnumerator>)(() => PlaceBuildings())),
                ("Размещение растительности", "🌳", (System.Func<IEnumerator>)(() => PlaceVegetation())),
                ("Размещение ресурсов", "⛏️", (System.Func<IEnumerator>)(() => PlaceResources())),
                ("Размещение лута", "📦", (System.Func<IEnumerator>)(() => PlaceLoot())),
                ("Объекты на дорогах", "🚗", (System.Func<IEnumerator>)(() => PlaceRoadObjects())),
                ("Размещение декораций", "🎨", (System.Func<IEnumerator>)(() => PlaceDecorations()))
            };

            for (int i = 0; i < steps.Length; i++)
            {
                var (name, emoji, action) = steps[i];
                
                Debug.Log($"\n{emoji} Этап {i + 1}/{steps.Length}: {name}");
                currentStage = (GenerationStage)(i + 1);

                yield return StartCoroutine(action());
                yield return StartCoroutine(UpdateVisuals());
                yield return new WaitForSeconds(animationSpeed * 2);
            }
        }

        #endregion

        #region Generation Steps

        private IEnumerator InitializeBaseLayer()
        {
            cityGrid.Initialize();
            yield return StartCoroutine(tileSpawner.SpawnAllTiles(grassPrefab, roadPrefab, GetAllPrefabs(), animationSpeed));
        }

        private IEnumerator GenerateRoads()
        {
            if (useImprovedRoadGenerator)
            {
                yield return StartCoroutine(improvedRoadGenerator.GenerateRoads(roadDensity, roadLength, animationSpeed));
            }
            else
            {
                yield return StartCoroutine(roadGenerator.GenerateRoads(roadDensity, roadLength, animationSpeed));
            }
        }

        private IEnumerator PlaceBuildings()
        {
            yield return StartCoroutine(objectPlacer.PlaceObjects(buildingDensity, animationSpeed));
        }

        private IEnumerator PlaceVegetation()
        {
            yield return StartCoroutine(vegetationPlacer.PlaceVegetation(vegetationDensity, animationSpeed));
        }

        private IEnumerator PlaceResources()
        {
            yield return StartCoroutine(resourcePlacer.PlaceResources(resourceDensity, animationSpeed));
        }

        private IEnumerator PlaceLoot()
        {
            yield return StartCoroutine(lootPlacer.PlaceLoot(animationSpeed));
        }

        private IEnumerator PlaceRoadObjects()
        {
            yield return StartCoroutine(roadObjectsPlacer.PlaceRoadObjects(roadObjectDensity, animationSpeed));
        }

        private IEnumerator PlaceDecorations()
        {
            yield return StartCoroutine(decorationPlacer.PlaceDecorations(decorationDensity, animationSpeed));
        }

        private IEnumerator UpdateVisuals()
        {
            var allPrefabs = GetAllPrefabs();
            float updateSpeed = useBatchedVisualUpdates ? animationSpeed * 0.5f : animationSpeed;
            
            yield return StartCoroutine(tileSpawner.UpdateChangedTiles(grassPrefab, roadPrefab, allPrefabs, updateSpeed));
        }

        #endregion

        #region Statistics and Calculations

        public void CalculateExpectedCounts()
        {
            int totalCells = gridWidth * gridHeight;
            int roadCells = Mathf.RoundToInt(totalCells * roadDensity);
            int freeCells = totalCells - roadCells;

            var expectedCounts = new Dictionary<string, int>
            {
                ["buildings"] = Mathf.RoundToInt(freeCells * buildingDensity),
                ["vegetation"] = Mathf.RoundToInt(freeCells * vegetationDensity),
                ["resources"] = Mathf.RoundToInt(freeCells * resourceDensity),
                ["decorations"] = Mathf.RoundToInt(freeCells * decorationDensity),
                ["roadObjects"] = Mathf.RoundToInt(roadCells * roadObjectDensity),
                ["loot"] = Mathf.RoundToInt(roadCells * lootDensity)
            };

            LogCalculationResults(totalCells, roadCells, freeCells, expectedCounts);
        }

        private void LogCalculationResults(int totalCells, int roadCells, int freeCells, Dictionary<string, int> expectedCounts)
        {
            Debug.Log($"📊 === РАСЧЕТ РАСПРЕДЕЛЕНИЯ КАРТЫ {gridWidth}x{gridHeight} ===");
            Debug.Log($"📏 Общая площадь: {totalCells} клеток (100%)");
            Debug.Log($"🛣️ Дороги: ~{roadCells} клеток ({roadDensity * 100:F1}%)");
            Debug.Log($"🟩 Свободно: ~{freeCells} клеток ({(float)freeCells / totalCells * 100:F1}%)");
            Debug.Log("");
            
            Debug.Log($"🏢 Здания: ~{expectedCounts["buildings"]} клеток ({buildingDensity * 100:F1}% от свободных)");
            Debug.Log($"🌳 Растительность: ~{expectedCounts["vegetation"]} клеток ({vegetationDensity * 100:F1}% от свободных)");
            Debug.Log($"⛏️ Ресурсы: ~{expectedCounts["resources"]} клеток ({resourceDensity * 100:F1}% от свободных)");
            Debug.Log($"🎨 Декорации: ~{expectedCounts["decorations"]} клеток ({decorationDensity * 100:F1}% от свободных)");
            Debug.Log($"🚗 Объекты на дорогах: ~{expectedCounts["roadObjects"]} клеток ({roadObjectDensity * 100:F1}% от дорог)");
            Debug.Log($"📦 Лут: ~{expectedCounts["loot"]} клеток ({lootDensity * 100:F1}% от дорог)");
        }

        public void LogMapStatistics()
        {
            if (cityGrid?.Grid == null) return;

            var statisticsCalculator = new MapStatisticsCalculator(cityGrid);
            var stats = statisticsCalculator.CalculateStatistics();
            
            Debug.Log(stats.GetStatisticsSummary());
            
            // Выводим предупреждения если есть
            var warnings = stats.GetDistributionWarnings();
            foreach (var warning in warnings)
            {
                Debug.LogWarning(warning);
            }
        }

        #endregion

        #region Prefab Management

        /// <summary>
        /// Валидация конфигурации префабов
        /// </summary>
        [ContextMenu("Validate Prefab Configuration")]
        public void ValidatePrefabConfiguration()
        {
            var result = prefabConfig.ValidatePrefabs();
            Debug.Log(result.GetReport());
            
            if (!result.IsValid)
            {
                Debug.LogError("Найдены ошибки в конфигурации префабов! Проверьте консоль.");
            }
        }

        /// <summary>
        /// Миграция старых префабов в новую систему
        /// </summary>
        [ContextMenu("Migrate Legacy Prefabs")]
        public void MigrateLegacyPrefabs()
        {
            if (legacyPrefabsWithSettings.Count == 0)
            {
                Debug.Log("Нет legacy префабов для миграции");
                return;
            }

            Debug.Log($"Начинаем миграцию {legacyPrefabsWithSettings.Count} префабов...");
            
            prefabConfig.AutoSortPrefabs(legacyPrefabsWithSettings);
            
            Debug.Log("Миграция завершена! Проверьте новую конфигурацию префабов.");
            Debug.Log(prefabConfig.GetPrefabStatistics());
            
            // Можно очистить legacy список после успешной миграции
            // legacyPrefabsWithSettings.Clear();
        }

        /// <summary>
        /// Получить статистику префабов
        /// </summary>
        [ContextMenu("Show Prefab Statistics")]
        public void ShowPrefabStatistics()
        {
            Debug.Log(prefabConfig.GetPrefabStatistics());
        }

        #endregion

        #region City Management

        public void ClearCity()
        {
            StopAllCoroutines();
            SafeClearAllTiles();
            ResetState();
            
            Debug.Log("🧹 Город полностью очищен!");
        }

        private void ResetState()
        {
            isGenerating = false;
            currentStage = GenerationStage.None;

            if (cityGrid != null)
            {
                cityGrid.Initialize();
                cityGrid.BuildingOccupancy.Clear();
            }

            // Сброс всех компонентов
            objectPlacer = null;
            vegetationPlacer = null;
            roadObjectsPlacer = null;
            lootPlacer = null;
            decorationPlacer = null;
            resourcePlacer = null;
        }

        void SafeClearAllTiles()
        {
            if (cityGrid?.SpawnedTiles == null) return;

            // Удаляем базовые тайлы
            for (int x = 0; x < cityGrid.Width; x++)
            {
                if (cityGrid.SpawnedTiles[x] != null)
                {
                    for (int y = 0; y < cityGrid.Height; y++)
                    {
                        SafeDestroyTile(x, y);
                    }
                }
            }

            // Удаляем все дочерние объекты
            SafeDestroyAllChildren();
            cityGrid?.BuildingOccupancy.Clear();
        }

        private void SafeDestroyTile(int x, int y)
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
                    Debug.LogWarning($"Ошибка при удалении тайла в ({x},{y}): {e.Message}");
                    cityGrid.SpawnedTiles[x][y] = null;
                }
            }
        }

        private void SafeDestroyAllChildren()
        {
            if (transform.childCount == 0) return;

            var children = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                children[i] = transform.GetChild(i);
            }

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

        #endregion

        #region Utility Methods

        private void LogDebug(string message)
        {
            if (verboseLogging)
                Debug.Log($"[ExoformMapGenerator] {message}");
        }

        #endregion

        #region Gizmos

        void OnDrawGizmos()
        {
            if (!showGizmos || cityGrid?.Grid == null) return;

            DrawGridGizmos();
            DrawGenerationProgress();
        }

        private void DrawGridGizmos()
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector3 pos = new Vector3(x * tileSize, 0.1f, y * tileSize);
                    Vector2Int gridPos = new Vector2Int(x, y);

                    TileType? buildingType = cityGrid.GetBuildingTypeAt(gridPos);
                    
                    if (buildingType.HasValue)
                    {
                        Gizmos.color = TileTypeHelper.GetObjectColor(buildingType.Value);
                    }
                    else
                    {
                        Gizmos.color = GetBaseTileColor(cityGrid.Grid[x][y]);
                    }

                    Gizmos.DrawCube(pos, Vector3.one * tileSize * 0.8f);
                }
            }
        }

        private void DrawGenerationProgress()
        {
            if (!isGenerating) return;

#if UNITY_EDITOR
            Vector3 labelPosition = new Vector3(gridWidth * tileSize / 2, 5f, gridHeight * tileSize / 2);
            Handles.Label(labelPosition, $"Генерация: {currentStage}");
#endif
        }

        private Color GetBaseTileColor(TileType tileType) => tileType switch
        {
            TileType.Grass => new Color(0.2f, 0.8f, 0.2f, 0.7f),
            TileType.RoadStraight => new Color(0.5f, 0.5f, 0.5f, 0.8f),
            _ => new Color(1f, 1f, 1f, 0.3f)
        };

        #endregion
    }
}