using EXOFORM.Scripts.Ecs.Components.Spawning;
using EXOFORM.Scripts.Ecs.Systems.Spawing;
using Unity.Entities;
using UnityEngine;

namespace Exoform.Scripts.Hybrid
{
    /// <summary>
    /// Публичный интерфейс для взаимодействия с системой спауна
    /// </summary>
    public static class SpawnManager
    {
        /// <summary>
        /// Заспаунить игрока
        /// </summary>
        public static bool SpawnPlayer(int playerId)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return false;

            var playerSpawnSystem = world.GetExistingSystemManaged<PlayerSpawnSystem>();
            return playerSpawnSystem?.RequestPlayerSpawn(playerId) ?? false;
        }

        /// <summary>
        /// Запустить волну врагов принудительно
        /// </summary>
        public static void TriggerEnemyWave()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<SpawnerStateComponent>());
            
            if (query.TryGetSingleton<SpawnerStateComponent>(out var spawnerState))
            {
                spawnerState.NextWaveTime = 0f; // Запускаем немедленно
                query.SetSingleton(spawnerState);
            }
            
            query.Dispose();
        }

        /// <summary>
        /// Остановить/запустить спаун
        /// </summary>
        public static void SetSpawningActive(bool active)
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<SpawnerStateComponent>());
            
            if (query.TryGetSingleton<SpawnerStateComponent>(out var spawnerState))
            {
                spawnerState.IsActive = active;
                query.SetSingleton(spawnerState);
                Debug.Log($"🎯 Система спауна {(active ? "включена" : "отключена")}");
            }
            
            query.Dispose();
        }
    }
}