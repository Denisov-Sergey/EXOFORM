using UnityEngine;
using Unity.Entities;
using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;

namespace PandemicWars.Scripts.Ecs.Authoring
{
    /// <summary>
    /// Authoring компонент для настройки анимаций юнита
    /// </summary>
    public class UnitAnimationAuthoring : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] public Animator animator; // Сделал public
        [SerializeField] private bool autoFindAnimator = true;

        [Header("Animation Names")]
        [SerializeField] public string idleAnimationName = "Idle"; // Сделал public
        [SerializeField] public string moveAnimationName = "Move"; // Сделал public
        [SerializeField] public string attackAnimationName = "Attack"; // Сделал public
        [SerializeField] public string deathAnimationName = "Death"; // Сделал public
        [SerializeField] public string selectionAnimationName = "Selected"; // Сделал public

        [Header("Animator Parameters")]
        [SerializeField] public string speedParameterName = "Speed"; // Сделал public
        [SerializeField] public string isMovingParameterName = "IsMoving"; // Сделал public
        [SerializeField] public string isSelectedParameterName = "IsSelected"; // Сделал public
        [SerializeField] public string triggerAttackParameterName = "TriggerAttack"; // Сделал public
        [SerializeField] public string triggerDeathParameterName = "TriggerDeath"; // Сделал public

        [Header("Settings")]
        [SerializeField] private float transitionSpeed = 0.2f;
        [SerializeField] private bool useRootMotion = false;
        [SerializeField] private float animationSpeedMultiplier = 1f;

        [Header("Speed Sync")]
        [SerializeField] private bool syncMoveSpeedWithAnimation = true;
        [SerializeField] private float speedSmoothTime = 0.1f;

        // Публичные свойства для доступа
        public string IdleAnimationName => idleAnimationName;
        public string MoveAnimationName => moveAnimationName;
        public string AttackAnimationName => attackAnimationName;
        public string DeathAnimationName => deathAnimationName;
        public string SelectionAnimationName => selectionAnimationName;

        class Baker : Baker<UnitAnimationAuthoring>
        {
            public override void Bake(UnitAnimationAuthoring authoring)
            {
                if (authoring == null) return;

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (entity == Entity.Null) return;

                // Автоматически ищем Animator если не назначен
                Animator targetAnimator = authoring.animator;
                if (targetAnimator == null && authoring.autoFindAnimator)
                {
                    targetAnimator = authoring.GetComponent<Animator>();
                    if (targetAnimator == null)
                    {
                        targetAnimator = authoring.GetComponentInChildren<Animator>();
                    }
                }

                if (targetAnimator == null)
                {
                    Debug.LogWarning($"Animator не найден для {authoring.gameObject.name}!");
                    return;
                }

                // Добавляем компонент анимации как Managed Component
                var animationComponent = new UnitAnimationComponent
                {
                    Animator = targetAnimator,
                    CurrentState = UnitAnimationState.Idle,
                    PreviousState = UnitAnimationState.Idle,
                    
                    IdleAnimationName = authoring.idleAnimationName,
                    MoveAnimationName = authoring.moveAnimationName,
                    AttackAnimationName = authoring.attackAnimationName,
                    DeathAnimationName = authoring.deathAnimationName,
                    SelectionAnimationName = authoring.selectionAnimationName,
                    
                    SpeedParameterName = authoring.speedParameterName,
                    IsMovingParameterName = authoring.isMovingParameterName,
                    IsSelectedParameterName = authoring.isSelectedParameterName,
                    TriggerAttackParameterName = authoring.triggerAttackParameterName,
                    TriggerDeathParameterName = authoring.triggerDeathParameterName,
                    
                    TransitionSpeed = authoring.transitionSpeed,
                    UseRootMotion = authoring.useRootMotion,
                    AnimationSpeedMultiplier = authoring.animationSpeedMultiplier,
                    
                    HasPreviousPosition = false,
                    WasSelected = false
                };

                AddComponentObject(entity, animationComponent);

                // Добавляем компонент триггеров
                AddComponent(entity, new AnimationTriggerComponent
                {
                    TriggerAttack = false,
                    TriggerDeath = false,
                    TriggerSelection = false,
                    TriggerHit = false,
                    TriggerTime = 0f
                });

                // Добавляем компонент синхронизации скорости
                if (authoring.syncMoveSpeedWithAnimation)
                {
                    AddComponent(entity, new AnimationSpeedSyncComponent
                    {
                        SyncMoveSpeedWithAnimation = true,
                        BaseAnimationSpeed = 1f,
                        CurrentAnimationSpeed = 1f,
                        SpeedSmoothTime = authoring.speedSmoothTime,
                        SpeedVelocity = 0f
                    });
                }

                // Настраиваем Animator
                if (targetAnimator != null)
                {
                    targetAnimator.applyRootMotion = authoring.useRootMotion;
                    targetAnimator.speed = authoring.animationSpeedMultiplier;
                }
            }
        }

        /// <summary>
        /// Автоматическое заполнение полей в редакторе
        /// </summary>
        [ContextMenu("Auto Setup")]
        private void AutoSetup()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                Debug.Log($"Animator найден: {animator.runtimeAnimatorController.name}");
                
                // Можно добавить автоматическое определение имен анимаций
                // из AnimatorController, если это необходимо
            }
        }

        /// <summary>
        /// Валидация в редакторе
        /// </summary>
        private void OnValidate()
        {
            transitionSpeed = Mathf.Clamp01(transitionSpeed);
            animationSpeedMultiplier = Mathf.Max(0.1f, animationSpeedMultiplier);
            speedSmoothTime = Mathf.Max(0.01f, speedSmoothTime);

            // Автоматически ищем Animator если включен autoFindAnimator
            if (autoFindAnimator && animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }
        }

        /// <summary>
        /// Отображение в Scene View
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (animator != null)
            {
                // Показываем информацию об аниматоре
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2.5f, 
                    $"Animator: {(animator.runtimeAnimatorController != null ? animator.runtimeAnimatorController.name : "None")}");
                #endif
            }
        }
    }

    /// <summary>
    /// Вспомогательный класс для тестирования анимаций в редакторе
    /// </summary>
    public class AnimationTester : MonoBehaviour
    {
        [Header("Test Controls")]
        [SerializeField] private UnitAnimationAuthoring animationAuthoring;
        [SerializeField] private KeyCode testIdleKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode testMoveKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode testAttackKey = KeyCode.Alpha3;

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (animationAuthoring?.animator == null) return;

            // Тестовые клавиши для проверки анимаций
            if (Input.GetKeyDown(testIdleKey))
            {
                animationAuthoring.animator.CrossFade(animationAuthoring.IdleAnimationName, 0.2f);
                Debug.Log("Test: Idle Animation");
            }
            
            if (Input.GetKeyDown(testMoveKey))
            {
                animationAuthoring.animator.CrossFade(animationAuthoring.MoveAnimationName, 0.2f);
                Debug.Log("Test: Move Animation");
            }
            
            if (Input.GetKeyDown(testAttackKey))
            {
                animationAuthoring.animator.CrossFade(animationAuthoring.AttackAnimationName, 0.2f);
                Debug.Log("Test: Attack Animation");
            }
        }
    }
}