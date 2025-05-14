using UnityEngine;
using System.Collections.Generic;
using VoxelEngine.Core;

namespace VoxelEngine.Rendering.Meshing
{
    public class GreedyMesher : MonoBehaviour
    {
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Vector2> _uvs = new List<Vector2>();
        private int _currentVertexIndex;

        public Mesh GenerateMesh(VoxelData[,,] voxels)
        {
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _currentVertexIndex = 0;

            // Проход по всем осям
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        if (voxels[x, y, z].Type == 0) continue;

                        // Проверка видимости граней
                        if (IsTransparent(x + 1, y, z, voxels)) CreateFace(x, y, z, FaceDirection.East, (byte)voxels[x, y, z].Type);
                        if (IsTransparent(x - 1, y, z, voxels)) CreateFace(x, y, z, FaceDirection.West, (byte)voxels[x, y, z].Type);
                        if (IsTransparent(x, y + 1, z, voxels)) CreateFace(x, y, z, FaceDirection.Top, (byte)voxels[x, y, z].Type);
                        if (IsTransparent(x, y - 1, z, voxels)) CreateFace(x, y, z, FaceDirection.Bottom, (byte)voxels[x, y, z].Type);
                        if (IsTransparent(x, y, z + 1, voxels)) CreateFace(x, y, z, FaceDirection.North, (byte)voxels[x, y, z].Type);
                        if (IsTransparent(x, y, z - 1, voxels)) CreateFace(x, y, z, FaceDirection.South, (byte)voxels[x, y, z].Type);
                    }
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "Chunk Mesh";
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.uv = _uvs.ToArray();
            mesh.RecalculateNormals();
            return mesh;
        }

        private bool IsTransparent(int x, int y, int z, VoxelData[,,] voxels)
        {
            if (x < 0 || x >= 16 || y < 0 || y >= 16 || z < 0 || z >= 16) return true;
            return voxels[x, y, z].Type == VoxelType.Air;
        }

        private void CreateFace(int x, int y, int z, FaceDirection direction, byte blockType)
        {
            Vector3 offset = new Vector3(x, y, z);
            Vector3[] faceVertices = GetFaceVertices(direction);
            Vector2[] faceUVs = GetFaceUVs(blockType, direction);

            foreach (Vector3 vertex in faceVertices)
            {
                _vertices.Add(vertex + offset);
            }

            _triangles.Add(_currentVertexIndex);
            _triangles.Add(_currentVertexIndex + 1);
            _triangles.Add(_currentVertexIndex + 2);
            _triangles.Add(_currentVertexIndex);
            _triangles.Add(_currentVertexIndex + 2);
            _triangles.Add(_currentVertexIndex + 3);

            _uvs.AddRange(faceUVs);
            _currentVertexIndex += 4;
        }

        private Vector3[] GetFaceVertices(FaceDirection direction)
        {
            switch (direction)
            {
                case FaceDirection.North:
                    return new Vector3[] {
                        new Vector3(0, 0, 1),
                        new Vector3(1, 0, 1),
                        new Vector3(1, 1, 1),
                        new Vector3(0, 1, 1)
                    };
        
                case FaceDirection.South:
                    return new Vector3[] {
                        new Vector3(1, 0, 0),
                        new Vector3(0, 0, 0),
                        new Vector3(0, 1, 0),
                        new Vector3(1, 1, 0)
                    };

                case FaceDirection.East:
                    return new Vector3[] {
                        new Vector3(1, 0, 1),
                        new Vector3(1, 0, 0),
                        new Vector3(1, 1, 0),
                        new Vector3(1, 1, 1)
                    };

                case FaceDirection.West:
                    return new Vector3[] {
                        new Vector3(0, 0, 0),
                        new Vector3(0, 0, 1),
                        new Vector3(0, 1, 1),
                        new Vector3(0, 1, 0)
                    };

                case FaceDirection.Top:
                    return new Vector3[] {
                        new Vector3(0, 1, 1),
                        new Vector3(1, 1, 1),
                        new Vector3(1, 1, 0),
                        new Vector3(0, 1, 0)
                    };

                case FaceDirection.Bottom:
                    return new Vector3[] {
                        new Vector3(0, 0, 0),
                        new Vector3(1, 0, 0),
                        new Vector3(1, 0, 1),
                        new Vector3(0, 0, 1)
                    };

                default: return new Vector3[4];
            }
        }

        private Vector2[] GetFaceUVs(byte blockType, FaceDirection direction)
        {
            // Реализуйте логику текстур в зависимости от типа блока и направления
            return new Vector2[] {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };
        }

        private enum FaceDirection
        {
            North,
            South,
            East,
            West,
            Top,
            Bottom
        }
    }
}