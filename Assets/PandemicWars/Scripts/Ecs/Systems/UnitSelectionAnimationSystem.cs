using Unity.Entities;
using UnityEngine;
using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;

namespace PandemicWars.Scripts.Ecs.Systems
{
    /// <summary>
    /// Система для синхронизации анимаций с выбором юнитов
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class UnitSelectionAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities
                .WithAll<PlayerUnitComponent>()
                .ForEach((Entity entity, UnitAnimationComponent animComp, in PlayerUnitComponent playerUnit) =>
                {
                    UpdateSelectionAnimation(animComp, playerUnit, entity);
                })
                .WithoutBurst()
                .Run();
        }

        private void UpdateSelectionAnimation(UnitAnimationComponent animComp, PlayerUnitComponent playerUnit, Entity entity)
        {
            if (animComp?.Animator == null) return;

            // Устанавливаем параметр выбора в аниматоре
            if (!string.IsNullOrEmpty(animComp.IsSelectedParameterName))
            {
                animComp.Animator.SetBool(animComp.IsSelectedParameterName, playerUnit.IsSelected);
            }

            // Можно добавить специальные эффекты для выбранных юнитов
            if (playerUnit.IsSelected && !animComp.WasSelected)
            {
                // Юнит только что выбран - можно проиграть анимацию выбора
                if (!string.IsNullOrEmpty(animComp.SelectionAnimationName))
                {
                    animComp.Animator.Play(animComp.SelectionAnimationName);
                }
                
                Debug.Log($"Entity {entity.Index}: Selection animation triggered");
            }

            animComp.WasSelected = playerUnit.IsSelected;
        }
    }
}