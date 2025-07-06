using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Класс для размещения декоративных объектов в любых свободных местах
    /// </summary>
    public class DecorationPlacer
    {
        private CityGrid cityGrid;
        private ExoformZoneSystem zoneSystem;
        private List<PrefabSettings> decorationPrefabs;
        private MonoBehaviour coroutineRunner;
        private Dictionary<PrefabSettings, int> spawnedCounts;

        public DecorationPlacer(CityGrid grid, ExoformZoneSystem zones, List<GameObject> prefabs, MonoBehaviour runner)
        {
            cityGrid = grid;
            zoneSystem = zones;
            coroutineRunner = runner;
            spawnedCounts = new Dictionary<PrefabSettings, int>();
            LoadDecorationPrefabs(prefabs);
        }

        void LoadDecorationPrefabs(List<GameObject> prefabs)
        {
            decorationPrefabs = new List<PrefabSettings>();

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null && IsDecorationType(settings.tileType))
                    {
                        decorationPrefabs.Add(settings);
                    }
                }
            }

            Debug.Log($"🎨 Загружено {decorationPrefabs.Count} префабов декораций");
        }

        bool IsDecorationType(TileType type)
        {
            // Добавьте новый тип в enum: Decoration
            return type == TileType.Decoration;
        }

        public IEnumerator PlaceDecorations(float density, float animationSpeed)
        {
            if (decorationPrefabs.Count == 0)
            {
                Debug.Log("  ⚠️ Нет префабов декораций для размещения");
                yield break;
            }

            Debug.Log("🎨 === РАЗМЕЩЕНИЕ ДЕКОРАЦИЙ ===");

            // Находим все свободные позиции
            List<Vector2Int> freePositions = FindFreePositions();
            
            if (freePositions.Count == 0)
            {
                Debug.LogError("  ❌ Нет свободных мест для декораций!");
                yield break;
            }

            Debug.Log($"  📍 Найдено свободных позиций: {freePositions.Count}");

            // Перемешиваем позиции для случайности
            ShuffleList(freePositions);

            foreach (var settings in decorationPrefabs)
            {
                yield return coroutineRunner.StartCoroutine(
                    PlaceDecorationType(settings, freePositions, density, animationSpeed)
                );
            }

            LogStatistics();
        }

        IEnumerator PlaceDecorationType(PrefabSettings settings, List<Vector2Int> availablePositions, 
            float density, float animationSpeed)
        {
            // Фильтруем подходящие позиции для этого типа декорации
            List<Vector2Int> validPositions = new List<Vector2Int>();
            
            foreach (var pos in availablePositions)
            {
                if (CanPlaceDecoration(pos, settings))
                {
                    validPositions.Add(pos);
                }
            }

            if (validPositions.Count == 0)
            {
                Debug.LogWarning($"  ❌ Нет подходящих мест для {settings.objectName}");
                yield break;
            }

            // Рассчитываем количество для размещения
            float adjustedDensity = density * settings.spawnWeight;
            int objectsToPlace = Mathf.RoundToInt(validPositions.Count * adjustedDensity);

            if (settings.maxCount > 0)
            {
                objectsToPlace = Mathf.Min(objectsToPlace, settings.maxCount);
            }

            Debug.Log($"  🎨 {settings.objectName}: размещаем {objectsToPlace} из {validPositions.Count} позиций");

            int placedCount = 0;
            for (int i = 0; i < objectsToPlace && validPositions.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, validPositions.Count);
                Vector2Int position = validPositions[randomIndex];

                if (TryPlaceDecoration(position, settings))
                {
                    placedCount++;
                    // Удаляем использованную позицию и близлежащие
                    RemoveNearbyPositions(validPositions, position, settings.minDistanceFromSameType);
                    RemoveNearbyPositions(availablePositions, position, 1);
                    
                    if (placedCount % 10 == 0)
                    {
                        yield return new WaitForSeconds(animationSpeed * 0.1f);
                    }
                }
                else
                {
                    validPositions.RemoveAt(randomIndex);
                }
            }

            Debug.Log($"    ✅ Размещено {placedCount} объектов {settings.objectName}");
        }

        List<Vector2Int> FindFreePositions()
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    // Свободная позиция - трава без зданий
                    if (cityGrid.Grid[x][y] == TileType.Grass && 
                        !cityGrid.IsCellOccupiedByBuilding(pos))
                    {
                        positions.Add(pos);
                    }
                }
            }

            return positions;
        }

        bool CanPlaceDecoration(Vector2Int position, PrefabSettings settings)
        {
            // Базовая проверка
            if (!cityGrid.IsValidPosition(position))
                return false;

            if (zoneSystem != null)
            {
                var zone = zoneSystem.GetZoneAt(position);
                if (zone.HasValue && settings.allowedZones.Count > 0 &&
                    !settings.allowedZones.Contains(zone.Value.zoneType))
                    return false;
            }

            // Проверка занятости
            if (cityGrid.IsCellOccupiedByBuilding(position))
                return false;

            // Проверка типа поверхности
            if (cityGrid.Grid[position.x][position.y] != TileType.Grass)
                return false;

            // Специальные правила для разных декораций
            if (settings.minDistanceFromRoad > 0)
            {
                if (HasRoadNearby(position, settings.minDistanceFromRoad - 1))
                    return false;
            }

            return true;
        }

        bool TryPlaceDecoration(Vector2Int position, PrefabSettings settings)
        {
            // Проверяем лимиты
            int currentCount = GetSpawnedCount(settings);
            if (settings.maxCount > 0 && currentCount >= settings.maxCount)
            {
                return false;
            }

            // Регистрируем декорацию
            if (!cityGrid.BuildingOccupancy.ContainsKey(TileType.Decoration))
                cityGrid.BuildingOccupancy[TileType.Decoration] = new List<Vector2Int>();
            
            cityGrid.BuildingOccupancy[TileType.Decoration].Add(position);

            // Увеличиваем счетчик
            if (!spawnedCounts.ContainsKey(settings))
                spawnedCounts[settings] = 0;
            spawnedCounts[settings]++;

            return true;
        }

        int GetSpawnedCount(PrefabSettings settings)
        {
            return spawnedCounts.ContainsKey(settings) ? spawnedCounts[settings] : 0;
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

        void RemoveNearbyPositions(List<Vector2Int> positions, Vector2Int center, int distance)
        {
            positions.RemoveAll(pos => 
                Mathf.Abs(pos.x - center.x) <= distance && 
                Mathf.Abs(pos.y - center.y) <= distance);
        }

        void ShuffleList<T>(List<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        void LogStatistics()
        {
            Debug.Log("  📊 === СТАТИСТИКА ДЕКОРАЦИЙ ===");
            int total = 0;
            foreach (var kvp in spawnedCounts)
            {
                var settings = kvp.Key;
                int count = kvp.Value;
                total += count;
                string limitText = settings.maxCount > 0 ? $"/{settings.maxCount}" : "/∞";
                Debug.Log($"    • {settings.objectName}: {count}{limitText}");
            }
            Debug.Log($"  🎨 Всего декораций: {total}");
            Debug.Log("  ================================");
        }
    }
}