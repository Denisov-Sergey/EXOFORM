using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PandemicWars.Scripts.Map
{
    /// <summary>
    /// Класс для генерации дорог
    /// </summary>
    public class RoadGenerator
    {
        private CityGrid cityGrid;

        public RoadGenerator(CityGrid grid)
        {
            cityGrid = grid;
        }

        public IEnumerator GenerateRoads(float density, int roadLength, float animationSpeed)
        {
            int totalCells = cityGrid.Width * cityGrid.Height;
            int targetRoadCells = Mathf.RoundToInt(totalCells * density);
            int roadSegments = Mathf.Max(1, targetRoadCells / roadLength);

            Debug.Log($"🛤️ Планируем создать {roadSegments} сегментов (целевое количество клеток: {targetRoadCells}, {density * 100:F1}% карты)");

            // Подсчитываем дороги до генерации
            int roadsBefore = CountRoadCells();

            for (int i = 0; i < roadSegments; i++)
            {
                Vector2Int start = GetRandomGrassPosition();
                if (start.x >= 0)
                {
                    yield return CreateRoadSegment(start, roadLength, animationSpeed);
                }
            }

            // Подсчитываем дороги после генерации
            int roadsAfter = CountRoadCells();
            int actualRoadsCreated = roadsAfter - roadsBefore;
            float actualPercentage = (float)roadsAfter / totalCells * 100f;

            Debug.Log($"🛤️ Дороги созданы! Фактически: {roadsAfter} клеток ({actualPercentage:F2}% карты), создано новых: {actualRoadsCreated}");
        }

        /// <summary>
        /// Подсчитывает количество дорожных клеток на карте
        /// </summary>
        private int CountRoadCells()
        {
            int roadCount = 0;
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    if (cityGrid.Grid[x][y] == TileType.RoadStraight)
                    {
                        roadCount++;
                    }
                }
            }
            return roadCount;
        }


        Vector2Int GetRandomGrassPosition()
        {
            List<Vector2Int> grassPositions = new List<Vector2Int>();

            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    if (cityGrid.Grid[x][y] == TileType.Grass)
                    {
                        grassPositions.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (grassPositions.Count > 0)
            {
                return grassPositions[Random.Range(0, grassPositions.Count)];
            }

            return new Vector2Int(-1, -1);
        }

        IEnumerator CreateRoadSegment(Vector2Int start, int length, float animationSpeed)
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            Vector2Int direction = directions[Random.Range(0, directions.Length)];
            Vector2Int current = start;

            for (int i = 0; i < length; i++)
            {
                if (cityGrid.IsValidPosition(current))
                {
                    cityGrid.Grid[current.x][current.y] = TileType.RoadStraight;
                    current += direction;
                    yield return new WaitForSeconds(animationSpeed * 0.2f);
                }
                else
                {
                    break;
                }
            }
        }
    }
}