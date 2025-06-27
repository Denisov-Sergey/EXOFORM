using Unity.Entities;
using UnityEngine;
using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;

namespace PandemicWars.Scripts.Ecs.Systems
{
    /// <summary>
    /// Система отладки анимаций - выводит статистику и диагностическую информацию
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class AnimationDebugSystem : SystemBase
    {
        private float lastLogTime = 0f;
        private const float logInterval = 5f; // Логи каждые 5 секунд

        [Header("Debug Settings")]
        public bool enableDebugLogs = true;
        public bool enableDetailedLogs = false;

        protected override void OnUpdate()
        {
            if (!enableDebugLogs) return;

            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            
            if (currentTime - lastLogTime < logInterval) return;
            lastLogTime = currentTime;

            // Собираем статистику анимаций
            var stats = new AnimationStatistics();

            Entities
                .ForEach((Entity entity, UnitAnimationComponent animComp) =>
                {
                    CollectAnimationStatistics(entity, animComp, ref stats);
                })
                .WithoutBurst()
                .Run();

            // Выводим статистику
            LogAnimationStatistics(stats);
        }

        private void CollectAnimationStatistics(Entity entity, UnitAnimationComponent animComp, ref AnimationStatistics stats)
        {
            if (animComp == null) return;

            stats.TotalUnits++;

            // Проверяем наличие Animator
            if (animComp.Animator == null)
            {
                stats.UnitsWithoutAnimator++;
                if (enableDetailedLogs)
                {
                    Debug.LogWarning($"Entity {entity.Index}: Missing Animator component");
                }
                return;
            }

            stats.UnitsWithAnimator++;

            // Собираем статистику по состояниям
            switch (animComp.CurrentState)
            {
                case UnitAnimationState.Idle:
                    stats.IdleUnits++;
                    break;
                case UnitAnimationState.Moving:
                    stats.MovingUnits++;
                    break;
                case UnitAnimationState.Attacking:
                    stats.AttackingUnits++;
                    break;
                case UnitAnimationState.Dead:
                    stats.DeadUnits++;
                    break;
            }

            // Проверяем работоспособность Animator
            if (animComp.Animator.runtimeAnimatorController == null)
            {
                stats.UnitsWithMissingController++;
                if (enableDetailedLogs)
                {
                    Debug.LogWarning($"Entity {entity.Index}: Missing Animator Controller");
                }
            }
        }

        private void LogAnimationStatistics(AnimationStatistics stats)
        {
            if (stats.TotalUnits == 0)
            {
                Debug.Log("Animation Debug: No units with animation components found");
                return;
            }

            string report = $"=== ANIMATION SYSTEM STATISTICS ===\n" +
                           $"Total Units: {stats.TotalUnits}\n" +
                           $"Units with Animator: {stats.UnitsWithAnimator}\n" +
                           $"Units without Animator: {stats.UnitsWithoutAnimator}\n" +
                           $"Units with missing Controller: {stats.UnitsWithMissingController}\n" +
                           $"\nAnimation States:\n" +
                           $"- Idle: {stats.IdleUnits}\n" +
                           $"- Moving: {stats.MovingUnits}\n" +
                           $"- Attacking: {stats.AttackingUnits}\n" +
                           $"- Dead: {stats.DeadUnits}\n" +
                           $"=====================================";

            Debug.Log(report);

            // Предупреждения о проблемах
            if (stats.UnitsWithoutAnimator > 0)
            {
                Debug.LogWarning($"Found {stats.UnitsWithoutAnimator} units without Animator component!");
            }

            if (stats.UnitsWithMissingController > 0)
            {
                Debug.LogWarning($"Found {stats.UnitsWithMissingController} units with missing Animator Controller!");
            }
        }

        /// <summary>
        /// Структура для сбора статистики анимаций
        /// </summary>
        private struct AnimationStatistics
        {
            public int TotalUnits;
            public int UnitsWithAnimator;
            public int UnitsWithoutAnimator;
            public int UnitsWithMissingController;
            
            public int IdleUnits;
            public int MovingUnits;
            public int AttackingUnits;
            public int DeadUnits;
        }
    }

    /// <summary>
    /// Вспомогательная система для визуальной отладки анимаций в Scene View
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class AnimationVisualDebugSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Эта система работает только в редакторе для визуальной отладки
            #if UNITY_EDITOR
            
            Entities
                .ForEach((Entity entity, UnitAnimationComponent animComp, in Unity.Transforms.LocalTransform transform) =>
                {
                    DrawAnimationDebugInfo(entity, animComp, transform);
                })
                .WithoutBurst()
                .Run();
                
            #endif
        }

        #if UNITY_EDITOR
        private void DrawAnimationDebugInfo(Entity entity, UnitAnimationComponent animComp, Unity.Transforms.LocalTransform transform)
        {
            if (animComp?.Animator == null) return;

            var position = transform.Position;
            
            // Рисуем состояние анимации над юнитом
            string stateText = $"State: {animComp.CurrentState}";
            if (animComp.Animator.runtimeAnimatorController != null)
            {
                stateText += $"\nController: {animComp.Animator.runtimeAnimatorController.name}";
            }
            
            // Цвет зависит от состояния
            Color stateColor = animComp.CurrentState switch
            {
                UnitAnimationState.Idle => Color.white,
                UnitAnimationState.Moving => Color.green,
                UnitAnimationState.Attacking => Color.red,
                UnitAnimationState.Dead => Color.black,
                _ => Color.gray
            };

            // Рисуем сферу состояния
            UnityEngine.Gizmos.color = stateColor;
            UnityEngine.Gizmos.DrawWireSphere(position + new Unity.Mathematics.float3(0, 2, 0), 0.2f);
        }
        #endif
    }
}