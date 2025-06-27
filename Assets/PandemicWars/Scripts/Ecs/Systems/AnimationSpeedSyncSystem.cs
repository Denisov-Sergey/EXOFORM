using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;

namespace PandemicWars.Scripts.Ecs.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class AnimationSpeedSyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Новый синтаксис DOTS 1.0+
            foreach (var (speedSync, navAgent, localTransform, animatorComp, entity) in
                SystemAPI.Query<RefRW<AnimationSpeedSyncComponent>, RefRO<NavAgentComponent>, RefRO<LocalTransform>, UnitAnimatorComponent>()
                    .WithEntityAccess())
            {
                if (animatorComp?.Animator == null) continue;

                UpdateAnimationSpeed(
                    ref speedSync.ValueRW, 
                    animatorComp, 
                    navAgent.ValueRO, 
                    localTransform.ValueRO, 
                    deltaTime
                );
            }
        }

        private void UpdateAnimationSpeed(
            ref AnimationSpeedSyncComponent speedSync,
            UnitAnimatorComponent animatorComp,
            NavAgentComponent navAgent,
            LocalTransform localTransform,
            float deltaTime)
        {
            if (!speedSync.SyncMoveSpeedWithAnimation || animatorComp?.Animator == null) return;

            // Вычисляем текущую скорость (нужно добавить предыдущую позицию в компонент)
            float currentSpeed = navAgent.MovementSpeed; // Упрощенно используем заданную скорость

            float targetAnimationSpeed = navAgent.MovementSpeed > 0
                ? (currentSpeed / navAgent.MovementSpeed) * speedSync.BaseAnimationSpeed
                : speedSync.BaseAnimationSpeed;

            targetAnimationSpeed = math.clamp(targetAnimationSpeed, 0.1f, 3f);

            speedSync.CurrentAnimationSpeed = Mathf.SmoothDamp(
                speedSync.CurrentAnimationSpeed,
                targetAnimationSpeed,
                ref speedSync.SpeedVelocity,
                speedSync.SpeedSmoothTime
            );

            float finalSpeed = speedSync.CurrentAnimationSpeed * animatorComp.AnimationSpeedMultiplier;
            if (animatorComp.Animator.enabled)
            {
                animatorComp.Animator.speed = math.max(finalSpeed, 0.1f);
            }
        }
    }
}