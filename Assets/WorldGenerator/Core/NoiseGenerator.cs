using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using WorldGenerator.Abstract;
using WorldGenerator.Factory;
using WorldGenerator.Interface;
using WorldGenerator.Noise;
using WorldGenerator.Settings;

namespace WorldGenerator.Core
{
    [ExecuteInEditMode]
    public class NoiseGenerator : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private BaseNoiseSettings baseNoiseSettings;
        [SerializeField] private VoronoiSettings voronoiSettings;
        [SerializeField] private CombinedNoiseSettings combinedNoiseSettings;
        [SerializeField] private CrackSettings crackSettings;
        [SerializeField] private DepressionSettings depressionSettings;
        [SerializeField] private DomainWarpSettings warpSettings;
        [SerializeField] private MeshSettings meshSettings;
        [Header("Global Settings")]
        [SerializeField] private GlobalNoiseSettings globalSettings;
        
        [Header("Generation Options")] [SerializeField]
        private bool useBaseNoise;

        [SerializeField] private bool useVoronoiNoise;
        [SerializeField] private bool useCombinedNoise = true;
        [SerializeField] private bool useCracks = true;
        [SerializeField] private bool useDepressions = true;
        [SerializeField] private bool useDomainWarp = true;
        [SerializeField] private bool autoRegenerate = true;
        
        [Header("Debug")] [SerializeField] private bool showGizmos = true;
        [SerializeField] private float gizmoSize = 0.1f;
        [SerializeField] private Color gizmoColor = Color.red;

        // Генераторы для каждого типа шума
        private readonly Dictionary<Type, INoiseGenerator> _generators = new();
        private float[,] _cachedNoiseMap;
        private int _settingsHash;

        // Unity компоненты
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Vector3[] _vertices;
        private Material _cachedMaterial;

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            // Очистка ресурсов
            _generators.Clear();
            _cachedNoiseMap = null;
        }

        private void Start()
        {
            InitializeComponents();
            if (autoRegenerate) RegenerateTerrain();
        }

        private void Initialize()
        {
            var newHash = CalculateSettingsHash();

            if (_generators.Count > 0 && _settingsHash == newHash) return;

            CreateGenerators();
            _settingsHash = newHash;
        }

        private void CreateGenerators()
        {
            _generators.Clear();

            try
            {
                // Создаем генераторы через фабрику
                if (useBaseNoise && baseNoiseSettings != null)
                    _generators[typeof(BaseNoiseSettings)] = NoiseFactory.CreateGenerator(baseNoiseSettings);

                if (useVoronoiNoise && voronoiSettings != null)
                    _generators[typeof(VoronoiSettings)] = NoiseFactory.CreateGenerator(voronoiSettings);

                if (useCombinedNoise && combinedNoiseSettings != null)
                    _generators[typeof(CombinedNoiseSettings)] = NoiseFactory.CreateGenerator(combinedNoiseSettings);


                if (useCracks && crackSettings != null)
                    _generators[typeof(CrackSettings)] = NoiseFactory.CreateGenerator(crackSettings);

                if (useDepressions && depressionSettings != null)
                    _generators[typeof(DepressionSettings)] = NoiseFactory.CreateGenerator(depressionSettings);

                if (useDomainWarp && warpSettings != null)
                    _generators[typeof(DomainWarpSettings)] = NoiseFactory.CreateGenerator(warpSettings);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create generators: {e.Message}");
            }
        }

        private void InitializeComponents()
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null)
            {
                _meshFilter = gameObject.AddComponent<MeshFilter>();
#if UNITY_EDITOR
                Undo.RecordObject(gameObject, "Add MeshFilter");
#endif
            }

            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
#if UNITY_EDITOR
                Undo.RecordObject(gameObject, "Add MeshRenderer");
#endif

                if (_cachedMaterial == null)
                {
                    _cachedMaterial = new Material(Shader.Find("Standard"));
                    _cachedMaterial.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                }

                _meshRenderer.material = _cachedMaterial;
            }
        }

        private void HandleNoiseUpdate()
        {
            if (NeedsRegeneration()) RegenerateTerrain();
        }

        private bool NeedsRegeneration()
        {
            return CalculateSettingsHash() != _settingsHash;
        }

        private int CalculateSettingsHash()
        {
            var hash = 0;

            if (useBaseNoise && baseNoiseSettings != null)
                hash ^= baseNoiseSettings.GetHashCode();
            if (useVoronoiNoise && voronoiSettings != null)
                hash ^= voronoiSettings.GetHashCode();
            if (useCombinedNoise && combinedNoiseSettings != null)
                hash ^= combinedNoiseSettings.GetHashCode();
            if (useCracks && crackSettings != null)
                hash ^= crackSettings.GetHashCode();
            if (useDepressions && depressionSettings != null)
                hash ^= depressionSettings.GetHashCode();
            if (useDomainWarp && warpSettings != null)
                hash ^= warpSettings.GetHashCode();
            if (meshSettings != null)
                hash ^= meshSettings.GetHashCode();

            return hash;
        }

        public void RegenerateTerrain()
        {
            if (meshSettings == null)
            {
                Debug.LogError("Mesh settings not assigned!");
                return;
            }

            try
            {
                Initialize();
                GenerateCompositeNoiseMap();
                Create3DTerrain();
                Debug.Log("Terrain regenerated!");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to regenerate terrain: {e.Message}");
            }
        }

        // Основной метод композиции шумов
        private void GenerateCompositeNoiseMap()
        {
            _cachedNoiseMap = new float[meshSettings.width, meshSettings.height];

            // Получаем базовый шум
            var baseMap = GetBaseNoiseMap();

            // Применяем Domain Warping если нужно
            if (useDomainWarp && _generators.ContainsKey(typeof(DomainWarpSettings)))
                baseMap = ApplyDomainWarping(baseMap);
            
            // Можем применить впадины ко всей карте сразу (более эффективно)
            // if (useDepressions && _generators.ContainsKey(typeof(DepressionSettings)))
            // {
            //     baseMap = ApplyDepressionsToMap(baseMap);
            // }
            //
            // if (useCracks && _generators.ContainsKey(typeof(CrackSettings)))
            // {
            //     baseMap = ApplyCracksToMap(baseMap);
            // }

            // Основная композиция (как в TestNoiseGenerator)
            for (var x = 0; x < meshSettings.width; x++)
            for (var y = 0; y < meshSettings.height; y++)
            {
                var noiseValue = baseMap[x, y];

                // Применяем постобработку резкости (КО ВСЕМ ТИПАМ как в оригинале)
                float sharpnessValue = GetCurrentSharpness();
                if (sharpnessValue > 0)
                {
                    noiseValue = Mathf.Pow(Mathf.Abs(noiseValue), sharpnessValue);
                    noiseValue *= Mathf.Sign(noiseValue);
                }
                
                // Применяем впадины
                if (useDepressions && _generators.ContainsKey(typeof(DepressionSettings)))
                noiseValue = ApplyDepressions(noiseValue, x, y);

                // Применяем трещины
                if (useCracks && _generators.ContainsKey(typeof(CrackSettings)))
                noiseValue = ApplyCracks(noiseValue, x, y);

                // Квантование (КО ВСЕМ ТИПАМ как в оригинале)
                float quantizeValue = GetCurrentQuantizeSteps();
                if (quantizeValue > 0)
                {
                    noiseValue = Mathf.Round(noiseValue / quantizeValue) * quantizeValue;
                }
                
                _cachedNoiseMap[x, y] = noiseValue;
            }
        }
        
        
        private float GetCurrentSharpness()
        {
            if (useVoronoiNoise && voronoiSettings != null)
                return voronoiSettings.sharpness;
            if (useCombinedNoise && combinedNoiseSettings != null)
                return combinedNoiseSettings.sharpness;
            if (globalSettings != null)
                return globalSettings.globalSharpness;
    
            return 5f; // Значение по умолчанию из TestNoiseGenerator
        }

        private float GetCurrentQuantizeSteps()
        {
            if (useVoronoiNoise && voronoiSettings != null)
                return voronoiSettings.quantizeSteps;
            if (useCombinedNoise && combinedNoiseSettings != null)
                return combinedNoiseSettings.quantizeSteps;
            if (globalSettings != null)
                return globalSettings.globalQuantizeSteps;
    
            return 0.1f; // Значение по умолчанию из TestNoiseGenerator
        }

        private float[,] GetBaseNoiseMap()
        {
            if (useVoronoiNoise && _generators.ContainsKey(typeof(VoronoiSettings)))
                return _generators[typeof(VoronoiSettings)]
                    .GenerateNoiseMap(meshSettings.width, meshSettings.height);

            if (useBaseNoise && _generators.ContainsKey(typeof(BaseNoiseSettings)))
                return _generators[typeof(BaseNoiseSettings)]
                    .GenerateNoiseMap(meshSettings.width, meshSettings.height);

            if (useCombinedNoise && _generators.ContainsKey(typeof(CombinedNoiseSettings)))
                return _generators[typeof(CombinedNoiseSettings)]
                    .GenerateNoiseMap(meshSettings.width, meshSettings.height);

            // Возвращаем плоскую карту
            return new float[meshSettings.width, meshSettings.height];
        }

        private float[,] ApplyDomainWarping(float[,] baseMap)
        {
            if (!_generators.ContainsKey(typeof(DomainWarpSettings)))
                return baseMap;

            var warpGenerator = _generators[typeof(DomainWarpSettings)] as DomainWarpNoiseGenerator;
            
            return warpGenerator.ApplyWarpingToExternalMap(baseMap);
        }

        private float ApplyDepressions(float noiseValue, int x, int y)
        {
            if (!_generators.ContainsKey(typeof(DepressionSettings)))
                return noiseValue;

            var depressionGenerator = _generators[typeof(DepressionSettings)] as DepressionNoiseGenerator;
            if (depressionGenerator == null)
                return noiseValue;

            // Используем метод DepressionNoiseGenerator для применения впадин к точке
            return depressionGenerator.ApplyDepressionToPoint(noiseValue, x, y);
        }
        
        // Альтернативный метод для применения впадин ко всей карте сразу
        private float[,] ApplyDepressionsToMap(float[,] baseMap)
        {
            if (!_generators.ContainsKey(typeof(DepressionSettings)))
                return baseMap;

            var depressionGenerator = _generators[typeof(DepressionSettings)] as DepressionNoiseGenerator;
            if (depressionGenerator == null)
                return baseMap;

            // Используем DepressionNoiseGenerator для применения впадин ко всей карте
            return depressionGenerator.ApplyDepressionsToExternalMap(baseMap);
        }
        
        private float ApplyCracks(float noiseValue, int x, int y)
        {
            if (!_generators.ContainsKey(typeof(CrackSettings)))
                return noiseValue;

            var crackGenerator = _generators[typeof(CrackSettings)] as CrackNoiseGenerator;
            if (crackGenerator == null)
                return noiseValue;

            // Используем метод CrackNoiseGenerator для применения трещин к точке
            return crackGenerator.ApplyCrackToPoint(noiseValue, x, y);
        }
        
        private float[,] ApplyCracksToMap(float[,] baseMap)
        {
            if (!_generators.ContainsKey(typeof(CrackSettings)))
                return baseMap;

            var crackGenerator = _generators[typeof(CrackSettings)] as CrackNoiseGenerator;
            if (crackGenerator == null)
                return baseMap;

            // Используем CrackNoiseGenerator для применения трещин ко всей карте
            return crackGenerator.ApplyCracksToExternalMap(baseMap);
        }

        // Создание 3D меша
        private void Create3DTerrain()
        {
            var mesh = new Mesh();
            mesh.name = "FactoryGeneratedTerrain";

            _vertices = new Vector3[meshSettings.width * meshSettings.height];
            var triangles = new int[(meshSettings.width - 1) * (meshSettings.height - 1) * 6];
            var triIndex = 0;

            // Заполнение вершин и треугольников
            for (var x = 0; x < meshSettings.width; x++)
            for (var y = 0; y < meshSettings.height; y++)
            {
                var index = x * meshSettings.height + y;
                _vertices[index] = new Vector3(
                    x,
                    _cachedNoiseMap[x, y] * meshSettings.heightMultiplier,
                    y
                );

                if (x < meshSettings.width - 1 && y < meshSettings.height - 1)
                {
                    // Первый треугольник (A → B → C)
                    triangles[triIndex] = index; // A (x, y)
                    triangles[triIndex + 1] = index + 1; // B (x+1, y)
                    triangles[triIndex + 2] = index + meshSettings.height; // C (x, y+1)

                    // Второй треугольник (B → D → C)
                    triangles[triIndex + 3] = index + 1; // B (x+1, y)
                    triangles[triIndex + 4] = index + meshSettings.height + 1; // D (x+1, y+1)
                    triangles[triIndex + 5] = index + meshSettings.height; // C (x, y+1)
                    triIndex += 6;
                }
            }

            mesh.vertices = _vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            _meshFilter.mesh = mesh;

#if UNITY_EDITOR
            EditorUtility.SetDirty(_meshFilter);
            SceneView.RepaintAll();
#endif
        }
        
        // Gizmos для отладки
        private void OnDrawGizmos()
        {
            if (!showGizmos || _vertices == null) return;

            Gizmos.color = gizmoColor;
            foreach (var vertex in _vertices) Gizmos.DrawSphere(transform.position + vertex, gizmoSize);
        }

        // Автоматическая регенерация при изменении настроек
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                Initialize();
                if (autoRegenerate) RegenerateTerrain();
            };
        }
#endif

        // Публичные методы
        public void ForceRegenerate()
        {
            _settingsHash = 0; // Сброс кэша
            RegenerateTerrain();
        }

        public float[,] GetNoiseMap()
        {
            return _cachedNoiseMap;
        }

        public INoiseGenerator GetGenerator<T>() where T : NoiseSettings
        {
            return _generators.ContainsKey(typeof(T)) ? _generators[typeof(T)] : null;
        }

        public void SetSettings<T>(T settings) where T : NoiseSettings
        {
            switch (settings)
            {
                case BaseNoiseSettings baseSettings:
                    baseNoiseSettings = baseSettings;
                    break;
                case VoronoiSettings voronoiSet:
                    voronoiSettings = voronoiSet;
                    break;
                case CrackSettings crackSet:
                    crackSettings = crackSet;
                    break;
                case DepressionSettings depressionSet:
                    depressionSettings = depressionSet;
                    break;
                case DomainWarpSettings warpSet:
                    warpSettings = warpSet;
                    break;
            }

            if (autoRegenerate) RegenerateTerrain();
        }
    }
}