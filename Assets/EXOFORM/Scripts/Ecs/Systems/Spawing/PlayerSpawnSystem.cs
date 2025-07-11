using EXOFORM.Scripts.Ecs.Components.Spawning;
using Exoform.Scripts.Ecs.Components.UnitLogicComponents;
using Unity.Entities;
using UnityEngine;

namespace EXOFORM.Scripts.Ecs.Systems.Spawing
{
    /// <summary>
    /// Система для управления спауном игроков по запросу
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class PlayerSpawnSystem : SystemBase
    {
        private BeginInitializationEntityCommandBufferSystem beginInitECBS;

        protected override void OnCreate()
        {
            beginInitECBS = World.GetOrCreateSystemManaged<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            // Эта система будет вызываться извне для спауна игроков
            // Например, при подключении нового игрока или респауне
        }

        /// <summary>
        /// Публичный метод для спауна игрока
        /// </summary>
        public bool RequestPlayerSpawn(int playerId)
        {
            var ecb = beginInitECBS.CreateCommandBuffer();

            // Ищем свободную точку спауна игрока
            foreach (var (spawnPoint, entity) in 
                     SystemAPI.Query<RefRW<SpawnPointComponent>>().WithEntityAccess())
            {
                if (spawnPoint.ValueRO.PointType == SpawnPointType.PlayerSpawn && 
                    spawnPoint.ValueRO.IsActive)
                {
                    var unitSpawner = UnityEngine.Object.FindObjectOfType<Exoform.Scripts.Hybrid.UnitSpawner>();
                    if (unitSpawner != null)
                    {
                        var playerEntity = unitSpawner.SpawnUnitAtPosition(
                            spawnPoint.ValueRO.Position, 
                            UnitType.Infantry, 
                            1);

                        if (playerEntity != Entity.Null)
                        {
                            Debug.Log($"👤 Игрок {playerId} заспаунен в {spawnPoint.ValueRO.Position}");
                            spawnPoint.ValueRW.LastUsedTime = (float)SystemAPI.Time.ElapsedTime;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}