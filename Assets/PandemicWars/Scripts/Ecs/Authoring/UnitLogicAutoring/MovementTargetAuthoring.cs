using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Authoring.UnitLogicAutoring
{
    /// <summary>
    /// Простой Authoring для целей движения
    /// </summary>
    public class MovementTargetAuthoring : MonoBehaviour
    {
        class Baker : Baker<MovementTargetAuthoring>
        {
            public override void Bake(MovementTargetAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                
                AddComponent(entity, new MovementTargetTag
                {
                    CreationTime = UnityEngine.Time.time
                });
            }
        }
    }
}