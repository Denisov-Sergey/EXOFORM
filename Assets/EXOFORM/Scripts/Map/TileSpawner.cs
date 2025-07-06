using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Класс для создания и обновления тайлов с поддержкой массивов префабов
    /// </summary>
    public class TileSpawner
    {
        private CityGrid cityGrid;
        private Transform parent;
        private Dictionary<string, int> spawnedPrefabCounts;

        public TileSpawner(CityGrid grid, Transform parentTransform)
        {
            cityGrid = grid;
            parent = parentTransform;
            spawnedPrefabCounts = new Dictionary<string, int>();
        }

        public IEnumerator SpawnAllTiles(GameObject[] grassPrefabs, GameObject[] pathwayPrefabs,
            List<GameObject> prefabsWithSettings, float animationSpeed)
        {
            Debug.Log("  🎯 Создание базовых тайлов...");
            spawnedPrefabCounts.Clear();

            // Создаем базовые тайлы (трава или дорога)
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    CreateTileAt(x, y, grassPrefabs, pathwayPrefabs);

                    if ((x * cityGrid.Height + y) % 10 == 0)
                    {
                        yield return new WaitForSeconds(animationSpeed * 0.1f);
                    }
                }
            }


            Debug.Log("  🏢 Создание зданий поверх базы...");
            CreateBuildingsLayer(prefabsWithSettings);
            LogSpawnedCounts();
        }

        public IEnumerator UpdateChangedTiles(GameObject[] grassPrefabs, GameObject[] pathwayPrefabs,
            List<GameObject> prefabsWithSettings, float animationSpeed)
        {
            // Очищаем старые объекты
            ClearExistingBuildings();
            
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    // Удаляем старые тайлы
                    if (cityGrid.SpawnedTiles[x][y] != null)
                    {
                        if (cityGrid.SpawnedTiles[x][y].name.StartsWith("Base_") ||
                            cityGrid.SpawnedTiles[x][y].name.StartsWith("Pathway_"))
                        {
                            Object.DestroyImmediate(cityGrid.SpawnedTiles[x][y]);
                            cityGrid.SpawnedTiles[x][y] = null;
                        }
                    }

                    // Создаем новый тайл в соответствии с типом клетки
                    CreateTileAt(x, y, grassPrefabs, pathwayPrefabs);
                }

                yield return new WaitForSeconds(animationSpeed * 0.1f);
            }

            Debug.Log("  🏢 Обновление зданий...");
            CreateBuildingsLayer(prefabsWithSettings);
            LogSpawnedCounts();
        }

        void ClearExistingBuildings()
        {
            // Удаляем все объекты кроме базовых тайлов и объединенной травы
            List<GameObject> toDestroy = new List<GameObject>();
            
            foreach (Transform child in parent)
            {
                    
                if (child.name.StartsWith("Building_") || 
                    child.name.StartsWith("Vegetation_") || 
                    child.name.StartsWith("RoadObject_") ||
                    child.name.StartsWith("Loot_") ||
                    child.name.StartsWith("Pathway_"))
                {
                    toDestroy.Add(child.gameObject);
                }
            }
            
            foreach (var obj in toDestroy)
            {
                Object.DestroyImmediate(obj);
            }
            
            spawnedPrefabCounts.Clear();
        }

        void CreateGrassTileAt(int x, int y, GameObject[] grassPrefabs)
        {
            if (grassPrefabs == null || grassPrefabs.Length == 0) return;
            
            Vector3 position = cityGrid.GetWorldPosition(x, y);
            
            // Выбираем случайный префаб травы
            GameObject grassPrefab = GetRandomPrefab(grassPrefabs);
            if (grassPrefab == null) return;

            GameObject grassTile = Object.Instantiate(grassPrefab, position, Quaternion.Euler(0, Random.Range(0, 4) * 90, 0));
            grassTile.name = $"Base_{x}_{y}_Grass";
            grassTile.transform.SetParent(parent);
            cityGrid.SpawnedTiles[x][y] = grassTile;
        }


        void CreateTileAt(int x, int y, GameObject[] grassPrefabs, GameObject[] pathwayPrefabs)
        {
            if (cityGrid.SpawnedTiles[x][y] != null)
                return;

            Vector3 position = cityGrid.GetWorldPosition(x, y);
            TileType baseTileType = cityGrid.Grid[x][y];
            
            GameObject tilePrefab = null;
            if (baseTileType == TileType.PathwayStraight)
            {
                tilePrefab = GetRandomPrefab(pathwayPrefabs);
            }
            else
            {
                tilePrefab = GetRandomPrefab(grassPrefabs);
            }

            if (tilePrefab != null)
            {
                GameObject baseTile = Object.Instantiate(tilePrefab, position, Quaternion.Euler(0, Random.Range(0, 4) * 90, 0));
                baseTile.name = $"Base_{x}_{y}_{baseTileType}";
                baseTile.transform.SetParent(parent);
                cityGrid.SpawnedTiles[x][y] = baseTile;
            }
        }

        GameObject GetRandomPrefab(GameObject[] prefabArray)
        {
            if (prefabArray == null || prefabArray.Length == 0)
                return null;
                
            // Фильтруем null элементы
            List<GameObject> validPrefabs = new List<GameObject>();
            foreach (var prefab in prefabArray)
            {
                if (prefab != null)
                    validPrefabs.Add(prefab);
            }
            
            if (validPrefabs.Count == 0)
                return null;
                
            return validPrefabs[Random.Range(0, validPrefabs.Count)];
        }

        void CreateBuildingsLayer(List<GameObject> prefabsWithSettings)
        {
            Debug.Log("  📊 Начинаем создание объектов на карте...");
            HashSet<Vector2Int> processedBuildings = new HashSet<Vector2Int>();

            // Создаем словарь префабов по типам для быстрого доступа
            Dictionary<TileType, List<GameObject>> prefabsByType = new Dictionary<TileType, List<GameObject>>();
            
            foreach (var prefab in prefabsWithSettings)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null)
                    {
                        if (!prefabsByType.ContainsKey(settings.tileType))
                            prefabsByType[settings.tileType] = new List<GameObject>();
                        prefabsByType[settings.tileType].Add(prefab);
                    }
                }
            }

            foreach (var kvp in cityGrid.BuildingOccupancy)
            {
                TileType buildingType = kvp.Key;
                List<OccupiedCell> buildingCells = kvp.Value;

                if (!prefabsByType.ContainsKey(buildingType))
                {
                    Debug.LogWarning($"  ⚠️ Нет префабов для типа {buildingType}");
                    continue;
                }

                Debug.Log($"  🏗️ Создание объектов типа {buildingType}: {buildingCells.Count} клеток");

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
                    CreateSingleBuilding(buildingType, buildingGroup, prefabsByType[buildingType]);
                }
            }
        }

        List<List<OccupiedCell>> GroupConnectedCells(List<OccupiedCell> allCells)
        {
            List<List<OccupiedCell>> groups = new List<List<OccupiedCell>>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            Dictionary<Vector2Int, OccupiedCell> lookup = new Dictionary<Vector2Int, OccupiedCell>();
            foreach (var c in allCells)
                lookup[c.Cell] = c;

            foreach (var cellData in allCells)
            {
                Vector2Int cell = cellData.Cell;
                if (visited.Contains(cell))
                    continue;

                List<OccupiedCell> group = new List<OccupiedCell>();
                Queue<Vector2Int> toCheck = new Queue<Vector2Int>();
                toCheck.Enqueue(cell);
                visited.Add(cell);

                while (toCheck.Count > 0)
                {
                    Vector2Int current = toCheck.Dequeue();
                    group.Add(lookup[current]);

                    Vector2Int[] directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
                    foreach (var dir in directions)
                    {
                        Vector2Int neighbor = current + dir;
                        if (lookup.ContainsKey(neighbor) && !visited.Contains(neighbor))
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

        void CreateSingleBuilding(TileType buildingType, List<OccupiedCell> buildingCells,
            List<GameObject> availablePrefabs)
        {
            GameObject buildingPrefab = GetBuildingPrefab(buildingType, availablePrefabs);
            if (buildingPrefab == null)
            {
                Debug.LogWarning($"Не найден префаб для здания типа {buildingType}");
                return;
            }

            var settings = buildingPrefab.GetComponent<PrefabSettings>();
            string prefabKey = GetPrefabKey(buildingPrefab);

            Vector2Int minCell = buildingCells[0].Cell;
            Vector2Int maxCell = buildingCells[0].Cell;

            foreach (var c in buildingCells)
            {
                Vector2Int cell = c.Cell;
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

            // Добавляем визуальное смещение для мелких объектов
            if (settings != null && settings.useVisualOffset)
            {
                centerPosition += settings.GetVisualOffset() * cityGrid.TileSize;
            }

            Quaternion rotation = Quaternion.Euler(0, buildingCells[0].Rotation, 0);

            GameObject building = Object.Instantiate(buildingPrefab, centerPosition, rotation);
            
            // Определяем категорию для имени
            string category = GetObjectCategory(buildingType);
            building.name = $"{category}_{buildingType}_{minCell.x}_{minCell.y}_{prefabKey}";
            building.transform.SetParent(parent);

            // Увеличиваем счетчик для конкретного префаба
            if (!spawnedPrefabCounts.ContainsKey(prefabKey))
                spawnedPrefabCounts[prefabKey] = 0;
            spawnedPrefabCounts[prefabKey]++;

            Debug.Log($"    ✅ Создан {settings.objectName} в позиции {centerPosition}, поворот: {rotation.eulerAngles.y}°");
        }

        string GetObjectCategory(TileType type)
        {
            if (IsVegetationType(type)) return "Vegetation";
            if (IsRoadObjectType(type)) return "RoadObject";
            if (type == TileType.SupplyCache) return "SupplyCache";
            return "Structure";
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
            return type == TileType.AbandonedVehicle || type == TileType.Barricade || 
                   type == TileType.WreckageDebris;
        }

        bool IsDecorationType(TileType type)
        {
            return type == TileType.Decoration;
        }


        /// <summary>
        /// Получить случайный префаб с учетом весов и текущих лимитов
        /// </summary>
        GameObject GetBuildingPrefab(TileType buildingType, List<GameObject> availablePrefabs)
        {
            List<GameObject> validPrefabs = new List<GameObject>();
            List<float> weights = new List<float>();

            foreach (var prefab in availablePrefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null && settings.tileType == buildingType)
                    {
                        string prefabKey = GetPrefabKey(prefab);
                        int currentCount = GetCurrentSpawnCount(prefabKey);
                        bool hasLimit = settings.maxCount > 0;
                        bool underLimit = !hasLimit || currentCount < settings.maxCount;

                        if (underLimit)
                        {
                            validPrefabs.Add(prefab);
                            weights.Add(settings.spawnWeight);
                            Debug.Log($"      • {settings.objectName}: {currentCount}/{(hasLimit ? settings.maxCount.ToString() : "∞")} - доступен");
                        }
                        else
                        {
                            Debug.Log($"      ⚠️ {settings.objectName}: {currentCount}/{settings.maxCount} - ЛИМИТ ДОСТИГНУТ");
                        }
                    }
                }
            }

            if (validPrefabs.Count == 0)
            {
                Debug.LogWarning($"    ❌ Все префабы типа {buildingType} достигли лимита!");
                return null;
            }

            // Взвешенный случайный выбор
            return GetWeightedRandomPrefab(validPrefabs, weights);
        }

        GameObject GetWeightedRandomPrefab(List<GameObject> prefabs, List<float> weights)
        {
            float totalWeight = 0f;
            for (int i = 0; i < weights.Count; i++)
                totalWeight += weights[i];

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < prefabs.Count; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return prefabs[i];
                }
            }

            return prefabs[0]; // Fallback
        }

        string GetPrefabKey(GameObject prefab)
        {
            // Используем имя префаба как уникальный ключ
            return prefab.name;
        }

        int GetCurrentSpawnCount(string prefabKey)
        {
            return spawnedPrefabCounts.ContainsKey(prefabKey) ? spawnedPrefabCounts[prefabKey] : 0;
        }

        void LogSpawnedCounts()
        {
            Debug.Log("  📊 === СОЗДАННЫЕ ОБЪЕКТЫ НА КАРТЕ ===");
            foreach (var kvp in spawnedPrefabCounts)
            {
                Debug.Log($"    • {kvp.Key}: {kvp.Value} штук");
            }
            Debug.Log("  =====================================");
        }
    }
}