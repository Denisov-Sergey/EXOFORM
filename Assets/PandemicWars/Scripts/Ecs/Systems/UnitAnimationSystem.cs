using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;

namespace PandemicWars.Scripts.Ecs.Systems
{
    /// <summary>
    /// Основная система анимаций для юнитов - автоматически переключает анимации на основе состояния
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class UnitAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Обрабатываем игровых юнитов
            Entities
                .WithAll<PlayerUnitComponent>()
                .ForEach((Entity entity, UnitAnimationComponent animComp, in NavAgentComponent navAgent, 
                         in LocalTransform transform) =>
                {
                    UpdateUnitAnimation(entity, animComp, navAgent, transform, deltaTime, currentTime);
                })
                .WithoutBurst()
                .Run();

            // Обрабатываем вражеских юнитов
            Entities
                .WithAll<EnemyUnitComponent>()
                .ForEach((Entity entity, UnitAnimationComponent animComp, in NavAgentComponent navAgent, 
                         in LocalTransform transform) =>
                {
                    UpdateUnitAnimation(entity, animComp, navAgent, transform, deltaTime, currentTime);
                })
                .WithoutBurst()
                .Run();
        }

        private void UpdateUnitAnimation(Entity entity, UnitAnimationComponent animComp, 
            NavAgentComponent navAgent, LocalTransform transform, float deltaTime, float currentTime)
        {
            if (animComp == null || animComp.Animator == null) return;

            // Определяем текущее состояние юнита
            UnitAnimationState newState = DetermineAnimationState(navAgent, transform, animComp);

            // Обновляем состояние если изменилось
            if (newState != animComp.CurrentState)
            {
                ChangeAnimationState(animComp, newState, entity, currentTime);
            }

            // Обновляем параметры анимации
            UpdateAnimationParameters(animComp, navAgent, transform, deltaTime);
        }

        private UnitAnimationState DetermineAnimationState(NavAgentComponent navAgent, 
            LocalTransform transform, UnitAnimationComponent animComp)
        {
            // Проверяем наличие цели и движения
            bool hasTarget = navAgent.TargetEntity != Entity.Null;
            bool isMoving = navAgent.PathCalculated && hasTarget;
            
            // Проверяем скорость движения (если есть предыдущая позиция)
            bool actuallyMoving = false;
            if (animComp.HasPreviousPosition)
            {
                float distanceMoved = math.distance(transform.Position, animComp.PreviousPosition);
                actuallyMoving = distanceMoved > 0.01f; // Минимальный порог движения
            }

            // Определяем состояние
            if (isMoving && actuallyMoving)
            {
                return UnitAnimationState.Moving;
            }
            else if (hasTarget && !actuallyMoving)
            {
                return UnitAnimationState.Idle; // Есть цель, но не движется (пересчет пути и т.д.)
            }
            else
            {
                return UnitAnimationState.Idle;
            }
        }

        private void ChangeAnimationState(UnitAnimationComponent animComp, UnitAnimationState newState, 
            Entity entity, float currentTime)
        {
            if (animComp.Animator == null)
            {
                Debug.LogWarning($"Animator не найден для entity {entity.Index}");
                return;
            }

            // Записываем предыдущее состояние
            animComp.PreviousState = animComp.CurrentState;
            animComp.CurrentState = newState;
            animComp.StateChangeTime = currentTime;

            // Переключаем анимацию
            switch (newState)
            {
                case UnitAnimationState.Idle:
                    PlayAnimation(animComp.Animator, animComp.IdleAnimationName, animComp.TransitionSpeed);
                    break;

                case UnitAnimationState.Moving:
                    PlayAnimation(animComp.Animator, animComp.MoveAnimationName, animComp.TransitionSpeed);
                    break;

                case UnitAnimationState.Attacking:
                    PlayAnimation(animComp.Animator, animComp.AttackAnimationName, animComp.TransitionSpeed);
                    break;

                case UnitAnimationState.Dead:
                    PlayAnimation(animComp.Animator, animComp.DeathAnimationName, animComp.TransitionSpeed);
                    break;
            }

            Debug.Log($"Entity {entity.Index}: Animation {animComp.PreviousState} → {newState}");
        }

        private void PlayAnimation(Animator animator, string animationName, float transitionSpeed)
        {
            if (animator != null && !string.IsNullOrEmpty(animationName))
            {
                animator.CrossFade(animationName, transitionSpeed);
            }
        }

        private void UpdateAnimationParameters(UnitAnimationComponent animComp, NavAgentComponent navAgent, 
            LocalTransform transform, float deltaTime)
        {
            if (animComp?.Animator == null) return;

            // Обновляем параметры аниматора
            
            // Скорость движения
            float currentSpeed = 0f;
            if (animComp.HasPreviousPosition)
            {
                float distanceMoved = math.distance(transform.Position, animComp.PreviousPosition);
                currentSpeed = distanceMoved / deltaTime;
            }

            // Нормализуем скорость (0-1)
            float normalizedSpeed = math.clamp(currentSpeed / navAgent.MovementSpeed, 0f, 1f);
            
            // Устанавливаем параметры в аниматор
            if (!string.IsNullOrEmpty(animComp.SpeedParameterName))
            {
                animComp.Animator.SetFloat(animComp.SpeedParameterName, normalizedSpeed);
            }

            if (!string.IsNullOrEmpty(animComp.IsMovingParameterName))
            {
                animComp.Animator.SetBool(animComp.IsMovingParameterName, normalizedSpeed > 0.1f);
            }

            // Сохраняем текущую позицию для следующего кадра
            animComp.PreviousPosition = transform.Position;
            animComp.HasPreviousPosition = true;
        }
    }
}