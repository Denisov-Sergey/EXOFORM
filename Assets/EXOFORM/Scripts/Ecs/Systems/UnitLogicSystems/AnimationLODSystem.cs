using Exoform.Scripts.Ecs.Components;
using Exoform.Scripts.Ecs.Components.UnitLogicComponents;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Exoform.Scripts.Ecs.Systems.UnitLogicSystems
{
    /// <summary>
    /// Система расчета LOD для анимации
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class AnimationLODSystem : SystemBase
    {
        private Camera mainCamera;

        protected override void OnCreate()
        {
            RequireForUpdate<AnimationLODComponent>();
        }

        protected override void OnStartRunning()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = Object.FindObjectOfType<Camera>();
        }

        protected override void OnUpdate()
        {
            if (mainCamera == null) return;

            float3 cameraPosition = mainCamera.transform.position;

            Entities
                .ForEach((ref AnimationLODComponent lodComponent,
                    in LocalTransform transform,
                    in PlayerUnitComponent playerUnit) =>
                {
                    float distance = math.distance(transform.Position, cameraPosition);
                    lodComponent.DistanceToCamera = distance;

                    // Выбранные юниты всегда высокое качество
                    if (playerUnit.IsSelected || lodComponent.ForceHighLOD)
                    {
                        lodComponent.CurrentLOD = AnimationLODLevel.High;
                        return;
                    }

                    // Определяем LOD на основе дистанции
                    if (distance > 100f)
                        lodComponent.CurrentLOD = AnimationLODLevel.Disabled;
                    else if (distance > 50f)
                        lodComponent.CurrentLOD = AnimationLODLevel.Low;
                    else if (distance > 20f)
                        lodComponent.CurrentLOD = AnimationLODLevel.Medium;
                    else
                        lodComponent.CurrentLOD = AnimationLODLevel.High;
                })
                .Run();
        }
    }
}