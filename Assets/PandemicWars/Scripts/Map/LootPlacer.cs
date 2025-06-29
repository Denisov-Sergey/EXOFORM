using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PandemicWars.Scripts.Map
{
    /// <summary>
    /// Специализированный класс для размещения лута на карте
    /// </summary>
    public class LootPlacer
    {
        private CityGrid cityGrid;
        private List<PrefabSettings> lootPrefabs;
        private MonoBehaviour coroutineRunner;
        private ExoformMapGenerator _exoformMapGenerator;

        public LootPlacer(CityGrid grid, List<GameObject> prefabs, MonoBehaviour runner, ExoformMapGenerator generator)
        {
            cityGrid = grid;
            coroutineRunner = runner;
            _exoformMapGenerator = generator;
            LoadLootPrefabs(prefabs);
        }

        void LoadLootPrefabs(List<GameObject> prefabs)
        {
            lootPrefabs = new List<PrefabSettings>();

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null && settings.tileType == TileType.SupplyCache)
                    {
                        lootPrefabs.Add(settings);
                    }
                }
            }

            Debug.Log($"📦 Загружено {lootPrefabs.Count} префабов лута");
        }

        public IEnumerator PlaceLoot(float animationSpeed)
        {
            if (lootPrefabs.Count == 0)
            {
                Debug.Log("  ⚠️ Нет префабов лута для размещения");
                yield break;
            }

            Debug.Log("📦 === РАЗМЕЩЕНИЕ ЛУТА ===");

            // Находим все подходящие позиции
            List<Vector2Int> lootPositions = FindLootPositions();
            
            if (lootPositions.Count == 0)
            {
                Debug.LogError("  ❌ Нет подходящих мест для лута!");
                yield break;
            }

            // Рассчитываем количество лута
            int roadCellsCount = CountRoadCells();
            int targetLootCount = Mathf.RoundToInt(roadCellsCount * _exoformMapGenerator.lootDensity);
            targetLootCount = Mathf.Clamp(targetLootCount, _exoformMapGenerator.minLootCount, _exoformMapGenerator.maxLootCount);

            Debug.Log($"  📊 Дорожных клеток: {roadCellsCount}");
            Debug.Log($"  🎯 Целевое количество лута: {targetLootCount} ({_exoformMapGenerator.lootDensity * 100:F1}% от дорог)");
            Debug.Log($"  📍 Доступных позиций: {lootPositions.Count}");

            // Размещаем лут
            if (_exoformMapGenerator.clusterLoot)
            {
                yield return PlaceLootClusters(lootPositions, targetLootCount, animationSpeed);
            }
            else
            {
                yield return PlaceLootSingle(lootPositions, targetLootCount, animationSpeed);
            }
        }

        IEnumerator PlaceLootClusters(List<Vector2Int> positions, int targetCount, float animationSpeed)
        {
            int placedCount = 0;
            int clusterCount = Mathf.CeilToInt((float)targetCount / _exoformMapGenerator.lootClusterSize);

            Debug.Log($"  🎯 Создаем {clusterCount} групп лута по {_exoformMapGenerator.lootClusterSize} штук");

            for (int i = 0; i < clusterCount && positions.Count > 0 && placedCount < targetCount; i++)
            {
                // Выбираем центр кластера
                int centerIndex = Random.Range(0, positions.Count);
                Vector2Int center = positions[centerIndex];

                // Размещаем кластер
                var clusterPositions = GetClusterPositions(center, positions, _exoformMapGenerator.lootClusterSize);
                
                foreach (var pos in clusterPositions)
                {
                    if (placedCount >= targetCount) break;
                    
                    if (TryPlaceLoot(pos))
                    {
                        placedCount++;
                        positions.Remove(pos);
                        yield return new WaitForSeconds(animationSpeed * 0.2f);
                    }
                }

                // Удаляем позиции вокруг кластера
                RemoveNearbyPositions(positions, center, 5);
                
                Debug.Log($"  ✅ Группа {i + 1}/{clusterCount} размещена в {center} ({clusterPositions.Count} ящиков)");
            }

            Debug.Log($"  📦 Размещено лута: {placedCount}/{targetCount}");
        }

        IEnumerator PlaceLootSingle(List<Vector2Int> positions, int targetCount, float animationSpeed)
        {
            int placedCount = 0;

            // Распределяем лут равномерно
            int skipInterval = Mathf.Max(1, positions.Count / targetCount);

            for (int i = 0; i < positions.Count && placedCount < targetCount; i += skipInterval)
            {
                int randomOffset = Random.Range(-skipInterval/2, skipInterval/2);
                int index = Mathf.Clamp(i + randomOffset, 0, positions.Count - 1);
                
                if (TryPlaceLoot(positions[index]))
                {
                    placedCount++;
                    positions.RemoveAt(index);
                    
                    if (placedCount % 5 == 0)
                    {
                        Debug.Log($"  📈 Прогресс: {placedCount}/{targetCount}");
                    }
                    
                    yield return new WaitForSeconds(animationSpeed * 0.3f);
                }
            }

            Debug.Log($"  📦 Размещено лута: {placedCount}/{targetCount}");
        }

        List<Vector2Int> FindLootPositions()
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    // Лут может быть на дорогах и рядом со зданиями
                    if (IsGoodLootPosition(pos))
                    {
                        positions.Add(pos);
                    }
                }
            }

            // Перемешиваем для случайности
            for (int i = 0; i < positions.Count; i++)
            {
                int randomIndex = Random.Range(i, positions.Count);
                var temp = positions[i];
                positions[i] = positions[randomIndex];
                positions[randomIndex] = temp;
            }

            return positions;
        }

        bool IsGoodLootPosition(Vector2Int pos)
        {
            // На дороге
            if (cityGrid.Grid[pos.x][pos.y] == TileType.RoadStraight)
            {
                return !cityGrid.IsCellOccupiedByBuilding(pos);
            }

            // Или рядом со зданием (но не на траве далеко от всего)
            if (cityGrid.Grid[pos.x][pos.y] == TileType.Grass)
            {
                return !cityGrid.IsCellOccupiedByBuilding(pos) && 
                       HasBuildingNearby(pos, 1) && 
                       HasRoadNearby(pos, 2);
            }

            return false;
        }

        List<Vector2Int> GetClusterPositions(Vector2Int center, List<Vector2Int> availablePositions, int clusterSize)
        {
            List<Vector2Int> cluster = new List<Vector2Int> { center };
            
            // Ищем позиции рядом с центром
            var nearbyPositions = availablePositions
                .Where(p => p != center && Vector2Int.Distance(p, center) <= 2)
                .OrderBy(p => Vector2Int.Distance(p, center))
                .Take(clusterSize - 1)
                .ToList();

            cluster.AddRange(nearbyPositions);
            return cluster;
        }

        bool TryPlaceLoot(Vector2Int position)
        {
            if (!cityGrid.IsValidPosition(position) || cityGrid.IsCellOccupiedByBuilding(position))
                return false;

            // Выбираем случайный префаб лута
            var lootPrefab = lootPrefabs[Random.Range(0, lootPrefabs.Count)];
            
            if (!cityGrid.BuildingOccupancy.ContainsKey(TileType.SupplyCache))
                cityGrid.BuildingOccupancy[TileType.SupplyCache] = new List<Vector2Int>();
            
            cityGrid.BuildingOccupancy[TileType.SupplyCache].Add(position);
            
            return true;
        }

        int CountRoadCells()
        {
            int count = 0;
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    if (cityGrid.Grid[x][y] == TileType.RoadStraight)
                        count++;
                }
            }
            return count;
        }

        bool HasBuildingNearby(Vector2Int pos, int distance)
        {
            for (int dx = -distance; dx <= distance; dx++)
            {
                for (int dy = -distance; dy <= distance; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    Vector2Int checkPos = pos + new Vector2Int(dx, dy);
                    if (cityGrid.IsValidPosition(checkPos) && 
                        cityGrid.IsCellOccupiedByBuilding(checkPos))
                    {
                        var buildingType = cityGrid.GetBuildingTypeAt(checkPos);
                        if (buildingType.HasValue && !IsVegetationType(buildingType.Value))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        bool HasRoadNearby(Vector2Int pos, int distance)
        {
            for (int dx = -distance; dx <= distance; dx++)
            {
                for (int dy = -distance; dy <= distance; dy++)
                {
                    Vector2Int checkPos = pos + new Vector2Int(dx, dy);
                    if (cityGrid.IsValidPosition(checkPos) && 
                        cityGrid.Grid[checkPos.x][checkPos.y] == TileType.RoadStraight)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool IsVegetationType(TileType type)
        {
            return type == TileType.Spore || type == TileType.SporeCluster || 
                   type == TileType.CorruptedVegetation || 
                   type == TileType.Forest || 
                   type == TileType.AlienGrowth;
        }

        void RemoveNearbyPositions(List<Vector2Int> positions, Vector2Int center, int distance)
        {
            positions.RemoveAll(pos => 
                Mathf.Abs(pos.x - center.x) <= distance && 
                Mathf.Abs(pos.y - center.y) <= distance);
        }
    }
}