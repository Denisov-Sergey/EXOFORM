using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace PandemicWars.Scripts.Ecs.Authoring
{
    public class UnitAuthoring : MonoBehaviour
    {
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float moveSpeed = 5f;

        class Baker : Baker<UnitAuthoring>
        {
            public override void Bake(UnitAuthoring authoring)
            {
                if (authoring == null) return;
                
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (entity == Entity.Null) return;
                
                AddComponent(entity, new NavAgentComponent
                {
                    TargetEntity = GetEntity(authoring.targetTransform, TransformUsageFlags.Dynamic),
                    MovementSpeed = authoring.moveSpeed
                });
                AddBuffer<WaypointBuffer>(entity);
            }
        }
    }
}