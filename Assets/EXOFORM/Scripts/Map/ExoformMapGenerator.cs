using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Главный генератор карты EXOFORM с улучшенной архитектурой и оптимизацией тайлов
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

        [Header("📊 Basic Density Settings")]
        [Range(0.05f, 0.5f)] [Tooltip("Процент карты под путями (5-50%)")]
        public float pathwayDensity = 0.15f;
        
        [Range(0.05f, 0.4f)] [Tooltip("Процент свободной площади под структурами (5-40%)")]
        public float structureDensity = 0.20f;
        
        [Range(0.1f, 0.6f)] [Tooltip("Процент свободной площади под растительностью (10-60%)")]
        public float vegetationDensity = 0.40f;
        
        [Range(0.02f, 0.30f)] [Tooltip("Процент свободной площади под ресурсами (2-30%)")]
        public float resourceDensity = 0.08f;
        
        [Range(0.0f, 0.4f)] [Tooltip("Процент свободной площади под декорациями (0-40%)")]
        public float decorationDensity = 0.1f;

        [Header("🧬 EXOFORM Zone Settings")]
        [Range(5, 20)] [Tooltip("Размер зоны в клетках")]
        public int zoneSize = 10;
        
        [Range(0.1f, 0.8f)] [Tooltip("Плотность стандартных зон")]
        public float standardZoneDensity = 0.6f;
        
        [Range(0.05f, 0.3f)] [Tooltip("Плотность технических зон")]
        public float technicalZoneDensity = 0.2f;
        
        [Range(0.05f, 0.2f)] [Tooltip("Плотность зон артефактов")]
        public float artifactZoneDensity = 0.15f;
        
        [Range(0.01f, 0.1f)] [Tooltip("Плотность заражённых ловушек")]
        public float corruptedTrapDensity = 0.05f;

        [Header("🦠 Corruption System")]
        [Range(0.0f, 0.3f)] [Tooltip("Начальный уровень заражения карты")]
        public float initialCorruptionLevel = 0.1f;
        
        [Range(0.0f, 0.2f)] [Tooltip("Плотность статичных элементов Порчи")]
        public float staticCorruptionDensity = 0.1f;
        
        [Tooltip("Создавать связанные группы заражения")]
        public bool createCorruptionClusters = true;

        [Header("🔧 Tech Salvage")]
        [Range(0.02f, 0.15f)] [Tooltip("Плотность техники для восстановления")]
        public float techSalvageDensity = 0.05f;

        [Header("🛤️ Pathway Settings")]
        [Range(3, 30)] [Tooltip("Длина сегмента пути")]
        public int pathwayLength = 15;
        
        [Tooltip("Настройки для улучшенного генератора путей")]
        public ImprovedRoadGenerator.RoadSettings advancedPathwaySettings = new ImprovedRoadGenerator.RoadSettings();

        [Header("🚗 Pathway Objects")]
        [Range(0.0f, 0.2f)] [Tooltip("Процент путей с декоративными объектами (0-20%)")]
        public float pathwayObjectDensity = 0.10f;

        [Header("📦 Supply Cache Settings")]
        [Range(0.0f, 0.1f)] [Tooltip("Процент путей с снабжением (0-10%)")]
        public float supplyCacheDensity = 0.05f;
        
        [Tooltip("Минимальное количество снабжения на карте")]
        public int minSupplyCacheCount = 5;
        
        [Tooltip("Максимальное количество снабжения на карте")]
        public int maxSupplyCacheCount = 30;
        
        [Tooltip("Группировать снабжение (несколько ящиков рядом)")]
        public bool clusterSupplyCache = true;
        
        [Range(1, 5)] [Tooltip("Размер группы снабжения")]
        public int supplyCacheClusterSize = 3;

        [Header("⚡ Performance")]
        [Range(0.01f, 1f)] [Tooltip("Скорость анимации")]
        public float animationSpeed = 0.1f;
        
        [Tooltip("Пакетное обновление визуала (улучшает производительность)")]
        public bool useBatchedVisualUpdates = true;
        

        [Header("🎯 Base Prefabs")]
        [Tooltip("Массив префабов травы (выбирается случайный)")]
        public GameObject[] grassPrefabs;
        
        [Tooltip("Массив префабов путей (выбирается случайный)")]
        public GameObject[] pathwayPrefabs;

        [Header("🏗️ Prefab Configuration")]
        [Tooltip("Конфигурация префабов по категориям")]
        public PrefabConfiguration prefabConfig = new PrefabConfiguration();

        [Header("🎮 Editor Controls")]
        [SerializeField] private bool _generateMap;
        [SerializeField] private bool _clearMap;
        [SerializeField] private bool _validatePrefabs;

        [Header("🐛 Debug")]
        [Tooltip("Показывать Gizmos")]
        public bool showGizmos = true;
        
        [Tooltip("Подробные логи генерации")]
        public bool verboseLogging = false;

        #endregion

        #region Public Enums

        public enum GenerationStage
        {
            None,
            Initialization,
            ZoneSetup,
            Pathways,
            Structures,
            Vegetation,
            Resources,
            StaticCorruption,
            TechSalvage,
            SupplyCache,
            PathwayObjects,
            Decorations,
            Optimization,
            Completed
        }

        #endregion

        #region Private Fields

        // Компоненты системы
        private CityGrid cityGrid;
        private ImprovedRoadGenerator improvedRoadGenerator;
        private ObjectPlacer objectPlacer;
        private VegetationPlacer vegetationPlacer;
        private TileSpawner tileSpawner;
        private RoadObjectsPlacer roadObjectsPlacer;
        private DecorationPlacer decorationPlacer;
        private LootPlacer lootPlacer;
        private ResourcePlacer resourcePlacer;

        // EXOFORM системы
        private ExoformZoneSystem zoneSystem;
        private StaticCorruptionPlacer staticCorruptionPlacer;
        private TechSalvagePlacer techSalvagePlacer;

        // Состояние
        private bool isGenerating = false;
        private GenerationStage currentStage = GenerationStage.None;

        #endregion

        #region Public Properties (для ECS интеграции)

        /// <summary>
        /// Доступ к сетке карты для ECS систем
        /// </summary>
        public CityGrid CityGrid => cityGrid;

        /// <summary>
        /// Доступ к системе зон для ECS систем
        /// </summary>
        public ExoformZoneSystem ZoneSystem => zoneSystem;

        /// <summary>
        /// Проверить, идет ли генерация
        /// </summary>
        public bool IsGenerating => isGenerating;

        /// <summary>
        /// Текущий этап генерации
        /// </summary>
        public GenerationStage CurrentStage => currentStage;

        /// <summary>
        /// Размер зоны EXOFORM
        /// </summary>
        public int ZoneSize => zoneSize;

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            if (Application.isPlaying)
            {
                try
                {
                    InitializeComponents();
                    if (!isGenerating)
                    {
                        StartCoroutine(GenerateMap());
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Ошибка при инициализации ExoformMapGenerator: {e.Message}\n{e.StackTrace}");
                }
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

            // Инициализация генераторов путей
            InitializePathwayGenerators();

            // Инициализация всех плейсеров
            InitializePlacers(allPrefabs);

            // Инициализация EXOFORM систем
            InitializeExoformSystems(allPrefabs);

            // Инициализация спавнера тайлов с поддержкой массивов префабов
            tileSpawner ??= new TileSpawner(cityGrid, transform);

            LogDebug("Все компоненты инициализированы");
        }

        private void InitializePathwayGenerators()
        {
            improvedRoadGenerator ??= new ImprovedRoadGenerator(cityGrid, advancedPathwaySettings);
        }

        private void InitializePlacers(List<GameObject> allPrefabs)
        {
            objectPlacer ??= new ObjectPlacer(cityGrid, allPrefabs, this);
            vegetationPlacer ??= new VegetationPlacer(cityGrid, allPrefabs, this);
            resourcePlacer ??= new ResourcePlacer(cityGrid, allPrefabs, this, this);
            decorationPlacer ??= new DecorationPlacer(cityGrid, allPrefabs, this);
            roadObjectsPlacer ??= new RoadObjectsPlacer(cityGrid, allPrefabs, this);
            lootPlacer ??= new LootPlacer(cityGrid, allPrefabs, this, this);
        }

        private void InitializeExoformSystems(List<GameObject> allPrefabs)
        {
            zoneSystem ??= new ExoformZoneSystem(cityGrid);
            staticCorruptionPlacer ??= new StaticCorruptionPlacer(cityGrid, allPrefabs, this);
            techSalvagePlacer ??= new TechSalvagePlacer(cityGrid, allPrefabs, this);
        }

        /// <summary>
        /// Получить все префабы из конфигурации
        /// </summary>
        private List<GameObject> GetAllPrefabs()
        {
            return prefabConfig.GetAllPrefabs();
        }

        void ValidateAndClampValues()
        {
            // Валидация основных параметров
            gridWidth = Mathf.Max(5, gridWidth);
            gridHeight = Mathf.Max(5, gridHeight);
            tileSize = Mathf.Max(0.1f, tileSize);
            pathwayLength = Mathf.Max(1, pathwayLength);
            animationSpeed = Mathf.Max(0.01f, animationSpeed);

            // Валидация массивов префабов
            if (grassPrefabs == null || grassPrefabs.Length == 0)
            {
                Debug.LogError("⚠️ Массив префабов травы пуст! Добавьте хотя бы один префаб травы.");
            }
            
            if (pathwayPrefabs == null || pathwayPrefabs.Length == 0)
            {
                Debug.LogError("⚠️ Массив префабов путей пуст! Добавьте хотя бы один префаб пути.");
            }

            // Валидация плотностей
            ValidateDensityValues();

            // Валидация EXOFORM параметров
            ValidateExoformValues();

            // Валидация снабжения
            ValidateSupplyCacheValues();
        }

        private void ValidateDensityValues()
        {
            pathwayDensity = Mathf.Clamp(pathwayDensity, 0.05f, 0.5f);
            structureDensity = Mathf.Clamp(structureDensity, 0.05f, 0.4f);
            vegetationDensity = Mathf.Clamp(vegetationDensity, 0.1f, 0.6f);
            resourceDensity = Mathf.Clamp(resourceDensity, 0.02f, 0.30f);
            decorationDensity = Mathf.Clamp(decorationDensity, 0f, 0.4f);
            pathwayObjectDensity = Mathf.Clamp(pathwayObjectDensity, 0f, 0.2f);
            supplyCacheDensity = Mathf.Clamp(supplyCacheDensity, 0f, 0.1f);
        }

        private void ValidateExoformValues()
        {
            zoneSize = Mathf.Clamp(zoneSize, 5, 20);

            // Проверяем, что сумма плотностей зон не превышает 100%
            float totalZoneDensity = standardZoneDensity + technicalZoneDensity + artifactZoneDensity + corruptedTrapDensity;
            if (totalZoneDensity > 1.0f)
            {
                Debug.LogWarning($"⚠️ Сумма плотностей зон превышает 100%: {totalZoneDensity * 100:F1}%");
                NormalizeZoneDensities(totalZoneDensity);
            }

            initialCorruptionLevel = Mathf.Clamp01(initialCorruptionLevel);
            staticCorruptionDensity = Mathf.Clamp(staticCorruptionDensity, 0f, 0.3f);
            techSalvageDensity = Mathf.Clamp(techSalvageDensity, 0.02f, 0.15f);
        }

        private void NormalizeZoneDensities(float totalZoneDensity)
        {
            float normalizer = 1.0f / totalZoneDensity;
            standardZoneDensity *= normalizer;
            technicalZoneDensity *= normalizer;
            artifactZoneDensity *= normalizer;
            corruptedTrapDensity *= normalizer;
        }

        private void ValidateSupplyCacheValues()
        {
            minSupplyCacheCount = Mathf.Max(0, minSupplyCacheCount);
            maxSupplyCacheCount = Mathf.Max(minSupplyCacheCount, maxSupplyCacheCount);
            supplyCacheClusterSize = Mathf.Clamp(supplyCacheClusterSize, 1, 5);
        }

        void HandleEditorControls()
        {
            if (_generateMap)
            {
                _generateMap = false;
                if (!isGenerating)
                {
                    StartCoroutine(GenerateMap());
                }
            }

            if (_clearMap)
            {
                _clearMap = false;
                ClearMap();
            }

            if (_validatePrefabs)
            {
                _validatePrefabs = false;
                ValidatePrefabConfiguration();
            }
        }

        #endregion

        #region Generation Pipeline

        private IEnumerator GenerateMap()
        {
            if (isGenerating) yield break;

            isGenerating = true;
            var startTime = Time.time;

            Debug.Log("🧬 Начинаем генерацию карты EXOFORM...");
            CalculateExpectedCounts();

            // Выполняем генерацию
            yield return StartCoroutine(ExecuteGenerationPipeline());

            // Завершение генерации
            currentStage = GenerationStage.Completed;
            var generationTime = Time.time - startTime;

            Debug.Log($"\n✅ Генерация EXOFORM завершена за {generationTime:F2} секунд!");
            LogExoformStatistics();

            isGenerating = false;
        }

        private IEnumerator ExecuteGenerationPipeline()
        {
            var steps = new (string name, string emoji, System.Func<IEnumerator> action)[]
            {
                ("Создание базы (трава)", "🟩", InitializeBaseLayer),
                ("Инициализация зон EXOFORM", "🗺️", InitializeExoformZones),
                ("Генерация путей", "🛤️", GeneratePathways),
                ("Размещение структур", "🏢", PlaceStructures),
                ("Размещение растительности", "🌳", PlaceVegetation),
                ("Размещение ресурсов", "⛏️", PlaceResources),
                ("Размещение статичной Порчи", "🦠", PlaceStaticCorruption),
                ("Размещение техники", "🔧", PlaceTechSalvage),
                ("Размещение снабжения", "📦", PlaceSupplyCache),
                ("Объекты на путях", "🚗", PlacePathwayObjects),
                ("Размещение декораций", "🎨", PlaceDecorations)
            };

            for (int i = 0; i < steps.Length; i++)
            {
                var (name, emoji, action) = steps[i];

                Debug.Log($"\n{emoji} Этап {i + 1}/{steps.Length}: {name}");
                currentStage = (GenerationStage)(i + 1);

                // Выполняем этап напрямую - обработка ошибок внутри каждого метода
                yield return StartCoroutine(action());

                // Не обновляем визуалы после оптимизации
                if (currentStage != GenerationStage.Optimization)
                {
                    yield return StartCoroutine(UpdateVisuals());
                }
                
                yield return new WaitForSeconds(animationSpeed * 2);
            }
        }

        #endregion

        #region Generation Steps

        private IEnumerator InitializeBaseLayer()
        {
            cityGrid.Initialize();
            yield return StartCoroutine(tileSpawner.SpawnAllTiles(grassPrefabs, pathwayPrefabs, GetAllPrefabs(), animationSpeed));
        }

        private IEnumerator InitializeExoformZones()
        {
            zoneSystem.InitializeZones(zoneSize, zoneSize);
            Debug.Log(zoneSystem.GetZoneStatistics());
            yield return new WaitForSeconds(animationSpeed);
        }

        private IEnumerator GeneratePathways()
        {
            yield return StartCoroutine(improvedRoadGenerator.GenerateRoads(pathwayDensity, pathwayLength, animationSpeed));
        }

        private IEnumerator PlaceStructures()
        {
            yield return StartCoroutine(objectPlacer.PlaceObjects(structureDensity, animationSpeed));
        }

        private IEnumerator PlaceVegetation()
        {
            yield return StartCoroutine(vegetationPlacer.PlaceVegetation(vegetationDensity, animationSpeed));
        }

        private IEnumerator PlaceResources()
        {
            yield return StartCoroutine(resourcePlacer.PlaceResources(resourceDensity, animationSpeed));
        }

        private IEnumerator PlaceStaticCorruption()
        {
            if (staticCorruptionPlacer != null)
            {
                yield return StartCoroutine(staticCorruptionPlacer.PlaceStaticCorruption(staticCorruptionDensity, animationSpeed));
            }
            else
            {
                LogDebug("StaticCorruptionPlacer не инициализирован - пропускаем этап");
            }
        }

        private IEnumerator PlaceTechSalvage()
        {
            if (techSalvagePlacer != null)
            {
                yield return StartCoroutine(techSalvagePlacer.PlaceTechSalvage(techSalvageDensity, animationSpeed));
            }
            else
            {
                LogDebug("TechSalvagePlacer не инициализирован - пропускаем этап");
            }
        }

        private IEnumerator PlaceSupplyCache()
        {
            yield return StartCoroutine(lootPlacer.PlaceLoot(animationSpeed));
        }

        private IEnumerator PlacePathwayObjects()
        {
            yield return StartCoroutine(roadObjectsPlacer.PlaceRoadObjects(pathwayObjectDensity, animationSpeed));
        }

        private IEnumerator PlaceDecorations()
        {
            yield return StartCoroutine(decorationPlacer.PlaceDecorations(decorationDensity, animationSpeed));
        }
        

        private IEnumerator UpdateVisuals()
        {
            var allPrefabs = GetAllPrefabs();
            float updateSpeed = useBatchedVisualUpdates ? animationSpeed * 0.5f : animationSpeed;

            yield return StartCoroutine(tileSpawner.UpdateChangedTiles(grassPrefabs, pathwayPrefabs, allPrefabs, updateSpeed));
        }

        #endregion

        #region Statistics and Calculations

        public void CalculateExpectedCounts()
        {
            int totalCells = gridWidth * gridHeight;
            int pathwayCells = Mathf.RoundToInt(totalCells * pathwayDensity);
            int freeCells = totalCells - pathwayCells;

            var expectedCounts = CalculateBasicExpectedCounts(freeCells, pathwayCells);
            var (totalZones, expectedZones) = CalculateZoneExpectedCounts();
            var (corruptionElements, techSalvageItems) = CalculateExoformExpectedCounts(totalCells);

            LogCalculationResults(totalCells, pathwayCells, freeCells, expectedCounts, totalZones, expectedZones, corruptionElements, techSalvageItems);
        }

        private Dictionary<string, int> CalculateBasicExpectedCounts(int freeCells, int pathwayCells)
        {
            return new Dictionary<string, int>
            {
                ["structures"] = Mathf.RoundToInt(freeCells * structureDensity),
                ["vegetation"] = Mathf.RoundToInt(freeCells * vegetationDensity),
                ["resources"] = Mathf.RoundToInt(freeCells * resourceDensity),
                ["decorations"] = Mathf.RoundToInt(freeCells * decorationDensity),
                ["pathwayObjects"] = Mathf.RoundToInt(pathwayCells * pathwayObjectDensity),
                ["supplyCache"] = Mathf.RoundToInt(pathwayCells * supplyCacheDensity)
            };
        }

        private (int totalZones, Dictionary<string, int> expectedZones) CalculateZoneExpectedCounts()
        {
            int totalZones = Mathf.RoundToInt((float)(gridWidth * gridHeight) / (zoneSize * zoneSize));

            var expectedZones = new Dictionary<string, int>
            {
                ["standard"] = Mathf.RoundToInt(totalZones * standardZoneDensity),
                ["technical"] = Mathf.RoundToInt(totalZones * technicalZoneDensity),
                ["artifact"] = Mathf.RoundToInt(totalZones * artifactZoneDensity),
                ["corrupted"] = Mathf.RoundToInt(totalZones * corruptedTrapDensity)
            };

            return (totalZones, expectedZones);
        }

        private (int corruptionElements, int techSalvageItems) CalculateExoformExpectedCounts(int totalCells)
        {
            int corruptionElements = Mathf.RoundToInt(totalCells * staticCorruptionDensity);
            int techSalvageItems = Mathf.RoundToInt(totalCells * techSalvageDensity);
            return (corruptionElements, techSalvageItems);
        }

        private void LogCalculationResults(int totalCells, int pathwayCells, int freeCells, Dictionary<string, int> expectedCounts,
            int totalZones, Dictionary<string, int> expectedZones, int corruptionElements, int techSalvageItems)
        {
            Debug.Log($"📊 === РАСЧЕТ РАСПРЕДЕЛЕНИЯ КАРТЫ EXOFORM {gridWidth}x{gridHeight} ===");
            Debug.Log($"📏 Общая площадь: {totalCells} клеток (100%)");
            Debug.Log($"🛤️ Пути: ~{pathwayCells} клеток ({pathwayDensity * 100:F1}%)");
            Debug.Log($"🟩 Свободно: ~{freeCells} клеток ({(float)freeCells / totalCells * 100:F1}%)");
            Debug.Log("");

            LogBasicCounts(expectedCounts);
            LogExoformCounts(totalZones, expectedZones, corruptionElements, techSalvageItems);
        }

        private void LogBasicCounts(Dictionary<string, int> expectedCounts)
        {
            Debug.Log($"🏢 Структуры: ~{expectedCounts["structures"]} клеток ({structureDensity * 100:F1}% от свободных)");
            Debug.Log($"🌳 Растительность: ~{expectedCounts["vegetation"]} клеток ({vegetationDensity * 100:F1}% от свободных)");
            Debug.Log($"⛏️ Ресурсы: ~{expectedCounts["resources"]} клеток ({resourceDensity * 100:F1}% от свободных)");
            Debug.Log($"🎨 Декорации: ~{expectedCounts["decorations"]} клеток ({decorationDensity * 100:F1}% от свободных)");
            Debug.Log($"🚗 Объекты на путях: ~{expectedCounts["pathwayObjects"]} клеток ({pathwayObjectDensity * 100:F1}% от путей)");
            Debug.Log($"📦 Снабжение: ~{expectedCounts["supplyCache"]} клеток ({supplyCacheDensity * 100:F1}% от путей)");
        }

        private void LogExoformCounts(int totalZones, Dictionary<string, int> expectedZones, int corruptionElements, int techSalvageItems)
        {
            Debug.Log($"\n🧬 === EXOFORM КОНТЕНТ ===");
            Debug.Log($"🗺️ Всего зон: {totalZones} (размер зоны: {zoneSize}x{zoneSize})");
            Debug.Log($"🟢 Стандартные: {expectedZones["standard"]} ({standardZoneDensity * 100:F1}%)");
            Debug.Log($"🔧 Технические: {expectedZones["technical"]} ({technicalZoneDensity * 100:F1}%)");
            Debug.Log($"🧬 Артефактные: {expectedZones["artifact"]} ({artifactZoneDensity * 100:F1}%)");
            Debug.Log($"⚠️ Заражённые: {expectedZones["corrupted"]} ({corruptedTrapDensity * 100:F1}%)");
            Debug.Log($"🦠 Элементы Порчи: {corruptionElements} ({staticCorruptionDensity * 100:F1}%)");
            Debug.Log($"🔧 Техника для восстановления: {techSalvageItems} ({techSalvageDensity * 100:F1}%)");
        }

        public void LogMapStatistics()
        {
            if (cityGrid?.Grid == null) return;

            var statisticsCalculator = new MapStatisticsCalculator(cityGrid);
            var stats = statisticsCalculator.CalculateStatistics();

            Debug.Log(stats.GetStatisticsSummary());

            var warnings = stats.GetDistributionWarnings();
            foreach (var warning in warnings)
            {
                Debug.LogWarning(warning);
            }
        }

        public void LogExoformStatistics()
        {
            LogMapStatistics();
            Debug.Log("\n" + zoneSystem.GetZoneStatistics());
            LogCorruptionStatistics();
        }

        private void LogCorruptionStatistics()
        {
            if (cityGrid?.BuildingOccupancy == null) return;

            int tentacles = cityGrid.BuildingOccupancy.ContainsKey(TileType.TentacleGrowth) ?
                cityGrid.BuildingOccupancy[TileType.TentacleGrowth].Count : 0;
            int tumors = cityGrid.BuildingOccupancy.ContainsKey(TileType.TumorNode) ?
                cityGrid.BuildingOccupancy[TileType.TumorNode].Count : 0;
            int corruptedGround = cityGrid.BuildingOccupancy.ContainsKey(TileType.CorruptedGround) ?
                cityGrid.BuildingOccupancy[TileType.CorruptedGround].Count : 0;

            Debug.Log("🦠 === СТАТИСТИКА ПОРЧИ ===");
            Debug.Log($"🐙 Щупальца: {tentacles}");
            Debug.Log($"🧬 Опухоли: {tumors}");
            Debug.Log($"🌫️ Заражённая земля: {corruptedGround}");
            Debug.Log($"☣️ Общий уровень заражения: {initialCorruptionLevel * 100:F1}%");
        }

        #endregion

        #region Zone Access Methods (для ECS)

        /// <summary>
        /// Получить данные зоны по позиции (для ECS систем)
        /// </summary>
        public ExoformZoneSystem.ZoneData? GetZoneAt(Vector2Int position)
        {
            return zoneSystem?.GetZoneAt(position);
        }

        /// <summary>
        /// Отметить зону как очищенную (вызывается ECS системой)
        /// </summary>
        public void ClearZone(Vector2Int zonePosition)
        {
            zoneSystem?.ClearZone(zonePosition);
        }

        /// <summary>
        /// Заразить зону (вызывается системой распространения Порчи)
        /// </summary>
        public void CorruptZone(Vector2Int zonePosition, float corruptionIncrease)
        {
            zoneSystem?.CorruptZone(zonePosition, corruptionIncrease);
        }

        /// <summary>
        /// Экспорт всех данных зон для ECS
        /// </summary>
        public Dictionary<Vector2Int, ExoformZoneSystem.ZoneData> ExportZoneData()
        {
            return zoneSystem?.ExportZoneData() ?? new Dictionary<Vector2Int, ExoformZoneSystem.ZoneData>();
        }

        /// <summary>
        /// Получить позиции всех элементов Порчи
        /// </summary>
        public List<Vector2Int> GetCorruptionPositions()
        {
            var positions = new List<Vector2Int>();

            if (cityGrid?.BuildingOccupancy != null)
            {
                var corruptionTypes = new[]
                {
                    TileType.TentacleGrowth,
                    TileType.TumorNode,
                    TileType.CorruptedGround,
                    TileType.SporeEmitter,
                    TileType.BiologicalMass
                };

                foreach (var type in corruptionTypes)
                {
                    if (cityGrid.BuildingOccupancy.ContainsKey(type))
                    {
                        positions.AddRange(cityGrid.BuildingOccupancy[type].Select(c => c.Cell));
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// Получить позиции всей техники для восстановления
        /// </summary>
        public List<Vector2Int> GetTechSalvagePositions()
        {
            var positions = new List<Vector2Int>();

            if (cityGrid?.BuildingOccupancy != null)
            {
                var techTypes = new[]
                {
                    TileType.DamagedGenerator,
                    TileType.BrokenRobot,
                    TileType.CorruptedTerminal,
                    TileType.TechSalvageResource
                };

                foreach (var type in techTypes)
                {
                    if (cityGrid.BuildingOccupancy.ContainsKey(type))
                    {
                        positions.AddRange(cityGrid.BuildingOccupancy[type].Select(c => c.Cell));
                    }
                }
            }

            return positions;
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
        /// Получить статистику префабов
        /// </summary>
        [ContextMenu("Show Prefab Statistics")]
        public void ShowPrefabStatistics()
        {
            Debug.Log(prefabConfig.GetPrefabStatistics());
        }

        #endregion

        #region Map Management

        public void ClearMap()
        {
            StopAllCoroutines();
            SafeClearAllTiles();
            ResetState();

            Debug.Log("🧹 Карта EXOFORM полностью очищена!");
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
            ResetPlacers();
            ResetExoformSystems();
        }

        private void ResetPlacers()
        {
            objectPlacer = null;
            vegetationPlacer = null;
            roadObjectsPlacer = null;
            lootPlacer = null;
            decorationPlacer = null;
            resourcePlacer = null;
        }

        private void ResetExoformSystems()
        {
            zoneSystem = null;
            staticCorruptionPlacer = null;
            techSalvagePlacer = null;
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

        /// <summary>
        /// Получить информацию о карте для UI
        /// </summary>
        public string GetMapInfo()
        {
            if (cityGrid == null) return "Карта не инициализирована";

            var info = $"🧬 EXOFORM Map {gridWidth}x{gridHeight}\n";
            info += $"📏 Размер: {gridWidth * gridHeight} клеток\n";
            info += $"🗺️ Размер зоны: {zoneSize}x{zoneSize}\n";
            info += $"⚡ Статус: {(isGenerating ? "Генерация..." : "Готова")}\n";

            if (zoneSystem != null)
            {
                var zoneData = zoneSystem.ExportZoneData();
                info += $"🏢 Зон создано: {zoneData.Count}\n";
            }

            return info;
        }

        #endregion

        #region Gizmos

        void OnDrawGizmos()
        {
            if (!showGizmos || cityGrid?.Grid == null) return;

            DrawGridGizmos();
            DrawZoneGizmos();
            DrawGenerationProgress();
        }

        private void DrawGridGizmos()
        {
            if (cityGrid?.Grid == null) return;

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

        private void DrawZoneGizmos()
        {
            if (zoneSystem == null) return;

            var zones = zoneSystem.ExportZoneData();

            foreach (var kvp in zones)
            {
                var zonePos = kvp.Key;
                var zoneData = kvp.Value;

                Vector3 center = new Vector3(
                    (zonePos.x + zoneData.size.x * 0.5f) * tileSize,
                    1f,
                    (zonePos.y + zoneData.size.y * 0.5f) * tileSize
                );

                Vector3 size = new Vector3(
                    zoneData.size.x * tileSize,
                    0.2f,
                    zoneData.size.y * tileSize
                );

                Gizmos.color = GetZoneColor(zoneData.zoneType);
                Gizmos.DrawWireCube(center, size);

                // Показываем уровень заражения
                if (zoneData.corruptionLevel > 0)
                {
                    Gizmos.color = Color.Lerp(Color.clear, Color.red, zoneData.corruptionLevel);
                    Gizmos.DrawCube(center + Vector3.up * 0.5f, size * 0.3f);
                }
            }
        }

        private void DrawGenerationProgress()
        {
            if (!isGenerating) return;

#if UNITY_EDITOR
            Vector3 labelPosition = new Vector3(gridWidth * tileSize / 2, 5f, gridHeight * tileSize / 2);
            Handles.Label(labelPosition, $"🧬 EXOFORM Генерация: {currentStage}");
#endif
        }

        private Color GetBaseTileColor(TileType tileType) => tileType switch
        {
            TileType.Grass => new Color(0.2f, 0.8f, 0.2f, 0.7f),
            TileType.PathwayStraight => new Color(0.5f, 0.5f, 0.5f, 0.8f),
            _ => new Color(1f, 1f, 1f, 0.3f)
        };

        private Color GetZoneColor(TileType zoneType) => zoneType switch
        {
            TileType.StandardZone => new Color(0.2f, 0.8f, 0.2f, 0.5f),
            TileType.TechnicalZone => new Color(0.2f, 0.5f, 0.8f, 0.5f),
            TileType.ArtifactZone => new Color(0.8f, 0.2f, 0.8f, 0.5f),
            TileType.CorruptedTrap => new Color(0.8f, 0.2f, 0.2f, 0.5f),
            _ => new Color(0.5f, 0.5f, 0.5f, 0.3f)
        };

        #endregion


    }
}