using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;

namespace PandemicWars.Scripts.Ecs.Systems.UnitLogicSystems
{
    /// <summary>
    /// Система для управления состоянием юнитов
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class UnitStateSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            Entities
                .ForEach((ref UnitStateComponent unitState, in NavAgentComponent navAgent) =>
                {
                    // Встраиваем логику UpdateUnitState прямо в лямбду
                    UnitState newState = unitState.CurrentState;

                    // Определяем новое состояние на основе навигации
                    if (navAgent.PathCalculated && navAgent.TargetEntity != Entity.Null)
                    {
                        newState = UnitState.Moving;
                    }
                    else
                    {
                        newState = UnitState.Idle;
                    }

                    // Обновляем состояние если оно изменилось
                    if (newState != unitState.CurrentState)
                    {
                        unitState.CurrentState = newState;
                        unitState.StateChangeTime = currentTime;
                    }
                })
                .Run();
        }
    }
}