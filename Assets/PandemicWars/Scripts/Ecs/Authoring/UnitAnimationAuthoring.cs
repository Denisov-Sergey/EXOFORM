using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Authoring
{
    /// <summary>
    /// Authoring компонент для анимаций юнитов
    /// </summary>
    public class UnitAnimationAuthoring : MonoBehaviour
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;

        [Header("Animation Names")]
        [SerializeField] private string idleAnimationName = "Idle";
        [SerializeField] private string moveAnimationName = "Move";
        [SerializeField] private string attackAnimationName = "Attack";
        [SerializeField] private string deathAnimationName = "Death";
        [SerializeField] private string selectionAnimationName = "Selected";

        [Header("Animator Parameters")]
        [SerializeField] private string speedParameterName = "Speed";
        [SerializeField] private string isMovingParameterName = "IsMoving";
        [SerializeField] private string isSelectedParameterName = "IsSelected";
        [SerializeField] private string triggerAttackParameterName = "Attack";
        [SerializeField] private string triggerDeathParameterName = "Death";

        [Header("Animation Settings")]
        [SerializeField] private float transitionSpeed = 0.2f;
        [SerializeField] private bool useRootMotion = false;
        [SerializeField] private float animationSpeedMultiplier = 1f;

        [Header("Speed Sync Settings")]
        [SerializeField] private bool syncMoveSpeedWithAnimation = true;
        [SerializeField] private float baseAnimationSpeed = 1f;
        [SerializeField] private float speedSmoothTime = 0.3f;

        [Header("Debug")]
        [SerializeField] private bool autoFindAnimator = true;

        class Baker : Baker<UnitAnimationAuthoring>
        {
            public override void Bake(UnitAnimationAuthoring authoring)
            {
                if (authoring == null) return;

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (entity == Entity.Null) return;

                // Автоматически находим Animator если не указан
                var animator = authoring.animator;
                if (animator == null && authoring.autoFindAnimator)
                {
                    animator = authoring.GetComponent<Animator>();
                    if (animator == null)
                        animator = authoring.GetComponentInChildren<Animator>();
                }

                if (animator == null)
                {
                    Debug.LogWarning($"Animator не найден для {authoring.gameObject.name}! Анимации не будут работать.");
                    return;
                }

                // Добавляем компонент состояния анимации
                AddComponent(entity, new UnitAnimationComponent
                {
                    CurrentState = UnitAnimationState.Idle,
                    PreviousState = UnitAnimationState.Idle,
                    StateChangeTime = 0f,
                    HasPreviousPosition = false,
                    WasSelected = false,
                    LastAnimationTime = 0f
                });

                // Добавляем managed компонент с Animator
                AddComponentObject(entity, new UnitAnimatorComponent
                {
                    Animator = animator,
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
                    AnimationSpeedMultiplier = authoring.animationSpeedMultiplier
                });

                // Добавляем компонент синхронизации скорости (если нужен)
                if (authoring.syncMoveSpeedWithAnimation)
                {
                    AddComponent(entity, new AnimationSpeedSyncComponent
                    {
                        SyncMoveSpeedWithAnimation = true,
                        BaseAnimationSpeed = authoring.baseAnimationSpeed,
                        CurrentAnimationSpeed = authoring.baseAnimationSpeed,
                        SpeedSmoothTime = authoring.speedSmoothTime,
                        SpeedVelocity = 0f
                    });
                }

                // Добавляем компонент триггеров
                AddComponent(entity, new AnimationTriggerComponent
                {
                    TriggerAttack = false,
                    TriggerDeath = false,
                    TriggerSelection = false,
                    TriggerHit = false,
                    TriggerTime = 0f
                });

                Debug.Log($"Анимационные компоненты добавлены для {authoring.gameObject.name}");
            }
        }

        private void Reset()
        {
            // Автоматически настраиваем компонент при добавлении
            if (autoFindAnimator && animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                    animator = GetComponentInChildren<Animator>();
            }
        }

        private void OnValidate()
        {
            transitionSpeed = Mathf.Max(0.01f, transitionSpeed);
            animationSpeedMultiplier = Mathf.Max(0.1f, animationSpeedMultiplier);
            baseAnimationSpeed = Mathf.Max(0.1f, baseAnimationSpeed);
            speedSmoothTime = Mathf.Max(0.01f, speedSmoothTime);
        }
    }
}