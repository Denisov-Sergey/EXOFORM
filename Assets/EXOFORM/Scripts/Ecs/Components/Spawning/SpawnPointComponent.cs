using Exoform.Scripts.Map;
using Unity.Entities;
using Unity.Mathematics;

namespace EXOFORM.Scripts.Ecs.Components.Spawning
{
    /// <summary>
    /// Компонент для точек спауна
    /// </summary>
    public struct SpawnPointComponent : IComponentData
    {
        public float3 Position;
        public SpawnPointType PointType;
        public TileType ZoneType;
        public bool IsActive;
        public float CooldownTime;
        public float LastUsedTime;
    }
}