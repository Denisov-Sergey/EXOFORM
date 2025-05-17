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
        private List<Color32> _colors  = new List<Color32>();
        // [SerializeField] private Texture2D _textureAtlas;
        // private float _tileSize = 1f;
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
            
            Destroy(chunkObj);
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
            _colors.Clear();
            _currentVertexIndex = 0;

            // Проход по всем вокселям чанка
            for (int y = 0; y < _chunkSize; y++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    for (int x = 0; x < _chunkSize; x++)
                    {
                        var voxel = voxels[x, y, z];
                        if (voxels[x, y, z].Type == 0) continue;
                        
                        Color32 voxelColor = voxel.Color;

                        // Проверка и создание видимых граней
                        if (IsTransparent(x + 1, y, z, voxels)) CreateFace(x, y, z, FaceDirection.East,voxelColor);
                        if (IsTransparent(x - 1, y, z, voxels)) CreateFace(x, y, z, FaceDirection.West, voxelColor);
                        if (IsTransparent(x, y + 1, z, voxels)) CreateFace(x, y, z, FaceDirection.Top, voxelColor );
                        if (IsTransparent(x, y - 1, z, voxels)) CreateFace(x, y, z, FaceDirection.Bottom, voxelColor );
                        if (IsTransparent(x, y, z + 1, voxels)) CreateFace(x, y, z, FaceDirection.North,voxelColor );
                        if (IsTransparent(x, y, z - 1, voxels)) CreateFace(x, y, z, FaceDirection.South, voxelColor );
                    }
                }
            }

            // Создание и настройка меша
            Mesh mesh = new Mesh();
            mesh.name = "Chunk Mesh";
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.uv = _uvs.ToArray();
            mesh.colors32 = _colors.ToArray();
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
        /// <param name="color">Цвет блока</param>
        private void CreateFace(int x, int y, int z, FaceDirection direction, Color32 color)
        {
            // Рассчитываем реальный размер с учетом VoxelSize
            Vector3 offset = new Vector3(
                x * _voxelSize,
                y * _voxelSize,
                z * _voxelSize
            );
            
            Vector3[] faceVertices = GetFaceVertices(direction);
            // Vector2[] faceUVs = GetFaceUVs(blockType, direction);
            

            // Добавление вершин с учетом позиции вокселя
            foreach (Vector3 vertex in faceVertices)
            {
                _vertices.Add(vertex + offset);
                _colors.Add(color);
            }

            // Формирование двух треугольников для грани
            _triangles.Add(_currentVertexIndex);
            _triangles.Add(_currentVertexIndex + 1);
            _triangles.Add(_currentVertexIndex + 2);
            _triangles.Add(_currentVertexIndex);
            _triangles.Add(_currentVertexIndex + 2);
            _triangles.Add(_currentVertexIndex + 3);

            // _uvs.AddRange(faceUVs);
            _currentVertexIndex += 4;
            
            Debug.Log($"Грань блока ({x}, {y}, {z}): Цвет = {color}");

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
        // private Vector2[] GetFaceUVs(byte blockType, FaceDirection direction)
        // {
        //     // Определяем текстуру по типу блока и направлению
        //     int textureIndex = GetTextureIndex((VoxelType)blockType, direction);
        //
        //     // Рассчитываем UV
        //     float x = (textureIndex % 4) * _tileSize;
        //     float y = (textureIndex / 4) * _tileSize;
        //     
        //     return new Vector2[] {
        //         new Vector2(x, y), // Левый нижний угол UV
        //         new Vector2(x + _tileSize, y), // Правый нижний
        //         new Vector2(x + _tileSize, y + _tileSize), // Правый верхний
        //         new Vector2(x, y + _tileSize) // Левый верхний
        //     };
        // }
        // private int GetTextureIndex(VoxelType type, FaceDirection dir)
        // {
        //     switch(type)
        //     {
        //         case VoxelType.Grass: 
        //             return dir == FaceDirection.Top ? 0 : 1; // 0 - верх, 1 - бок
        //         case VoxelType.Stone: return 2;
        //         case VoxelType.Dirt: return 3;
        //         default: return 0;
        //     }
        // }

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