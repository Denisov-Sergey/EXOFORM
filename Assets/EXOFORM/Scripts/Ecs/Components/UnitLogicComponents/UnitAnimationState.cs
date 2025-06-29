namespace Exoform.Scripts.Ecs.Components.UnitLogicComponents
{
    /// <summary>
    /// ЕДИНЫЙ тип состояний анимации (заменяет оба старых)
    /// </summary>
    public enum UnitAnimationState : byte
    {
        Idle = 0,
        Moving = 1,
        Attacking = 2,
        Dead = 3
    }
}