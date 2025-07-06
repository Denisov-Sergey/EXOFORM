using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Класс для размещения объектов на дорогах (машины, лут, препятствия)
    /// </summary>
    public class RoadObjectsPlacer
    {
        private CityGrid cityGrid;
        private ExoformZoneSystem zoneSystem;
        private List<PrefabSettings> roadObjectPrefabs;
        private MonoBehaviour coroutineRunner;
        private Dictionary<PrefabSettings, int> spawnedCounts;

        public RoadObjectsPlacer(CityGrid grid, ExoformZoneSystem zones, List<GameObject> prefabs, MonoBehaviour runner)
        {
            cityGrid = grid;
            zoneSystem = zones;
            coroutineRunner = runner;
            spawnedCounts = new Dictionary<PrefabSettings, int>();
            LoadRoadObjectPrefabs(prefabs);
        }

        void LoadRoadObjectPrefabs(List<GameObject> prefabs)
        {
            roadObjectPrefabs = new List<PrefabSettings>();

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null && IsRoadObjectType(settings.tileType))
                    {
                        roadObjectPrefabs.Add(settings);
                    }
                }
            }

            Debug.Log($"Загружено {roadObjectPrefabs.Count} префабов декоративных объектов для дорог (без лута)");
        }

        bool IsRoadObjectType(TileType type)
        {
            // ВАЖНО: Лут теперь обрабатывается отдельно через LootPlacer
            // Здесь только декоративные объекты
            return type == TileType.AbandonedVehicle || 
                   type == TileType.Barricade ||
                   type == TileType.WreckageDebris;
            // type == TileType.SupplyCache исключен!
        }

        public IEnumerator PlaceRoadObjects(float density, float animationSpeed)
        {
            if (roadObjectPrefabs.Count == 0)
            {
                Debug.Log("  🚗 Нет префабов для размещения на дорогах");
                yield break;
            }

            Debug.Log("🚗 Этап: Размещение декоративных объектов на дорогах (машины, баррикады, мусор)");

            // Находим все дорожные клетки
            List<Vector2Int> roadCells = FindAllRoadCells();
            Debug.Log($"  📍 Найдено {roadCells.Count} дорожных клеток");

            foreach (var settings in roadObjectPrefabs)
            {
                yield return coroutineRunner.StartCoroutine(
                    PlaceRoadObjectType(settings, roadCells, density, animationSpeed)
                );
            }
        }

        List<Vector2Int> FindAllRoadCells()
        {
            List<Vector2Int> roadCells = new List<Vector2Int>();

            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    if (cityGrid.Grid[x][y] == TileType.PathwayStraight)
                    {
                        roadCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            return roadCells;
        }

        IEnumerator PlaceRoadObjectType(PrefabSettings settings, List<Vector2Int> roadCells, 
            float baseDensity, float animationSpeed)
        {
            // Фильтруем подходящие позиции
            List<Vector2Int> validPositions = new List<Vector2Int>();
            
            foreach (var roadCell in roadCells)
            {
                if (CanPlaceRoadObject(roadCell, settings))
                {
                    validPositions.Add(roadCell);
                }
            }

            if (validPositions.Count == 0)
            {
                Debug.LogWarning($"  ❌ Нет подходящих мест для {settings.objectName} на дорогах");
                yield break;
            }

            // Рассчитываем количество для размещения
            float adjustedDensity = baseDensity * settings.spawnWeight;
            int objectsToPlace = Mathf.RoundToInt(validPositions.Count * adjustedDensity);

            if (settings.maxCount > 0)
            {
                objectsToPlace = Mathf.Min(objectsToPlace, settings.maxCount);
            }

            Debug.Log($"  🚗 {settings.objectName}: размещаем {objectsToPlace} из {validPositions.Count} позиций");

            // Размещаем объекты
            for (int i = 0; i < objectsToPlace && validPositions.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, validPositions.Count);
                Vector2Int position = validPositions[randomIndex];

                if (TryPlaceRoadObject(position, settings))
                {
                    // Удаляем позиции вокруг размещенного объекта
                    RemoveNearbyPositions(validPositions, position, settings.minDistanceFromSameType);
                    yield return new WaitForSeconds(animationSpeed * 0.3f);
                }
                else
                {
                    validPositions.RemoveAt(randomIndex);
                }
            }
        }

        bool CanPlaceRoadObject(Vector2Int position, PrefabSettings settings)
        {
            // Проверяем, что это действительно дорога
            if (cityGrid.Grid[position.x][position.y] != TileType.PathwayStraight)
                return false;

            if (zoneSystem != null)
            {
                var zone = zoneSystem.GetZoneAt(position);
                if (zone.HasValue && settings.allowedZones.Count > 0 &&
                    !settings.allowedZones.Contains(zone.Value.zoneType))
                    return false;
            }

            // Проверяем, не занята ли позиция другим объектом
            if (cityGrid.IsCellOccupiedByBuilding(position))
                return false;

            // Специальные правила для разных типов
            switch (settings.tileType)
            {
                case TileType.AbandonedVehicle:
                    // Машины не должны быть слишком близко к перекресткам
                    return !HasIntersectionNearby(position, 2);
                    
                case TileType.Barricade:
                    // Блокпосты лучше на прямых участках
                    return IsLongStraightRoad(position, 3);
                    
                case TileType.WreckageDebris:
                    // Обломки могут быть везде
                    return true;
                    
                default:
                    return true;
            }
        }

        bool TryPlaceRoadObject(Vector2Int position, PrefabSettings settings)
        {
            // Проверяем лимиты
            int currentCount = GetSpawnedCount(settings);
            if (settings.maxCount > 0 && currentCount >= settings.maxCount)
            {
                return false;
            }

            // Регистрируем объект
            if (!cityGrid.BuildingOccupancy.ContainsKey(settings.tileType))
                cityGrid.BuildingOccupancy[settings.tileType] = new List<Vector2Int>();
            
            cityGrid.BuildingOccupancy[settings.tileType].Add(position);

            // Увеличиваем счетчик
            if (!spawnedCounts.ContainsKey(settings))
                spawnedCounts[settings] = 0;
            spawnedCounts[settings]++;

            Debug.Log($"    ✅ Размещен {settings.objectName} на дороге в {position}");
            return true;
        }

        int GetSpawnedCount(PrefabSettings settings)
        {
            return spawnedCounts.ContainsKey(settings) ? spawnedCounts[settings] : 0;
        }

        bool HasIntersectionNearby(Vector2Int position, int distance)
        {
            // Проверяем, есть ли перекресток (дороги в 3+ направлениях)
            int roadDirections = 0;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

            foreach (var dir in directions)
            {
                Vector2Int checkPos = position + dir;
                if (cityGrid.IsValidPosition(checkPos) && 
                    cityGrid.Grid[checkPos.x][checkPos.y] == TileType.PathwayStraight)
                {
                    roadDirections++;
                }
            }

            return roadDirections >= 3;
        }

        bool IsLongStraightRoad(Vector2Int position, int minLength)
        {
            // Проверяем, является ли это частью длинной прямой дороги
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.right };
            
            foreach (var dir in directions)
            {
                int length = 1;
                
                // Проверяем в положительном направлении
                Vector2Int checkPos = position + dir;
                while (cityGrid.IsValidPosition(checkPos) && 
                       cityGrid.Grid[checkPos.x][checkPos.y] == TileType.PathwayStraight)
                {
                    length++;
                    checkPos += dir;
                }
                
                // Проверяем в отрицательном направлении
                checkPos = position - dir;
                while (cityGrid.IsValidPosition(checkPos) && 
                       cityGrid.Grid[checkPos.x][checkPos.y] == TileType.PathwayStraight)
                {
                    length++;
                    checkPos -= dir;
                }
                
                if (length >= minLength)
                    return true;
            }
            
            return false;
        }

        void RemoveNearbyPositions(List<Vector2Int> positions, Vector2Int center, int distance)
        {
            positions.RemoveAll(pos => 
                Mathf.Abs(pos.x - center.x) <= distance && 
                Mathf.Abs(pos.y - center.y) <= distance);
        }
    }
}