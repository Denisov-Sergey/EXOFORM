using Unity.Entities;

namespace Exoform.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Возможные состояния юнита
    /// </summary>
    public enum UnitState : byte
    {
        Idle = 0,
        Moving = 1,
        Attacking = 2,
        Dead = 3,
        Stunned = 4
    }
    /// <summary>
    /// Компонент для состояния юнита
    /// </summary>
    public struct UnitStateComponent : IComponentData
    {
        public UnitState CurrentState;
        public float StateChangeTime;
    }
}