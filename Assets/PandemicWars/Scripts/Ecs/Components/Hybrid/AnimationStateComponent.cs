using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;

namespace PandemicWars.Scripts.Ecs.Components.Hybrid
{
    /// <summary>
    /// Состояние анимации для синхронизации
    /// </summary>
    public struct AnimationStateComponent : IComponentData
    {
        public UnitAnimationState CurrentState;
        public UnitAnimationState PreviousState;
        public float StateChangeTime;
        public float MovementSpeed;
        public bool IsSelected;
        public bool TriggerAttack;
        public bool TriggerDeath;
        public float Health;
        public float MaxHealth;
    }
}