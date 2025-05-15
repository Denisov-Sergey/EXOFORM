using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using VoxelEngine.Core;

namespace VoxelEngine.Rendering.Meshing
{
    /// <summary>
    /// Реализация Greedy Meshing алгоритма для оптимизации генерации мешей воксельных чанков
    /// </summary>
    public class GreedyMesher : MonoBehaviour
    {
        // Временные буферы для данных меша
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Vector2> _uvs = new List<Vector2>();
        private int _currentVertexIndex;

        private GameObject _chunkPrefab;
        private int _chunkSize;
        private float _voxelSize;
        
        void Start()
        {
            _chunkPrefab = VoxelEngineManager.Instance.ChunkManager.ChunkPrefab;
            GameObject chunkObj = PrefabUtility.InstantiatePrefab(_chunkPrefab) as GameObject;
            
            _chunkSize = chunkObj.GetComponent<Chunk>().Size;
            _voxelSize = chunkObj.GetComponent<Chunk>().VoxelSize;
        }
        
        /// <summary>
        /// Генерирует оптимизированный меш на основе воксельных данных
        /// </summary>
        /// <param name="voxels">3D массив воксельных данных чанка (16x16x16)</param>
        /// <returns>Сгенерированный меш с объединенными гранями</returns>
        public Mesh GenerateMesh(VoxelData[,,] voxels)
        {
            // Сброс временных данных
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _currentVertexIndex = 0;

            // Проход по всем вокселям чанка
            for (int y = 0; y < _chunkSize; y++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    for (int x = 0; x < _chunkSize; x++)
                    {
                        if (voxels[x, y, z].Type == 0) continue;

                        // Проверка и создание видимых граней
                        if (IsTransparent(x + 1, y, z, voxels)) CreateFace(x, y, z, FaceDirection.East, (byte)voxels[x, y, z].Type);
                        if (IsTransparent(x - 1, y, z, voxels)) CreateFace(x, y, z, FaceDirection.West, (byte)voxels[x, y, z].Type);
                        if (IsTransparent(x, y + 1, z, voxels)) CreateFace(x, y, z, FaceDirection.Top, (byte)voxels[x, y, z].Type);
                        if (IsTransparent(x, y - 1, z, voxels)) CreateFace(x, y, z, FaceDirection.Bottom, (byte)voxels[x, y, z].Type);
                        if (IsTransparent(x, y, z + 1, voxels)) CreateFace(x, y, z, FaceDirection.North, (byte)voxels[x, y, z].Type);
                        if (IsTransparent(x, y, z - 1, voxels)) CreateFace(x, y, z, FaceDirection.South, (byte)voxels[x, y, z].Type);
                    }
                }
            }

            // Создание и настройка меша
            Mesh mesh = new Mesh();
            mesh.name = "Chunk Mesh";
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.uv = _uvs.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        /// <summary>
        /// Проверяет прозрачность соседнего вокселя
        /// </summary>
        /// <param name="x">X-координата вокселя</param>
        /// <param name="y">Y-координата вокселя</param>
        /// <param name="z">Z-координата вокселя</param>
        /// <param name="voxels">Массив воксельных данных</param>
        /// <returns>True если грань должна быть видимой</returns>
        private bool IsTransparent(int x, int y, int z, VoxelData[,,] voxels)
        {
            // Граничные проверки считаются прозрачными (вне чанка)
            if (x < 0 || x >= _chunkSize || y < 0 || y >= _chunkSize || z < 0 || z >= _chunkSize) return true;
            return voxels[x, y, z].Type == VoxelType.Air;
        }

        /// <summary>
        /// Создает грань вокселя в указанном направлении
        /// </summary>
        /// <param name="x">X-позиция вокселя</param>
        /// <param name="y">Y-позиция вокселя</param>
        /// <param name="z">Z-позиция вокселя</param>
        /// <param name="direction">Направление грани</param>
        /// <param name="blockType">Тип блока для текстурных координат</param>
        private void CreateFace(int x, int y, int z, FaceDirection direction, byte blockType)
        {
            // Рассчитываем реальный размер с учетом VoxelSize
            Vector3 offset = new Vector3(
                x * _voxelSize,
                y * _voxelSize,
                z * _voxelSize
            );
            // Центрирование чанка
            // offset -= new Vector3(
            //     _chunkSize * _voxelSize, 
            //     _chunkSize * _voxelSize,
            //     _chunkSize * _voxelSize
            // );
            
            Vector3[] faceVertices = GetFaceVertices(direction);
            Vector2[] faceUVs = GetFaceUVs(blockType, direction);

            // Добавление вершин с учетом позиции вокселя
            foreach (Vector3 vertex in faceVertices)
            {
                _vertices.Add(vertex + offset);
            }

            // Формирование двух треугольников для грани
            _triangles.Add(_currentVertexIndex);
            _triangles.Add(_currentVertexIndex + 1);
            _triangles.Add(_currentVertexIndex + 2);
            _triangles.Add(_currentVertexIndex);
            _triangles.Add(_currentVertexIndex + 2);
            _triangles.Add(_currentVertexIndex + 3);

            _uvs.AddRange(faceUVs);
            _currentVertexIndex += 4;
        }

        /// <summary>
        /// Возвращает локальные координаты вершин для указанной грани
        /// </summary>
        /// <param name="direction">Направление грани</param>
        /// <returns>Массив из 4 вершин в локальных координатах</returns>
        private Vector3[] GetFaceVertices(FaceDirection direction)
        {
            Vector3[] vertices;
            switch (direction)
            {
                case FaceDirection.North:
                    vertices = new Vector3[] {
                        new Vector3(0, 0, 1),  // Левый нижний
                        new Vector3(1, 0, 1),  // Правый нижний
                        new Vector3(1, 1, 1),  // Правый верхний
                        new Vector3(0, 1, 1)   // Левый верхний
                    };
                    break;
        
                case FaceDirection.South:
                    vertices =  new Vector3[] {
                        new Vector3(1, 0, 0),
                        new Vector3(0, 0, 0),
                        new Vector3(0, 1, 0),
                        new Vector3(1, 1, 0)
                    };
                    break;

                case FaceDirection.East:
                    vertices =  new Vector3[] {
                        new Vector3(1, 0, 1),
                        new Vector3(1, 0, 0),
                        new Vector3(1, 1, 0),
                        new Vector3(1, 1, 1)
                    };
                    break;

                case FaceDirection.West:
                    vertices =  new Vector3[] {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 1),
                        new Vector3(0, 1, 1),
                        new Vector3(0, 1, 0)
                    };
                    break;

                case FaceDirection.Top:
                    vertices =  new Vector3[] {
                        new Vector3(0, 1, 1),
                        new Vector3(1, 1, 1),
                        new Vector3(1, 1, 0),
                        new Vector3(0, 1, 0)
                    };
                    break;

                case FaceDirection.Bottom:
                    vertices =  new Vector3[] {
                        new Vector3(0, 0, 0),
                        new Vector3(1, 0, 0),
                        new Vector3(1, 0, 1),
                        new Vector3(0, 0, 1)
                    };
                    break;

                default: vertices =  new Vector3[4];
                    break;
            }
            
            // Масштабируем вершины
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] *= _voxelSize;
            }

            return vertices;
        }

        /// <summary>
        /// Возвращает UV-координаты для текстуры грани (заглушка)
        /// </summary>
        /// <param name="blockType">Тип блока</param>
        /// <param name="direction">Направление грани</param>
        /// <returns>Стандартные UV-координаты</returns>
        /// <remarks>
        /// Реализуйте логику текстур на основе вашей системы текстурных атласов
        /// </remarks>
        private Vector2[] GetFaceUVs(byte blockType, FaceDirection direction)
        {
            // Заглушка: равномерное текстурирование
            return new Vector2[] {
                new Vector2(0, 0),  // Левый нижний угол UV
                new Vector2(1, 0),  // Правый нижний
                new Vector2(1, 1),  // Правый верхний
                new Vector2(0, 1)   // Левый верхний
            };
        }

        /// <summary>
        /// Направления граней вокселя
        /// </summary>
        private enum FaceDirection
        {
            /// <summary> Грань с положительным Z-направлением </summary>
            North,
            /// <summary> Грань с отрицательным Z-направлением </summary>
            South,
            /// <summary> Грань с положительным X-направлением </summary>
            East,
            /// <summary> Грань с отрицательным X-направлением </summary>
            West,
            /// <summary> Верхняя грань (положительное Y) </summary>
            Top,
            /// <summary> Нижняя грань (отрицательное Y) </summary>
            Bottom
        }
    }
}