using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;

namespace PandemicWars.Scripts.Ecs.Systems.UnitLogicSystems
{
    /// <summary>
    /// Система для очистки старых целей движения
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class MovementTargetCleanupSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            const float targetLifetime = 30f; // Цели живут 30 секунд

            // Собираем Entity для удаления
            var entitiesToDestroy = new Unity.Collections.NativeList<Entity>(Unity.Collections.Allocator.Temp);

            Entities
                .WithAll<MovementTargetTag>()
                .ForEach((Entity entity, in MovementTargetTag target) =>
                {
                    if (currentTime - target.CreationTime > targetLifetime)
                    {
                        entitiesToDestroy.Add(entity);
                    }
                })
                .WithoutBurst()
                .Run();

            // Удаляем собранные Entity
            if (entitiesToDestroy.Length > 0)
            {
                EntityManager.DestroyEntity(entitiesToDestroy.AsArray());
                UnityEngine.Debug.Log($"Удалено {entitiesToDestroy.Length} старых целей движения");
            }

            entitiesToDestroy.Dispose();
        }
    }
}