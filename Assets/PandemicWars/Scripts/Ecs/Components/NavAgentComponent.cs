using Unity.Entities;
using Unity.Mathematics;

namespace PandemicWars.Scripts.Ecs.Components
{
    public struct NavAgentComponent : IComponentData
    {
        public Entity TargetEntity;
        public bool PathCalculated;
        public int CurrentWaypoint;
        public float MovementSpeed;
        public float NextPathCalculatedTime;
    }
}