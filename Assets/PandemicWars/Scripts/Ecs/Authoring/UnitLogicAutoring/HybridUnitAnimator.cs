using PandemicWars.Scripts.Ecs.Components.Hybrid;
using Unity.Entities;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Authoring.UnitLogicAutoring
{
    /// <summary>
    /// MonoBehaviour компонент на GameObject'е юнита
    /// Получает данные от DOTS Entity и управляет Animator'ом
    /// </summary>
    public class HybridUnitAnimator : MonoBehaviour
    {
        [Header("Animation Settings")]
        public Animator animator;
        public bool debugMode = false;
        
        [Header("Animation Names")]
        public string idleAnimationName = "Idle";
        public string moveAnimationName = "Run";
        public string attackAnimationName = "Attack";
        public string deathAnimationName = "Death";
        
        [Header("Animator Parameters")]
        public string speedParameterName = "Speed";
        public string isMovingParameterName = "IsMoving";
        public string isSelectedParameterName = "IsSelected";
        public string healthParameterName = "Health";
        public string attackTriggerName = "AttackTrigger";
        public string deathTriggerName = "DeathTrigger";

        // Кэшированные ID параметров для производительности
        private int speedParamId;
        private int isMovingParamId;
        private int isSelectedParamId;
        private int healthParamId;
        private int attackTriggerParamId;
        private int deathTriggerParamId;

        // Связанная Entity
        public Entity LinkedEntity { get; set; } = Entity.Null;
        
        // Текущее состояние анимации
        private UnitAnimationState currentState = UnitAnimationState.Idle;
        private float lastUpdateTime;

        void Start()
        {
            InitializeAnimator();
            CacheParameterIds();
        }

        void InitializeAnimator()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
                
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
                
            if (animator == null)
            {
                Debug.LogError($"Animator not found on {gameObject.name}!");
                enabled = false;
                return;
            }

            // Оптимизация Animator'а
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.updateMode = AnimatorUpdateMode.Normal;
        }

        void CacheParameterIds()
        {
            speedParamId = GetParameterId(speedParameterName);
            isMovingParamId = GetParameterId(isMovingParameterName);
            isSelectedParamId = GetParameterId(isSelectedParameterName);
            healthParamId = GetParameterId(healthParameterName);
            attackTriggerParamId = GetParameterId(attackTriggerName);
            deathTriggerParamId = GetParameterId(deathTriggerName);
        }

        int GetParameterId(string paramName)
        {
            if (string.IsNullOrEmpty(paramName)) return -1;
            
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName)
                    return Animator.StringToHash(paramName);
            }
            return -1;
        }

        /// <summary>
        /// Обновление анимации на основе данных от DOTS Entity
        /// Вызывается из HybridAnimationSyncSystem
        /// </summary>
        public void UpdateAnimation(AnimationStateComponent animState, AnimationLODComponent lodState)
        {
            if (animator == null || !animator.gameObject.activeInHierarchy)
                return;

            lastUpdateTime = Time.time;

            // Обновляем LOD
            UpdateAnimationLOD(lodState);

            // Если анимация отключена, пропускаем обновление
            if (lodState.CurrentLOD == AnimationLODLevel.Disabled)
                return;

            // Обновляем параметры анимации
            UpdateAnimatorParameters(animState);

            // Переключаем состояние если нужно
            if (animState.CurrentState != currentState)
            {
                ChangeAnimationState(animState.CurrentState);
                currentState = animState.CurrentState;
            }

            // Обрабатываем триггеры
            HandleTriggers(animState);

            if (debugMode)
            {
                Debug.Log($"[{gameObject.name}] Animation: {currentState}, Speed: {animState.MovementSpeed:F2}, LOD: {lodState.CurrentLOD}");
            }
        }

        void UpdateAnimationLOD(AnimationLODComponent lodState)
        {
            switch (lodState.CurrentLOD)
            {
                case AnimationLODLevel.High:
                    animator.enabled = true;
                    animator.updateMode = AnimatorUpdateMode.Normal;
                    break;
                    
                case AnimationLODLevel.Medium:
                    animator.enabled = true;
                    animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                    // Можно добавить frame skipping
                    break;
                    
                case AnimationLODLevel.Low:
                    animator.enabled = true;
                    animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                    // Еще больше frame skipping
                    break;
                    
                case AnimationLODLevel.Disabled:
                    animator.enabled = false;
                    break;
            }
        }

        void UpdateAnimatorParameters(AnimationStateComponent animState)
        {
            bool isMoving = animState.MovementSpeed > 0.1f;
            float healthPercent = animState.MaxHealth > 0 ? (animState.Health / animState.MaxHealth) : 1f;

            // Обновляем параметры только если они существуют
            if (speedParamId != -1)
                animator.SetFloat(speedParamId, animState.MovementSpeed);
                
            if (isMovingParamId != -1)
                animator.SetBool(isMovingParamId, isMoving);
                
            if (isSelectedParamId != -1)
                animator.SetBool(isSelectedParamId, animState.IsSelected);
                
            if (healthParamId != -1)
                animator.SetFloat(healthParamId, healthPercent);
        }

        void ChangeAnimationState(UnitAnimationState newState)
        {
            string animationName = GetAnimationName(newState);
            
            if (string.IsNullOrEmpty(animationName))
                return;

            try
            {
                animator.CrossFade(animationName, 0.2f);
                
                if (debugMode)
                {
                    Debug.Log($"[{gameObject.name}] Changed animation to: {animationName}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to play animation '{animationName}' on {gameObject.name}: {e.Message}");
            }
        }

        void HandleTriggers(AnimationStateComponent animState)
        {
            if (animState.TriggerAttack && attackTriggerParamId != -1)
            {
                animator.SetTrigger(attackTriggerParamId);
                if (debugMode) Debug.Log($"[{gameObject.name}] Attack trigger");
            }

            if (animState.TriggerDeath && deathTriggerParamId != -1)
            {
                animator.SetTrigger(deathTriggerParamId);
                if (debugMode) Debug.Log($"[{gameObject.name}] Death trigger");
            }
        }

        string GetAnimationName(UnitAnimationState state)
        {
            return state switch
            {
                UnitAnimationState.Idle => idleAnimationName,
                UnitAnimationState.Moving => moveAnimationName,
                UnitAnimationState.Attacking => attackAnimationName,
                UnitAnimationState.Dead => deathAnimationName,
                _ => idleAnimationName
            };
        }

        /// <summary>
        /// Установка качества анимации вручную (для отладки)
        /// </summary>
        public void SetAnimationLOD(AnimationLODLevel lod)
        {
            var tempLOD = new AnimationLODComponent { CurrentLOD = lod };
            UpdateAnimationLOD(tempLOD);
        }

        void OnDestroy()
        {
            // Очистка при уничтожении
            LinkedEntity = Entity.Null;
        }

        // Отладочная информация в Inspector
        void OnValidate()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
        }
    }
}