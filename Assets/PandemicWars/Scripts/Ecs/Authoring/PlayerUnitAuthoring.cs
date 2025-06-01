using PandemicWars.Scripts.Ecs.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Authoring
{
    /// <summary>
    /// Authoring для управляемых игроком юнитов
    /// </summary>
    public class PlayerUnitAuthoring : MonoBehaviour
    {
        [Header("Player Unit Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private bool isSelectedByDefault = false;
        [SerializeField] private float selectionRadius = 1f; // Радиус для выбора мышью
        [SerializeField] private int maxPathIterations = 1000;
        
        [Header("Visual Settings")]
        [SerializeField] private GameObject selectionIndicator; // Визуальный индикатор выбора

        class Baker : Baker<PlayerUnitAuthoring>
        {
            public override void Bake(PlayerUnitAuthoring authoring)
            {
                if (authoring == null) return;
                
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (entity == Entity.Null) return;
                
                // Базовые компоненты навигации
                AddComponent(entity, new NavAgentComponent
                {
                    TargetEntity = Entity.Null, // Изначально нет цели
                    MovementSpeed = authoring.moveSpeed,
                    PathCalculated = false,
                    CurrentWaypoint = 0,
                    NextPathCalculatedTime = 0f,
                    MaxPathIterations = authoring.maxPathIterations
                });
                
                AddBuffer<WaypointBuffer>(entity);
                
                // Компонент управляемого юнита
                AddComponent(entity, new PlayerUnitComponent
                {
                    IsSelected = authoring.isSelectedByDefault,
                    SelectionRadius = authoring.selectionRadius
                });

                // Устанавливаем связь GameObject-Entity для InputSystem
                var entityReference = authoring.GetComponent<EntityReference>();
                if (entityReference == null)
                {
                    entityReference = authoring.gameObject.AddComponent<EntityReference>();
                }
                entityReference.SetEntity(entity);
            }
        }
    }
}
