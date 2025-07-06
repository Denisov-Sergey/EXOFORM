using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Класс для размещения ресурсных точек на карте
    /// </summary>
    public class ResourcePlacer
    {
        private CityGrid cityGrid;
        private List<PrefabSettings> resourcePrefabs;
        private MonoBehaviour coroutineRunner;
        private ExoformMapGenerator _exoformMapGenerator;
        private ExoformZoneSystem zoneSystem;

        public ResourcePlacer(CityGrid grid, List<GameObject> prefabs, MonoBehaviour runner, ExoformMapGenerator generator, ExoformZoneSystem zoneSystem)
        {
            cityGrid = grid;
            coroutineRunner = runner;
            _exoformMapGenerator = generator;
            this.zoneSystem = zoneSystem;
            LoadResourcePrefabs(prefabs);
        }

        void LoadResourcePrefabs(List<GameObject> prefabs)
        {
            resourcePrefabs = new List<PrefabSettings>();

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null && IsResourceType(settings.tileType))
                    {
                        resourcePrefabs.Add(settings);
                    }
                }
            }

            Debug.Log($"⛏️ Загружено {resourcePrefabs.Count} префабов ресурсов");
        }

        public IEnumerator PlaceResources(float resourceDensity, float animationSpeed)
        {
            if (resourcePrefabs.Count == 0)
            {
                Debug.Log("  ⚠️ Нет префабов ресурсов для размещения");
                yield break;
            }

            Debug.Log("⛏️ === РАЗМЕЩЕНИЕ РЕСУРСОВ ===");

            // Находим подходящие позиции
            List<Vector2Int> resourcePositions = FindResourcePositions();
            
            if (resourcePositions.Count == 0)
            {
                Debug.LogError("  ❌ Нет подходящих мест для ресурсов!");
                yield break;
            }

            // Рассчитываем количество ресурсов
            int freeCells = CountFreeCells();
            int targetResourceCount = Mathf.RoundToInt(freeCells * resourceDensity);
            targetResourceCount = Mathf.Clamp(targetResourceCount, 5, resourcePositions.Count);

            Debug.Log($"  📊 Свободных клеток: {freeCells}");
            Debug.Log($"  🎯 Целевое количество ресурсов: {targetResourceCount} ({resourceDensity * 100:F1}% от свободных)");
            Debug.Log($"  📍 Доступных позиций: {resourcePositions.Count}");

            // Размещаем ресурсы с группировкой по типам
            yield return PlaceResourceClusters(resourcePositions, targetResourceCount, animationSpeed);
        }

        IEnumerator PlaceResourceClusters(List<Vector2Int> positions, int targetCount, float animationSpeed)
        {
            int placedCount = 0;
            
            // Группируем ресурсы по типам
            var resourceTypes = resourcePrefabs.Select(p => p.tileType).Distinct().ToList();
            int resourcesPerType = targetCount / resourceTypes.Count;

            foreach (var resourceType in resourceTypes)
            {
                int placedForType = 0;
                int targetForType = (resourceType == resourceTypes.Last()) ? 
                    targetCount - placedCount : resourcesPerType;

                Debug.Log($"  🔨 Размещаем {GetResourceEmoji(resourceType)} {resourceType}: {targetForType} ресурсов");

                // Создаем несколько кластеров для каждого типа
                int clustersCount = Mathf.Max(1, targetForType / 3);
                int resourcesPerCluster = targetForType / clustersCount;

                for (int cluster = 0; cluster < clustersCount && positions.Count > 0; cluster++)
                {
                    // Выбираем центр кластера
                    Vector2Int center = positions[Random.Range(0, positions.Count)];
                    
                    // Размещаем кластер
                    var clusterPositions = GetResourceClusterPositions(center, positions, resourcesPerCluster);
                    
                    foreach (var pos in clusterPositions)
                    {
                        if (placedForType >= targetForType) break;
                        
                        if (TryPlaceResource(pos, resourceType))
                        {
                            placedForType++;
                            placedCount++;
                            positions.Remove(pos);
                            yield return new WaitForSeconds(animationSpeed * 0.15f);
                        }
                    }

                    // Удаляем позиции вокруг кластера для разнообразия
                    RemoveNearbyPositions(positions, center, 3);
                    
                    Debug.Log($"    ✅ Кластер {cluster + 1}/{clustersCount} размещен в {center} ({clusterPositions.Count} ресурсов)");
                }

                Debug.Log($"  📈 {resourceType}: {placedForType}/{targetForType}");
            }

            Debug.Log($"  ⛏️ Всего размещено ресурсов: {placedCount}/{targetCount}");
        }

        List<Vector2Int> FindResourcePositions()
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    if (IsGoodResourcePosition(pos) && IsStandardZone(pos))
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

        bool IsGoodResourcePosition(Vector2Int pos)
        {
            // Проверяем, что позиция валидна
            if (!cityGrid.IsValidPosition(pos)) return false;
            
            // Не на дорогах
            if (cityGrid.Grid[pos.x][pos.y] == TileType.PathwayStraight) return false;
            
            // Не занято зданиями
            if (cityGrid.IsCellOccupiedByBuilding(pos)) return false;
            
            // На траве или в особых зонах
            if (cityGrid.Grid[pos.x][pos.y] != TileType.Grass) return false;
            
            // Не слишком близко к зданиям (минимум 2 клетки)
            if (HasBuildingNearby(pos, 1)) return false;
            
            // Не слишком далеко от дорог (максимум 8 клеток для доступности)
            if (!HasRoadNearby(pos, 8)) return false;

            return true;
        }

        List<Vector2Int> GetResourceClusterPositions(Vector2Int center, List<Vector2Int> availablePositions, int clusterSize)
        {
            List<Vector2Int> cluster = new List<Vector2Int> { center };
            
            // Ищем позиции рядом с центром
            var nearbyPositions = availablePositions
                .Where(p => p != center && Vector2Int.Distance(p, center) <= 3)
                .OrderBy(p => Vector2Int.Distance(p, center))
                .Take(clusterSize - 1)
                .ToList();

            cluster.AddRange(nearbyPositions);
            return cluster;
        }

        bool TryPlaceResource(Vector2Int position, TileType resourceType)
        {
            if (!IsStandardZone(position))
            {
                Debug.Log($"[ResourcePlacer] Зона несовместима в {position}");
                return false;
            }

            if (!cityGrid.IsValidPosition(position) || cityGrid.IsCellOccupiedByBuilding(position))
                return false;

            // Добавляем ресурс в сетку
            if (!cityGrid.BuildingOccupancy.ContainsKey(resourceType))
                cityGrid.BuildingOccupancy[resourceType] = new List<OccupiedCell>();

            cityGrid.BuildingOccupancy[resourceType].Add(new OccupiedCell(position, 0));
            
            return true;
        }

        int CountFreeCells()
        {
            int count = 0;
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (cityGrid.Grid[x][y] == TileType.Grass && !cityGrid.IsCellOccupiedByBuilding(pos))
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
                        if (buildingType.HasValue && !IsVegetationType(buildingType.Value) && !IsResourceType(buildingType.Value))
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
                        cityGrid.Grid[checkPos.x][checkPos.y] == TileType.PathwayStraight)
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

        bool IsResourceType(TileType type)
        {
            return type == TileType.WoodResource || type == TileType.StoneResource || 
                   type == TileType.BiomassResource || type == TileType.MetalResource;
        }

        string GetResourceEmoji(TileType resourceType)
        {
            return resourceType switch
            {
                TileType.WoodResource => "🪵",
                TileType.StoneResource => "🪨", 
                TileType.BiomassResource => "🌾",
                TileType.MetalResource => "⚡",
                _ => "⛏️"
            };
        }

        void RemoveNearbyPositions(List<Vector2Int> positions, Vector2Int center, int distance)
        {
            positions.RemoveAll(pos =>
                Mathf.Abs(pos.x - center.x) <= distance &&
                Mathf.Abs(pos.y - center.y) <= distance);
        }

        bool IsStandardZone(Vector2Int pos)
        {
            var zone = zoneSystem?.GetZoneAt(pos);
            if (zone.HasValue && zone.Value.zoneType == TileType.StandardZone)
                return true;
            return false;
        }
    }
}