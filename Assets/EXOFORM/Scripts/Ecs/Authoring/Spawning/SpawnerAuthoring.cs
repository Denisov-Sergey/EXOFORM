using EXOFORM.Scripts.Ecs.Components.Spawning;
using Exoform.Scripts.Ecs.Components.UnitLogicComponents;
using Exoform.Scripts.Map;
using Unity.Entities;
using UnityEngine;

namespace EXOFORM.Scripts.Ecs.Authoring.Spawning
{
    /// <summary>
    /// Authoring компонент для настройки спаунера
    /// </summary>
    public class SpawnerAuthoring : MonoBehaviour
    {
        [Header("Player Spawn Settings")]
        public GameObject playerPrefab;
        public Transform[] playerSpawnPoints;
        public int maxPlayers = 4;

        [Header("Enemy Spawn Settings")]
        public GameObject[] enemyPrefabs;
        public float enemySpawnRate = 2f;
        public int maxEnemiesPerWave = 10;
        public float waveInterval = 30f;

        [Header("Zone Integration")]
        public bool useZoneBasedSpawning = true;
        public TileType[] enemySpawnZones = { TileType.CorruptedTrap, TileType.InfestationZone };

        class Baker : Baker<SpawnerAuthoring>
        {
            public override void Bake(SpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                // Основной компонент спаунера
                AddComponent(entity, new SpawnerStateComponent
                {
                    LastSpawnTime = 0f,
                    CurrentPlayerCount = 0,
                    CurrentEnemyCount = 0,
                    WaveNumber = 0,
                    NextWaveTime = 5f, // Первая волна через 5 секунд
                    IsActive = true
                });

                // Буфер для очереди спауна
                AddBuffer<SpawnQueueBuffer>(entity);

                // Конфигурации для разных типов юнитов
                if (authoring.playerPrefab != null)
                {
                    var playerEntity = GetEntity(authoring.playerPrefab, TransformUsageFlags.Dynamic);
                    AddComponent(entity, new SpawnConfiguration
                    {
                        UnitPrefab = playerEntity,
                        UnitType = UnitType.Infantry,
                        TeamId = 1,
                        SpawnRate = 0f, // Игроки спаунятся по запросу
                        MaxUnits = authoring.maxPlayers,
                        SpawnRadius = 2f,
                        UseZoneSpawning = false,
                        PreferredZone = TileType.StandardZone
                    });
                }

                // Создаем точки спауна игроков
                foreach (var spawnPoint in authoring.playerSpawnPoints)
                {
                    if (spawnPoint != null)
                    {
                        var spawnEntity = CreateAdditionalEntity(TransformUsageFlags.None);
                        AddComponent(spawnEntity, new SpawnPointComponent
                        {
                            Position = spawnPoint.position,
                            PointType = SpawnPointType.PlayerSpawn,
                            ZoneType = TileType.StandardZone,
                            IsActive = true,
                            CooldownTime = 5f,
                            LastUsedTime = 0f
                        });
                    }
                }
            }
        }
    }
}