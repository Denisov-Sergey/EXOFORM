using System;
using UnityEngine;
using UnityEditor;
using WorldGenerator.Settings;

namespace WorldGenerator.Core
{
    /// <summary>
    /// Главный контроллер генерации террейна, объединяющий все компоненты.
    /// Управляет жизненным циклом генерации и взаимодействием с Unity.
    /// </summary>
    [ExecuteInEditMode]
    public class NoiseGenerator : MonoBehaviour
    {
        #region Serialized Fields - Настройки из инспектора

        [Header("Noise Settings")]
        [SerializeField] private BaseNoiseSettings baseNoiseSettings;
        [SerializeField] private VoronoiSettings voronoiSettings;
        [SerializeField] private CombinedNoiseSettings combinedNoiseSettings;
        [SerializeField] private CrackSettings crackSettings;
        [SerializeField] private DepressionSettings depressionSettings;
        [SerializeField] private DomainWarpSettings warpSettings;
        [SerializeField] private MeshSettings meshSettings;
        
        [Header("Global Settings")]
        [SerializeField] private GlobalNoiseSettings globalSettings;
        
        [Header("Generation Options")]
        [SerializeField] private bool useBaseNoise;
        [SerializeField] private bool useVoronoiNoise;
        [SerializeField] private bool useCombinedNoise = true;
        [SerializeField] private bool useCracks = true;
        [SerializeField] private bool useDepressions = true;
        [SerializeField] private bool useDomainWarp = true;
        [SerializeField] private bool autoRegenerate = true;
        
        [Header("Debug Options")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private float gizmoSize = 0.1f;
        [SerializeField] private Color gizmoColor = Color.red;

        #endregion

        #region Private Components - Модульная архитектура

        private NoiseSettingsManager _settingsManager;
        private NoiseGeneratorRegistry _generatorRegistry;
        private NoiseCompositor _noiseCompositor;
        private TerrainMeshGenerator _meshGenerator;
        private TerrainRenderer _terrainRenderer;

        // Кэшированные данные
        private float[,] _cachedNoiseMap;
        private Vector3[] _lastGeneratedVertices;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            InitializeComponents();
            UpdateSettingsFromInspector();
        }

        private void OnDisable()
        {
            CleanupResources();
        }

        private void Start()
        {
            if (autoRegenerate) 
            {
                RegenerateTerrain();
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализирует все модульные компоненты системы генерации.
        /// </summary>
        private void InitializeComponents()
        {
            _settingsManager = new NoiseSettingsManager();
            _generatorRegistry = new NoiseGeneratorRegistry(_settingsManager);
            _noiseCompositor = new NoiseCompositor(_settingsManager, _generatorRegistry);
            _meshGenerator = new TerrainMeshGenerator();
            _terrainRenderer = new TerrainRenderer(gameObject);
        }

        /// <summary>
        /// Обновляет настройки компонентов из полей инспектора.
        /// </summary>
        private void UpdateSettingsFromInspector()
        {
            if (_settingsManager == null) return;

            // Передаем настройки из инспектора в менеджер настроек
            _settingsManager.BaseNoiseSettings = baseNoiseSettings;
            _settingsManager.VoronoiSettings = voronoiSettings;
            _settingsManager.CombinedNoiseSettings = combinedNoiseSettings;
            _settingsManager.CrackSettings = crackSettings;
            _settingsManager.DepressionSettings = depressionSettings;
            _settingsManager.WarpSettings = warpSettings;
            _settingsManager.MeshSettings = meshSettings;
            _settingsManager.GlobalSettings = globalSettings;

            // Устанавливаем флаги активации
            _settingsManager.UseBaseNoise = useBaseNoise;
            _settingsManager.UseVoronoiNoise = useVoronoiNoise;
            _settingsManager.UseCombinedNoise = useCombinedNoise;
            _settingsManager.UseCracks = useCracks;
            _settingsManager.UseDepressions = useDepressions;
            _settingsManager.UseDomainWarp = useDomainWarp;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Основной метод регенерации террейна.
        /// Выполняет полный цикл: валидация → генерация шума → создание меша → рендеринг.
        /// </summary>
        public void RegenerateTerrain()
        {
            try
            {
                // 1. Обновляем настройки из инспектора
                UpdateSettingsFromInspector();

                // 2. Валидируем настройки
                if (!_settingsManager.ValidateSettings())
                {
                    Debug.LogError("Invalid settings, terrain generation aborted!");
                    return;
                }

                // 3. Проверяем необходимость регенерации
                if (!_settingsManager.HasSettingsChanged() && _cachedNoiseMap != null)
                {
                    Debug.Log("Settings unchanged, using cached noise map");
                    return;
                }

                // 4. Обновляем генераторы при изменении настроек
                _generatorRegistry.UpdateGenerators();

                // 5. Генерируем карту шума
                _cachedNoiseMap = _noiseCompositor.GenerateCompositeNoiseMap(
                    _settingsManager.MeshSettings.width, 
                    _settingsManager.MeshSettings.height);

                // 6. Создаем 3D меш
                var mesh = _meshGenerator.GenerateTerrainMesh(_cachedNoiseMap, _settingsManager.MeshSettings);
                _lastGeneratedVertices = _meshGenerator.GetLastGeneratedVertices(mesh);

                // 7. Применяем меш к объекту
                _terrainRenderer.ApplyMesh(mesh);

                Debug.Log("Terrain regenerated successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to regenerate terrain: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// Принудительная регенерация террейна (игнорирует кэш).
        /// </summary>
        public void ForceRegenerate()
        {
            _cachedNoiseMap = null;
            RegenerateTerrain();
        }

        /// <summary>
        /// Получает последнюю сгенерированную карту шума.
        /// </summary>
        /// <returns>Двумерный массив значений высот или null</returns>
        public float[,] GetNoiseMap()
        {
            return _cachedNoiseMap;
        }

        #endregion

        #region Editor Integration

#if UNITY_EDITOR
        /// <summary>
        /// Обработчик изменения значений в инспекторе (только в редакторе).
        /// </summary>
        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // Отложенный вызов для избежания проблем с порядком инициализации
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                
                if (autoRegenerate)
                {
                    RegenerateTerrain();
                }
            };
        }
#endif

        #endregion

        #region Debug and Gizmos

        /// <summary>
        /// Отрисовка отладочных гизмо в редакторе.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmos || _lastGeneratedVertices == null) 
                return;

            Gizmos.color = gizmoColor;
            var transformPosition = transform.position;

            foreach (var vertex in _lastGeneratedVertices)
            {
                Gizmos.DrawSphere(transformPosition + vertex, gizmoSize);
            }
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Очищает ресурсы при деактивации компонента.
        /// </summary>
        private void CleanupResources()
        {
            _generatorRegistry?.ClearGenerators();
            _cachedNoiseMap = null;
            _lastGeneratedVertices = null;
        }

        #endregion
    }
}
