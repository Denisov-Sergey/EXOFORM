using Unity.Entities;
using Unity.Mathematics;

namespace PandemicWars.Scripts.Ecs.Components
{
    public struct WaypointBuffer : IBufferElementData
    {
        public float3 waypoint;
    }
}