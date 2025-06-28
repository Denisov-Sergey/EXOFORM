namespace PandemicWars.Scripts.Ecs.Components.Hybrid
{
    /// <summary>
    /// Состояния анимации (упрощенные для RTS)
    /// </summary>
    public enum UnitAnimationState : byte
    {
        Idle = 0,
        Moving = 1,
        Attacking = 2,
        Dead = 3,
        Celebrating = 4
    }
}