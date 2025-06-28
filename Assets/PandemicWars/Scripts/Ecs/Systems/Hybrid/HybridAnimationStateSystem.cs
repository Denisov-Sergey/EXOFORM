using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.Hybrid;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnitAnimationState = PandemicWars.Scripts.Ecs.Components.Hybrid.UnitAnimationState;

namespace PandemicWars.Scripts.Ecs.Systems.Hybrid
{
    /// <summary>
    /// Система обновления состояния анимации на основе DOTS данных
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(UnitLogicSystems.NavAgentSystem))]
    public partial class HybridAnimationStateSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Обновляем состояние анимации для всех юнитов
            foreach (var (animState, navAgent, playerUnit, transform) in 
                     SystemAPI.Query<RefRW<AnimationStateComponent>, 
                         RefRO<NavAgentComponent>, 
                         RefRO<PlayerUnitComponent>, 
                         RefRO<LocalTransform>>())
            {
                // Определяем состояние на основе навигации
                UnitAnimationState newState = DetermineAnimationState(navAgent.ValueRO, animState.ValueRO, currentTime);
                
                if (newState != animState.ValueRO.CurrentState)
                {
                    animState.ValueRW.PreviousState = animState.ValueRO.CurrentState;
                    animState.ValueRW.CurrentState = newState;
                    animState.ValueRW.StateChangeTime = currentTime;
                }

                // Вычисляем скорость движения
                animState.ValueRW.MovementSpeed = CalculateMovementSpeed(navAgent.ValueRO, animState.ValueRO, deltaTime);
                
                // Обновляем состояние выбора
                animState.ValueRW.IsSelected = playerUnit.ValueRO.IsSelected;

                // Сбрасываем триггеры
                if (currentTime - animState.ValueRO.StateChangeTime > 0.1f)
                {
                    animState.ValueRW.TriggerAttack = false;
                    animState.ValueRW.TriggerDeath = false;
                }
            }
        }

        private UnitAnimationState DetermineAnimationState(NavAgentComponent navAgent,
            AnimationStateComponent animState,
            float currentTime)
        {
            // Если юнит мертв
            if (animState.Health <= 0)
                return UnitAnimationState.Dead;

            // Если юнит движется
            if (navAgent.PathCalculated && navAgent.TargetEntity != Entity.Null)
            {
                return UnitAnimationState.Moving;
            }

            // По умолчанию - idle
            return UnitAnimationState.Idle;
        }

        private float CalculateMovementSpeed(NavAgentComponent navAgent,
            AnimationStateComponent animState,
            float deltaTime)
        {
            if (navAgent.PathCalculated && navAgent.TargetEntity != Entity.Null)
            {
                return navAgent.MovementSpeed;
            }

            // Плавно уменьшаем скорость когда останавливаемся
            return math.lerp(animState.MovementSpeed, 0f, deltaTime * 5f);
        }
    }
}