using Unity.Entities;
using Unity.Mathematics;

namespace Exoform.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Буфер waypoint'ов для навигации
    /// </summary>
    public struct WaypointBuffer : IBufferElementData
    {
        public float3 waypoint;
    }
}