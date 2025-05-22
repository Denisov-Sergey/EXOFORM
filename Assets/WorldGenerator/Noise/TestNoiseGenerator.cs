using UnityEngine;
using VoxelEngine.Generation.Noise;

namespace Worldgenerator.Noise
{
    [ExecuteInEditMode] // Скрипт работает в режиме редактирования
    public class TestNoiseGenerator : MonoBehaviour
    {
        [Header("Noise Settings")] 
        [Tooltip("Сид для генерации шума. Измените, чтобы получить другую карту.")]
        public int seed = 12345;

        [Tooltip("Использовать шум Вороного для резких перепадов")]
        public bool useVoronoiNoise = true;
        
        [Tooltip("Использовать смешанный шум")]
        public bool useCombineNoise = false;

        [Tooltip("Резкость скал (степенная функция)")]
        [Range(1, 5)] public float sharpness = 5f;

        [Tooltip("Квантование высот для ступенчатых уступов")]
        [Range(0, 1)] public float quantizeSteps = 0.1f;
        
        [Tooltip("Масштаб шума. Меньше = более детализировано.")]
        public float scale = 250f;

        [Range(1, 8)] [Tooltip("Количество октав. Увеличивает детализацию шума.")]
        public int octaves = 5;

        [Range(0.1f, 1f)] [Tooltip("Влияние каждой октавы. Меньше = сглаженный шум.")]
        public float persistence = 0.7f;

        [Tooltip("Множитель высоты ландшафта.")]
        public float heightMultiplier = 15f;
        
        [Header("Crack Settings")]
        [Tooltip("Включить мелкие трещины")]
        public bool enableCracks = true;

        [Tooltip("Масштаб шума трещин")]
        public float crackScale = 10f;

        [Tooltip("Сила трещин")]
        [Range(0, 1)] public float crackStrength = 0.1f;

        [Tooltip("Порог для трещин")]
        [Range(0, 1)] public float crackThreshold = 0.5f;

        [Tooltip("Резкость трещин")]
        [Range(1, 5)] public float crackSharpness = 5f;
        
        [Header("Depression Settings")]
        [Tooltip("Сила впадин (0 = нет впадин, 1 = максимальные)")]
        [Range(0, 1)] public float depressionStrength = 0.55f;

        [Tooltip("Масштаб шума для впадин")]
        public float depressionScale = 15f;

        [Tooltip("Порог активации впадин")]
        [Range(-1, 1)] public float depressionThreshold = -0.3f;
        
        [Header("Domain Warping Settings")]
        [Tooltip("Смещение для вторичного шума по X")]
        public float warpOffsetX = 150f;

        [Tooltip("Смещение для вторичного шума по Y")]
        public float warpOffsetY = 150f;

        [Tooltip("Сила искажения координат")]
        public float warpStrength = 100f;
        
        [Header("Dimensions")] [Tooltip("Ширина генерируемой карты.")]
        public int width = 200;

        [Tooltip("Высота генерируемой карты.")]
        public int height = 200;

        [Header("Gizmos")] [Tooltip("Показывать вершины меша как точки в сцене.")]
        public bool showGizmos = true;

        [Tooltip("Размер точек для визуализации.")]
        public float gizmoSize = 0.1f;

        [Tooltip("Цвет точек.")] public Color gizmoColor = Color.red;

        // Приватные переменные
        private float[,] _noiseMap;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Vector3[] _vertices;
        private Material _cachedMaterial;
        private FastNoiseLite _warpNoise;


        // Инициализация при старте или изменении
        private void Start()
        {
            InitializeComponents();
            Generate();
        }

        // Создание компонентов, если их нет
        private void InitializeComponents()
        {
            _meshFilter = GetComponent<MeshFilter>();
            if (_meshFilter == null){
                _meshFilter = gameObject.AddComponent<MeshFilter>();
                #if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(gameObject, "Add MeshFilter");
                #endif
            }

            _meshRenderer = GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                _meshRenderer = gameObject.AddComponent<MeshRenderer>();
                #if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(gameObject, "Add MeshRenderer");
                #endif
        
                if (_cachedMaterial == null)
                {
                    _cachedMaterial = new Material(Shader.Find("Standard"));
                    _cachedMaterial.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                }
                _meshRenderer.material = _cachedMaterial;
            }
        }

        // Основной метод генерации
        public void Generate()
        {
            GenerateNoiseMap();
            Create3DTerrain();
            Debug.Log("Ландшафт сгенерирован!");
        }

        // Генерация карты шума
        private void GenerateNoiseMap()
        {
            _noiseMap = new float[width, height];
            var noise = new FastNoiseLite();
            noise.SetSeed(seed);

            if (useVoronoiNoise)
            {
                noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
                noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Euclidean);
                noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance);
            }
            else if (useCombineNoise)
            {
                noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            }
            else
            {
                noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            }
            
            // Инициализация отдельного шума для Domain Warping
            _warpNoise = new FastNoiseLite();
            _warpNoise.SetSeed(seed + 500);
            
            // Копируем настройки основного шума для _warpNoise
            _warpNoise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            _warpNoise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Euclidean);
            _warpNoise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance);
            _warpNoise.SetFractalOctaves(octaves);
            _warpNoise.SetFractalGain(persistence);
            
            // Шум для мелких трещин
            var crackNoise = new FastNoiseLite(seed + 2000);
            crackNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            crackNoise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Manhattan);
            crackNoise.SetFrequency(1f / crackScale);
            crackNoise.SetFractalOctaves(3);
            
            // Шум для впадин
            var depressionNoise = new FastNoiseLite(seed + 1000);
            depressionNoise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            depressionNoise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Manhattan);
            depressionNoise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance);
            depressionNoise.SetFrequency(1f / depressionScale);
            depressionNoise.SetFractalOctaves(octaves);
            
            noise.SetFractalOctaves(octaves);
            noise.SetFractalGain(persistence);

            // Проход по всем точкам карты
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            {
                // Используем обе координаты (X и Y) для 2D-шума
                var xCoord = (float)x / width * scale;
                var yCoord = (float)y / height * scale;
                
                // Domain Warping
                ApplyDomainWarping(ref xCoord, ref yCoord);

                // Основной шум
                float noiseValue = noise.GetNoise(xCoord, yCoord);

                
                // Постобработка
                noiseValue = Mathf.Pow(Mathf.Abs(noiseValue), sharpness); // Усиление контраста
                noiseValue *= Mathf.Sign(noiseValue); // Восстановление знака
                
                // Генерация впадин
                float depressionValue = depressionNoise.GetNoise(x, y);
                // Debug.Log($"Depression: {depressionValue}, Threshold: {depressionThreshold}");
                
                // Применение впадин
                if (depressionValue > depressionThreshold)
                {
                    Debug.Log($"Depression: {depressionValue}, Threshold: {depressionThreshold}");
                    noiseValue = -noiseValue * depressionStrength * 2f;
                }
                
                // Добавляем мелкие трещины
                if(enableCracks)
                {
                    float crackValue = crackNoise.GetNoise(x * 1.5f, y * 1.5f); // Удвоенная частота
                    crackValue = Mathf.Pow(Mathf.Abs(crackValue), crackSharpness);
                    if(crackValue > crackThreshold)
                    {
                        noiseValue -= crackStrength * crackValue;
                    }
                }
                
                // Квантование высот
                if (quantizeSteps > 0)
                {
                    noiseValue = Mathf.Round(noiseValue / quantizeSteps) * quantizeSteps;
                }

                _noiseMap[x, y] = noiseValue;
            }
        }
        
        private void ApplyDomainWarping(ref float xCoord, ref float yCoord)
        {
            float warpX = _warpNoise.GetNoise(
                xCoord + warpOffsetX, 
                yCoord + warpOffsetY
            ) * warpStrength;

            float warpY = _warpNoise.GetNoise(
                xCoord - warpOffsetX, 
                yCoord - warpOffsetY
            ) * warpStrength;

            xCoord += warpX;
            yCoord += warpY;
        }

        // Создание 3D-меша
        private void Create3DTerrain()
        {
            var mesh = new Mesh();
            mesh.name = "ProceduralTerrain";
            
            _vertices = new Vector3[width * height];
            var triangles = new int[(width - 1) * (height - 1) * 6];
            var triIndex = 0;

            // Заполнение вершин и треугольников
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            {
                var index = x * height + y;
                _vertices[index] = new Vector3(
                    x,
                    _noiseMap[x, y] * heightMultiplier,
                    y
                );

                // Формирование треугольников
                if (x < width - 1 && y < height - 1)
                {
                    // Первый треугольник соединит точки (0,0,0) → (1,0,0) → (0,1,0).
                    // Второй треугольник соединит (1,0,0) → (1,1,0) → (0,1,0).
                    // Квадрат из 4 вершин:
                    // A (x, y) ---- B (x+1, y)
                        // |           |
                        // |           |
                    // C (x, y+1) -- D (x+1, y+1)

                    
                    // Первый треугольник (A → B → C)
                    triangles[triIndex]     = index;          // A (x, y)
                    triangles[triIndex + 1] = index + 1;      // B (x+1, y)
                    triangles[triIndex + 2] = index + width;  // C (x, y+1)

                    // Второй треугольник (B → D → C)
                    triangles[triIndex + 3] = index + 1;          // B (x+1, y)
                    triangles[triIndex + 4] = index + width + 1;  // D (x+1, y+1)
                    triangles[triIndex + 5] = index + width;      // C (x, y+1)
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
                        UnityEditor.EditorUtility.SetDirty(_meshFilter);
                        UnityEditor.SceneView.RepaintAll();
            #endif
        }

        // Отрисовка Gizmos
        private void OnDrawGizmos()
        {
            if (!showGizmos || _vertices == null) return;

            Gizmos.color = gizmoColor;
            foreach (var vertex in _vertices) Gizmos.DrawSphere(transform.position + vertex, gizmoSize);
        }

        // Автоматическая генерация при изменении параметров
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Защита от частых вызовов
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            // Обновление с задержкой
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                InitializeComponents();
                Generate();
            };
        }
        #endif
    }
}