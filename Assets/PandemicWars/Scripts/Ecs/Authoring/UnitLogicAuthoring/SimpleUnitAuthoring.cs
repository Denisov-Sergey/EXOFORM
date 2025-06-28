using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using PandemicWars.Scripts.Ecs.Components.UnitLogicComponents;
using Unity.Entities;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Authoring.UnitLogicAuthoring
{
    /// <summary>
    /// ПРОСТОЙ и РАБОЧИЙ Authoring для игровых юнитов
    /// Заменяет все сложные варианты
    /// </summary>
    public class SimpleUnitAuthoring : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private int maxPathIterations = 100;
        
        [Header("Selection Settings")]
        [SerializeField] private bool isSelectedByDefault = false;
        [SerializeField] private float selectionRadius = 1f;
        
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        
        [Header("Unit Type")]
        [SerializeField] private UnitType unitType = UnitType.Infantry;
        [SerializeField] private int teamId = 1;

        class Baker : Baker<SimpleUnitAuthoring>
        {
            public override void Bake(SimpleUnitAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                // Базовые компоненты навигации
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
                
                // Компонент игрока
                AddComponent(entity, new PlayerUnitComponent
                {
                    IsSelected = authoring.isSelectedByDefault,
                    SelectionRadius = authoring.selectionRadius
                });

                // Компоненты анимации
                AddComponent(entity, new AnimationStateComponent
                {
                    CurrentState = UnitAnimationState.Idle,
                    PreviousState = UnitAnimationState.Idle,
                    StateChangeTime = 0f,
                    MovementSpeed = 0f,
                    IsSelected = authoring.isSelectedByDefault,
                    Health = authoring.maxHealth,
                    MaxHealth = authoring.maxHealth,
                    TriggerAttack = false,
                    TriggerDeath = false
                });

                AddComponent(entity, new AnimationLODComponent
                {
                    CurrentLOD = AnimationLODLevel.High,
                    DistanceToCamera = 0f,
                    ForceHighLOD = authoring.isSelectedByDefault
                });

                // Боевые компоненты
                AddComponent(entity, new CombatComponent
                {
                    Health = authoring.maxHealth,
                    MaxHealth = authoring.maxHealth,
                    Armor = 0f,
                    IsDead = false,
                    LastDamageTime = 0f,
                    LastAttacker = Entity.Null
                });

                // Логика юнита
                AddComponent(entity, new UnitLogicComponent
                {
                    TeamId = authoring.teamId,
                    UnitType = authoring.unitType,
                    AttackRange = 10f,
                    AttackDamage = 25f,
                    AttackCooldown = 1f,
                    LastAttackTime = 0f
                });
            }
        }

        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            maxHealth = Mathf.Max(1f, maxHealth);
            maxPathIterations = Mathf.Max(10, maxPathIterations);
        }
    }
}