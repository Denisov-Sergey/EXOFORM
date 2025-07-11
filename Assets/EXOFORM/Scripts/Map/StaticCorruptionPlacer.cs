using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Класс для размещения статичных элементов Порчи на карте
    /// </summary>
    public class StaticCorruptionPlacer
    {
        private CityGrid cityGrid;
        private List<PrefabSettings> corruptionPrefabs;
        private MonoBehaviour coroutineRunner;
        private Dictionary<PrefabSettings, int> spawnedCounts;
        private ExoformZoneSystem zoneSystem;
        private bool trapDeactivated;
        public bool TrapDeactivated { get => trapDeactivated; set => trapDeactivated = value; }

        public StaticCorruptionPlacer(CityGrid grid, List<GameObject> prefabs, MonoBehaviour runner, ExoformZoneSystem zoneSystem)
        {
            cityGrid = grid;
            coroutineRunner = runner;
            spawnedCounts = new Dictionary<PrefabSettings, int>();
            this.zoneSystem = zoneSystem;
            trapDeactivated = false;
            LoadCorruptionPrefabs(prefabs);
        }

        void LoadCorruptionPrefabs(List<GameObject> prefabs)
        {
            corruptionPrefabs = new List<PrefabSettings>();

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null && IsCorruptionType(settings.tileType))
                    {
                        corruptionPrefabs.Add(settings);
                    }
                }
            }

            Debug.Log($"🦠 Загружено {corruptionPrefabs.Count} префабов элементов Порчи");
        }

        bool IsCorruptionType(TileType type)
        {
            return type == TileType.TentacleGrowth ||
                   type == TileType.TumorNode ||
                   type == TileType.CorruptedGround ||
                   type == TileType.SporeEmitter ||
                   type == TileType.BiologicalMass;
        }

        public IEnumerator PlaceStaticCorruption(float density, float animationSpeed)
        {
            if (corruptionPrefabs.Count == 0)
            {
                Debug.Log("  ⚠️ Нет префабов элементов Порчи для размещения");
                yield break;
            }

            Debug.Log("🦠 === РАЗМЕЩЕНИЕ СТАТИЧНОЙ ПОРЧИ ===");

            // Размещаем в несколько этапов для создания кластеров
            yield return PlaceCorruptionClusters(density, animationSpeed);
            yield return PlaceRandomCorruption(density * 0.3f, animationSpeed);
            
            LogCorruptionStatistics();
        }

        IEnumerator PlaceCorruptionClusters(float density, float animationSpeed)
        {
            Debug.Log("  🌑 Создание кластеров заражения...");
            
            // Находим подходящие места для центров кластеров
            List<Vector2Int> clusterCenters = FindClusterCenters();
            
            int clustersToCreate = Mathf.RoundToInt(clusterCenters.Count * density * 0.5f);
            clustersToCreate = Mathf.Clamp(clustersToCreate, 2, 8);
            
            Debug.Log($"  📍 Создаем {clustersToCreate} кластеров заражения");

            for (int i = 0; i < clustersToCreate && clusterCenters.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, clusterCenters.Count);
                Vector2Int center = clusterCenters[randomIndex];
                
                yield return PlaceCorruptionCluster(center, animationSpeed);
                
                // Удаляем близлежащие центры
                RemoveNearbyPositions(clusterCenters, center, 8);
                
                yield return new WaitForSeconds(animationSpeed * 2f);
            }
        }

        IEnumerator PlaceCorruptionCluster(Vector2Int center, float animationSpeed)
        {
            int clusterSize = UnityEngine.Random.Range(3, 8);
            float clusterRadius = UnityEngine.Random.Range(2f, 4f);
            
            Debug.Log($"    🦠 Создание кластера в {center}, размер: {clusterSize}, радиус: {clusterRadius}");
            
            List<Vector2Int> clusterPositions = GetClusterPositions(center, clusterRadius, clusterSize);
            
            foreach (var pos in clusterPositions)
            {
                // Выбираем случайный тип Порчи для этой позиции
                var corruptionType = ChooseCorruptionType(Vector2.Distance(pos, center), clusterRadius);
                
                if (TryPlaceCorruption(pos, corruptionType))
                {
                    yield return new WaitForSeconds(animationSpeed * 0.2f);
                }
            }
            
            Debug.Log($"    ✅ Кластер создан: {clusterPositions.Count} элементов");
        }

        IEnumerator PlaceRandomCorruption(float density, float animationSpeed)
        {
            Debug.Log("  🎲 Размещение случайной Порчи...");
            
            List<Vector2Int> validPositions = FindValidCorruptionPositions();
            int randomCorruptionCount = Mathf.RoundToInt(validPositions.Count * density);
            
            Debug.Log($"    🎯 Размещаем {randomCorruptionCount} случайных элементов из {validPositions.Count} позиций");
            
            for (int i = 0; i < randomCorruptionCount && validPositions.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, validPositions.Count);
                Vector2Int position = validPositions[randomIndex];
                
                var randomType = corruptionPrefabs[UnityEngine.Random.Range(0, corruptionPrefabs.Count)].tileType;
                
                if (TryPlaceCorruption(position, randomType))
                {
                    RemoveNearbyPositions(validPositions, position, 2);
                    yield return new WaitForSeconds(animationSpeed * 0.1f);
                }
                else
                {
                    validPositions.RemoveAt(randomIndex);
                }
            }
        }

        List<Vector2Int> FindClusterCenters()
        {
            List<Vector2Int> centers = new List<Vector2Int>();
            
            // Ищем места подальше от дорог и зданий
            for (int x = 5; x < cityGrid.Width - 5; x += 4)
            {
                for (int y = 5; y < cityGrid.Height - 5; y += 4)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    if (IsGoodClusterCenter(pos) && IsAllowedZone(pos))
                    {
                        centers.Add(pos);
                    }
                }
            }
            
            return centers;
        }

        bool IsGoodClusterCenter(Vector2Int pos)
        {
            // Проверяем область 5x5 вокруг позиции
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    Vector2Int checkPos = pos + new Vector2Int(dx, dy);
                    
                    if (!cityGrid.IsValidPosition(checkPos)) return false;
                    if (cityGrid.Grid[checkPos.x][checkPos.y] != TileType.Grass) return false;
                    if (cityGrid.IsCellOccupiedByBuilding(checkPos)) return false;
                }
            }
            
            // Не должно быть дорог поблизости
            return !HasRoadNearby(pos, 3);
        }

        List<Vector2Int> GetClusterPositions(Vector2Int center, float radius, int count)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            List<Vector2Int> candidates = new List<Vector2Int>();
            
            // Собираем все возможные позиции в радиусе
            int intRadius = Mathf.CeilToInt(radius);
            for (int dx = -intRadius; dx <= intRadius; dx++)
            {
                for (int dy = -intRadius; dy <= intRadius; dy++)
                {
                    Vector2Int pos = center + new Vector2Int(dx, dy);
                    float distance = Vector2.Distance(pos, center);
                    
                    if (distance <= radius && CanPlaceCorruptionAt(pos) && IsAllowedZone(pos))
                    {
                        candidates.Add(pos);
                    }
                }
            }
            
            // Выбираем случайные позиции
            while (positions.Count < count && candidates.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, candidates.Count);
                positions.Add(candidates[randomIndex]);
                candidates.RemoveAt(randomIndex);
            }
            
            return positions;
        }

        TileType ChooseCorruptionType(float distanceFromCenter, float maxRadius)
        {
            float normalizedDistance = distanceFromCenter / maxRadius;
            
            // В центре кластера - более опасные элементы
            if (normalizedDistance < 0.3f)
            {
                return Random.value < 0.6f ? TileType.TumorNode : TileType.BiologicalMass;
            }
            else if (normalizedDistance < 0.7f)
            {
                return Random.value < 0.5f ? TileType.TentacleGrowth : TileType.SporeEmitter;
            }
            else
            {
                return TileType.CorruptedGround;
            }
        }

        List<Vector2Int> FindValidCorruptionPositions()
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    
                    if (CanPlaceCorruptionAt(pos) && IsAllowedZone(pos))
                    {
                        positions.Add(pos);
                    }
                }
            }
            
            return positions;
        }

        bool CanPlaceCorruptionAt(Vector2Int pos)
        {
            if (!cityGrid.IsValidPosition(pos)) return false;
            if (cityGrid.Grid[pos.x][pos.y] != TileType.Grass) return false;
            if (cityGrid.IsCellOccupiedByBuilding(pos)) return false;

            // Порча не размещается слишком близко к дорогам
            if (HasRoadNearby(pos, 1)) return false;

            if (!IsAllowedZone(pos)) return false;

            return true;
        }

        bool TryPlaceCorruption(Vector2Int position, TileType corruptionType)
        {
            if (!IsAllowedZone(position))
            {
                Debug.Log($"[StaticCorruptionPlacer] Зона несовместима в {position}");
                return false;
            }

            if (!CanPlaceCorruptionAt(position)) return false;
            
            // Проверяем лимиты
            var prefab = corruptionPrefabs.FirstOrDefault(p => p.tileType == corruptionType);
            if (prefab != null)
            {
                int currentCount = GetSpawnedCount(prefab);
                if (prefab.maxCount > 0 && currentCount >= prefab.maxCount)
                {
                    return false;
                }
            }
            
            // Регистрируем элемент Порчи
            if (!cityGrid.BuildingOccupancy.ContainsKey(corruptionType))
                cityGrid.BuildingOccupancy[corruptionType] = new List<OccupiedCell>();

            cityGrid.BuildingOccupancy[corruptionType].Add(new OccupiedCell(position, 0));
            
            // Увеличиваем счетчик
            if (prefab != null)
            {
                if (!spawnedCounts.ContainsKey(prefab))
                    spawnedCounts[prefab] = 0;
                spawnedCounts[prefab]++;
            }
            
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

        void LogCorruptionStatistics()
        {
            Debug.Log("  📊 === СТАТИСТИКА ПОРЧИ ===");
            
            foreach (var kvp in spawnedCounts)
            {
                var settings = kvp.Key;
                int count = kvp.Value;
                string emoji = GetCorruptionEmoji(settings.tileType);
                Debug.Log($"  {emoji} {settings.objectName}: {count}");
            }
            
            int totalCorruption = spawnedCounts.Values.Sum();
            Debug.Log($"  🦠 Всего элементов Порчи: {totalCorruption}");
        }

        string GetCorruptionEmoji(TileType type)
        {
            return type switch
            {
                TileType.TentacleGrowth => "🐙",
                TileType.TumorNode => "🧬",
                TileType.CorruptedGround => "🌫️",
                TileType.SporeEmitter => "💨",
                TileType.BiologicalMass => "🦠",
                _ => "☣️"
            };
        }

        bool IsAllowedZone(Vector2Int pos)
        {
            var zone = zoneSystem?.GetZoneAt(pos);
            if (!zone.HasValue) return false;

            if (zone.Value.zoneType == TileType.CorruptedTrap && !trapDeactivated)
                return true;
            if (zone.Value.zoneType == TileType.InfestationZone)
                return true;
            return false;
        }

        public IEnumerator SpawnInfestationRoutine(float interval)
        {
            while (true)
            {
                var zones = zoneSystem.GetZonesByType(TileType.InfestationZone);
                foreach (var z in zones)
                {
                    Vector2Int randPos = new Vector2Int(
                        UnityEngine.Random.Range(z.position.x, z.position.x + z.size.x),
                        UnityEngine.Random.Range(z.position.y, z.position.y + z.size.y));

                    var type = corruptionPrefabs[UnityEngine.Random.Range(0, corruptionPrefabs.Count)].tileType;
                    TryPlaceCorruption(randPos, type);
                }

                yield return new WaitForSeconds(interval);
            }
        }
    }
}