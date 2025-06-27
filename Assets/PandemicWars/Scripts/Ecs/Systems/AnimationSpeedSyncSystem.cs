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
            var entityManager = EntityManager;

            foreach (var (speedSync, navAgent, localTransform, entity) in
                SystemAPI.Query<RefRW<AnimationSpeedSyncComponent>, RefRO<NavAgentComponent>, RefRO<LocalTransform>>()
                    .WithEntityAccess())
            {
                if (entityManager.HasComponent<UnitAnimationComponent>(entity))
                {
                    var animComp = entityManager.GetComponentObject<UnitAnimationComponent>(entity);
                    UpdateAnimationSpeed(ref speedSync.ValueRW, animComp, navAgent.ValueRO, localTransform.ValueRO, deltaTime);
                }
            }
        }

        private void UpdateAnimationSpeed(
            ref AnimationSpeedSyncComponent speedSync,
            UnitAnimationComponent animComp,
            NavAgentComponent navAgent,
            LocalTransform localTransform,
            float deltaTime)
        {
            if (!speedSync.SyncMoveSpeedWithAnimation || animComp?.Animator == null) return;

            float currentSpeed = 0f;
            if (animComp.HasPreviousPosition)
            {
                float distanceMoved = math.distance(localTransform.Position, animComp.PreviousPosition);
                currentSpeed = distanceMoved / deltaTime;
            }

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

            float finalSpeed = speedSync.CurrentAnimationSpeed * animComp.AnimationSpeedMultiplier;
            if (animComp.Animator.enabled)
            {
                animComp.Animator.speed = math.max(finalSpeed, 0.1f);
            }

            animComp.PreviousPosition = localTransform.Position;
            animComp.HasPreviousPosition = true;
        }
    }
}
