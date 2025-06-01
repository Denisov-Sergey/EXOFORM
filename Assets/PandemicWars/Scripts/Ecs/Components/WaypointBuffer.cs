using Unity.Entities;
using Unity.Mathematics;

namespace PandemicWars.Scripts.Ecs.Components
{
    /// <summary>
    /// Буфер waypoint'ов для навигации
    /// </summary>
    public struct WaypointBuffer : IBufferElementData
    {
        public float3 waypoint;
    }
}