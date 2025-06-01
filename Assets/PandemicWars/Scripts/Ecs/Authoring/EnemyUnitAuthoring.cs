using PandemicWars.Scripts.Ecs.Components;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Authoring
{
    /// <summary>
    /// Authoring для вражеских юнитов, которые следуют за фиксированной целью
    /// </summary>
    public class EnemyUnitAuthoring : MonoBehaviour
    {
        [Header("Enemy Settings")]
        [SerializeField] private Transform targetTransform; // Цель для преследования (игрок/база)
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float aggroRange = 10f; // Дистанция обнаружения
        [SerializeField] private int maxPathIterations = 100; // Количество итеграций поиска, для НПС можно поставить меньше так как они не будут ходить по всей карте 

        class Baker : Baker<EnemyUnitAuthoring>
        {
            public override void Bake(EnemyUnitAuthoring authoring)
            {
                if (authoring == null) return;
                
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (entity == Entity.Null) return;
                
                // Базовые компоненты навигации
                AddComponent(entity, new NavAgentComponent
                {
                    TargetEntity = authoring.targetTransform != null ? 
                        GetEntity(authoring.targetTransform, TransformUsageFlags.Dynamic) : 
                        Entity.Null,
                    MovementSpeed = authoring.moveSpeed,
                    PathCalculated = false,
                    CurrentWaypoint = 0,
                    NextPathCalculatedTime = 0f,
                    MaxPathIterations = authoring.maxPathIterations
                });
                
                AddBuffer<WaypointBuffer>(entity);
                
                // Компонент вражеского юнита
                AddComponent(entity, new EnemyUnitComponent
                {
                    DefaultTarget = authoring.targetTransform != null ? 
                        GetEntity(authoring.targetTransform, TransformUsageFlags.Dynamic) : 
                        Entity.Null,
                    AggroRange = authoring.aggroRange
                });
            }
        }
    }
}