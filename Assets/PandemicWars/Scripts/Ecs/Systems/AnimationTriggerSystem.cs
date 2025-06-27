using Unity.Entities;
using UnityEngine;
using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;

namespace PandemicWars.Scripts.Ecs.Systems
{
    /// <summary>
    /// Система для обработки триггеров анимации (атака, смерть, специальные действия)
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class AnimationTriggerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var entityManager = EntityManager;

            // Используем Entities.ForEach для работы с управляемыми компонентами
            Entities
                .WithAll<UnitAnimationComponent>()
                .ForEach((Entity entity, ref AnimationTriggerComponent triggers) =>
                {
                    var animComp = entityManager.GetComponentObject<UnitAnimationComponent>(entity);
                    ProcessAnimationTriggers(entity, ref triggers, animComp, currentTime);
                })
                .WithoutBurst()
                .Run();
        }

        private void ProcessAnimationTriggers(Entity entity, ref AnimationTriggerComponent triggers, 
            UnitAnimationComponent animComp, float currentTime)
        {
            if (animComp?.Animator == null) return;

            // Обрабатываем триггер атаки
            if (triggers.TriggerAttack)
            {
                TriggerAttackAnimation(animComp, entity, currentTime);
                triggers.TriggerAttack = false;
                triggers.TriggerTime = currentTime;
            }

            // Обрабатываем триггер смерти
            if (triggers.TriggerDeath)
            {
                TriggerDeathAnimation(animComp, entity, currentTime);
                triggers.TriggerDeath = false;
                triggers.TriggerTime = currentTime;
            }

            // Обрабатываем триггер выбора
            if (triggers.TriggerSelection)
            {
                TriggerSelectionAnimation(animComp, entity, currentTime);
                triggers.TriggerSelection = false;
                triggers.TriggerTime = currentTime;
            }

            // Обрабатываем триггер получения урона
            if (triggers.TriggerHit)
            {
                TriggerHitAnimation(animComp, entity, currentTime);
                triggers.TriggerHit = false;
                triggers.TriggerTime = currentTime;
            }
        }

        private void TriggerAttackAnimation(UnitAnimationComponent animComp, Entity entity, float currentTime)
        {
            if (!string.IsNullOrEmpty(animComp.TriggerAttackParameterName))
            {
                animComp.Animator.SetTrigger(animComp.TriggerAttackParameterName);
            }
            
            // Принудительно переключаем на состояние атаки
            animComp.PreviousState = animComp.CurrentState;
            animComp.CurrentState = UnitAnimationState.Attacking;
            animComp.StateChangeTime = currentTime;
            
            Debug.Log($"Entity {entity.Index}: Attack animation triggered");
        }

        private void TriggerDeathAnimation(UnitAnimationComponent animComp, Entity entity, float currentTime)
        {
            if (!string.IsNullOrEmpty(animComp.TriggerDeathParameterName))
            {
                animComp.Animator.SetTrigger(animComp.TriggerDeathParameterName);
            }
            
            // Принудительно переключаем на состояние смерти
            animComp.PreviousState = animComp.CurrentState;
            animComp.CurrentState = UnitAnimationState.Dead;
            animComp.StateChangeTime = currentTime;
            
            Debug.Log($"Entity {entity.Index}: Death animation triggered");
        }

        private void TriggerSelectionAnimation(UnitAnimationComponent animComp, Entity entity, float currentTime)
        {
            if (!string.IsNullOrEmpty(animComp.SelectionAnimationName))
            {
                animComp.Animator.Play(animComp.SelectionAnimationName);
            }
            
            Debug.Log($"Entity {entity.Index}: Selection animation triggered");
        }

        private void TriggerHitAnimation(UnitAnimationComponent animComp, Entity entity, float currentTime)
        {
            // Можно добавить параметр для анимации получения урона
            // Например, короткая анимация реакции на удар
            Debug.Log($"Entity {entity.Index}: Hit animation triggered");
        }
    }

    /// <summary>
    /// Вспомогательные методы для запуска триггеров анимации из других систем
    /// </summary>
    public static class AnimationTriggerHelper
    {
        /// <summary>
        /// Запускает анимацию атаки для указанного юнита
        /// </summary>
        public static void TriggerAttack(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<AnimationTriggerComponent>(entity))
            {
                var triggers = entityManager.GetComponentData<AnimationTriggerComponent>(entity);
                triggers.TriggerAttack = true;
                entityManager.SetComponentData(entity, triggers);
            }
        }

        /// <summary>
        /// Запускает анимацию смерти для указанного юнита
        /// </summary>
        public static void TriggerDeath(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<AnimationTriggerComponent>(entity))
            {
                var triggers = entityManager.GetComponentData<AnimationTriggerComponent>(entity);
                triggers.TriggerDeath = true;
                entityManager.SetComponentData(entity, triggers);
            }
        }

        /// <summary>
        /// Запускает анимацию выбора для указанного юнита
        /// </summary>
        public static void TriggerSelection(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<AnimationTriggerComponent>(entity))
            {
                var triggers = entityManager.GetComponentData<AnimationTriggerComponent>(entity);
                triggers.TriggerSelection = true;
                entityManager.SetComponentData(entity, triggers);
            }
        }

        /// <summary>
        /// Запускает анимацию получения урона для указанного юнита
        /// </summary>
        public static void TriggerHit(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<AnimationTriggerComponent>(entity))
            {
                var triggers = entityManager.GetComponentData<AnimationTriggerComponent>(entity);
                triggers.TriggerHit = true;
                entityManager.SetComponentData(entity, triggers);
            }
        }
    }
}