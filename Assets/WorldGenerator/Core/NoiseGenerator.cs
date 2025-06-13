using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEditor;
using WorldGenerator.Abstract;
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
        [SerializeField] private bool autoUpdateTexturesOnly = true;
        [SerializeField] private bool autoUpdateMeshOnNoiseChange = true; 
        
        [Header("Texture Settings")]
        [SerializeField] private TerrainTextures terrainTextures;
        [SerializeField] private Material heightBasedMaterial;
        [SerializeField] private bool useHeightTextures = true;
        
        [Header("NavMesh Settings")]
        [SerializeField] private bool generateNavMesh = true;
        private NavMeshSurface navMeshSurface;
        
        [Header("Debug Options")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private float gizmoSize = 0.1f;
        [SerializeField] private Color gizmoColor = Color.red;
        
        [Header("Debug UV Settings")]
        [SerializeField] private Material uvCheckerMaterial;
        [SerializeField] private bool useUVChecker = false;

        #endregion

        #region Private Components - Модульная архитектура

        private NoiseSettingsManager _settingsManager;
        private NoiseGeneratorRegistry _generatorRegistry;
        private NoiseCompositor _noiseCompositor;
        private TerrainMeshGenerator _meshGenerator;
        private TerrainRenderer _terrainRenderer;
        private bool _forceRegeneration = false;

        // Кэшированные данные
        private float[,] _cachedNoiseMap;
        private Vector3[] _lastGeneratedVertices;
        private static System.Reflection.FieldInfo[] _noiseSettingsFields;
        private readonly List<NoiseSettings> _subscribedSettings = new();

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            InitializeComponents();
            UpdateSettingsFromInspector();
            SubscribeToSettingsChanges();
        }

        private void OnDisable()
        {
            UnsubscribeFromSettingsChanges();
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

        #region Subscripbe to Settings Changes

        private void SubscribeToSettingsChanges()
        {
            UnsubscribeFromSettingsChanges();
        
            // Инициализируем кэш полей только один раз
            if (_noiseSettingsFields == null)
            {
                _noiseSettingsFields = this.GetType()
                    .GetFields(System.Reflection.BindingFlags.Instance | 
                               System.Reflection.BindingFlags.NonPublic | 
                               System.Reflection.BindingFlags.Public)
                    .Where(field => typeof(NoiseSettings).IsAssignableFrom(field.FieldType))
                    .ToArray();
            }

            foreach (var field in _noiseSettingsFields)
            {
                var settings = field.GetValue(this) as NoiseSettings;
                if (settings != null)
                {
                    settings.OnSettingsChanged += OnAnySettingsChanged;
                    _subscribedSettings.Add(settings);
                }
            }
            
            // Специальная подписка на изменения текстур
            if (terrainTextures != null)
            {
                terrainTextures.OnSettingsChanged += OnTextureSettingsChanged;
                if (!_subscribedSettings.Contains(terrainTextures))
                {
                    _subscribedSettings.Add(terrainTextures);
                }
            }
        }

        private void UnsubscribeFromSettingsChanges()
        {
            foreach (var settings in _subscribedSettings)
            {
                if (settings != null)
                {
                    settings.OnSettingsChanged -= OnAnySettingsChanged;
                }
            }
            _subscribedSettings.Clear();
        }

        #endregion
        
        /// <summary>
        /// Обработчик изменений настроек текстур.
        /// Обновляет только материалы без перегенерации меша.
        /// </summary>
        private void OnTextureSettingsChanged()
        {
            if (autoRegenerate && useHeightTextures)
            {
                Debug.Log("Texture settings changed, updating materials...");
        
                // Обновляем только материалы без перегенерации всего меша
                if (heightBasedMaterial != null && terrainTextures != null)
                {
                    ApplyHeightBasedTextures();
            
#if UNITY_EDITOR
                    // В редакторе принудительно обновляем сцену
                    UnityEditor.SceneView.RepaintAll();
#endif
                }
            }
        }


        /// <summary>
        /// Обработчик изменений настроек шума.
        /// Выполняет полную перегенерацию террейна.
        /// </summary>
        private void OnAnySettingsChanged()
        {
            if (autoRegenerate)
            {
                _forceRegeneration = true;
                Debug.Log("Settings changed, regenerating terrain...");
                RegenerateTerrain();
                _forceRegeneration = false;
                
            }
        }

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
            
            if (generateNavMesh)
            {
                navMeshSurface = GetComponent<NavMeshSurface>();
                if (navMeshSurface == null)
                {
                    navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
                }
            }
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
            _settingsManager.UseHeightTextures = useHeightTextures;
            _settingsManager.UseUVChecker = useUVChecker;
            _settingsManager.AutoUpdateTexturesOnly = autoUpdateTexturesOnly;
            _settingsManager.AutoUpdateMeshOnNoiseChange = autoUpdateMeshOnNoiseChange;
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
                if (!_settingsManager.HasSettingsChanged() && _cachedNoiseMap != null && !_forceRegeneration)
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
                
              
                // Применяем дебаг текстуру
                // if (useUVChecker && uvCheckerMaterial != null)
                // {
                //     ApplyUVCheckerMaterial(mesh);
                // }
                
                // 8. Применяем текстурированный материал
                if (useHeightTextures && heightBasedMaterial != null && terrainTextures != null)
                {
                    ApplyHeightBasedTextures();
                }
                
                // 9. Генерируем NavMesh после создания террейна
                if (generateNavMesh && navMeshSurface != null)
                {
                    StartCoroutine(BuildNavMeshDelayed());
                }

                Debug.Log("Terrain regenerated successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to regenerate terrain: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private IEnumerator BuildNavMeshDelayed()
        {
            // Ждем один кадр, чтобы меш точно применился
            yield return null;
    
            navMeshSurface.BuildNavMesh();
            Debug.Log("NavMesh generated successfully!");
        }
        
        /// <summary>
        /// Применяет height-based текстурирование к террейну.
        /// </summary>
        private void ApplyHeightBasedTextures()
        {
            if (heightBasedMaterial != null && terrainTextures != null)
            {
                // Устанавливаем текстуры в материал
                heightBasedMaterial.SetTexture("_ValleyTexture", terrainTextures.valleyTexture);
                heightBasedMaterial.SetTexture("_PlainTexture", terrainTextures.plainTexture);
                heightBasedMaterial.SetTexture("_HillTexture", terrainTextures.hillTexture);
                heightBasedMaterial.SetTexture("_PeakTexture", terrainTextures.peakTexture);
        
                // Устанавливаем пороги высот
                heightBasedMaterial.SetFloat("_ValleyHeight", terrainTextures.valleyHeight);
                heightBasedMaterial.SetFloat("_PlainHeight", terrainTextures.plainHeight);
                heightBasedMaterial.SetFloat("_HillHeight", terrainTextures.hillHeight);
                heightBasedMaterial.SetFloat("_TextureScale", terrainTextures.textureScale);
                heightBasedMaterial.SetFloat("_BlendSmoothness", terrainTextures.blendSmoothness);
        
                
                // Настройки пикселизации
                heightBasedMaterial.SetFloat("_PixelationFactor", terrainTextures.pixelationFactor);
                heightBasedMaterial.SetFloat("_PixelSnap", terrainTextures.pixelSnap);
                
                // Применяем материал
                _terrainRenderer.SetMaterial(heightBasedMaterial);
        
                Debug.Log("Height-based textures applied successfully!");
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
        
        private void ApplyUVCheckerMaterial(Mesh mesh)
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material = uvCheckerMaterial;
        
                // Логирование информации об UV
                LogUVDebugInfo(mesh);
            }
        }

// Добавьте метод для отладочной информации:
        private void LogUVDebugInfo(Mesh mesh)
        {
            if (mesh == null || mesh.uv == null) return;

            Debug.Log($"=== UV DEBUG INFO ===");
            Debug.Log($"Vertices count: {mesh.vertices.Length}");
            Debug.Log($"UV coordinates count: {mesh.uv.Length}");
            Debug.Log($"UV count matches vertices: {mesh.vertices.Length == mesh.uv.Length}");
    
            if (mesh.uv.Length > 0)
            {
                var minUV = GetMinUV(mesh.uv);
                var maxUV = GetMaxUV(mesh.uv);
                Debug.Log($"UV range: min({minUV.x:F3}, {minUV.y:F3}), max({maxUV.x:F3}, {maxUV.y:F3})");
        
                // Показываем несколько примеров UV координат
                for (int i = 0; i < Mathf.Min(5, mesh.uv.Length); i++)
                {
                    Debug.Log($"UV[{i}]: ({mesh.uv[i].x:F3}, {mesh.uv[i].y:F3})");
                }
            }
        }

        private Vector2 GetMinUV(Vector2[] uvs)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            foreach (var uv in uvs)
            {
                min.x = Mathf.Min(min.x, uv.x);
                min.y = Mathf.Min(min.y, uv.y);
            }
            return min;
        }

        private Vector2 GetMaxUV(Vector2[] uvs)
        {
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (var uv in uvs)
            {
                max.x = Mathf.Max(max.x, uv.x);
                max.y = Mathf.Max(max.y, uv.y);
            }
            return max;
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
            
            if (navMeshSurface != null)
            {
                navMeshSurface.RemoveData();
            }
        }

        #endregion
    }
}
