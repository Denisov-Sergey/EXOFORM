using Exoform.Scripts.Ecs.Components;
using Exoform.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Exoform.Scripts.Ecs.Systems.UnitLogicSystems
{
    /// <summary>
    /// МИНИМАЛЬНАЯ система отладки без GUI функций
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class MinimalNavDebugSystem : SystemBase
    {
        private float lastLogTime = 0f;
        private const float logInterval = 3f; // Логи каждые 3 секунды

        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            // Ограничиваем частоту логов
            if (currentTime - lastLogTime < logInterval)
                return;
                
            lastLogTime = currentTime;

            // Только рисуем Debug.DrawLine (без GUI)
            Entities
                .WithAll<NavAgentComponent, PlayerUnitComponent>()
                .ForEach((Entity entity, in NavAgentComponent navAgent, in LocalTransform transform, 
                         in PlayerUnitComponent playerUnit) =>
                {
                    // Рисуем только Debug.DrawLine - это безопасно
                    DrawMinimalDebug(entity, navAgent, transform, playerUnit);
                })
                .WithoutBurst()
                .Run();
        }

        private void DrawMinimalDebug(Entity entity, NavAgentComponent navAgent, LocalTransform transform, PlayerUnitComponent playerUnit)
        {
            var position = transform.Position;

            // Только для выбранных юнитов и только Debug.DrawLine
            if (!playerUnit.IsSelected) return;

            // Логи только в консоль (раз в 3 секунды)
            Debug.Log($"Unit {entity.Index}: Target={navAgent.TargetEntity != Entity.Null}, Path={navAgent.PathCalculated}, Waypoint={navAgent.CurrentWaypoint}");

            // Рисуем путь (только Debug.DrawLine)
            if (EntityManager.HasBuffer<WaypointBuffer>(entity))
            {
                var waypointBuffer = EntityManager.GetBuffer<WaypointBuffer>(entity);
                
                if (waypointBuffer.Length > 0)
                {
                    var currentPos = position;
                    for (int i = 0; i < waypointBuffer.Length; i++)
                    {
                        var waypoint = waypointBuffer[i].waypoint;
                        Color lineColor = (i == navAgent.CurrentWaypoint) ? Color.yellow : Color.green;
                        Debug.DrawLine(currentPos, waypoint, lineColor, logInterval);
                        currentPos = waypoint;
                    }
                }
            }

            // Рисуем линию к цели
            if (navAgent.TargetEntity != Entity.Null && EntityManager.Exists(navAgent.TargetEntity))
            {
                if (EntityManager.HasComponent<LocalTransform>(navAgent.TargetEntity))
                {
                    var targetTransform = EntityManager.GetComponentData<LocalTransform>(navAgent.TargetEntity);
                    Debug.DrawLine(position, targetTransform.Position, Color.red, logInterval);
                }
            }
        }
    }
}