using Unity.Entities;

namespace Exoform.Scripts.Ecs.Components.UnitLogicComponents
{
    /// <summary>
    /// Компонент для боевой системы
    /// </summary>
    public struct CombatComponent : IComponentData
    {
        public float Health;
        public float MaxHealth;
        public float Armor;
        public bool IsDead;
        public float LastDamageTime;
        public Entity LastAttacker;
    }
}