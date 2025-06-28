using Unity.Entities;

namespace PandemicWars.Scripts.Ecs.Components.UnitLogicComponents
{
    /// <summary>
    /// Компонент для игровой логики юнита
    /// </summary>
    public struct UnitLogicComponent : IComponentData
    {
        public int TeamId;
        public UnitType UnitType;
        public float AttackRange;
        public float AttackDamage;
        public float AttackCooldown;
        public float LastAttackTime;
    }
}