using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null)
                    {
                        prefabSettings.Add(settings);
                    }
                    else
                    {
                        Debug.LogWarning($"Префаб {prefab.name} не имеет компонента PrefabSettings!");
                    }
                }
            }

            Debug.Log($"Загружено {prefabSettings.Count} префабов с настройками");
        }

        public IEnumerator PlaceObjects(float baseDensity, float animationSpeed)
        {
            if (prefabSettings.Count == 0)
            {
                Debug.Log("  ⚠️ Нет префабов с настройками для размещения");
                yield break;
            }

            spawnedCounts.Clear();

            foreach (var settings in prefabSettings)
            {
                Debug.Log($"  🏗️ Размещение: {settings.objectName} (размер {settings.gridSize})");
                
                yield return coroutineRunner.StartCoroutine(PlaceObjectTypeCoroutine(settings, baseDensity, animationSpeed));
                yield return new WaitForSeconds(animationSpeed * 0.5f);
            }
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

            if (settings.maxCount > 0)
            {
                objectsToPlace = Mathf.Min(objectsToPlace, settings.maxCount);
            }

            Debug.Log($"    📍 Найдено {validPositions.Count} позиций, размещаем {objectsToPlace}");

            for (int i = 0; i < objectsToPlace && validPositions.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, validPositions.Count);
                Vector2Int position = validPositions[randomIndex];

                if (TryPlaceObject(settings, position))
                {
                    RemoveOccupiedPositions(validPositions, settings, position);
                    yield return new WaitForSeconds(animationSpeed * 0.8f);
                }
                else
                {
                    validPositions.RemoveAt(randomIndex);
                }
            }
        }

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
                Debug.Log($"    ❌ Не удается разместить {settings.objectName} в {position} - место занято");
                return false;
            }

            int currentCount = GetSpawnedCount(settings);
            if (settings.maxCount > 0 && currentCount >= settings.maxCount)
            {
                Debug.Log($"    ❌ Достигнут лимит для {settings.objectName}: {currentCount}/{settings.maxCount}");
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

            Debug.Log($"    ✅ Размещен {settings.objectName} в {position}, размер {settings.gridSize}, занимает клетки: {string.Join(", ", occupiedCells)}");
            return true;
        }
        
        // Получение счетчика по настройкам
        int GetSpawnedCount(PrefabSettings settings)
        {
            return spawnedCounts.ContainsKey(settings) ? spawnedCounts[settings] : 0;
        }
        
        // Удаляем старый метод GetSpawnedCount(TileType tileType)
        // так как он больше не нужен
        
        public void LogSpawnedCounts()
        {
            Debug.Log("📊 === СТАТИСТИКА РАЗМЕЩЕННЫХ ОБЪЕКТОВ ===");
            foreach (var kvp in spawnedCounts)
            {
                var settings = kvp.Key;
                int count = kvp.Value;
                string limitText = settings.maxCount > 0 ? $"/{settings.maxCount}" : "/∞";
                Debug.Log($"🏗️ {settings.objectName}: {count}{limitText}");
            }
            Debug.Log("===========================================");
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
            
            Debug.Log($"    🚫 После размещения {settings.objectName} в {placedPosition} (отступ {minDistance}) осталось {positions.Count} валидных позиций");
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