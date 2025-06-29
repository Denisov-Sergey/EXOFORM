using System.Collections.Generic;
using UnityEngine;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Класс для управления сеткой города
    /// </summary>
    public class CityGrid
    {
        public TileType[][] Grid { get; private set; }
        public GameObject[][] SpawnedTiles { get; private set; }
        public Dictionary<TileType, List<Vector2Int>> BuildingOccupancy { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public float TileSize { get; private set; }

        public CityGrid(int width, int height, float tileSize)
        {
            Width = width;
            Height = height;
            TileSize = tileSize;
            BuildingOccupancy = new Dictionary<TileType, List<Vector2Int>>();
            Initialize();
        }

        public void Initialize()
        {
            Grid = new TileType[Width][];
            SpawnedTiles = new GameObject[Width][];
            BuildingOccupancy.Clear();

            for (int x = 0; x < Width; x++)
            {
                Grid[x] = new TileType[Height];
                SpawnedTiles[x] = new GameObject[Height];

                for (int y = 0; y < Height; y++)
                {
                    Grid[x][y] = TileType.Grass;
                }
            }
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < Width && pos.y >= 0 && pos.y < Height;
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            return new Vector3(x * TileSize, 0, y * TileSize);
        }

        /// <summary>
        /// Проверить, занята ли клетка зданием
        /// </summary>
        public bool IsCellOccupiedByBuilding(Vector2Int cell)
        {
            foreach (var buildingType in BuildingOccupancy.Values)
            {
                if (buildingType.Contains(cell))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Получить тип здания в клетке (если есть)
        /// </summary>
        public TileType? GetBuildingTypeAt(Vector2Int cell)
        {
            foreach (var kvp in BuildingOccupancy)
            {
                if (kvp.Value.Contains(cell))
                    return kvp.Key;
            }
            return null;
        }
    }
}