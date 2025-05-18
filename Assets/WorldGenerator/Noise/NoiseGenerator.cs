using UnityEngine;
using VoxelEngine.Generation.Noise;

namespace Worldgenerator.Noise
{
    [ExecuteInEditMode] // Скрипт работает в режиме редактирования
    public class NoiseGenerator : MonoBehaviour
    {
        [Header("Noise Settings")] [Tooltip("Сид для генерации шума. Измените, чтобы получить другую карту.")]
        public int seed = 12345;

        [Header("Noise Settings")]
        [Tooltip("Использовать шум Вороного для резких перепадов")]
        public bool useVoronoiNoise = true;
        
        [Tooltip("Использовать смешанный шум")]
        public bool useCombineNoise = false;

        [Tooltip("Резкость скал (степенная функция)")]
        [Range(1, 5)] public float sharpness = 3f;

        [Tooltip("Квантование высот для ступенчатых уступов")]
        [Range(0, 1)] public float quantizeSteps = 0.2f;
        
        [Tooltip("Масштаб шума. Меньше = более детализировано.")]
        public float scale = 20f;

        [Range(1, 8)] [Tooltip("Количество октав. Увеличивает детализацию шума.")]
        public int octaves = 4;

        [Range(0.1f, 1f)] [Tooltip("Влияние каждой октавы. Меньше = сглаженный шум.")]
        public float persistence = 0.5f;

        [Tooltip("Множитель высоты ландшафта.")]
        public float heightMultiplier = 50f;
        
        [Header("Depression Settings")]
        [Tooltip("Сила впадин (0 = нет впадин, 1 = максимальные)")]
        [Range(0, 1)] public float depressionStrength = 0.5f;

        [Tooltip("Масштаб шума для впадин")]
        public float depressionScale = 40f;

        [Tooltip("Порог активации впадин")]
        [Range(-1, 1)] public float depressionThreshold = 0.7f;
        
        [Header("Domain Warping Settings")]
        [Tooltip("Смещение для вторичного шума по X")]
        public float warpOffsetX = 100f;

        [Tooltip("Смещение для вторичного шума по Y")]
        public float warpOffsetY = 100f;

        [Tooltip("Сила искажения координат")]
        public float warpStrength = 10f;
        
        [Header("Dimensions")] [Tooltip("Ширина генерируемой карты.")]
        public int width = 100;

        [Tooltip("Высота генерируемой карты.")]
        public int height = 100;

        [Header("Gizmos")] [Tooltip("Показывать вершины меша как точки в сцене.")]
        public bool showGizmos = true;

        [Tooltip("Размер точек для визуализации.")]
        public float gizmoSize = 0.1f;

        [Tooltip("Цвет точек.")] public Color gizmoColor = Color.red;

        // Приватные переменные
        private float[,] noiseMap;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Vector3[] vertices;

        // Инициализация при старте или изменении
        private void Start()
        {
            InitializeComponents();
            Generate();
        }

        // Создание компонентов, если их нет
        private void InitializeComponents()
        {
            meshFilter = GetComponent<MeshFilter>();
            if (meshFilter == null){
                meshFilter = gameObject.AddComponent<MeshFilter>();
                #if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(gameObject, "Add MeshFilter");
                #endif
            }

            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
                #if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(gameObject, "Add MeshRenderer");
                #endif
        
                // Яркий материал для видимости
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0.8f, 0.8f, 0.8f, 1f);
                meshRenderer.material = material;
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
            noiseMap = new float[width, height];
            var noise = new FastNoiseLite();
            noise.SetSeed(seed);

            if (useVoronoiNoise)
            {
                noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
                noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Euclidean);
                noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance);
            }
            else if (useCombineNoise && !useVoronoiNoise)
            {
                noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular);
            }
            else
            {
                noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            }
            
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
                ApplyDomainWarping(ref xCoord, ref yCoord, noise);

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
                
                // Квантование высот
                if (quantizeSteps > 0)
                {
                    noiseValue = Mathf.Round(noiseValue / quantizeSteps) * quantizeSteps;
                }

                noiseMap[x, y] = noiseValue;
            }
        }
        
        private void ApplyDomainWarping(ref float xCoord, ref float yCoord, FastNoiseLite noise)
        {
            float warpX = noise.GetNoise(
                xCoord + warpOffsetX, 
                yCoord + warpOffsetY
            ) * warpStrength;

            float warpY = noise.GetNoise(
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
            
            vertices = new Vector3[width * height];
            var triangles = new int[(width - 1) * (height - 1) * 6];
            var triIndex = 0;

            // Заполнение вершин и треугольников
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
            {
                var index = x * height + y;
                vertices[index] = new Vector3(
                    x,
                    noiseMap[x, y] * heightMultiplier,
                    y
                );

                // Формирование треугольников
                if (x < width - 1 && y < height - 1)
                {
                    triangles[triIndex] = index;
                    triangles[triIndex + 1] = index + height;
                    triangles[triIndex + 2] = index + 1;
                    triangles[triIndex + 3] = index + 1;
                    triangles[triIndex + 4] = index + height;
                    triangles[triIndex + 5] = index + height + 1;
                    triIndex += 6;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            meshFilter.mesh = mesh;
            
            #if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(meshFilter);
                        UnityEditor.SceneView.RepaintAll();
            #endif
        }

        // Отрисовка Gizmos
        private void OnDrawGizmos()
        {
            if (!showGizmos || vertices == null) return;

            Gizmos.color = gizmoColor;
            foreach (var vertex in vertices) Gizmos.DrawSphere(transform.position + vertex, gizmoSize);
        }

        // Автоматическая генерация при изменении параметров
        private void OnValidate()
        {
            #if UNITY_EDITOR
                if (!Application.isPlaying && UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    InitializeComponents();
                    Generate();
                }
            #endif
        }
    }
}