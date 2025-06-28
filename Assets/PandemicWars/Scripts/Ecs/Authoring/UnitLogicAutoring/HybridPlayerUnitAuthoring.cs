using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.Hybrid;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Authoring.UnitLogicAutoring
{
    /// <summary>
    /// Обновленный PlayerUnitAuthoring с поддержкой гибридной анимации
    /// </summary>
    public class HybridPlayerUnitAuthoring : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float stoppingDistance = 0.5f;
        [SerializeField] private int maxPathIterations = 1000;
        
        [Header("Selection Settings")]
        [SerializeField] private bool isSelectedByDefault = false;
        [SerializeField] private float selectionRadius = 1f;
        
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        
        [Header("Hybrid Animation")]
        [SerializeField] private HybridUnitAnimator hybridAnimator;
        [SerializeField] private float syncInterval = 0.033f; // 30fps sync
        [SerializeField] private bool forceHighLOD = false;

        class Baker : Baker<HybridPlayerUnitAuthoring>
        {
            public override void Bake(HybridPlayerUnitAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // Базовые компоненты DOTS
                AddComponent(entity, new NavAgentComponent
                {
                    TargetEntity = Entity.Null,
                    MovementSpeed = authoring.moveSpeed,
                    PathCalculated = false,
                    CurrentWaypoint = 0,
                    NextPathCalculatedTime = 0f,
                    MaxPathIterations = authoring.maxPathIterations
                });
                
                AddBuffer<WaypointBuffer>(entity);
                
                AddComponent(entity, new PlayerUnitComponent
                {
                    IsSelected = authoring.isSelectedByDefault,
                    SelectionRadius = authoring.selectionRadius
                });

                // Компоненты гибридной анимации
                AddComponent(entity, new AnimationStateComponent
                {
                    CurrentState = UnitAnimationState.Idle,
                    PreviousState = UnitAnimationState.Idle,
                    StateChangeTime = 0f,
                    MovementSpeed = 0f,
                    IsSelected = authoring.isSelectedByDefault,
                    Health = authoring.maxHealth,
                    MaxHealth = authoring.maxHealth
                });

                AddComponent(entity, new AnimationLODComponent
                {
                    CurrentLOD = AnimationLODLevel.High,
                    DistanceToCamera = 0f,
                    ForceHighLOD = authoring.forceHighLOD
                });

                // Связь с GameObject (если есть HybridAnimator)
                if (authoring.hybridAnimator != null)
                {
                    var gameObjectEntity = GetEntity(authoring.hybridAnimator.transform, TransformUsageFlags.Dynamic);
                    
                    AddComponent(entity, new HybridUnitComponent
                    {
                        LinkedGameObject = gameObjectEntity,
                        AnimationEnabled = true,
                        LastSyncTime = 0f,
                        SyncInterval = authoring.syncInterval
                    });

                    // Устанавливаем связь в обратную сторону
                    authoring.hybridAnimator.LinkedEntity = entity;
                }
            }
        }

        private void Reset()
        {
            // Автоматически находим HybridUnitAnimator
            if (hybridAnimator == null)
            {
                hybridAnimator = GetComponent<HybridUnitAnimator>();
                if (hybridAnimator == null)
                    hybridAnimator = GetComponentInChildren<HybridUnitAnimator>();
            }
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            maxHealth = Mathf.Max(1f, maxHealth);
            syncInterval = Mathf.Clamp(syncInterval, 0.016f, 0.1f); // 10-60fps
        }
    }
}