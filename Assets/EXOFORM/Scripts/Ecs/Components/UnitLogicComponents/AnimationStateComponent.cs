using Unity.Entities;

namespace Exoform.Scripts.Ecs.Components.UnitLogicComponents
{
    /// <summary>
    /// Компонент состояния анимации (ЕДИНЫЙ для всей системы)
    /// </summary>
    public struct AnimationStateComponent : IComponentData
    {
        public UnitAnimationState CurrentState;
        public UnitAnimationState PreviousState;
        public float StateChangeTime;
        public float MovementSpeed;
        public bool IsSelected;
        public bool WasSelected;
        public bool TriggerSelection;
        public bool TriggerAttack;
        public bool TriggerDeath;
        public float Health;
        public float MaxHealth;
    }
}