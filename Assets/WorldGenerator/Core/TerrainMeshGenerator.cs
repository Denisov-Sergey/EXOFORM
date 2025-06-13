using UnityEngine;
using WorldGenerator.Settings;

namespace WorldGenerator.Core
{
    /// <summary>
    /// Генерирует 3D меш террейна на основе карты высот.
    /// Создает вершины, треугольники и вычисляет нормали для корректного освещения.
    /// </summary>
    public class TerrainMeshGenerator
    {
        /// <summary>
        /// Создает меш террейна на основе карты высот и настроек меша.
        /// </summary>
        /// <param name="heightMap">Двумерный массив значений высот</param>
        /// <param name="meshSettings">Настройки размеров и масштаба меша</param>
        /// <returns>Готовый меш для использования в Unity</returns>
        public Mesh GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings)
        {
            if (heightMap == null)
                throw new System.ArgumentNullException(nameof(heightMap));
            if (meshSettings == null)
                throw new System.ArgumentNullException(nameof(meshSettings));

            var mesh = new Mesh
            {
                name = "FactoryGeneratedTerrain"
            };

            var vertices = CreateVertices(heightMap, meshSettings);
            var triangles = CreateTriangles(meshSettings);
            var uvs = CreateUVs(heightMap, meshSettings, vertices);
            var colors = CreateVertexColors(heightMap, meshSettings);

            // Назначаем данные мешу
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors = colors;

            // Вычисляем дополнительные данные для корректного рендеринга
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }

        /// <summary>
        /// Создает массив вершин на основе карты высот.
        /// Каждая точка карты становится вершиной в 3D пространстве.
        /// </summary>
        private Vector3[] CreateVertices(float[,] heightMap, MeshSettings meshSettings)
        {
            var width = meshSettings.width;
            var height = meshSettings.height;
            var vertices = new Vector3[width * height];

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var index = x * height + y;
                    
                    vertices[index] = new Vector3(
                        x,
                        heightMap[x, y] * meshSettings.heightMultiplier,
                        y
                    );
                }
            }

            return vertices;
        }

        
        /// <summary>
        /// Создает массив индексов треугольников для меша.
        /// Каждый квад сетки состоит из двух треугольников.
        /// </summary>
        private int[] CreateTriangles(MeshSettings meshSettings)
        {
            var width = meshSettings.width;
            var height = meshSettings.height;
            
            // Каждый квад = 2 треугольника = 6 индексов
            var triangles = new int[(width - 1) * (height - 1) * 6];
            var triIndex = 0;

            for (var x = 0; x < width - 1; x++)
            {
                for (var y = 0; y < height - 1; y++)
                {
                    var index = x * height + y;

                    // Первый треугольник: bottom-left → top-left → bottom-right
                    // Первый треугольник (A → B → C)
                    triangles[triIndex] = index; // A (x, y)
                    triangles[triIndex + 1] = index + 1; // B (x+1, y)
                    triangles[triIndex + 2] = index + height; // C (x, y+1)

                    // Второй треугольник (B → D → C)
                    triangles[triIndex + 3] = index + 1; // B (x+1, y)
                    triangles[triIndex + 4] = index + height + 1; // D (x+1, y+1)
                    triangles[triIndex + 5] = index + height; // C (x, y+1)

                    triIndex += 6;
                }
            }

            return triangles;
        }

        /// <summary>
        /// Создает UV-координаты для террейна. Поддерживает несколько режимов.
        /// </summary>
        private Vector2[] CreateUVs(float[,] heightMap, MeshSettings meshSettings, Vector3[] vertices)
        {
            var width = meshSettings.width;
            var height = meshSettings.height;
            var uvs = new Vector2[width * height];

            // Метод 1: Grid-based UV (равномерное распределение текстуры)[2][4]
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var index = x * height + y;
                    
                    // Нормализуем координаты от 0 до 1[2][5]
                    uvs[index] = new Vector2(
                        (float)x / (width - 1),  // U координата
                        (float)y / (height - 1)  // V координата
                    );
                }
            }

            return uvs;
        }

        /// <summary>
        /// Альтернативный метод создания UV на основе world position (для triplanar mapping).
        /// </summary>
        private Vector2[] CreateWorldPositionUVs(Vector3[] vertices, float textureScale = 1f)
        {
            var uvs = new Vector2[vertices.Length];

            for (int i = 0; i < vertices.Length; i++)
            {
                // Используем world position для UV (подходит для triplanar mapping)[4][5]
                uvs[i] = new Vector2(
                    vertices[i].x * textureScale,
                    vertices[i].z * textureScale
                );
            }

            return uvs;
        }
        
        /// <summary>
        /// Создает vertex colors для передачи высотной информации в шейдер.
        /// </summary>
        private Color[] CreateVertexColors(float[,] heightMap, MeshSettings meshSettings)
        {
            var width = meshSettings.width;
            var height = meshSettings.height;
            var colors = new Color[width * height];

            // Найдем минимальную и максимальную высоты для нормализации
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    float currentHeight = heightMap[x, y];
                    minHeight = Mathf.Min(minHeight, currentHeight);
                    maxHeight = Mathf.Max(maxHeight, currentHeight);
                }
            }

            // Создаем цвета с нормализованной высотой в альфа канале
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var index = x * height + y;
                    float normalizedHeight = (heightMap[x, y] - minHeight) / (maxHeight - minHeight);
                    
                    // Красный канал = нормализованная высота (0-1)
                    colors[index] = new Color(normalizedHeight, 0f, 0f, 1f);
                }
            }

            return colors;
        }

        /// <summary>
        /// Получает вершины последнего сгенерированного меша для отладки.
        /// </summary>
        public Vector3[] GetLastGeneratedVertices(Mesh mesh)
        {
            return mesh?.vertices;
        }
        
        /// <summary>
        /// Отладочный метод для проверки корректности UV-координат.
        /// </summary>
        public void LogUVDebugInfo(Mesh mesh)
        {
            if (mesh == null || mesh.uv == null) return;

            Debug.Log($"Mesh has {mesh.uv.Length} UV coordinates");
            Debug.Log($"UV range: min({GetMinUV(mesh.uv)}), max({GetMaxUV(mesh.uv)})");
            
            // Проверяем, что количество UV совпадает с количеством вершин
            bool uvCountMatches = mesh.vertices.Length == mesh.uv.Length;
            Debug.Log($"UV count matches vertex count: {uvCountMatches}");
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
    }
}
