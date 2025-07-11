using Exoform.Scripts.Ecs.Components.UnitLogicComponents;
using Unity.Entities;
using Unity.Mathematics;

namespace EXOFORM.Scripts.Ecs.Components.Spawning
{
    /// <summary>
    /// Запрос на спаун
    /// </summary>
    public struct SpawnRequestComponent : IComponentData
    {
        public UnitType UnitType;
        public int TeamId;
        public float3 Position;
        public quaternion Rotation;
        public Entity PrefabToSpawn;
        public bool UseRandomPosition;
        public SpawnPointType PreferredSpawnType;
    }
}