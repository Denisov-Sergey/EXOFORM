using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using EXOFORM.Scripts.Ecs.Authoring.Spawning;
using EXOFORM.Scripts.Ecs.Components.Spawning;
using Exoform.Scripts.Map;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Интеграция системы спауна с генератором карты
    /// Автоматически создает точки спауна на основе сгенерированной карты
    /// </summary>
    public class SpawnSystemIntegration : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [Tooltip("Префаб точки спауна игроков")]
        public GameObject playerSpawnPointPrefab;
        
        [Tooltip("Префаб точки спауна врагов")]
        public GameObject enemySpawnPointPrefab;
        
        [Tooltip("Префаб точки спауна боссов")]
        public GameObject bossSpawnPointPrefab;

        [Header("Generation Settings")]
        [Range(1, 10)]
        [Tooltip("Количество точек спауна игроков")]
        public int playerSpawnCount = 4;
        
        [Range(5, 20)]
        [Tooltip("Количество точек спауна врагов")]
        public int enemySpawnCount = 10;
        
        [Range(1, 5)]
        [Tooltip("Количество точек спауна боссов")]
        public int bossSpawnCount = 2;

        [Header("Placement Rules")]
        [Tooltip("Минимальное расстояние между точками спауна")]
        public float minSpawnDistance = 10f;
        
        [Tooltip("Минимальное расстояние от точек спауна до дорог")]
        public float minRoadDistance = 3f;

        [Header("Zone Preferences")]
        [Tooltip("Зоны для спауна игроков")]
        public TileType[] playerPreferredZones = { TileType.StandardZone, TileType.TechnicalZone };
        
        [Tooltip("Зоны для спауна врагов")]
        public TileType[] enemyPreferredZones = { TileType.CorruptedTrap, TileType.InfestationZone };
        
        [Tooltip("Зоны для спауна боссов")]
        public TileType[] bossPreferredZones = { TileType.BossZone, TileType.ArtifactZone };

        // Ссылки
        private ExoformMapGenerator mapGenerator;
        private ExoformZoneSystem zoneSystem;
        private CityGrid cityGrid;
        
        // Созданные точки спауна
        private List<GameObject> createdSpawnPoints = new List<GameObject>();

        void Start()
        {
            // Найдем генератор карты
            mapGenerator = GetComponent<ExoformMapGenerator>();
            if (mapGenerator == null)
            {
                mapGenerator = FindObjectOfType<ExoformMapGenerator>();
            }

            if (mapGenerator == null)
            {
                Debug.LogError("❌ SpawnSystemIntegration: ExoformMapGenerator не найден!");
                return;
            }

            // Подписываемся на завершение генерации карты
            StartCoroutine(WaitForMapGenerationComplete());
        }

        IEnumerator WaitForMapGenerationComplete()
        {
            // Ждем пока карта сгенерируется
            while (mapGenerator.IsGenerating || mapGenerator.CurrentStage != ExoformMapGenerator.GenerationStage.Completed)
            {
                yield return new WaitForSeconds(1f);
            }

            Debug.Log("🗺️ Карта сгенерирована, начинаем размещение точек спауна...");

            // Получаем данные карты
            cityGrid = mapGenerator.CityGrid;
            zoneSystem = mapGenerator.ZoneSystem;

            if (cityGrid == null || zoneSystem == null)
            {
                Debug.LogError("❌ Не удалось получить данные карты для размещения спауна!");
                yield break;
            }

            // Размещаем точки спауна
            yield return StartCoroutine(PlaceAllSpawnPoints());

            Debug.Log("✅ Система спауна успешно интегрирована с картой!");
        }

        IEnumerator PlaceAllSpawnPoints()
        {
            Debug.Log("🎯 === РАЗМЕЩЕНИЕ ТОЧЕК СПАУНА ===");

            // Очистим старые точки
            ClearExistingSpawnPoints();

            // Размещаем точки спауна игроков
            yield return StartCoroutine(PlacePlayerSpawnPoints());
            
            // Размещаем точки спауна врагов
            yield return StartCoroutine(PlaceEnemySpawnPoints());
            
            // Размещаем точки спауна боссов
            yield return StartCoroutine(PlaceBossSpawnPoints());

            LogSpawnStatistics();
        }

        IEnumerator PlacePlayerSpawnPoints()
        {
            Debug.Log("👤 Размещение точек спауна игроков...");

            var playerPositions = FindSpawnPositions(
                playerSpawnCount, 
                playerPreferredZones, 
                SpawnPointType.PlayerSpawn,
                requireRoadAccess: true
            );

            foreach (var position in playerPositions)
            {
                CreateSpawnPoint(position, SpawnPointType.PlayerSpawn, playerSpawnPointPrefab);
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log($"  ✅ Создано {playerPositions.Count} точек спауна игроков");
        }

        IEnumerator PlaceEnemySpawnPoints()
        {
            Debug.Log("👹 Размещение точек спауна врагов...");

            var enemyPositions = FindSpawnPositions(
                enemySpawnCount, 
                enemyPreferredZones, 
                SpawnPointType.EnemySpawn,
                requireRoadAccess: false
            );

            foreach (var position in enemyPositions)
            {
                CreateSpawnPoint(position, SpawnPointType.EnemySpawn, enemySpawnPointPrefab);
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log($"  ✅ Создано {enemyPositions.Count} точек спауна врагов");
        }

        IEnumerator PlaceBossSpawnPoints()
        {
            Debug.Log("🐲 Размещение точек спауна боссов...");

            var bossPositions = FindSpawnPositions(
                bossSpawnCount, 
                bossPreferredZones, 
                SpawnPointType.BossSpawn,
                requireRoadAccess: false,
                minDistanceMultiplier: 2f
            );

            foreach (var position in bossPositions)
            {
                CreateSpawnPoint(position, SpawnPointType.BossSpawn, bossSpawnPointPrefab);
                yield return new WaitForSeconds(0.1f);
            }

            Debug.Log($"  ✅ Создано {bossPositions.Count} точек спауна боссов");
        }

        List<Vector3> FindSpawnPositions(int count, TileType[] preferredZones, SpawnPointType spawnType, 
                                        bool requireRoadAccess = false, float minDistanceMultiplier = 1f)
        {
            var positions = new List<Vector3>();
            var candidates = new List<Vector3>();

            // Собираем все возможные позиции в предпочитаемых зонах
            foreach (var zoneType in preferredZones)
            {
                var zones = zoneSystem.GetZonesByType(zoneType);
                foreach (var zone in zones)
                {
                    candidates.AddRange(GetCandidatePositionsInZone(zone, requireRoadAccess));
                }
            }

            // Если не хватает кандидатов, добавляем из других зон
            if (candidates.Count < count * 2) // Нужен запас
            {
                Debug.LogWarning($"Мало кандидатов для {spawnType}, ищем в дополнительных зонах...");
                candidates.AddRange(GetFallbackPositions(spawnType, requireRoadAccess));
            }

            // Перемешиваем кандидатов
            ShuffleList(candidates);

            // Выбираем позиции с учетом минимального расстояния
            float effectiveMinDistance = minSpawnDistance * minDistanceMultiplier;
            
            for (int i = 0; i < candidates.Count && positions.Count < count; i++)
            {
                var candidate = candidates[i];
                bool validPosition = true;

                // Проверяем расстояние до уже выбранных позиций
                foreach (var existingPos in positions)
                {
                    if (Vector3.Distance(candidate, existingPos) < effectiveMinDistance)
                    {
                        validPosition = false;
                        break;
                    }
                }

                // Проверяем расстояние до других точек спауна
                foreach (var spawnPoint in createdSpawnPoints)
                {
                    if (spawnPoint != null && Vector3.Distance(candidate, spawnPoint.transform.position) < effectiveMinDistance)
                    {
                        validPosition = false;
                        break;
                    }
                }

                if (validPosition)
                {
                    positions.Add(candidate);
                }
            }

            return positions;
        }

        List<Vector3> GetCandidatePositionsInZone(ExoformZoneSystem.ZoneData zone, bool requireRoadAccess)
        {
            var candidates = new List<Vector3>();
            
            // Проверяем каждую клетку в зоне
            for (int x = zone.position.x; x < zone.position.x + zone.size.x; x += 2) // Каждая вторая клетка
            {
                for (int y = zone.position.y; y < zone.position.y + zone.size.y; y += 2)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    
                    if (IsValidSpawnPosition(gridPos, requireRoadAccess))
                    {
                        Vector3 worldPos = new Vector3(
                            x * mapGenerator.tileSize,
                            0.5f, // Чуть приподнимаем над землей
                            y * mapGenerator.tileSize
                        );
                        candidates.Add(worldPos);
                    }
                }
            }

            return candidates;
        }

        List<Vector3> GetFallbackPositions(SpawnPointType spawnType, bool requireRoadAccess)
        {
            var fallbackPositions = new List<Vector3>();

            // Ищем по всей карте
            for (int x = 2; x < cityGrid.Width - 2; x += 3)
            {
                for (int y = 2; y < cityGrid.Height - 2; y += 3)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    
                    if (IsValidSpawnPosition(gridPos, requireRoadAccess))
                    {
                        Vector3 worldPos = new Vector3(
                            x * mapGenerator.tileSize,
                            0.5f,
                            y * mapGenerator.tileSize
                        );
                        fallbackPositions.Add(worldPos);
                    }
                }
            }

            return fallbackPositions;
        }

        bool IsValidSpawnPosition(Vector2Int gridPos, bool requireRoadAccess)
        {
            // Проверяем границы карты
            if (!cityGrid.IsValidPosition(gridPos)) return false;

            // Должна быть трава (не дорога)
            if (cityGrid.Grid[gridPos.x][gridPos.y] != TileType.Grass) return false;

            // Не должно быть зданий
            if (cityGrid.IsCellOccupiedByBuilding(gridPos)) return false;

            // Проверяем доступ к дорогам если требуется
            if (requireRoadAccess)
            {
                if (!HasRoadNearby(gridPos, 5)) return false; // В пределах 5 клеток
                if (HasRoadNearby(gridPos, 1)) return false;  // Но не вплотную
            }
            else
            {
                // Для врагов и боссов - подальше от дорог
                if (HasRoadNearby(gridPos, (int)minRoadDistance)) return false;
            }

            return true;
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

        void CreateSpawnPoint(Vector3 position, SpawnPointType spawnType, GameObject prefab)
        {
            GameObject spawnPoint;

            if (prefab != null)
            {
                spawnPoint = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                // Создаем базовую точку спауна
                spawnPoint = CreateDefaultSpawnPoint(position, spawnType);
            }

            spawnPoint.name = $"SpawnPoint_{spawnType}_{createdSpawnPoints.Count}";
            spawnPoint.transform.SetParent(transform);

            // Добавляем SpawnPointAuthoring если его нет
            var authoring = spawnPoint.GetComponent<SpawnPointAuthoring>();
            if (authoring == null)
            {
                authoring = spawnPoint.AddComponent<SpawnPointAuthoring>();
            }

            authoring.spawnType = spawnType;
            authoring.zoneType = GetZoneTypeAtPosition(position);
            authoring.cooldownTime = GetCooldownForSpawnType(spawnType);
            authoring.isActive = true;

            createdSpawnPoints.Add(spawnPoint);
        }

        GameObject CreateDefaultSpawnPoint(Vector3 position, SpawnPointType spawnType)
        {
            var spawnPoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spawnPoint.transform.position = position;
            spawnPoint.transform.localScale = new Vector3(2f, 0.1f, 2f);

            // Убираем коллайдер
            var collider = spawnPoint.GetComponent<Collider>();
            if (collider != null)
                DestroyImmediate(collider);

            // Настраиваем материал
            var renderer = spawnPoint.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = GetColorForSpawnType(spawnType);
                material.SetFloat("_Mode", 3); // Transparent
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                renderer.material = material;
            }

            return spawnPoint;
        }

        TileType GetZoneTypeAtPosition(Vector3 worldPosition)
        {
            // Конвертируем мировую позицию в координаты сетки
            int gridX = Mathf.RoundToInt(worldPosition.x / mapGenerator.tileSize);
            int gridY = Mathf.RoundToInt(worldPosition.z / mapGenerator.tileSize);
            
            var zoneData = zoneSystem.GetZoneAt(new Vector2Int(gridX, gridY));
            return zoneData?.zoneType ?? TileType.StandardZone;
        }

        float GetCooldownForSpawnType(SpawnPointType spawnType)
        {
            return spawnType switch
            {
                SpawnPointType.PlayerSpawn => 5f,
                SpawnPointType.EnemySpawn => 10f,
                SpawnPointType.BossSpawn => 60f,
                SpawnPointType.ReinforcementSpawn => 15f,
                _ => 10f
            };
        }

        Color GetColorForSpawnType(SpawnPointType spawnType)
        {
            return spawnType switch
            {
                SpawnPointType.PlayerSpawn => new Color(0f, 1f, 0f, 0.7f),     // Зеленый
                SpawnPointType.EnemySpawn => new Color(1f, 0f, 0f, 0.7f),      // Красный
                SpawnPointType.BossSpawn => new Color(1f, 0f, 1f, 0.7f),       // Пурпурный
                SpawnPointType.ReinforcementSpawn => new Color(1f, 1f, 0f, 0.7f), // Желтый
                _ => new Color(1f, 1f, 1f, 0.7f)                               // Белый
            };
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

        void ClearExistingSpawnPoints()
        {
            foreach (var spawnPoint in createdSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    DestroyImmediate(spawnPoint);
                }
            }
            createdSpawnPoints.Clear();
        }

        void LogSpawnStatistics()
        {
            Debug.Log("📊 === СТАТИСТИКА ТОЧЕК СПАУНА ===");
            
            var spawnStats = new Dictionary<SpawnPointType, int>();
            
            foreach (var spawnPoint in createdSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    var authoring = spawnPoint.GetComponent<SpawnPointAuthoring>();
                    if (authoring != null)
                    {
                        if (!spawnStats.ContainsKey(authoring.spawnType))
                            spawnStats[authoring.spawnType] = 0;
                        spawnStats[authoring.spawnType]++;
                    }
                }
            }

            foreach (var kvp in spawnStats)
            {
                string emoji = GetEmojiForSpawnType(kvp.Key);
                Debug.Log($"{emoji} {kvp.Key}: {kvp.Value} точек");
            }
            
            Debug.Log($"🎯 Всего точек спауна: {createdSpawnPoints.Count}");
            Debug.Log("================================");
        }

        string GetEmojiForSpawnType(SpawnPointType spawnType)
        {
            return spawnType switch
            {
                SpawnPointType.PlayerSpawn => "👤",
                SpawnPointType.EnemySpawn => "👹",
                SpawnPointType.BossSpawn => "🐲",
                SpawnPointType.ReinforcementSpawn => "⚡",
                _ => "🎯"
            };
        }

        // ===== ПУБЛИЧНЫЕ МЕТОДЫ =====

        /// <summary>
        /// Принудительно пересоздать точки спауна
        /// </summary>
        [ContextMenu("Recreate Spawn Points")]
        public void RecreateSpawnPoints()
        {
            if (mapGenerator == null || !mapGenerator.IsGenerating)
            {
                StartCoroutine(PlaceAllSpawnPoints());
            }
            else
            {
                Debug.LogWarning("Нельзя пересоздать точки спауна во время генерации карты!");
            }
        }

        /// <summary>
        /// Получить все точки спауна определенного типа
        /// </summary>
        public List<Vector3> GetSpawnPointsOfType(SpawnPointType spawnType)
        {
            var positions = new List<Vector3>();
            
            foreach (var spawnPoint in createdSpawnPoints)
            {
                if (spawnPoint != null)
                {
                    var authoring = spawnPoint.GetComponent<SpawnPointAuthoring>();
                    if (authoring != null && authoring.spawnType == spawnType)
                    {
                        positions.Add(spawnPoint.transform.position);
                    }
                }
            }
            
            return positions;
        }

        /// <summary>
        /// Получить ближайшую свободную точку спауна
        /// </summary>
        public Vector3? GetNearestAvailableSpawnPoint(SpawnPointType spawnType, Vector3 referencePosition)
        {
            var spawnPoints = GetSpawnPointsOfType(spawnType);
            if (spawnPoints.Count == 0) return null;

            Vector3 nearest = spawnPoints[0];
            float nearestDistance = Vector3.Distance(referencePosition, nearest);

            foreach (var spawnPoint in spawnPoints)
            {
                float distance = Vector3.Distance(referencePosition, spawnPoint);
                if (distance < nearestDistance)
                {
                    nearest = spawnPoint;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        void OnDrawGizmos()
        {
            // Показываем настройки в редакторе
            if (mapGenerator != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(
                    new Vector3(mapGenerator.gridWidth * mapGenerator.tileSize * 0.5f, 1f, mapGenerator.gridHeight * mapGenerator.tileSize * 0.5f),
                    new Vector3(mapGenerator.gridWidth * mapGenerator.tileSize, 2f, mapGenerator.gridHeight * mapGenerator.tileSize)
                );
            }
        }
    }
}