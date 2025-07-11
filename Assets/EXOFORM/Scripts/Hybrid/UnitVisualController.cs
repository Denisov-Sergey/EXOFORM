using Exoform.Scripts.Ecs.Components;
using Exoform.Scripts.Ecs.Components.UnitLogicComponents;
using Exoform.Scripts.UI;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Exoform.Scripts.Hybrid
{
    /// <summary>
    /// ОСНОВНОЙ контроллер визуализации юнита
    /// Связывает ECS Entity с GameObject и управляет анимацией
    /// </summary>
    public class UnitVisualController : MonoBehaviour
    {
        [Header("Unit Settings")]
        public int unitId; // Должен совпадать с UnitProxyComponent.UnitId
        public bool autoFindComponents = true;
        
        [Header("Visual Components")]
        public Animator animator;
        public HealthBarUI healthBar;
        public SelectionRing selectionRing;
        public ParticleSystem hitEffect;
        public AudioSource audioSource;

        [Header("Animation Parameters")]
        public string speedParameterName = "Speed";
        public string isMovingParameterName = "IsMoving";
        public string isSelectedParameterName = "IsSelected";
        public string selectionTriggerName = "TriggerSelection";
        public string attackTriggerName = "AttackTrigger";
        public string deathTriggerName = "DeathTrigger";

        [Header("Combat Visual Settings")]
        public float damageFlashDuration = 0.2f;
        public Color damageFlashColor = Color.red;
        public float deathDelay = 2f;

        // Внутренние переменные
        private Entity linkedEntity = Entity.Null;
        private World world;
        private EntityManager entityManager;
        private Renderer[] renderers;
        private Color[] originalColors;
        private float damageFlashTimer = 0f;
        private bool isDying = false;

        // Кэшированные ID параметров для производительности
        private int speedParamId = -1;
        private int isMovingParamId = -1;
        private int isSelectedParamId = -1;
        private int selectionTriggerParamId = -1;
        private int attackTriggerParamId = -1;
        private int deathTriggerParamId = -1;
        private bool wasSelectedLastFrame = false;

        // Состояние анимации
        private UnitAnimationState currentAnimationState = UnitAnimationState.Idle;

        void Start()
        {
            InitializeComponents();
            CacheAnimatorParameters();

            if (linkedEntity == Entity.Null)
            {
                if (IsECSReady())
                    FindLinkedEntity();
                else
                    Invoke(nameof(DelayedEntitySearch), 0.1f);
            }
        }

        void DelayedEntitySearch()
        {
            if (linkedEntity == Entity.Null)
            {
                InitializeComponents();
                FindLinkedEntity();
                
                // Если все еще не нашли, попробуем еще раз
                if (linkedEntity == Entity.Null && world != null)
                {
                    Invoke(nameof(DelayedEntitySearch), 0.5f);
                }
            }
        }

        void InitializeComponents()
        {
            world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                entityManager = world.EntityManager;
            }
            else
            {
                Debug.LogError("DefaultGameObjectInjectionWorld is null! ECS not initialized.");
                return;
            }

            if (autoFindComponents)
            {
                if (animator == null)
                    animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

                if (healthBar == null)
                    healthBar = GetComponentInChildren<HealthBarUI>();

                if (selectionRing == null)
                    selectionRing = GetComponentInChildren<SelectionRing>();

                if (hitEffect == null)
                    hitEffect = GetComponentInChildren<ParticleSystem>();

                if (audioSource == null)
                    audioSource = GetComponent<AudioSource>();
            }

            // Кэшируем renderers для damage flash
            renderers = GetComponentsInChildren<Renderer>();
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].material != null)
                    originalColors[i] = renderers[i].material.color;
            }

            // Создаем UI компоненты если их нет
            if (healthBar == null)
            {
                var healthBarGO = new GameObject("HealthBar");
                healthBarGO.transform.SetParent(transform);
                healthBar = healthBarGO.AddComponent<HealthBarUI>();
            }

            if (selectionRing == null)
            {
                var selectionRingGO = new GameObject("SelectionRing");
                selectionRingGO.transform.SetParent(transform);
                selectionRing = selectionRingGO.AddComponent<SelectionRing>();
            }
        }

        void CacheAnimatorParameters()
        {
            if (animator == null) return;

            speedParamId = GetParameterId(speedParameterName);
            isMovingParamId = GetParameterId(isMovingParameterName);
            isSelectedParamId = GetParameterId(isSelectedParameterName);
            selectionTriggerParamId = GetParameterId(selectionTriggerName);
            attackTriggerParamId = GetParameterId(attackTriggerName);
            deathTriggerParamId = GetParameterId(deathTriggerName);
        }

        int GetParameterId(string paramName)
        {
            if (string.IsNullOrEmpty(paramName) || animator == null) return -1;
            
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName)
                    return Animator.StringToHash(paramName);
            }
            return -1;
        }

        void FindLinkedEntity()
        {
            if (!IsECSReady())
            {
                Debug.LogWarning("EntityManager not available, retrying in next frame...");
                return;
            }

            // Ищем Entity с нашим unitId
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<UnitProxyComponent>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<UnitProxyComponent>(entity))
                {
                    var proxy = entityManager.GetComponentData<UnitProxyComponent>(entity);
                    if (proxy.UnitId == unitId)
                    {
                        linkedEntity = entity;
                        Debug.Log($"Unit {unitId} linked to Entity {entity.Index}");
                        break;
                    }
                }
            }

            entities.Dispose();
            query.Dispose();
        }

        void Update()
        {
            if (linkedEntity == Entity.Null || !IsECSReady() || !entityManager.Exists(linkedEntity))
                return;

            UpdateFromECS();
            UpdateVisualEffects();
        }

        void UpdateFromECS()
        {
            if (!IsECSReady() || !entityManager.Exists(linkedEntity))
                return;

            // Получаем данные от ECS
            var transform = entityManager.GetComponentData<LocalTransform>(linkedEntity);
            
            // Синхронизируем позицию и поворот
            this.transform.position = transform.Position;
            this.transform.rotation = transform.Rotation;

            // Обновляем анимацию если есть соответствующие компоненты
            if (entityManager.HasComponent<AnimationStateComponent>(linkedEntity))
            {
                var animState = entityManager.GetComponentData<AnimationStateComponent>(linkedEntity);
                UpdateAnimation(animState);
            }

            // Обновляем UI если есть соответствующие компоненты
            if (entityManager.HasComponent<CombatComponent>(linkedEntity))
            {
                var combat = entityManager.GetComponentData<CombatComponent>(linkedEntity);
                
                if (healthBar != null)
                {
                    healthBar.UpdateHealth(combat.Health, combat.MaxHealth);
                    
                    // Показываем health bar только если выбран или поврежден
                    bool showHealthBar = false;
                    if (entityManager.HasComponent<PlayerUnitComponent>(linkedEntity))
                    {
                        var playerUnit = entityManager.GetComponentData<PlayerUnitComponent>(linkedEntity);
                        showHealthBar = playerUnit.IsSelected || combat.Health < combat.MaxHealth;
                    }
                    healthBar.SetVisible(showHealthBar);
                }

                // Проверяем смерть
                if (combat.IsDead && !isDying)
                {
                    StartDeathSequence();
                }
            }

            // Обновляем кольцо выбора
            if (entityManager.HasComponent<PlayerUnitComponent>(linkedEntity))
            {
                var playerUnit = entityManager.GetComponentData<PlayerUnitComponent>(linkedEntity);
                if (selectionRing != null)
                {
                    selectionRing.SetSelected(playerUnit.IsSelected);
                }
            }
        }

        void UpdateAnimation(AnimationStateComponent animState)
        {
            if (animator == null) return;

            // Обновляем параметры анимации
            bool isMoving = animState.MovementSpeed > 0.1f;
            bool isCurrentlySelected = animState.IsSelected;

            if (speedParamId != -1)
                animator.SetFloat(speedParamId, animState.MovementSpeed);
                
            if (isMovingParamId != -1)
                animator.SetBool(isMovingParamId, isMoving);
                
            if (isSelectedParamId != -1)
                animator.SetBool(isSelectedParamId, isCurrentlySelected);

            // Проверяем изменение состояния выбора
            if (isCurrentlySelected != wasSelectedLastFrame)
            {
                wasSelectedLastFrame = isCurrentlySelected;
        
                // Триггер срабатывает только при выборе (не при снятии выбора)
                if (isCurrentlySelected && selectionTriggerParamId != -1)
                {
                    animator.SetTrigger(selectionTriggerParamId);
                    Debug.Log($"[{gameObject.name}] Selection trigger activated");
                }
            }
            // Переключаем состояние анимации если изменилось
            if (animState.CurrentState != currentAnimationState)
            {
                ChangeAnimationState(animState.CurrentState);
                currentAnimationState = animState.CurrentState;
            }

            // Обрабатываем триггеры
            if (animState.TriggerAttack && attackTriggerParamId != -1)
            {
                animator.SetTrigger(attackTriggerParamId);
                Debug.Log($"[{gameObject.name}] Attack trigger");
            }

            if (animState.TriggerDeath && deathTriggerParamId != -1)
            {
                animator.SetTrigger(deathTriggerParamId);
                Debug.Log($"[{gameObject.name}] Death trigger");
            }
        }

        void ChangeAnimationState(UnitAnimationState newState)
        {
            if (animator == null) return;

            string animationName = GetAnimationName(newState);
            
            if (!string.IsNullOrEmpty(animationName))
            {
                try
                {
                    animator.CrossFade(animationName, 0.2f);
                    Debug.Log($"[{gameObject.name}] Changed animation to: {animationName}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to play animation '{animationName}' on {gameObject.name}: {e.Message}");
                }
            }
        }

        string GetAnimationName(UnitAnimationState state)
        {
            return state switch
            {
                UnitAnimationState.Idle => "Idle",
                UnitAnimationState.Moving => "Run",
                UnitAnimationState.Attacking => "Attack",
                UnitAnimationState.Dead => "Death",
                _ => "Idle"
            };
        }

        void StartDeathSequence()
        {
            isDying = true;
            
            // Запускаем анимацию смерти через ECS
            if (world != null && entityManager != null && entityManager.Exists(linkedEntity) && 
                entityManager.HasComponent<AnimationStateComponent>(linkedEntity))
            {
                var animState = entityManager.GetComponentData<AnimationStateComponent>(linkedEntity);
                animState.CurrentState = UnitAnimationState.Dead;
                animState.TriggerDeath = true;
                entityManager.SetComponentData(linkedEntity, animState);
            }

            // Отключаем коллайдеры
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
                col.enabled = false;

            // Планируем удаление
            Invoke(nameof(DestroyUnit), deathDelay);
        }

        void DestroyUnit()
        {
            // Уведомляем ECS о удалении GameObject
            if (world != null && entityManager != null && entityManager.Exists(linkedEntity))
            {
                var proxy = entityManager.GetComponentData<UnitProxyComponent>(linkedEntity);
                proxy.IsSpawned = false;
                entityManager.SetComponentData(linkedEntity, proxy);
            }

            Destroy(gameObject);
        }

        void UpdateVisualEffects()
        {
            // Обновляем damage flash
            if (damageFlashTimer > 0f)
            {
                damageFlashTimer -= Time.deltaTime;
                float flashIntensity = damageFlashTimer / damageFlashDuration;
                
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null && renderers[i].material != null)
                    {
                        renderers[i].material.color = Color.Lerp(originalColors[i], damageFlashColor, flashIntensity);
                    }
                }

                if (damageFlashTimer <= 0f)
                {
                    // Восстанавливаем оригинальные цвета
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i] != null && renderers[i].material != null)
                        {
                            renderers[i].material.color = originalColors[i];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Вызывается когда юнит получает урон (из ECS системы)
        /// </summary>
        public void OnDamageReceived(float damage, Vector3 hitPoint)
        {
            // Визуальный эффект урона
            damageFlashTimer = damageFlashDuration;

            // Эффект попадания
            if (hitEffect != null)
            {
                hitEffect.transform.position = hitPoint;
                hitEffect.Play();
            }

            // Звук урона
            if (audioSource != null)
            {
                audioSource.Play();
            }

            Debug.Log($"Unit {unitId} received {damage} damage at {hitPoint}");
        }

        void OnDestroy()
        {
            linkedEntity = Entity.Null;
        }

        public void LinkEntity(Entity entity)
        {
            linkedEntity = entity;
            if (world == null)
                world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
                entityManager = world.EntityManager;
        }

        bool IsECSReady()
        {
            return World.DefaultGameObjectInjectionWorld != null &&
                   World.DefaultGameObjectInjectionWorld.IsCreated &&
                   World.DefaultGameObjectInjectionWorld.EntityManager.IsCreated;
        }

        // Обработка кликов для выбора
        void OnMouseDown()
        {
            if (linkedEntity != Entity.Null && world != null && entityManager != null && entityManager.Exists(linkedEntity))
            {
                if (entityManager.HasComponent<PlayerUnitComponent>(linkedEntity))
                {
                    var playerUnit = entityManager.GetComponentData<PlayerUnitComponent>(linkedEntity);
                    playerUnit.IsSelected = !playerUnit.IsSelected;
                    entityManager.SetComponentData(linkedEntity, playerUnit);
                }
            }
        }
    }
}

