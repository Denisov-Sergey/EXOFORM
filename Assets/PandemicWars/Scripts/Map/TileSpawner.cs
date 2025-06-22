using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PandemicWars.Scripts.Map
{
    /// <summary>
    /// Класс для создания и обновления тайлов
    /// </summary>
    public class TileSpawner
    {
        private CityGrid cityGrid;
        private Transform parent;

        public TileSpawner(CityGrid grid, Transform parentTransform)
        {
            cityGrid = grid;
            parent = parentTransform;
        }

        public IEnumerator SpawnAllTiles(GameObject grassPrefab, GameObject roadPrefab,
            List<GameObject> prefabsWithSettings, float animationSpeed)
        {
            Debug.Log("  🎯 Создание базовых тайлов...");

            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    CreateTileAt(x, y, grassPrefab, roadPrefab);

                    if ((x * cityGrid.Height + y) % 10 == 0)
                    {
                        yield return new WaitForSeconds(animationSpeed * 0.1f);
                    }
                }
            }

            Debug.Log("  🏢 Создание зданий поверх базы...");
            CreateBuildingsLayer(prefabsWithSettings);
        }

        public IEnumerator UpdateChangedTiles(GameObject grassPrefab, GameObject roadPrefab,
            List<GameObject> prefabsWithSettings, float animationSpeed)
        {
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    if (cityGrid.SpawnedTiles[x][y] != null &&
                        cityGrid.SpawnedTiles[x][y].name.StartsWith("Base_"))
                    {
                        Object.DestroyImmediate(cityGrid.SpawnedTiles[x][y]);
                        cityGrid.SpawnedTiles[x][y] = null;
                    }

                    CreateTileAt(x, y, grassPrefab, roadPrefab);
                }

                yield return new WaitForSeconds(animationSpeed * 0.1f);
            }

            Debug.Log("  🏢 Обновление зданий...");
            CreateBuildingsLayer(prefabsWithSettings);
        }

        void CreateTileAt(int x, int y, GameObject grassPrefab, GameObject roadPrefab)
        {
            if (cityGrid.SpawnedTiles[x][y] != null)
                return;

            Vector3 position = cityGrid.GetWorldPosition(x, y);
            TileType baseTileType = cityGrid.Grid[x][y];
            GameObject basePrefab = baseTileType == TileType.RoadStraight ? roadPrefab : grassPrefab;

            if (basePrefab != null)
            {
                GameObject baseTile = Object.Instantiate(basePrefab, position, Quaternion.identity);
                baseTile.name = $"Base_{x}_{y}_{baseTileType}";
                baseTile.transform.SetParent(parent);
                cityGrid.SpawnedTiles[x][y] = baseTile;
            }
        }

        void CreateBuildingsLayer(List<GameObject> prefabsWithSettings)
        {
            HashSet<Vector2Int> processedBuildings = new HashSet<Vector2Int>();

            foreach (var kvp in cityGrid.BuildingOccupancy)
            {
                TileType buildingType = kvp.Key;
                List<Vector2Int> buildingCells = kvp.Value;

                var buildingGroups = GroupConnectedCells(buildingCells);

                foreach (var buildingGroup in buildingGroups)
                {
                    Vector2Int baseCell = buildingGroup[0];
                    foreach (var cell in buildingGroup)
                    {
                        if (cell.x < baseCell.x || (cell.x == baseCell.x && cell.y < baseCell.y))
                            baseCell = cell;
                    }

                    if (processedBuildings.Contains(baseCell))
                        continue;

                    processedBuildings.Add(baseCell);
                    CreateSingleBuilding(buildingType, buildingGroup, prefabsWithSettings);
                }
            }
        }

        List<List<Vector2Int>> GroupConnectedCells(List<Vector2Int> allCells)
        {
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            foreach (var cell in allCells)
            {
                if (visited.Contains(cell))
                    continue;

                List<Vector2Int> group = new List<Vector2Int>();
                Queue<Vector2Int> toCheck = new Queue<Vector2Int>();
                toCheck.Enqueue(cell);
                visited.Add(cell);

                while (toCheck.Count > 0)
                {
                    Vector2Int current = toCheck.Dequeue();
                    group.Add(current);

                    Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                    foreach (var dir in directions)
                    {
                        Vector2Int neighbor = current + dir;
                        if (allCells.Contains(neighbor) && !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            toCheck.Enqueue(neighbor);
                        }
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        void CreateSingleBuilding(TileType buildingType, List<Vector2Int> buildingCells,
            List<GameObject> prefabsWithSettings)
        {
            GameObject buildingPrefab = GetBuildingPrefab(buildingType, prefabsWithSettings);
            if (buildingPrefab == null)
            {
                Debug.LogWarning($"Не найден префаб для здания типа {buildingType}");
                return;
            }

            var settings = buildingPrefab.GetComponent<PrefabSettings>();

            Vector2Int minCell = buildingCells[0];
            Vector2Int maxCell = buildingCells[0];

            foreach (var cell in buildingCells)
            {
                if (cell.x < minCell.x) minCell.x = cell.x;
                if (cell.y < minCell.y) minCell.y = cell.y;
                if (cell.x > maxCell.x) maxCell.x = cell.x;
                if (cell.y > maxCell.y) maxCell.y = cell.y;
            }

            float centerX = (minCell.x + maxCell.x) * 0.5f;
            float centerY = (minCell.y + maxCell.y) * 0.5f;
            Vector3 centerPosition = new Vector3(
                centerX * cityGrid.TileSize,
                0.1f,
                centerY * cityGrid.TileSize
            );

            Quaternion rotation = Quaternion.identity;
            if (settings != null && settings.rotateTowardsRoad)
            {
                Vector2Int centerCell = new Vector2Int(Mathf.RoundToInt(centerX), Mathf.RoundToInt(centerY));
                rotation = CalculateBuildingRotation(centerCell, buildingCells, settings);
            }

            GameObject building = Object.Instantiate(buildingPrefab, centerPosition, rotation);
            building.name = $"Building_{buildingType}_{minCell.x}_{minCell.y}";
            building.transform.SetParent(parent);

            Debug.Log(
                $"Создано обьект {building.name} - {buildingType} в позиции {centerPosition}, поворот {rotation.eulerAngles.y}°");
        }

        Quaternion CalculateBuildingRotation(Vector2Int centerCell, List<Vector2Int> buildingCells,
            PrefabSettings settings)
        {
            if (!settings.rotateTowardsRoad || settings.allowedRotations.Count == 0)
                return Quaternion.identity;

            Vector2Int? nearestRoad = FindNearestRoad(centerCell, buildingCells);

            if (!nearestRoad.HasValue)
            {
                if (settings.randomRotationIfNoRoad && settings.allowedRotations.Count > 0)
                {
                    float randomAngle = settings.allowedRotations[Random.Range(0, settings.allowedRotations.Count)];
                    return Quaternion.Euler(0, randomAngle, 0);
                }

                return Quaternion.identity;
            }

            Vector2 directionToRoad = new Vector2(
                nearestRoad.Value.x - centerCell.x,
                nearestRoad.Value.y - centerCell.y
            ).normalized;

            float targetAngle = Mathf.Atan2(directionToRoad.x, directionToRoad.y) * Mathf.Rad2Deg;
            float bestAngle = FindClosestAllowedAngle(targetAngle, settings.allowedRotations);

            return Quaternion.Euler(0, bestAngle, 0);
        }

        Vector2Int? FindNearestRoad(Vector2Int centerCell, List<Vector2Int> buildingCells)
        {
            Vector2Int? nearestRoad = null;
            float minDistance = float.MaxValue;

            int searchRadius = 5;
            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    Vector2Int checkCell = centerCell + new Vector2Int(dx, dy);

                    if (buildingCells.Contains(checkCell))
                        continue;

                    if (cityGrid.IsValidPosition(checkCell) &&
                        cityGrid.Grid[checkCell.x][checkCell.y] == TileType.RoadStraight)
                    {
                        float distance = Vector2Int.Distance(centerCell, checkCell);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestRoad = checkCell;
                        }
                    }
                }
            }

            return nearestRoad;
        }

        float FindClosestAllowedAngle(float targetAngle, List<float> allowedAngles)
        {
            float bestAngle = allowedAngles[0];
            float minDifference = Mathf.Abs(Mathf.DeltaAngle(targetAngle, bestAngle));

            foreach (float angle in allowedAngles)
            {
                float difference = Mathf.Abs(Mathf.DeltaAngle(targetAngle, angle));
                if (difference < minDifference)
                {
                    minDifference = difference;
                    bestAngle = angle;
                }
            }

            return bestAngle;
        }

        /// <summary>
        /// Получить случайный префаб с учетом весов и лимитов
        /// </summary>
        GameObject GetBuildingPrefab(TileType buildingType, List<GameObject> prefabsWithSettings)
        {
            List<PrefabSettings> availableSettings = new List<PrefabSettings>();
            List<float> weights = new List<float>();

            foreach (var prefab in prefabsWithSettings)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null && settings.tileType == buildingType)
                    {
                        // ✅ Проверяем, не достигнут ли лимит этого конкретного префаба
                        int currentCount = GetCurrentSpawnCount(settings);
                        bool hasLimit = settings.maxCount > 0;
                        bool underLimit = !hasLimit || currentCount < settings.maxCount;

                        if (underLimit)
                        {
                            availableSettings.Add(settings);
                            weights.Add(settings.spawnWeight);
                        }
                        else
                        {
                            Debug.Log(
                                $"⚠️ Префаб {settings.objectName} достиг лимита: {currentCount}/{settings.maxCount}");
                        }
                    }
                }
            }

            if (availableSettings.Count == 0)
            {
                Debug.LogWarning($"❌ Нет доступных префабов для типа {buildingType} (все достигли лимита)");
                return null;
            }

            // ✅ Взвешенный случайный выбор
            return GetWeightedRandomPrefab(availableSettings, weights);
        }

        /// <summary>
        /// Взвешенный случайный выбор префаба
        /// </summary>
        GameObject GetWeightedRandomPrefab(List<PrefabSettings> settings, List<float> weights)
        {
            float totalWeight = 0f;
            for (int i = 0; i < weights.Count; i++)
                totalWeight += weights[i];

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < settings.Count; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return settings[i].gameObject;
                }
            }

            return settings[0].gameObject; // Fallback
        }

        /// <summary>
        /// Получить текущее количество размещенных объектов этого типа
        /// </summary>
        int GetCurrentSpawnCount(PrefabSettings settings)
        {
            if (cityGrid.BuildingOccupancy.ContainsKey(settings.tileType))
            {
                // Подсчитываем только объекты этого конкретного префаба
                int count = 0;

                // Ищем объекты по имени (так как имя содержит тип здания)
                Transform[] children = new Transform[parent.childCount];
                for (int i = 0; i < parent.childCount; i++)
                {
                    children[i] = parent.GetChild(i);
                }

                foreach (Transform child in children)
                {
                    if (child != null && child.name.Contains($"Building_{settings.tileType}_"))
                    {
                        count++;
                    }
                }

                return count;
            }

            return 0;
        }
    }
}