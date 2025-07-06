using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Специальный класс для размещения растительности
    /// </summary>
    public class VegetationPlacer
    {
        private CityGrid cityGrid;
        private ExoformZoneSystem zoneSystem;
        private List<PrefabSettings> vegetationPrefabs;
        private MonoBehaviour coroutineRunner;

        public VegetationPlacer(CityGrid grid, ExoformZoneSystem zones, List<GameObject> prefabs, MonoBehaviour runner)
        {
            cityGrid = grid;
            zoneSystem = zones;
            coroutineRunner = runner;
            LoadVegetationPrefabs(prefabs);
        }

        void LoadVegetationPrefabs(List<GameObject> prefabs)
        {
            vegetationPrefabs = new List<PrefabSettings>();

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null && IsVegetationType(settings.tileType))
                    {
                        vegetationPrefabs.Add(settings);
                        Debug.Log($"  🌱 Добавлен префаб растительности: {settings.objectName} (тип: {settings.tileType})");
                    }
                }
            }

            Debug.Log($"🌳 Загружено {vegetationPrefabs.Count} префабов растительности");
            
            if (vegetationPrefabs.Count == 0)
            {
                Debug.LogWarning("⚠️ Не найдено префабов растительности! Проверьте TileType у префабов.");
                LogAvailableTileTypes(prefabs);
            }
        }

        void LogAvailableTileTypes(List<GameObject> prefabs)
        {
            Debug.Log("📋 Доступные типы тайлов в префабах:");
            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null)
                    {
                        Debug.Log($"  • {prefab.name}: {settings.tileType}");
                    }
                }
            }
        }

        bool IsVegetationType(TileType type)
        {
            return type == TileType.Spore || type == TileType.SporeCluster || 
                   type == TileType.CorruptedVegetation || 
                   type == TileType.Forest || 
                   type == TileType.AlienGrowth;
        }

        public IEnumerator PlaceVegetation(float vegetationDensity, float animationSpeed)
        {
            if (vegetationPrefabs.Count == 0)
            {
                Debug.Log("  🌱 Нет префабов растительности для размещения");
                yield break;
            }

            Debug.Log("🌳 === РАЗМЕЩЕНИЕ РАСТИТЕЛЬНОСТИ ===");

            // Размещаем растительность в несколько этапов
            yield return coroutineRunner.StartCoroutine(PlaceForests(vegetationDensity, animationSpeed));
            yield return coroutineRunner.StartCoroutine(PlaceGardens(vegetationDensity, animationSpeed));
            yield return coroutineRunner.StartCoroutine(PlaceRandomVegetation(vegetationDensity, animationSpeed));
            
            LogVegetationStatistics();
        }

        IEnumerator PlaceForests(float density, float animationSpeed)
        {
            var forestSettings = vegetationPrefabs.Find(v => v.tileType == TileType.Forest);
            if (forestSettings == null) 
            {
                Debug.Log("  🌲 Нет префабов лесов для размещения");
                yield break;
            }

            Debug.Log("  🌲 Размещение лесных массивов...");

            List<Vector2Int> forestAreas = FindForestAreas();
            int forestsToPlace = Mathf.RoundToInt(forestAreas.Count * density * 0.1f); // 10% от density

            Debug.Log($"    📍 Найдено областей для лесов: {forestAreas.Count}");
            Debug.Log($"    🎯 Планируем разместить: {forestsToPlace} лесов");

            for (int i = 0; i < forestsToPlace && forestAreas.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, forestAreas.Count);
                Vector2Int position = forestAreas[randomIndex];

                if (TryPlaceForest(position, forestSettings))
                {
                    RemoveNearbyAreas(forestAreas, position, 8); // Леса далеко друг от друга
                    Debug.Log($"      ✅ Лес размещен в {position}");
                    yield return new WaitForSeconds(animationSpeed);
                }
                else
                {
                    forestAreas.RemoveAt(randomIndex);
                }
            }
        }

        IEnumerator PlaceGardens(float density, float animationSpeed)
        {
            var gardenSettings = vegetationPrefabs.Find(v => v.tileType == TileType.AlienGrowth);
            if (gardenSettings == null) 
            {
                Debug.Log("  🌺 Нет префабов садов для размещения");
                yield break;
            }

            Debug.Log("  🌺 Размещение садов...");

            List<Vector2Int> gardenAreas = FindGardenAreas();
            int gardensToPlace = Mathf.RoundToInt(gardenAreas.Count * density * 0.3f); // 30% от density

            Debug.Log($"    📍 Найдено областей для садов: {gardenAreas.Count}");
            Debug.Log($"    🎯 Планируем разместить: {gardensToPlace} садов");

            for (int i = 0; i < gardensToPlace && gardenAreas.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, gardenAreas.Count);
                Vector2Int position = gardenAreas[randomIndex];

                if (TryPlaceGarden(position, gardenSettings))
                {
                    RemoveNearbyAreas(gardenAreas, position, 4);
                    Debug.Log($"      ✅ Сад размещен в {position}");
                    yield return new WaitForSeconds(animationSpeed * 0.5f);
                }
                else
                {
                    gardenAreas.RemoveAt(randomIndex);
                }
            }
        }

        IEnumerator PlaceRandomVegetation(float density, float animationSpeed)
        {
            Debug.Log("  🌿 Размещение случайной растительности...");

            // Получаем мелкую растительность (деревья, кусты, цветы)
            var smallVegetation = vegetationPrefabs.FindAll(v => 
                v.tileType == TileType.Spore || 
                v.tileType == TileType.CorruptedVegetation ||
                v.tileType == TileType.SporeCluster);

            if (smallVegetation.Count == 0)
            {
                Debug.LogWarning("    ⚠️ Нет префабов мелкой растительности");
                yield break;
            }

            foreach (var vegetation in smallVegetation)
            {
                yield return coroutineRunner.StartCoroutine(PlaceVegetationType(vegetation, density, animationSpeed));
            }
        }

        IEnumerator PlaceVegetationType(PrefabSettings settings, float density, float animationSpeed)
        {
            Debug.Log($"  🌱 === РАЗМЕЩЕНИЕ {settings.objectName.ToUpper()} ===");
            
            List<Vector2Int> validPositions = FindValidVegetationPositions(settings);
            
            if (validPositions.Count == 0)
            {
                Debug.LogError($"    ❌ Нет валидных позиций для {settings.objectName}!");
                LogVegetationDiagnostics(settings);
                yield break;
            }
            
            // Корректируем плотность в зависимости от типа
            float adjustedDensity = GetAdjustedDensity(settings.tileType, density);
            int objectsToPlace = Mathf.RoundToInt(validPositions.Count * adjustedDensity * settings.spawnWeight);

            if (settings.maxCount > 0)
                objectsToPlace = Mathf.Min(objectsToPlace, settings.maxCount);

            Debug.Log($"    🎯 Размещаем {objectsToPlace} из {validPositions.Count} позиций (плотность: {adjustedDensity:P1})");

            int placedCount = 0;
            int failedAttempts = 0;
            
            for (int i = 0; i < objectsToPlace && validPositions.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, validPositions.Count);
                Vector2Int position = validPositions[randomIndex];

                if (TryPlaceVegetation(position, settings))
                {
                    placedCount++;
                    
                    // Определяем расстояние удаления в зависимости от типа
                    int removeDistance = GetRemoveDistance(settings.tileType, settings.minDistanceFromSameType);
                    RemoveNearbyAreas(validPositions, position, removeDistance);
                    
                    if (placedCount % 5 == 0)
                    {
                        Debug.Log($"      📈 Прогресс: {placedCount}/{objectsToPlace}");
                    }
                    
                    yield return new WaitForSeconds(animationSpeed * 0.2f);
                }
                else
                {
                    validPositions.RemoveAt(randomIndex);
                    failedAttempts++;
                }
            }

            Debug.Log($"    ✅ {settings.objectName}: размещено {placedCount}/{objectsToPlace} (неудач: {failedAttempts})");
        }

        float GetAdjustedDensity(TileType vegetationType, float baseDensity)
        {
            return vegetationType switch
            {
                TileType.Spore => baseDensity * 0.4f,        // 40% от базовой плотности
                TileType.CorruptedVegetation => baseDensity * 0.8f,      // 80% от базовой плотности
                TileType.SporeCluster => baseDensity * 0.2f, // 20% от базовой плотности
                _ => baseDensity * 0.5f
            };
        }

        // ВСПОМОГАТЕЛЬНЫЙ метод для определения расстояния удаления
        int GetRemoveDistance(TileType vegetationType, int defaultDistance)
        {
            return vegetationType switch
            {
                TileType.Spore => Mathf.Max(1, defaultDistance), // Деревья - минимальное расстояние
                TileType.SporeCluster => Mathf.Max(2, defaultDistance), // Кластеры - больше расстояние
                TileType.Forest => Mathf.Max(4, defaultDistance), // Леса - максимальное расстояние
                TileType.CorruptedVegetation => 1, // Кусты - минимальное расстояние
                TileType.AlienGrowth => 2, // Сады - среднее расстояние
                _ => defaultDistance
            };
        }

        List<Vector2Int> FindForestAreas()
        {
            List<Vector2Int> areas = new List<Vector2Int>();

            // Ищем большие свободные области для лесов (подальше от дорог и зданий)
            for (int x = 5; x < cityGrid.Width - 5; x += 3)
            {
                for (int y = 5; y < cityGrid.Height - 5; y += 3)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (IsAreaClearForForest(pos, 4))
                    {
                        areas.Add(pos);
                    }
                }
            }

            return areas;
        }

        List<Vector2Int> FindGardenAreas()
        {
            List<Vector2Int> areas = new List<Vector2Int>();

            // Ищем места рядом с зданиями для садов
            for (int x = 1; x < cityGrid.Width - 1; x++)
            {
                for (int y = 1; y < cityGrid.Height - 1; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (IsGoodForGarden(pos))
                    {
                        areas.Add(pos);
                    }
                }
            }

            return areas;
        }

        // ИСПРАВЛЕНИЕ: Улучшенный поиск позиций для растительности
        List<Vector2Int> FindValidVegetationPositions(PrefabSettings settings)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            int checkedPositions = 0;
            int validPositions = 0;

            Debug.Log($"  🔍 Поиск позиций для {settings.objectName} (тип: {settings.tileType})");

            for (int x = 0; x <= cityGrid.Width - settings.gridSize.x; x++)
            {
                for (int y = 0; y <= cityGrid.Height - settings.gridSize.y; y++)
                {
                    checkedPositions++;
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    if (CanPlaceVegetationAt(pos, settings))
                    {
                        // Дополнительная проверка зон
                        bool zoneAllowed = true;
                        if (zoneSystem != null && settings.allowedZones.Count > 0)
                        {
                            var zone = zoneSystem.GetZoneAt(pos);
                            if (zone.HasValue)
                            {
                                if (!settings.allowedZones.Contains(zone.Value.zoneType))
                                {
                                    zoneAllowed = false;
                                }
                            }
                            else
                            {
                                // Если зона не найдена, разрешаем размещение (fallback)
                                Debug.Log($"      ⚠️ Зона не найдена для позиции {pos}, разрешаем размещение");
                            }
                        }
                        
                        if (zoneAllowed)
                        {
                            positions.Add(pos);
                            validPositions++;
                        }
                    }
                }
            }

            Debug.Log($"    📊 {settings.objectName}: проверено {checkedPositions}, найдено валидных {validPositions}");
            
            if (validPositions == 0)
            {
                Debug.LogWarning($"    ❌ Не найдено валидных позиций для {settings.objectName}!");
            }

            return positions;
        }

        // ИСПРАВЛЕНИЕ: Более гибкие условия размещения растительности
        bool CanPlaceVegetationAt(Vector2Int pos, PrefabSettings settings)
        {
            var occupiedCells = settings.GetOccupiedCells(pos);

            // Базовая проверка всех клеток
            foreach (var cell in occupiedCells)
            {
                if (!cityGrid.IsValidPosition(cell))
                {
                    return false;
                }
                
                if (cityGrid.Grid[cell.x][cell.y] != TileType.Grass)
                {
                    return false;
                }
                
                if (cityGrid.IsCellOccupiedByBuilding(cell))
                {
                    return false;
                }
            }

            // ИСПРАВЛЕННЫЕ специальные правила для разных типов растительности
            switch (settings.tileType)
            {
                case TileType.Spore: // Обычные деревья
                    // Деревья могут быть везде, но предпочтительно не вплотную к дорогам
                    bool tooCloseToRoad = HasRoadNearby(pos, 0); // Только НА дороге запрещено
                    return !tooCloseToRoad;
                    
                case TileType.SporeCluster: // Группы деревьев
                    // Кластеры деревьев нужно размещать группами, подальше от дорог
                    return !HasRoadNearby(pos, 1);
                    
                case TileType.Forest: // Лесные массивы
                    // Леса должны быть в больших свободных областях
                    if (HasRoadNearby(pos, 2) || HasBuildingNearby(pos, 3))
                    {
                        return false;
                    }
                    return IsAreaClearForForest(pos, 2); // Проверяем свободную область 2x2
                    
                case TileType.CorruptedVegetation: // Кусты и цветы
                    // Могут расти где угодно, даже рядом с дорогами
                    return true;
                    
                case TileType.AlienGrowth: // Сады
                    // Предпочитают быть рядом со зданиями, но не на дорогах
                    return HasBuildingNearby(pos, 4) && !HasRoadNearby(pos, 0);
                    
                default:
                    Debug.LogWarning($"      ⚠️ Неизвестный тип растительности: {settings.tileType}");
                    return true;
            }
        }

        // ИСПРАВЛЕНИЕ: Более точная проверка лесной области
        bool IsAreaClearForForest(Vector2Int center, int radius)
        {
            int clearCells = 0;
            int totalCells = 0;
            
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int checkPos = center + new Vector2Int(dx, dy);
                    totalCells++;
                    
                    if (!cityGrid.IsValidPosition(checkPos))
                    {
                        continue; // Клетки вне карты не считаем
                    }
                    
                    if (cityGrid.Grid[checkPos.x][checkPos.y] == TileType.Grass && 
                        !cityGrid.IsCellOccupiedByBuilding(checkPos))
                    {
                        clearCells++;
                    }
                }
            }
            
            float clearPercentage = (float)clearCells / totalCells;
            bool isGood = clearPercentage >= 0.7f; // Минимум 70% области должно быть свободно
            
            return isGood;
        }

        bool IsGoodForGarden(Vector2Int pos)
        {
            // Должна быть трава
            if (cityGrid.Grid[pos.x][pos.y] != TileType.Grass) return false;
            if (cityGrid.IsCellOccupiedByBuilding(pos)) return false;

            // Должно быть здание рядом (в радиусе 2 клеток)
            return HasBuildingNearby(pos, 2) && !HasRoadNearby(pos, 1);
        }

        bool HasBuildingNearby(Vector2Int pos, int distance)
        {
            for (int dx = -distance; dx <= distance; dx++)
            {
                for (int dy = -distance; dy <= distance; dy++)
                {
                    Vector2Int checkPos = pos + new Vector2Int(dx, dy);
                    if (cityGrid.IsValidPosition(checkPos) && 
                        cityGrid.IsCellOccupiedByBuilding(checkPos))
                    {
                        return true;
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

        bool TryPlaceForest(Vector2Int position, PrefabSettings settings)
        {
            // Размещаем лес как группу деревьев
            Vector2Int forestSize = new Vector2Int(
                Random.Range(3, 6), 
                Random.Range(3, 6)
            );

            List<Vector2Int> forestCells = new List<Vector2Int>();
            for (int x = 0; x < forestSize.x; x++)
            {
                for (int y = 0; y < forestSize.y; y++)
                {
                    Vector2Int cell = position + new Vector2Int(x, y);
                    if (cityGrid.IsValidPosition(cell) && 
                        cityGrid.Grid[cell.x][cell.y] == TileType.Grass &&
                        !cityGrid.IsCellOccupiedByBuilding(cell))
                    {
                        forestCells.Add(cell);
                    }
                }
            }

            if (forestCells.Count >= forestSize.x * forestSize.y * 0.7f) // Минимум 70% площади
            {
                RegisterVegetationArea(TileType.Forest, forestCells);
                return true;
            }

            return false;
        }

        bool TryPlaceGarden(Vector2Int position, PrefabSettings settings)
        {
            var gardenCells = settings.GetOccupiedCells(position);
            
            foreach (var cell in gardenCells)
            {
                if (!CanPlaceVegetationAt(cell, settings)) return false;
            }

            RegisterVegetationArea(TileType.AlienGrowth, gardenCells);
            return true;
        }

        bool TryPlaceVegetation(Vector2Int position, PrefabSettings settings)
        {
            if (!CanPlaceVegetationAt(position, settings)) return false;

            var occupiedCells = settings.GetOccupiedCells(position);
            RegisterVegetationArea(settings.tileType, occupiedCells);
            return true;
        }

        void RegisterVegetationArea(TileType vegetationType, List<Vector2Int> cells)
        {
            if (!cityGrid.BuildingOccupancy.ContainsKey(vegetationType))
                cityGrid.BuildingOccupancy[vegetationType] = new List<Vector2Int>();

            foreach (var cell in cells)
            {
                cityGrid.BuildingOccupancy[vegetationType].Add(cell);
            }
        }

        // ИСПРАВЛЕНИЕ: Менее агрессивное удаление позиций
        void RemoveNearbyAreas(List<Vector2Int> positions, Vector2Int center, int distance)
        {
            int initialCount = positions.Count;
            
            positions.RemoveAll(pos =>
            {
                int dist = Mathf.Max(Mathf.Abs(pos.x - center.x), Mathf.Abs(pos.y - center.y));
                return dist <= distance;
            });
            
            int removedCount = initialCount - positions.Count;
            Debug.Log($"    🧹 Удалено позиций в радиусе {distance}: {removedCount} (осталось: {positions.Count})");
        }

        // ИСПРАВЛЕНИЕ: Диагностика проблем с растительностью
        void LogVegetationDiagnostics(PrefabSettings settings)
        {
            Debug.Log($"  🔍 === ДИАГНОСТИКА ДЛЯ {settings.objectName} ===");
            
            // Подсчитываем различные типы клеток на карте
            int grassCells = 0;
            int occupiedCells = 0;
            int roadCells = 0;
            
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    if (cityGrid.Grid[x][y] == TileType.Grass)
                        grassCells++;
                    else if (cityGrid.Grid[x][y] == TileType.PathwayStraight)
                        roadCells++;
                        
                    if (cityGrid.IsCellOccupiedByBuilding(new Vector2Int(x, y)))
                        occupiedCells++;
                }
            }
            
            int totalCells = cityGrid.Width * cityGrid.Height;
            Debug.Log($"    📊 Статистика карты:");
            Debug.Log($"      • Всего клеток: {totalCells}");
            Debug.Log($"      • Трава: {grassCells} ({(float)grassCells/totalCells:P1})");
            Debug.Log($"      • Дороги: {roadCells} ({(float)roadCells/totalCells:P1})");
            Debug.Log($"      • Занято зданиями: {occupiedCells} ({(float)occupiedCells/totalCells:P1})");
            
            // Проверяем настройки префаба
            Debug.Log($"    ⚙️ Настройки префаба:");
            Debug.Log($"      • Тип: {settings.tileType}");
            Debug.Log($"      • Размер: {settings.gridSize.x}x{settings.gridSize.y}");
            Debug.Log($"      • Вес спауна: {settings.spawnWeight}");
            Debug.Log($"      • Макс. количество: {(settings.maxCount > 0 ? settings.maxCount.ToString() : "без лимита")}");
            Debug.Log($"      • Разрешенные зоны: {string.Join(", ", settings.allowedZones)}");
        }

        void LogVegetationStatistics()
        {
            Debug.Log("  📊 === СТАТИСТИКА РАСТИТЕЛЬНОСТИ ===");
            
            foreach (var kvp in cityGrid.BuildingOccupancy)
            {
                if (IsVegetationType(kvp.Key))
                {
                    string emoji = GetVegetationEmoji(kvp.Key);
                    Debug.Log($"  {emoji} {kvp.Key}: {kvp.Value.Count} объектов");
                }
            }
        }

        string GetVegetationEmoji(TileType type)
        {
            return type switch
            {
                TileType.Spore => "🌲",
                TileType.SporeCluster => "🌳",
                TileType.CorruptedVegetation => "🌸",
                TileType.Forest => "🌲🌲",
                TileType.AlienGrowth => "🌺",
                _ => "🌱"
            };
        }
    }
}