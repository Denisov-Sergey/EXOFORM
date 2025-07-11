using System.Collections.Generic;
using EXOFORM.Scripts.Ecs.Components.Spawning;
using Exoform.Scripts.Ecs.Components.UnitLogicComponents;
using Exoform.Scripts.Map;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace EXOFORM.Scripts.Ecs.Systems.Spawning
{
    /// <summary>
    /// Главная система спауна
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SpawnManagementSystem : SystemBase
    {
        private ExoformMapGenerator mapGenerator;
        private BeginInitializationEntityCommandBufferSystem beginInitECBS;

        protected override void OnCreate()
        {
            RequireForUpdate<SpawnerStateComponent>();
            beginInitECBS = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnStartRunning()
        {
            // Ищем генератор карты в сцене
            mapGenerator = UnityEngine.Object.FindObjectOfType<ExoformMapGenerator>();
            if (mapGenerator == null)
            {
                Debug.LogError("ExoformMapGenerator не найден! Система спауна не может получить данные зон.");
            }
        }

        protected override void OnUpdate()
        {
            var ecb = beginInitECBS.CreateCommandBuffer();
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Обрабатываем спаунер
            foreach (var (spawnerState, entity) in 
                     SystemAPI.Query<RefRW<SpawnerStateComponent>>().WithEntityAccess())
            {
                if (!spawnerState.ValueRO.IsActive) continue;

                // Проверяем, нужно ли запустить новую волну врагов
                if (currentTime >= spawnerState.ValueRO.NextWaveTime)
                {
                    ScheduleEnemyWave(ecb, entity, spawnerState, currentTime);
                }

                // Обрабатываем очередь спауна
                ProcessSpawnQueue(ecb, entity, currentTime);
            }
        }

        void ScheduleEnemyWave(EntityCommandBuffer ecb, Entity spawnerEntity, 
                              RefRW<SpawnerStateComponent> spawnerState, float currentTime)
        {
            var waveNumber = spawnerState.ValueRO.WaveNumber + 1;
            var enemiesInWave = Mathf.Min(5 + waveNumber * 2, 20); // Увеличиваем сложность

            Debug.Log($"🌊 Запускаем волну {waveNumber} с {enemiesInWave} врагами");

            // Планируем спаун врагов
            var spawnQueue = SystemAPI.GetBuffer<SpawnQueueBuffer>(spawnerEntity);
            
            for (int i = 0; i < enemiesInWave; i++)
            {
                var spawnTime = currentTime + i * 2f; // Спаун каждые 2 секунды
                
                spawnQueue.Add(new SpawnQueueBuffer
                {
                    Request = new SpawnRequestComponent
                    {
                        UnitType = UnitType.Infantry,
                        TeamId = 2, // Команда врагов
                        Position = float3.zero, // Будет определена позже
                        Rotation = quaternion.identity,
                        PrefabToSpawn = Entity.Null, // Будет выбран случайно
                        UseRandomPosition = true,
                        PreferredSpawnType = SpawnPointType.EnemySpawn
                    },
                    ScheduledTime = spawnTime,
                    Priority = 1
                });
            }

            // Обновляем состояние спаунера
            spawnerState.ValueRW.WaveNumber = waveNumber;
            spawnerState.ValueRW.NextWaveTime = currentTime + 45f + waveNumber * 15f; // Увеличиваем интервал
        }

        void ProcessSpawnQueue(EntityCommandBuffer ecb, Entity spawnerEntity, float currentTime)
        {
            var spawnQueue = SystemAPI.GetBuffer<SpawnQueueBuffer>(spawnerEntity);
            
            for (int i = spawnQueue.Length - 1; i >= 0; i--)
            {
                var queueItem = spawnQueue[i];
                
                if (currentTime >= queueItem.ScheduledTime)
                {
                    // Пора спаунить!
                    if (TrySpawnUnit(ecb, queueItem.Request))
                    {
                        spawnQueue.RemoveAt(i);
                    }
                    else
                    {
                        // Не удалось спаунить, отложим на 2 секунды
                        queueItem.ScheduledTime = currentTime + 2f;
                        spawnQueue[i] = queueItem;
                    }
                }
            }
        }

        bool TrySpawnUnit(EntityCommandBuffer ecb, SpawnRequestComponent request)
        {
            float3 spawnPosition;
            
            if (request.UseRandomPosition)
            {
                spawnPosition = FindSpawnPosition(request.PreferredSpawnType, request.TeamId);
                if (spawnPosition.Equals(float3.zero))
                {
                    Debug.LogWarning($"Не удалось найти позицию для спауна {request.UnitType}");
                    return false;
                }
            }
            else
            {
                spawnPosition = request.Position;
            }

            // Создаем юнита используя UnitSpawner
            var unitSpawner = UnityEngine.Object.FindObjectOfType<Exoform.Scripts.Hybrid.UnitSpawner>();
            if (unitSpawner != null)
            {
                var spawnedEntity = unitSpawner.SpawnUnitAtPosition(spawnPosition, request.UnitType, request.TeamId);
                if (spawnedEntity != Entity.Null)
                {
                    Debug.Log($"✅ Спаун {request.UnitType} команды {request.TeamId} в {spawnPosition}");
                    return true;
                }
            }

            return false;
        }

        float3 FindSpawnPosition(SpawnPointType preferredType, int teamId)
        {
            // Сначала пробуем найти подходящую точку спауна
            foreach (var (spawnPoint, entity) in 
                     SystemAPI.Query<RefRW<SpawnPointComponent>>().WithEntityAccess())
            {
                if (spawnPoint.ValueRO.PointType == preferredType && 
                    spawnPoint.ValueRO.IsActive &&
                    SystemAPI.Time.ElapsedTime - spawnPoint.ValueRO.LastUsedTime > spawnPoint.ValueRO.CooldownTime)
                {
                    spawnPoint.ValueRW.LastUsedTime = (float)SystemAPI.Time.ElapsedTime;
                    return spawnPoint.ValueRO.Position;
                }
            }

            // Если не нашли точку спауна, используем зоны карты
            if (mapGenerator != null)
            {
                return FindSpawnPositionInZones(teamId);
            }

            // Fallback - случайная позиция
            return new float3(
                UnityEngine.Random.Range(-20f, 20f),
                0f,
                UnityEngine.Random.Range(-20f, 20f)
            );
        }

        float3 FindSpawnPositionInZones(int teamId)
        {
            var zoneSystem = mapGenerator.ZoneSystem;
            if (zoneSystem == null) return float3.zero;

            // Для врагов ищем опасные зоны
            if (teamId == 2) // Враги
            {
                var corruptedZones = zoneSystem.GetZonesByType(TileType.CorruptedTrap);
                var infestZones = zoneSystem.GetZonesByType(TileType.InfestationZone);
                
                var allEnemyZones = new List<ExoformZoneSystem.ZoneData>();
                allEnemyZones.AddRange(corruptedZones);
                allEnemyZones.AddRange(infestZones);

                if (allEnemyZones.Count > 0)
                {
                    var randomZone = allEnemyZones[UnityEngine.Random.Range(0, allEnemyZones.Count)];
                    
                    return new float3(
                        UnityEngine.Random.Range(randomZone.position.x, randomZone.position.x + randomZone.size.x),
                        0f,
                        UnityEngine.Random.Range(randomZone.position.y, randomZone.position.y + randomZone.size.y)
                    ) * mapGenerator.tileSize;
                }
            }
            else // Игроки
            {
                var standardZones = zoneSystem.GetZonesByType(TileType.StandardZone);
                if (standardZones.Count > 0)
                {
                    var randomZone = standardZones[UnityEngine.Random.Range(0, standardZones.Count)];
                    
                    return new float3(
                        UnityEngine.Random.Range(randomZone.position.x, randomZone.position.x + randomZone.size.x),
                        0f,
                        UnityEngine.Random.Range(randomZone.position.y, randomZone.position.y + randomZone.size.y)
                    ) * mapGenerator.tileSize;
                }
            }

            return float3.zero;
        }
    }
}