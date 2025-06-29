using Unity.Entities;
using Unity.Mathematics;

namespace Exoform.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Компонент для группового управления юнитами
    /// </summary>
    public struct UnitGroupComponent : IComponentData
    {
        public int GroupId;
        public bool IsGroupLeader;
        public float3 FormationOffset; // Смещение в строю относительно лидера
    }
}