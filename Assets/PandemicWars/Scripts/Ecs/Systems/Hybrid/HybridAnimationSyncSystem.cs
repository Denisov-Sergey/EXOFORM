using PandemicWars.Scripts.Ecs.Authoring.UnitLogicAutoring;
using PandemicWars.Scripts.Ecs.Components.Hybrid;
using Unity.Entities;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Systems.Hybrid
{
    /// <summary>
    /// Система синхронизации DOTS данных с GameObject анимацией
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(HybridAnimationStateSystem))]
    [UpdateAfter(typeof(AnimationLODSystem))]
    public partial class HybridAnimationSyncSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            Entities
                .ForEach((in HybridUnitComponent hybridUnit,
                    in AnimationStateComponent animState,
                    in AnimationLODComponent lodComponent) =>
                {
                    // Проверяем нужно ли обновление (для оптимизации)
                    if (currentTime - hybridUnit.LastSyncTime < hybridUnit.SyncInterval)
                        return;

                    // Находим HybridUnitAnimator на GameObject
                    if (EntityManager.Exists(hybridUnit.LinkedGameObject))
                    {
                        var gameObject = EntityManager.GetComponentObject<Transform>(hybridUnit.LinkedGameObject);
                        if (gameObject != null)
                        {
                            var hybridAnimator = gameObject.GetComponent<HybridUnitAnimator>();
                            if (hybridAnimator != null)
                            {
                                hybridAnimator.UpdateAnimation(animState, lodComponent);
                            }
                        }
                    }
                })
                .WithoutBurst()
                .Run();
        }
    }
}