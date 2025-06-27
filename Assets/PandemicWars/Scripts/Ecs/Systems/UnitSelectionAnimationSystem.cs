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
            // Новый синтаксис DOTS 1.0+
            foreach (var (animState, animatorComp, playerUnit, entity) in
                SystemAPI.Query<RefRW<UnitAnimationComponent>, UnitAnimatorComponent, RefRO<PlayerUnitComponent>>()
                    .WithEntityAccess())
            {
                if (animatorComp?.Animator == null) continue;

                UpdateSelectionAnimation(ref animState.ValueRW, animatorComp, playerUnit.ValueRO, entity);
            }
        }

        private void UpdateSelectionAnimation(ref UnitAnimationComponent animState, 
                                            UnitAnimatorComponent animatorComp, 
                                            PlayerUnitComponent playerUnit, 
                                            Entity entity)
        {
            if (animatorComp?.Animator == null) return;

            // Устанавливаем параметр выбора в аниматоре
            if (!string.IsNullOrEmpty(animatorComp.IsSelectedParameterName))
            {
                try
                {
                    if (HasParameter(animatorComp.Animator, animatorComp.IsSelectedParameterName))
                    {
                        animatorComp.Animator.SetBool(animatorComp.IsSelectedParameterName, playerUnit.IsSelected);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to set selection parameter: {e.Message}");
                }
            }

            // Можно добавить специальные эффекты для выбранных юнитов
            if (playerUnit.IsSelected && !animState.WasSelected)
            {
                // Юнит только что выбран - можно проиграть анимацию выбора
                if (!string.IsNullOrEmpty(animatorComp.SelectionAnimationName))
                {
                    try
                    {
                        animatorComp.Animator.Play(animatorComp.SelectionAnimationName);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Failed to play selection animation: {e.Message}");
                    }
                }
                
                Debug.Log($"Entity {entity.Index}: Selection animation triggered");
            }

            animState.WasSelected = playerUnit.IsSelected;
        }

        private bool HasParameter(Animator animator, string parameterName)
        {
            if (animator == null || string.IsNullOrEmpty(parameterName)) return false;
            
            foreach (var param in animator.parameters)
            {
                if (param.name == parameterName)
                    return true;
            }
            return false;
        }
    }
}