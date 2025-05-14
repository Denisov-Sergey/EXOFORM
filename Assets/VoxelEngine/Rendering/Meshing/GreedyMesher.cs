using UnityEngine;
using System.Collections.Generic;
using VoxelEngine.Core;

namespace VoxelEngine.Rendering.Meshing
{
    public class GreedyMesher : MonoBehaviour
    {
        private int _width, _height, _depth;
        private bool[,,] _processed;
        private List<Vector3> _vertices = new List<Vector3>();
        private List<int> _triangles = new List<int>();
        private List<Vector2> _uvs = new List<Vector2>();

        public Mesh GenerateMesh(VoxelData[,,] voxels)
        {
            _width = voxels.GetLength(0);
            _height = voxels.GetLength(1);
            _depth = voxels.GetLength(2);
            _processed = new bool[_width, _height, _depth];

            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();

            GenerateFaces(voxels, Direction.Forward);
            GenerateFaces(voxels, Direction.Back);
            GenerateFaces(voxels, Direction.Left);
            GenerateFaces(voxels, Direction.Right);
            GenerateFaces(voxels, Direction.Up);
            GenerateFaces(voxels, Direction.Down);

            Mesh mesh = new Mesh();
            mesh.vertices = _vertices.ToArray();
            mesh.triangles = _triangles.ToArray();
            mesh.uv = _uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            Debug.Log($"Vertices: {_vertices.Count}, Triangles: {_triangles.Count}");
            return mesh;
        }

        private enum Direction { Forward, Back, Left, Right, Up, Down }

        private void GenerateFaces(VoxelData[,,] voxels, Direction dir)
        {
            Axis axis1, axis2;
            int depth;
            switch (dir)
            {
                case Direction.Forward:
                case Direction.Back:
                    axis1 = Axis.X;
                    axis2 = Axis.Y;
                    depth = _depth;
                    break;
                case Direction.Left:
                case Direction.Right:
                    axis1 = Axis.Z;
                    axis2 = Axis.Y;
                    depth = _width;
                    break;
                default:
                    axis1 = Axis.X;
                    axis2 = Axis.Z;
                    depth = _height;
                    break;
            }

            for (int d = 0; d < depth; d++)
            {
                for (int a1 = 0; a1 < GetAxisSize(axis1); a1++)
                {
                    for (int a2 = 0; a2 < GetAxisSize(axis2); a2++)
                    {
                        GetVoxelCoords(dir, a1, a2, d, out int x, out int y, out int z);
                        if (x < 0 || y < 0 || z < 0 || x >= _width || y >= _height || z >= _depth)
                            continue;

                        if (_processed[x, y, z] || voxels[x, y, z].Type == 0)
                            continue;

                        int width = 1, height = 1;
                        while (a1 + width < GetAxisSize(axis1) && CanMerge(voxels, dir, a1, a2, d, width, 0))
                            width++;
                        while (a2 + height < GetAxisSize(axis2) && CanMerge(voxels, dir, a1, a2, d, width, height))
                            height++;

                        AddQuad(x, y, z, dir, width, height);
                        MarkProcessed(dir, a1, a2, d, width, height);
                    }
                }
            }
        }

        private bool CanMerge(VoxelData[,,] voxels, Direction dir, int a1, int a2, int d, int w, int h)
        {
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    GetVoxelCoords(dir, a1 + i, a2 + j, d, out int x, out int y, out int z);
                    if (x >= _width || y >= _height || z >= _depth || x < 0 || y < 0 || z < 0)
                        return false;
                    if (_processed[x, y, z] || voxels[x, y, z].Type == 0)
                        return false;
                }
            }
            return true;
        }

        private void AddQuad(int x, int y, int z, Direction dir, int width, int height)
        {
            Vector3[] corners = GetQuadCorners(x, y, z, dir, width, height);
            int startIndex = _vertices.Count;

            _vertices.AddRange(corners);

            _uvs.Add(new Vector2(0, 0));
            _uvs.Add(new Vector2(1, 0));
            _uvs.Add(new Vector2(0, 1));
            _uvs.Add(new Vector2(1, 1));

            // Исправленный порядок треугольников для корректных нормалей
            _triangles.Add(startIndex);
            _triangles.Add(startIndex + 2);
            _triangles.Add(startIndex + 1);

            _triangles.Add(startIndex + 1);
            _triangles.Add(startIndex + 2);
            _triangles.Add(startIndex + 3);
        }

        private Vector3[] GetQuadCorners(int x, int y, int z, Direction dir, int w, int h)
        {
            float half = 0.5f;
            Vector3[] corners = new Vector3[4];

            switch (dir)
            {
                case Direction.Forward:
                    corners[0] = new Vector3(x - half, y - half, z + half);
                    corners[1] = new Vector3(x + w - half, y - half, z + half);
                    corners[2] = new Vector3(x - half, y + h - half, z + half);
                    corners[3] = new Vector3(x + w - half, y + h - half, z + half);
                    break;
                case Direction.Back:
                    corners[0] = new Vector3(x - half, y - half, z - half);
                    corners[1] = new Vector3(x + w - half, y - half, z - half);
                    corners[2] = new Vector3(x - half, y + h - half, z - half);
                    corners[3] = new Vector3(x + w - half, y + h - half, z - half);
                    break;
                case Direction.Left:
                    corners[0] = new Vector3(x - half, y - half, z - half);
                    corners[1] = new Vector3(x - half, y - half, z + h - half);
                    corners[2] = new Vector3(x - half, y + h - half, z - half);
                    corners[3] = new Vector3(x - half, y + h - half, z + h - half);
                    break;
                case Direction.Right:
                    corners[0] = new Vector3(x + half, y - half, z + h - half);
                    corners[1] = new Vector3(x + half, y - half, z - half);
                    corners[2] = new Vector3(x + half, y + h - half, z + h - half);
                    corners[3] = new Vector3(x + half, y + h - half, z - half);
                    break;
                case Direction.Up:
                    corners[0] = new Vector3(x - half, y + half, z - half);
                    corners[1] = new Vector3(x + w - half, y + half, z - half);
                    corners[2] = new Vector3(x - half, y + half, z + h - half);
                    corners[3] = new Vector3(x + w - half, y + half, z + h - half);
                    break;
                case Direction.Down:
                    corners[0] = new Vector3(x - half, y - half, z - half);
                    corners[1] = new Vector3(x + w - half, y - half, z - half);
                    corners[2] = new Vector3(x - half, y - half, z + h - half);
                    corners[3] = new Vector3(x + w - half, y - half, z + h - half);
                    break;
            }

            return corners;
        }

        private enum Axis { X, Y, Z }

        private int GetAxisSize(Axis axis)
        {
            return axis switch
            {
                Axis.X => _width,
                Axis.Y => _height,
                Axis.Z => _depth,
                _ => 0
            };
        }

        private Vector3 GetDirectionNormal(Direction dir)
        {
            return dir switch
            {
                Direction.Forward => Vector3.forward,
                Direction.Back => Vector3.back,
                Direction.Left => Vector3.left,
                Direction.Right => Vector3.right,
                Direction.Up => Vector3.up,
                Direction.Down => Vector3.down,
                _ => Vector3.zero
            };
        }

        // Вспомогательные методы для преобразования координат
        private void GetVoxelCoords(Direction dir, int a1, int a2, int d, out int x, out int y, out int z)
        {
            x = y = z = 0;

            switch (dir)
            {
                case Direction.Forward:
                    x = a1;
                    y = a2;
                    z = d;
                    break;
                case Direction.Back:
                    x = a1;
                    y = a2;
                    z = _depth - 1 - d;
                    break;
                case Direction.Left:
                    x = d;
                    y = a2;
                    z = _depth - 1 - a1;
                    break;
                case Direction.Right:
                    x = _width - 1 - d;
                    y = a2;
                    z = a1;
                    break;
                case Direction.Up:
                    x = a1;
                    y = _height - 1 - d;
                    z = a2;
                    break;
                case Direction.Down:
                    x = a1;
                    y = d;
                    z = a2;
                    break;
            }
        }

        private void MarkProcessed(Direction dir, int a1, int a2, int d, int w, int h)
        {
            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    int x, y, z;
                    GetVoxelCoords(dir, a1 + i, a2 + j, d, out x, out y, out z);
                    _processed[x, y, z] = true;
                }
            }
        }
    }
}