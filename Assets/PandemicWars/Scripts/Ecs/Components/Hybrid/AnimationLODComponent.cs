using Unity.Entities;

namespace PandemicWars.Scripts.Ecs.Components.Hybrid
{
    /// <summary>
    /// Компонент для контроля LOD анимации
    /// </summary>
    public struct AnimationLODComponent : IComponentData
    {
        public AnimationLODLevel CurrentLOD;
        public float DistanceToCamera;
        public bool ForceHighLOD; // Для выбранных юнитов
    }
}