using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.Hybrid;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Authoring.UnitLogicAutoring
{
    /// <summary>
    /// MonoBehaviour на GameObject в Main Scene
    /// Получает данные от ECS и управляет визуализацией
    /// </summary>
    public class UnitVisualController : MonoBehaviour
    {
        [Header("Unit Settings")]
        public int unitId; // Должен совпадать с UnitProxyComponent.UnitId
        public bool autoFindAnimator = true;
        
        [Header("Visual Components")]
        public HybridUnitAnimator hybridAnimator;
        public HealthBarUI healthBar;
        public SelectionRing selectionRing;
        public ParticleSystem hitEffect;
        public AudioSource audioSource;

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

        void Start()
        {
            InitializeComponents();
            FindLinkedEntity();
        }

        void InitializeComponents()
        {
            world = World.DefaultGameObjectInjectionWorld;
            entityManager = world?.EntityManager;

            if (hybridAnimator == null && autoFindAnimator)
                hybridAnimator = GetComponent<HybridUnitAnimator>();

            if (healthBar == null)
                healthBar = GetComponentInChildren<HealthBarUI>();

            if (selectionRing == null)
                selectionRing = GetComponentInChildren<SelectionRing>();

            // Кэшируем renderers для damage flash
            renderers = GetComponentsInChildren<Renderer>();
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = renderers[i].material.color;
            }
        }

        void FindLinkedEntity()
        {
            if (entityManager == null) return;

            // Ищем Entity с нашим unitId
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<UnitProxyComponent>());
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);

            foreach (var entity in entities)
            {
                var proxy = entityManager.GetComponentData<UnitProxyComponent>(entity);
                if (proxy.UnitId == unitId)
                {
                    linkedEntity = entity;
                    Debug.Log($"Unit {unitId} linked to Entity {entity.Index}");
                    break;
                }
            }

            entities.Dispose();
            query.Dispose();
        }

        void Update()
        {
            if (linkedEntity == Entity.Null || entityManager == null || !entityManager.Exists(linkedEntity))
                return;

            UpdateFromECS();
            UpdateVisualEffects();
        }

        void UpdateFromECS()
        {
            // Получаем данные от ECS
            var transform = entityManager.GetComponentData<LocalTransform>(linkedEntity);
            var combat = entityManager.GetComponentData<CombatComponent>(linkedEntity);
            var animState = entityManager.GetComponentData<AnimationStateComponent>(linkedEntity);
            var playerUnit = entityManager.GetComponentData<PlayerUnitComponent>(linkedEntity);

            // Синхронизируем позицию и поворот
            this.transform.position = transform.Position;
            this.transform.rotation = transform.Rotation;

            // Обновляем анимацию
            if (hybridAnimator != null)
            {
                var lodComponent = entityManager.GetComponentData<AnimationLODComponent>(linkedEntity);
                hybridAnimator.UpdateAnimation(animState, lodComponent);
            }

            // Обновляем UI
            if (healthBar != null)
            {
                healthBar.UpdateHealth(combat.Health, combat.MaxHealth);
                healthBar.SetVisible(playerUnit.IsSelected || combat.Health < combat.MaxHealth);
            }

            // Обновляем кольцо выбора
            if (selectionRing != null)
            {
                selectionRing.SetSelected(playerUnit.IsSelected);
            }

            // Проверяем смерть
            if (combat.IsDead && !isDying)
            {
                StartDeathSequence();
            }
        }

        private bool isDying = false;

        void StartDeathSequence()
        {
            isDying = true;
            
            // Запускаем анимацию смерти
            if (hybridAnimator != null)
            {
                // Устанавливаем состояние смерти через ECS
                var animState = entityManager.GetComponentData<AnimationStateComponent>(linkedEntity);
                animState.CurrentState = HybridUnitAnimationState.Dead;
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
            if (entityManager != null && entityManager.Exists(linkedEntity))
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
                    renderers[i].material.color = Color.Lerp(originalColors[i], damageFlashColor, flashIntensity);
                }

                if (damageFlashTimer <= 0f)
                {
                    // Восстанавливаем оригинальные цвета
                    for (int i = 0; i < renderers.Length; i++)
                    {
                        renderers[i].material.color = originalColors[i];
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
                // Играем случайный звук урона
                audioSource.Play();
            }

            Debug.Log($"Unit {unitId} received {damage} damage at {hitPoint}");
        }

        void OnDestroy()
        {
            linkedEntity = Entity.Null;
        }

        // Обработка кликов для выбора (альтернатива через Collider)
        void OnMouseDown()
        {
            if (linkedEntity != Entity.Null && entityManager != null && entityManager.Exists(linkedEntity))
            {
                var playerUnit = entityManager.GetComponentData<PlayerUnitComponent>(linkedEntity);
                playerUnit.IsSelected = !playerUnit.IsSelected;
                entityManager.SetComponentData(linkedEntity, playerUnit);
            }
        }
    }
}