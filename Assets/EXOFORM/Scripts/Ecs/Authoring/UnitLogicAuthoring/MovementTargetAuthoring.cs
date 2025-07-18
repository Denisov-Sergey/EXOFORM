﻿using Exoform.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;
using UnityEngine;

namespace Exoform.Scripts.Ecs.Authoring.UnitLogicAuthoring
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