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

            // Назначаем данные мешу
            mesh.vertices = vertices;
            mesh.triangles = triangles;

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
        /// Получает вершины последнего сгенерированного меша для отладки.
        /// </summary>
        public Vector3[] GetLastGeneratedVertices(Mesh mesh)
        {
            return mesh?.vertices;
        }
    }
}
