using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;

namespace PandemicWars.Scripts.Ecs.Systems
{
    /// <summary>
    /// Простая система анимаций для юнитов - начинаем с базового функционала
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SimpleUnitAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Обрабатываем все юниты с анимационными компонентами - новый синтаксис DOTS 1.0+
            foreach (var (animState, animatorComp, transform, entity) in 
                SystemAPI.Query<RefRW<UnitAnimationComponent>, UnitAnimatorComponent, RefRO<LocalTransform>>()
                    .WithEntityAccess())
            {
                if (animatorComp?.Animator == null)
                {
                    Debug.LogWarning($"Animator отсутствует для Entity {entity.Index}");
                    continue;
                }

                // Определяем нужное состояние анимации
                UnitAnimationState targetState = DetermineTargetAnimationState(entity, animState.ValueRO, transform.ValueRO);

                // Переключаем анимацию если состояние изменилось
                if (targetState != animState.ValueRO.CurrentState)
                {
                    ChangeAnimationState(ref animState.ValueRW, targetState, animatorComp, currentTime, entity);
                }

                // Обновляем параметры анимации
                UpdateAnimationParameters(ref animState.ValueRW, animatorComp, transform.ValueRO, deltaTime);
            }
        }

        private UnitAnimationState DetermineTargetAnimationState(Entity entity, UnitAnimationComponent animState, LocalTransform transform)
        {
            // Проверяем есть ли компонент навигации
            if (EntityManager.HasComponent<NavAgentComponent>(entity))
            {
                var navAgent = EntityManager.GetComponentData<NavAgentComponent>(entity);
                
                // Проверяем движение
                bool hasTarget = navAgent.TargetEntity != Entity.Null;
                bool pathCalculated = navAgent.PathCalculated;
                
                // Проверяем реальное движение по изменению позиции
                bool actuallyMoving = false;
                if (animState.HasPreviousPosition)
                {
                    float distanceMoved = math.distance(transform.Position, animState.PreviousPosition);
                    actuallyMoving = distanceMoved > 0.01f; // Минимальный порог движения
                }

                // Определяем состояние
                if (hasTarget && pathCalculated && actuallyMoving)
                {
                    return UnitAnimationState.Moving;
                }
            }

            // По умолчанию - idle
            return UnitAnimationState.Idle;
        }

        private void ChangeAnimationState(ref UnitAnimationComponent animState, 
                                        UnitAnimationState newState, 
                                        UnitAnimatorComponent animatorComp, 
                                        float currentTime,
                                        Entity entity)
        {
            // Записываем изменение состояния
            animState.PreviousState = animState.CurrentState;
            animState.CurrentState = newState;
            animState.StateChangeTime = currentTime;

            Debug.Log($"Entity {entity.Index}: Animation {animState.PreviousState} → {newState}");

            if (animatorComp?.Animator == null)
            {
                Debug.LogError($"Entity {entity.Index}: Animator is null in ChangeAnimationState!");
                return;
            }

            var animator = animatorComp.Animator;

            // Дополнительные проверки
            if (!animator.enabled)
            {
                Debug.LogWarning($"Entity {entity.Index}: Animator is disabled! Enabling...");
                animator.enabled = true;
            }

            if (!animator.gameObject.activeInHierarchy)
            {
                Debug.LogError($"Entity {entity.Index}: GameObject is not active in hierarchy!");
                return;
            }

            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"Entity {entity.Index}: RuntimeAnimatorController is null!");
                return;
            }

            // Переключаем анимацию
            string animationName = "";
            switch (newState)
            {
                case UnitAnimationState.Idle:
                    animationName = animatorComp.IdleAnimationName;
                    break;

                case UnitAnimationState.Moving:
                    animationName = animatorComp.MoveAnimationName;
                    break;

                case UnitAnimationState.Attacking:
                    animationName = animatorComp.AttackAnimationName;
                    break;

                case UnitAnimationState.Dead:
                    animationName = animatorComp.DeathAnimationName;
                    break;

                default:
                    // Fallback на Idle
                    animationName = animatorComp.IdleAnimationName;
                    break;
            }

            Debug.Log($"Entity {entity.Index}: Trying to play animation '{animationName}'");

            // Пробуем несколько методов воспроизведения
            bool success = false;

            // Метод 1: Play
            try
            {
                animator.Play(animationName, 0, 0f);
                Debug.Log($"Entity {entity.Index}: Play() method SUCCESS for '{animationName}'");
                success = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Entity {entity.Index}: Play() method failed: {e.Message}");
            }

            // Метод 2: CrossFade (если Play не сработал)
            if (!success)
            {
                try
                {
                    animator.CrossFade(animationName, animatorComp.TransitionSpeed);
                    Debug.Log($"Entity {entity.Index}: CrossFade() method SUCCESS for '{animationName}'");
                    success = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Entity {entity.Index}: CrossFade() method failed: {e.Message}");
                }
            }

            // Метод 3: Если ничего не сработало, пробуем первую доступную анимацию
            if (!success)
            {
                Debug.LogError($"Entity {entity.Index}: Animation '{animationName}' not found! Trying first available animation...");
                
                if (animator.runtimeAnimatorController.animationClips.Length > 0)
                {
                    var firstClip = animator.runtimeAnimatorController.animationClips[0];
                    try
                    {
                        animator.Play(firstClip.name);
                        Debug.Log($"Entity {entity.Index}: Fallback SUCCESS - playing '{firstClip.name}'");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Entity {entity.Index}: Even fallback failed: {e.Message}");
                    }
                }
            }

            // Выводим состояние аниматора после попытки воспроизведения
            if (animator.layerCount > 0)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"Entity {entity.Index}: Current state hash: {stateInfo.shortNameHash}, Length: {stateInfo.length}");
            }
        }

        private void PlayAnimation(Animator animator, string animationName, float transitionSpeed)
        {
            if (animator == null)
            {
                Debug.LogError("Animator is null!");
                return;
            }

            if (string.IsNullOrEmpty(animationName))
            {
                Debug.LogWarning($"Animation name is empty or null!");
                return;
            }

            // КРИТИЧЕСКИЕ исправления перед воспроизведением
            if (animator.cullingMode != AnimatorCullingMode.AlwaysAnimate)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Debug.Log($"Fixed cullingMode to AlwaysAnimate");
            }

            if (!animator.enabled)
            {
                animator.enabled = true;
                Debug.Log($"Enabled Animator");
            }

            try
            {
                // Проверяем, что анимация существует
                if (HasAnimation(animator, animationName))
                {
                    // Метод 1: Play с принудительным обновлением
                    animator.Play(animationName, 0, 0f);
                    animator.Update(0f); // Принудительное обновление!
                    
                    Debug.Log($"✓ Playing animation: {animationName}");
                    
                    // Проверяем результат
                    if (animator.layerCount > 0)
                    {
                        var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                        int expectedHash = Animator.StringToHash(animationName);
                        
                        if (stateInfo.shortNameHash == expectedHash)
                        {
                            Debug.Log($"✓ Animation {animationName} is now playing! (hash: {stateInfo.shortNameHash})");
                        }
                        else
                        {
                            Debug.LogWarning($"✗ Animation {animationName} not playing. Expected hash: {expectedHash}, actual: {stateInfo.shortNameHash}");
                            
                            // Пробуем CrossFade как fallback
                            animator.CrossFade(animationName, transitionSpeed);
                            animator.Update(0f);
                            Debug.Log($"Tried CrossFade as fallback");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"Animation '{animationName}' not found in Animator Controller!");
                    
                    // Пытаемся проиграть любую доступную анимацию
                    if (animator.runtimeAnimatorController != null && 
                        animator.runtimeAnimatorController.animationClips.Length > 0)
                    {
                        var firstClip = animator.runtimeAnimatorController.animationClips[0];
                        Debug.Log($"Fallback: Playing first available animation: {firstClip.name}");
                        animator.Play(firstClip.name);
                        animator.Update(0f);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error playing animation '{animationName}': {e.Message}");
            }
        }

        private bool HasAnimation(Animator animator, string animationName)
        {
            if (animator.runtimeAnimatorController == null) return false;
            
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == animationName)
                    return true;
            }
            return false;
        }

        private void UpdateAnimationParameters(ref UnitAnimationComponent animState, 
                                             UnitAnimatorComponent animatorComp, 
                                             LocalTransform transform, 
                                             float deltaTime)
        {
            var animator = animatorComp.Animator;
            if (animator == null) return;

            // Вычисляем скорость движения
            float currentSpeed = 0f;
            if (animState.HasPreviousPosition)
            {
                float distanceMoved = math.distance(transform.Position, animState.PreviousPosition);
                currentSpeed = distanceMoved / deltaTime;
            }

            // Обновляем параметры в аниматоре (если они существуют)
            UpdateAnimatorParameter(animator, animatorComp.SpeedParameterName, currentSpeed);
            UpdateAnimatorParameter(animator, animatorComp.IsMovingParameterName, currentSpeed > 0.1f);

            // Сохраняем позицию для следующего кадра
            animState.PreviousPosition = transform.Position;
            animState.HasPreviousPosition = true;
        }

        private void UpdateAnimatorParameter(Animator animator, string parameterName, float value)
        {
            if (string.IsNullOrEmpty(parameterName)) return;
            
            try
            {
                if (HasParameter(animator, parameterName))
                {
                    animator.SetFloat(parameterName, value);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to set float parameter '{parameterName}': {e.Message}");
            }
        }

        private void UpdateAnimatorParameter(Animator animator, string parameterName, bool value)
        {
            if (string.IsNullOrEmpty(parameterName)) return;
            
            try
            {
                if (HasParameter(animator, parameterName))
                {
                    animator.SetBool(parameterName, value);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to set bool parameter '{parameterName}': {e.Message}");
            }
        }

        private bool HasParameter(Animator animator, string parameterName)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName)) return false;
            
            foreach (var param in animator.parameters)
            {
                if (param.name == parameterName)
                    return true;
            }
            return false;
        }
    }
}