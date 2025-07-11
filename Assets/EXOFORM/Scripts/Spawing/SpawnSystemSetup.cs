// using UnityEngine;
// using UnityEditor;
// using System.IO;
// using EXOFORM.Scripts.Ecs.Authoring.Spawing;
// using EXOFORM.Scripts.Ecs.Components.Spawning;
// using Exoform.Scripts.Map;
//
// namespace Exoform.Scripts.Spawning
// {
//     /// <summary>
//     /// Автоматический настройщик системы спауна EXOFORM
//     /// Помогает быстро настроить все компоненты системы
//     /// </summary>
//     public class SpawnSystemSetup : MonoBehaviour
//     {
//         [Header("Automatic Setup")]
//         [Tooltip("Автоматически создать все необходимые компоненты")]
//         public bool autoSetup = true;
//
//         [Header("Prefab References")]
//         [Tooltip("Префаб игрока")]
//         public GameObject playerPrefab;
//         
//         [Tooltip("Префабы врагов")]
//         public GameObject[] enemyPrefabs;
//         
//         [Tooltip("Префабы боссов")]
//         public GameObject[] bossPrefabs;
//
//         [Header("Spawn Points")]
//         [Tooltip("Точки спауна игроков (опционально)")]
//         public Transform[] playerSpawnPoints;
//
//         void Start()
//         {
//             if (autoSetup)
//             {
//                 SetupSpawnSystem();
//             }
//         }
//
//         /// <summary>
//         /// Настройка системы спауна
//         /// </summary>
//         [ContextMenu("Setup Spawn System")]
//         public void SetupSpawnSystem()
//         {
//             Debug.Log("🔧 === НАСТРОЙКА СИСТЕМЫ СПАУНА ===");
//
//             try
//             {
//                 // 1. Создаем конфигурацию префабов
//                 CreateSpawnPrefabConfiguration();
//
//                 // 2. Настраиваем SpawnerAuthoring
//                 SetupSpawnerAuthoring();
//
//                 // 3. Добавляем интеграцию с картой
//                 SetupMapIntegration();
//
//                 // 4. Создаем тестовый компонент
//                 SetupTestingComponent();
//
//                 Debug.Log("✅ Система спауна успешно настроена!");
//             }
//             catch (System.Exception e)
//             {
//                 Debug.LogError($"❌ Ошибка при настройке системы спауна: {e.Message}");
//             }
//         }
//
//         void CreateSpawnPrefabConfiguration()
//         {
//             Debug.Log("📦 Создание конфигурации префабов...");
//
// #if UNITY_EDITOR
//             // Проверяем, существует ли уже конфигурация
//             string configPath = "Assets/Resources/SpawnPrefabConfig.asset";
//             
//             SpawnPrefabConfiguration existingConfig = AssetDatabase.LoadAssetAtPath<SpawnPrefabConfiguration>(configPath);
//             if (existingConfig != null)
//             {
//                 Debug.Log("   ℹ️ Конфигурация уже существует, обновляем...");
//                 UpdateExistingConfiguration(existingConfig);
//                 return;
//             }
//
//             // Создаем папку Resources если её нет
//             string resourcesPath = "Assets/Resources";
//             if (!AssetDatabase.IsValidFolder(resourcesPath))
//             {
//                 AssetDatabase.CreateFolder("Assets", "Resources");
//             }
//
//             // Создаем новую конфигурацию
//             SpawnPrefabConfiguration config = CreateInstance<SpawnPrefabConfiguration>();
//             
//             // Заполняем конфигурацию
//             PopulateConfiguration(config);
//             
//             // Сохраняем
//             AssetDatabase.CreateAsset(config, configPath);
//             AssetDatabase.SaveAssets();
//             AssetDatabase.Refresh();
//             
//             Debug.Log($"   ✅ Конфигурация создана: {configPath}");
// #endif
//         }
//
//         void PopulateConfiguration(SpawnPrefabConfiguration config)
//         {
//             // Очищаем существующие списки
//             config.playerUnits.Clear();
//             config.enemyUnits.Clear();
//             config.bossUnits.Clear();
//
//             // Добавляем игрока
//             if (playerPrefab != null)
//             {
//                 config.playerUnits.Add(new SpawnPrefabConfiguration.UnitPrefabEntry
//                 {
//                     unitName = "Player",
//                     unitType = Exoform.Scripts.Ecs.Components.UnitLogicComponents.UnitType.Infantry,
//                     prefab = playerPrefab,
//                     spawnWeight = 1f,
//                     teamId = 1,
//                     cost = 0,
//                     cooldown = 5f,
//                     maxSimultaneous = 4,
//                     requiresSpecialZone = false
//                 });
//             }
//
//             // Добавляем врагов
//             for (int i = 0; i < enemyPrefabs.Length; i++)
//             {
//                 if (enemyPrefabs[i] != null)
//                 {
//                     config.enemyUnits.Add(new SpawnPrefabConfiguration.UnitPrefabEntry
//                     {
//                         unitName = $"Enemy_{i + 1}",
//                         unitType = Exoform.Scripts.Ecs.Components.UnitLogicComponents.UnitType.Infantry,
//                         prefab = enemyPrefabs[i],
//                         spawnWeight = 2f - (i * 0.2f), // Уменьшаем вес для более редких врагов
//                         teamId = 2,
//                         cost = 50 + (i * 25),
//                         cooldown = 2f + (i * 1f),
//                         maxSimultaneous = 10 - i,
//                         requiresSpecialZone = true,
//                         allowedZones = new Exoform.Scripts.Map.TileType[] 
//                         { 
//                             Exoform.Scripts.Map.TileType.CorruptedTrap, 
//                             Exoform.Scripts.Map.TileType.InfestationZone 
//                         }
//                     });
//                 }
//             }
//
//             // Добавляем боссов
//             for (int i = 0; i < bossPrefabs.Length; i++)
//             {
//                 if (bossPrefabs[i] != null)
//                 {
//                     config.bossUnits.Add(new SpawnPrefabConfiguration.UnitPrefabEntry
//                     {
//                         unitName = $"Boss_{i + 1}",
//                         unitType = Exoform.Scripts.Ecs.Components.UnitLogicComponents.UnitType.Building,
//                         prefab = bossPrefabs[i],
//                         spawnWeight = 1f,
//                         teamId = 0,
//                         cost = 1000 + (i * 500),
//                         cooldown = 60f + (i * 30f),
//                         maxSimultaneous = 1,
//                         requiresSpecialZone = true,
//                         allowedZones = new Exoform.Scripts.Map.TileType[] 
//                         { 
//                             Exoform.Scripts.Map.TileType.BossZone,
//                             Exoform.Scripts.Map.TileType.ArtifactZone 
//                         }
//                     });
//                 }
//             }
//
//             Debug.Log($"   📦 Добавлено: {config.playerUnits.Count} игроков, {config.enemyUnits.Count} врагов, {config.bossUnits.Count} боссов");
//         }
//
//         void UpdateExistingConfiguration(SpawnPrefabConfiguration config)
//         {
//             // Обновляем только если префабы не назначены
//             bool needsUpdate = false;
//
//             // Проверяем игроков
//             if (playerPrefab != null && (config.playerUnits.Count == 0 || config.playerUnits[0].prefab == null))
//             {
//                 if (config.playerUnits.Count == 0)
//                     config.playerUnits.Add(new SpawnPrefabConfiguration.UnitPrefabEntry());
//                 
//                 config.playerUnits[0].prefab = playerPrefab;
//                 config.playerUnits[0].unitName = "Player";
//                 needsUpdate = true;
//             }
//
//             // Обновляем врагов
//             for (int i = 0; i < enemyPrefabs.Length; i++)
//             {
//                 if (enemyPrefabs[i] != null)
//                 {
//                     while (config.enemyUnits.Count <= i)
//                     {
//                         config.enemyUnits.Add(new SpawnPrefabConfiguration.UnitPrefabEntry
//                         {
//                             teamId = 2,
//                             requiresSpecialZone = true,
//                             allowedZones = new Exoform.Scripts.Map.TileType[] 
//                             { 
//                                 Exoform.Scripts.Map.TileType.CorruptedTrap, 
//                                 Exoform.Scripts.Map.TileType.InfestationZone 
//                             }
//                         });
//                     }
//
//                     if (config.enemyUnits[i].prefab == null)
//                     {
//                         config.enemyUnits[i].prefab = enemyPrefabs[i];
//                         config.enemyUnits[i].unitName = $"Enemy_{i + 1}";
//                         needsUpdate = true;
//                     }
//                 }
//             }
//
//             if (needsUpdate)
//             {
// #if UNITY_EDITOR
//                 EditorUtility.SetDirty(config);
//                 AssetDatabase.SaveAssets();
// #endif
//                 Debug.Log("   ✅ Конфигурация обновлена");
//             }
//         }
//
//         void SetupSpawnerAuthoring()
//         {
//             Debug.Log("🎯 Настройка SpawnerAuthoring...");
//
//             // Ищем существующий SpawnerAuthoring
//             SpawnerAuthoring existingSpawner = FindObjectOfType<SpawnerAuthoring>();
//             
//             if (existingSpawner != null)
//             {
//                 Debug.Log("   ℹ️ SpawnerAuthoring уже существует, обновляем...");
//                 UpdateSpawnerAuthoring(existingSpawner);
//                 return;
//             }
//
//             // Создаем новый GameObject со SpawnerAuthoring
//             GameObject spawnerGO = new GameObject("EXOFORM_Spawner");
//             spawnerGO.transform.SetParent(transform);
//             
//             SpawnerAuthoring spawner = spawnerGO.AddComponent<SpawnerAuthoring>();
//             
//             // Настраиваем
//             spawner.playerPrefab = playerPrefab;
//             spawner.enemyPrefabs = enemyPrefabs;
//             spawner.maxPlayers = 4;
//             spawner.enemySpawnRate = 2f;
//             spawner.maxEnemiesPerWave = 10;
//             spawner.waveInterval = 30f;
//             spawner.useZoneBasedSpawning = true;
//
//             // Создаем точки спауна игроков если их нет
//             if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
//             {
//                 CreateDefaultPlayerSpawnPoints(spawnerGO.transform);
//             }
//             else
//             {
//                 spawner.playerSpawnPoints = playerSpawnPoints;
//             }
//
//             Debug.Log("   ✅ SpawnerAuthoring создан и настроен");
//         }
//
//         void UpdateSpawnerAuthoring(SpawnerAuthoring spawner)
//         {
//             bool needsUpdate = false;
//
//             if (spawner.playerPrefab == null && playerPrefab != null)
//             {
//                 spawner.playerPrefab = playerPrefab;
//                 needsUpdate = true;
//             }
//
//             if ((spawner.enemyPrefabs == null || spawner.enemyPrefabs.Length == 0) && enemyPrefabs != null)
//             {
//                 spawner.enemyPrefabs = enemyPrefabs;
//                 needsUpdate = true;
//             }
//
//             if (needsUpdate)
//             {
// #if UNITY_EDITOR
//                 EditorUtility.SetDirty(spawner);
// #endif
//                 Debug.Log("   ✅ SpawnerAuthoring обновлен");
//             }
//         }
//
//         void CreateDefaultPlayerSpawnPoints(Transform parent)
//         {
//             Debug.Log("   📍 Создание стандартных точек спауна игроков...");
//
//             Vector3[] positions = new Vector3[]
//             {
//                 new Vector3(-10, 0, -10),
//                 new Vector3(10, 0, -10),
//                 new Vector3(-10, 0, 10),
//                 new Vector3(10, 0, 10)
//             };
//
//             Transform[] spawnPoints = new Transform[positions.Length];
//
//             for (int i = 0; i < positions.Length; i++)
//             {
//                 GameObject spawnPoint = new GameObject($"PlayerSpawn_{i + 1}");
//                 spawnPoint.transform.SetParent(parent);
//                 spawnPoint.transform.position = positions[i];
//                 
//                 // Добавляем SpawnPointAuthoring
//                 SpawnPointAuthoring authoring = spawnPoint.AddComponent<SpawnPointAuthoring>();
//                 authoring.spawnType = SpawnPointType.PlayerSpawn;
//                 authoring.zoneType = Exoform.Scripts.Map.TileType.StandardZone;
//                 authoring.cooldownTime = 5f;
//                 authoring.isActive = true;
//
//                 spawnPoints[i] = spawnPoint.transform;
//             }
//
//             // Обновляем SpawnerAuthoring
//             SpawnerAuthoring spawner = parent.GetComponent<SpawnerAuthoring>();
//             if (spawner != null)
//             {
//                 spawner.playerSpawnPoints = spawnPoints;
//             }
//
//             Debug.Log($"   ✅ Создано {spawnPoints.Length} точек спауна игроков");
//         }
//
//         void SetupMapIntegration()
//         {
//             Debug.Log("🗺️ Настройка интеграции с картой...");
//
//             // Ищем ExoformMapGenerator
//             var mapGenerator = FindObjectOfType<Exoform.Scripts.Map.ExoformMapGenerator>();
//             if (mapGenerator == null)
//             {
//                 Debug.LogWarning("   ⚠️ ExoformMapGenerator не найден! Убедитесь, что он присутствует в сцене.");
//                 return;
//             }
//
//             // Проверяем SpawnSystemIntegration
//             SpawnSystemIntegration integration = mapGenerator.GetComponent<SpawnSystemIntegration>();
//             if (integration == null)
//             {
//                 integration = mapGenerator.gameObject.AddComponent<SpawnSystemIntegration>();
//                 Debug.Log("   ✅ SpawnSystemIntegration добавлен к ExoformMapGenerator");
//             }
//
//             // Настраиваем интеграцию
//             integration.playerSpawnCount = 4;
//             integration.enemySpawnCount = 12;
//             integration.bossSpawnCount = 2;
//             integration.minSpawnDistance = 10f;
//             integration.minRoadDistance = 3f;
//
// #if UNITY_EDITOR
//             EditorUtility.SetDirty(integration);
// #endif
//
//             Debug.Log("   ✅ Интеграция с картой настроена");
//         }
//
//         void SetupTestingComponent()
//         {
//             Debug.Log("🧪 Настройка тестового компонента...");
//
//             // Проверяем, есть ли уже SpawnSystemExample
//             SpawnSystemExample existingExample = FindObjectOfType<SpawnSystemExample>();
//             if (existingExample != null)
//             {
//                 Debug.Log("   ℹ️ SpawnSystemExample уже существует");
//                 return;
//             }
//
//             // Создаем тестовый компонент
//             GameObject testGO = new GameObject("EXOFORM_SpawnTester");
//             testGO.transform.SetParent(transform);
//             
//             SpawnSystemExample example = testGO.AddComponent<SpawnSystemExample>();
//             example.testOnStart = false; // По умолчанию отключаем автотест
//             example.autoTestInterval = 0f;
//             example.testPlayerCount = 2;
//
//             Debug.Log("   ✅ Тестовый компонент создан");
//         }
//
//         /// <summary>
//         /// Валидация системы спауна
//         /// </summary>
//         [ContextMenu("Validate Spawn System")]
//         public void ValidateSpawnSystem()
//         {
//             Debug.Log("🔍 === ВАЛИДАЦИЯ СИСТЕМЫ СПАУНА ===");
//
//             var errors = new System.Collections.Generic.List<string>();
//             var warnings = new System.Collections.Generic.List<string>();
//
//             // Проверяем конфигурацию префабов
//             var config = Resources.Load<SpawnPrefabConfiguration>("SpawnPrefabConfig");
//             if (config == null)
//             {
//                 errors.Add("SpawnPrefabConfiguration не найдена в Resources");
//             }
//             else
//             {
//                 config.ValidateConfiguration();
//             }
//
//             // Проверяем SpawnerAuthoring
//             var spawner = FindObjectOfType<SpawnerAuthoring>();
//             if (spawner == null)
//             {
//                 errors.Add("SpawnerAuthoring не найден в сцене");
//             }
//
//             // Проверяем интеграцию с картой
//             var mapGenerator = FindObjectOfType<Exoform.Scripts.Map.ExoformMapGenerator>();
//             if (mapGenerator == null)
//             {
//                 warnings.Add("ExoformMapGenerator не найден - спаун будет работать без интеграции с зонами");
//             }
//             else
//             {
//                 var integration = mapGenerator.GetComponent<SpawnSystemIntegration>();
//                 if (integration == null)
//                 {
//                     warnings.Add("SpawnSystemIntegration не найден на ExoformMapGenerator");
//                 }
//             }
//
//             // Выводим результаты
//             if (errors.Count == 0 && warnings.Count == 0)
//             {
//                 Debug.Log("✅ Система спауна корректно настроена!");
//             }
//             else
//             {
//                 if (errors.Count > 0)
//                 {
//                     Debug.LogError($"❌ Найдено {errors.Count} ошибок:\n" + string.Join("\n", errors));
//                 }
//                 
//                 if (warnings.Count > 0)
//                 {
//                     Debug.LogWarning($"⚠️ Найдено {warnings.Count} предупреждений:\n" + string.Join("\n", warnings));
//                 }
//             }
//         }
//
//         /// <summary>
//         /// Быстрая очистка всех созданных компонентов
//         /// </summary>
//         [ContextMenu("Clean Spawn System")]
//         public void CleanSpawnSystem()
//         {
//             Debug.Log("🧹 Очистка компонентов системы спауна...");
//
//             // Удаляем SpawnerAuthoring
//             var spawners = FindObjectsOfType<SpawnerAuthoring>();
//             foreach (var spawner in spawners)
//             {
//                 if (spawner.transform.IsChildOf(transform))
//                 {
//                     DestroyImmediate(spawner.gameObject);
//                 }
//             }
//
//             // Удаляем тестовые компоненты
//             var examples = FindObjectsOfType<SpawnSystemExample>();
//             foreach (var example in examples)
//             {
//                 if (example.transform.IsChildOf(transform))
//                 {
//                     DestroyImmediate(example.gameObject);
//                 }
//             }
//
//             Debug.Log("✅ Очистка завершена");
//         }
//
//         void OnDrawGizmos()
//         {
//             // Показываем область спауна
//             Gizmos.color = Color.cyan;
//             Gizmos.DrawWireCube(transform.position, Vector3.one * 20f);
//
//             // Показываем точки спауна игроков
//             if (playerSpawnPoints != null)
//             {
//                 Gizmos.color = Color.green;
//                 foreach (var spawnPoint in playerSpawnPoints)
//                 {
//                     if (spawnPoint != null)
//                     {
//                         Gizmos.DrawWireSphere(spawnPoint.position, 2f);
//                     }
//                 }
//             }
//         }
//     }
// }