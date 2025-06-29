using Unity.Entities;

namespace Exoform.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Тег для сущностей-маркеров целевых позиций
    /// </summary>
    public struct MovementTargetTag : IComponentData
    {
        public float CreationTime;
    }
}