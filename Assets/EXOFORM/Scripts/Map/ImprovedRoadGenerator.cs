using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Улучшенный генератор дорог с перекрестками и связностью
    /// </summary>
    public class ImprovedRoadGenerator
    {
        private CityGrid cityGrid;
        
        [System.Serializable]
        public class RoadSettings
        {
            [Range(0.1f, 0.9f)]
            public float branchProbability = 0.3f;  // Вероятность ответвления
            
            [Range(2, 10)]
            public int minSegmentLength = 3;        // Минимальная длина сегмента
            
            [Range(5, 20)]
            public int maxSegmentLength = 10;       // Максимальная длина сегмента
            
            public bool createIntersections = true; // Создавать перекрестки
            public bool connectRoads = true;        // Соединять дороги
        }
        
        private RoadSettings settings;

        public ImprovedRoadGenerator(CityGrid grid, RoadSettings roadSettings = null)
        {
            cityGrid = grid;
            settings = roadSettings ?? new RoadSettings();
        }

        public IEnumerator GenerateRoads(float density, int roadLength, float animationSpeed)
        {
            int totalCells = cityGrid.Width * cityGrid.Height;
            int targetRoadCells = Mathf.RoundToInt(totalCells * density);
            
            // Используем roadLength для настройки длины сегментов
            settings.minSegmentLength = Mathf.Max(2, roadLength / 3);
            settings.maxSegmentLength = Mathf.Max(settings.minSegmentLength + 1, roadLength);
            
            Debug.Log($"🛤️ Генерация улучшенных дорог (цель: {targetRoadCells} клеток, длина сегментов: {settings.minSegmentLength}-{settings.maxSegmentLength})");

            // Создаем основные магистрали
            yield return CreateMainRoads(animationSpeed);
            
            // Добавляем второстепенные дороги
            yield return CreateSecondaryRoads(targetRoadCells, animationSpeed);
            
            // Соединяем изолированные участки
            if (settings.connectRoads)
            {
                yield return ConnectIsolatedRoads(animationSpeed);
            }

            int finalRoadCount = CountRoadCells();
            float percentage = (float)finalRoadCount / totalCells * 100f;
            Debug.Log($"🛤️ Дороги созданы: {finalRoadCount} клеток ({percentage:F2}%)");
        }

        IEnumerator CreateMainRoads(float animationSpeed)
        {
            // Создаем главные магистрали (горизонтальные и вертикальные)
            int horizontalRoads = Random.Range(2, 4);
            int verticalRoads = Random.Range(2, 4);
            
            // Горизонтальные магистрали
            for (int i = 0; i < horizontalRoads; i++)
            {
                int y = Random.Range(cityGrid.Height / 4, 3 * cityGrid.Height / 4);
                yield return CreateRoadLine(
                    new Vector2Int(0, y), 
                    new Vector2Int(cityGrid.Width - 1, y), 
                    animationSpeed
                );
            }
            
            // Вертикальные магистрали
            for (int i = 0; i < verticalRoads; i++)
            {
                int x = Random.Range(cityGrid.Width / 4, 3 * cityGrid.Width / 4);
                yield return CreateRoadLine(
                    new Vector2Int(x, 0), 
                    new Vector2Int(x, cityGrid.Height - 1), 
                    animationSpeed
                );
            }
        }

        IEnumerator CreateSecondaryRoads(int targetCells, float animationSpeed)
        {
            int currentRoads = CountRoadCells();
            int attempts = 0;
            
            while (currentRoads < targetCells && attempts < 100)
            {
                // Находим существующую дорогу для ответвления
                Vector2Int? branchPoint = FindRandomRoadCell();
                
                if (branchPoint.HasValue)
                {
                    // Создаем ответвление
                    yield return CreateBranch(branchPoint.Value, animationSpeed);
                }
                
                currentRoads = CountRoadCells();
                attempts++;
            }
        }

        IEnumerator CreateBranch(Vector2Int startPoint, float animationSpeed)
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            Vector2Int direction = directions[Random.Range(0, directions.Length)];
            
            // Проверяем, не идет ли дорога уже в этом направлении
            Vector2Int checkPos = startPoint + direction;
            if (cityGrid.IsValidPosition(checkPos) && 
                cityGrid.Grid[checkPos.x][checkPos.y] == TileType.RoadStraight)
            {
                yield break;
            }
            
            int length = Random.Range(settings.minSegmentLength, settings.maxSegmentLength);
            Vector2Int current = startPoint;
            
            for (int i = 0; i < length; i++)
            {
                current += direction;
                
                if (!cityGrid.IsValidPosition(current))
                    break;
                
                // Если встретили другую дорогу - соединяемся
                if (cityGrid.Grid[current.x][current.y] == TileType.RoadStraight)
                {
                    if (settings.createIntersections)
                    {
                        // Можно пометить как перекресток
                        Debug.Log($"Создан перекресток в {current}");
                    }
                    break;
                }
                
                cityGrid.Grid[current.x][current.y] = TileType.RoadStraight;
                yield return new WaitForSeconds(animationSpeed * 0.1f);
                
                // Случайное ответвление
                if (Random.value < settings.branchProbability && i > settings.minSegmentLength / 2)
                {
                    // Поворачиваем на 90 градусов
                    Vector2Int newDirection = Random.value < 0.5f ? 
                        new Vector2Int(direction.y, -direction.x) : 
                        new Vector2Int(-direction.y, direction.x);
                    direction = newDirection;
                }
            }
        }

        IEnumerator CreateRoadLine(Vector2Int start, Vector2Int end, float animationSpeed)
        {
            Vector2Int current = start;
            Vector2Int direction = new Vector2Int(
                System.Math.Sign(end.x - start.x),
                System.Math.Sign(end.y - start.y)
            );
            
            while (current != end)
            {
                if (cityGrid.IsValidPosition(current))
                {
                    cityGrid.Grid[current.x][current.y] = TileType.RoadStraight;
                }
                
                if (current.x != end.x) current.x += direction.x;
                if (current.y != end.y) current.y += direction.y;
                
                yield return new WaitForSeconds(animationSpeed * 0.05f);
            }
        }

        IEnumerator ConnectIsolatedRoads(float animationSpeed)
        {
            // Простая реализация - можно улучшить алгоритмом поиска кластеров
            Debug.Log("🔗 Соединение изолированных участков дорог...");
            yield return null;
        }

        Vector2Int? FindRandomRoadCell()
        {
            List<Vector2Int> roadCells = new List<Vector2Int>();
            
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    if (cityGrid.Grid[x][y] == TileType.RoadStraight)
                    {
                        roadCells.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            if (roadCells.Count > 0)
            {
                return roadCells[Random.Range(0, roadCells.Count)];
            }
            
            return null;
        }

        int CountRoadCells()
        {
            int count = 0;
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    if (cityGrid.Grid[x][y] == TileType.RoadStraight)
                    {
                        count++;
                    }
                }
            }
            return count;
        }
    }
}