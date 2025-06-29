using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace PandemicWars.Scripts.Map
{
    /// <summary>
    /// Класс для размещения объектов с настройками
    /// </summary>
    public class ObjectPlacer
    {
        private CityGrid cityGrid;
        private List<PrefabSettings> prefabSettings;
        private Dictionary<PrefabSettings, int> spawnedCounts;
        private MonoBehaviour coroutineRunner;

        public ObjectPlacer(CityGrid grid, List<GameObject> prefabs, MonoBehaviour runner)
        {
            cityGrid = grid;
            coroutineRunner = runner;
            spawnedCounts = new Dictionary<PrefabSettings, int>();
            LoadPrefabSettings(prefabs);
        }

        void LoadPrefabSettings(List<GameObject> prefabs)
        {
            prefabSettings = new List<PrefabSettings>();
            int totalPrefabs = 0;
            int vegetationCount = 0;
            int roadObjectCount = 0;
            int lootCount = 0;

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    totalPrefabs++;
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null)
                    {
                        if (IsVegetationType(settings.tileType))
                        {
                            vegetationCount++;
                        }
                        else if (IsRoadObjectType(settings.tileType))
                        {
                            roadObjectCount++;
                        }
                        else if (IsLootType(settings.tileType))
                        {
                            lootCount++;
                        }
                        else
                        {
                            // Это здание
                            prefabSettings.Add(settings);
                            Debug.Log($"  ✅ Добавлен префаб здания: {settings.objectName} ({settings.tileType}), размер: {settings.gridSize.x}x{settings.gridSize.y}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"  ⚠️ Префаб {prefab.name} не имеет компонента PrefabSettings!");
                    }
                }
            }

            Debug.Log($"📊 Загружено префабов:");
            Debug.Log($"  • Всего: {totalPrefabs}");
            Debug.Log($"  • Зданий: {prefabSettings.Count}");
            Debug.Log($"  • Растительности: {vegetationCount}");
            Debug.Log($"  • Дорожных объектов: {roadObjectCount}");
            Debug.Log($"  • Лута: {lootCount}");
        }

        bool IsVegetationType(TileType type)
        {
            return type == TileType.Spore || type == TileType.SporeCluster || 
                   type == TileType.CorruptedVegetation || 
                   type == TileType.Forest || 
                   type == TileType.AlienGrowth;
        }

        bool IsRoadObjectType(TileType type)
        {
            return type == TileType.BrokenCar || type == TileType.Roadblock || 
                   type == TileType.Debris;
        }

        // Добавим метод для проверки типа лута
        bool IsLootType(TileType type)
        {
            return type == TileType.SupplyCache;
        }

        public IEnumerator PlaceObjects(float baseDensity, float animationSpeed)
        {
            if (prefabSettings.Count == 0)
            {
                Debug.Log("  ⚠️ Нет префабов с настройками для размещения");
                yield break;
            }

            spawnedCounts.Clear();

            // Предварительный расчет ожидаемых количеств
            LogExpectedPlacements(baseDensity);

            // Группируем префабы по типу для лучшего понимания
            var prefabsByType = prefabSettings.GroupBy(s => s.tileType);
            
            foreach (var typeGroup in prefabsByType)
            {
                Debug.Log($"\n  🏗️ === Размещение типа {typeGroup.Key} ===");
                Debug.Log($"  📊 Префабов этого типа: {typeGroup.Count()}");
                
                foreach (var settings in typeGroup)
                {
                    Debug.Log($"\n  🎯 Размещение: {settings.objectName} (размер {settings.gridSize.x}x{settings.gridSize.y})");
                    
                    yield return coroutineRunner.StartCoroutine(PlaceObjectTypeCoroutine(settings, baseDensity, animationSpeed));
                    yield return new WaitForSeconds(animationSpeed * 0.5f);
                }
            }

            // Финальная статистика
            LogFinalStatistics(baseDensity);
        }

        void LogExpectedPlacements(float baseDensity)
        {
            Debug.Log("\n📊 === ОЖИДАЕМОЕ РАЗМЕЩЕНИЕ ОБЪЕКТОВ ===");
            
            int totalMapCells = cityGrid.Width * cityGrid.Height;
            int estimatedFreeCells = Mathf.RoundToInt(totalMapCells * 0.7f); // ~70% карты доступно
            
            Debug.Log($"📏 Размер карты: {cityGrid.Width}x{cityGrid.Height} = {totalMapCells} клеток");
            Debug.Log($"🟩 Примерно свободных клеток: ~{estimatedFreeCells}");
            
            var prefabsByType = prefabSettings.GroupBy(s => s.tileType);
            
            foreach (var typeGroup in prefabsByType)
            {
                Debug.Log($"\n🏢 Тип: {typeGroup.Key}");
                
                foreach (var settings in typeGroup)
                {
                    float density = baseDensity * settings.spawnWeight;
                    int expectedCount = Mathf.RoundToInt(estimatedFreeCells * density / settings.Area);
                    
                    string limitInfo = settings.maxCount > 0 ? 
                        $" (лимит: {settings.maxCount})" : 
                        " (без лимита)";
                    
                    int finalExpected = settings.maxCount > 0 ? 
                        Mathf.Min(expectedCount, settings.maxCount) : 
                        expectedCount;
                    
                    Debug.Log($"  • {settings.objectName}:");
                    Debug.Log($"    - Вес: {settings.spawnWeight:F2}, Плотность: {density:F3}");
                    Debug.Log($"    - Размер: {settings.gridSize.x}x{settings.gridSize.y} ({settings.Area} клеток)");
                    Debug.Log($"    - Ожидается: ~{expectedCount} → {finalExpected}{limitInfo}");
                }
            }
            
            Debug.Log("========================================\n");
        }

        IEnumerator PlaceObjectTypeCoroutine(PrefabSettings settings, float baseDensity, float animationSpeed)
        {
            List<Vector2Int> validPositions = FindValidPositions(settings);
            
            if (validPositions.Count == 0)
            {
                Debug.LogWarning($"  ❌ Нет подходящих мест для {settings.objectName}");
                yield break;
            }

            float density = baseDensity * settings.spawnWeight;
            int objectsToPlace = Mathf.RoundToInt(validPositions.Count * density);
            int originalTarget = objectsToPlace;

            if (settings.maxCount > 0)
            {
                objectsToPlace = Mathf.Min(objectsToPlace, settings.maxCount);
            }

            Debug.Log($"    📍 Найдено позиций: {validPositions.Count}");
            Debug.Log($"    🎯 Целевое количество: {originalTarget} → {objectsToPlace} (с учетом лимита)");

            int placedCount = 0;
            int failedAttempts = 0;

            for (int i = 0; i < objectsToPlace && validPositions.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, validPositions.Count);
                Vector2Int position = validPositions[randomIndex];

                if (TryPlaceObject(settings, position))
                {
                    placedCount++;
                    RemoveOccupiedPositions(validPositions, settings, position);
                    
                    // Логирование прогресса каждые 5 объектов
                    if (placedCount % 5 == 0 || placedCount == objectsToPlace)
                    {
                        Debug.Log($"    📈 Прогресс: {placedCount}/{objectsToPlace}");
                    }
                    
                    yield return new WaitForSeconds(animationSpeed * 0.8f);
                }
                else
                {
                    validPositions.RemoveAt(randomIndex);
                    failedAttempts++;
                }
            }

            // Итоговая статистика для этого префаба
            Debug.Log($"    ✅ Размещено: {placedCount}/{objectsToPlace} ({(float)placedCount/objectsToPlace*100:F1}%)");
            
            if (failedAttempts > 0)
            {
                Debug.Log($"    ⚠️ Неудачных попыток: {failedAttempts}");
            }
            
            if (placedCount < objectsToPlace)
            {
                Debug.Log($"    ⚠️ Не удалось разместить {objectsToPlace - placedCount} объектов (кончились позиции)");
            }
            
            if (settings.maxCount > 0 && placedCount >= settings.maxCount)
            {
                Debug.Log($"    🚫 Достигнут лимит: {settings.maxCount}");
            }
        }

        void LogFinalStatistics(float baseDensity)
        {
            Debug.Log("\n📊 === ФИНАЛЬНАЯ СТАТИСТИКА РАЗМЕЩЕНИЯ ===");
            
            var prefabsByType = spawnedCounts.Keys.GroupBy(s => s.tileType);
            
            foreach (var typeGroup in prefabsByType)
            {
                Debug.Log($"\n🏢 {typeGroup.Key}:");
                
                int totalForType = 0;
                foreach (var settings in typeGroup)
                {
                    if (spawnedCounts.ContainsKey(settings))
                    {
                        int count = spawnedCounts[settings];
                        totalForType += count;
                        string limitText = settings.maxCount > 0 ? $"/{settings.maxCount}" : "/∞";
                        string percentage = settings.maxCount > 0 ? 
                            $" ({(float)count/settings.maxCount*100:F1}% от лимита)" : "";
                        
                        Debug.Log($"  • {settings.objectName}: {count}{limitText}{percentage}");
                    }
                }
                Debug.Log($"  📊 Всего типа {typeGroup.Key}: {totalForType}");
            }
            
            int totalPlaced = spawnedCounts.Values.Sum();
            Debug.Log($"\n🏗️ ВСЕГО РАЗМЕЩЕНО ОБЪЕКТОВ: {totalPlaced}");
            Debug.Log("==========================================\n");
        }

        // Остальные методы остаются без изменений...
        List<Vector2Int> FindValidPositions(PrefabSettings settings)
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            for (int x = 0; x <= cityGrid.Width - settings.gridSize.x; x++)
            {
                for (int y = 0; y <= cityGrid.Height - settings.gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (settings.CanPlaceAtWithBuildingCheck(pos, cityGrid.Grid, cityGrid.Width, cityGrid.Height, 
                        cityGrid.IsCellOccupiedByBuilding))
                    {
                        if (settings.minDistanceFromRoad == 0 || HasRoadNearby(pos, settings.minDistanceFromRoad))
                        {
                            positions.Add(pos);
                        }
                    }
                }
            }

            return positions;
        }

        bool TryPlaceObject(PrefabSettings settings, Vector2Int position)
        {
            if (!settings.CanPlaceAtWithBuildingCheck(position, cityGrid.Grid, cityGrid.Width, cityGrid.Height, 
                cityGrid.IsCellOccupiedByBuilding))
            {
                return false;
            }

            int currentCount = GetSpawnedCount(settings);
            if (settings.maxCount > 0 && currentCount >= settings.maxCount)
            {
                return false;
            }

            var occupiedCells = settings.GetOccupiedCells(position);
            
            if (!cityGrid.BuildingOccupancy.ContainsKey(settings.tileType))
                cityGrid.BuildingOccupancy[settings.tileType] = new List<Vector2Int>();
            
            foreach (var cell in occupiedCells)
            {
                cityGrid.BuildingOccupancy[settings.tileType].Add(cell);
            }

            if (!spawnedCounts.ContainsKey(settings))
                spawnedCounts[settings] = 0;
            spawnedCounts[settings]++;

            return true;
        }
        
        int GetSpawnedCount(PrefabSettings settings)
        {
            return spawnedCounts.ContainsKey(settings) ? spawnedCounts[settings] : 0;
        }

        void RemoveOccupiedPositions(List<Vector2Int> positions, PrefabSettings settings, Vector2Int placedPosition)
        {
            var occupiedCells = settings.GetOccupiedCells(placedPosition);
            int minDistance = settings.minDistanceFromSameType;
            
            positions.RemoveAll(pos =>
            {
                var testCells = settings.GetOccupiedCells(pos);
                
                foreach (var testCell in testCells)
                {
                    if (occupiedCells.Contains(testCell))
                    {
                        return true;
                    }
                }
                
                foreach (var testCell in testCells)
                {
                    foreach (var occupiedCell in occupiedCells)
                    {
                        int distance = Mathf.Max(
                            Mathf.Abs(testCell.x - occupiedCell.x),
                            Mathf.Abs(testCell.y - occupiedCell.y)
                        );
                        
                        if (distance < minDistance)
                        {
                            return true;
                        }
                    }
                }
                
                return false;
            });
        }

        bool HasRoadNearby(Vector2Int position, int distance)
        {
            for (int dx = -distance; dx <= distance; dx++)
            {
                for (int dy = -distance; dy <= distance; dy++)
                {
                    Vector2Int checkPos = position + new Vector2Int(dx, dy);
                    if (cityGrid.IsValidPosition(checkPos) && 
                        cityGrid.Grid[checkPos.x][checkPos.y] == TileType.RoadStraight)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}