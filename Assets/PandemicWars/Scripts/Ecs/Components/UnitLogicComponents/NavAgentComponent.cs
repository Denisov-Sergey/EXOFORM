using Unity.Entities;
using Unity.Mathematics;

namespace PandemicWars.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Базовый компонент навигации для всех юнитов
    /// </summary>
    public struct NavAgentComponent : IComponentData
    {
        public Entity TargetEntity;
        public bool PathCalculated;
        public int CurrentWaypoint;
        public float MovementSpeed;
        public float NextPathCalculatedTime;
        public int MaxPathIterations;
    }
}