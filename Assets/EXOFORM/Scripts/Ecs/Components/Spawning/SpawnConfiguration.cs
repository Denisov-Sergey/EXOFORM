using Exoform.Scripts.Ecs.Components.UnitLogicComponents;
using Exoform.Scripts.Map;
using Unity.Entities;

namespace EXOFORM.Scripts.Ecs.Components.Spawning
{
    /// <summary>
    /// Конфигурация спауна для разных типов юнитов
    /// </summary>
    [System.Serializable]
    public struct SpawnConfiguration : IComponentData
    {
        public Entity UnitPrefab;
        public UnitType UnitType;
        public int TeamId;
        public float SpawnRate;          // Интервал между спаунами
        public int MaxUnits;            // Максимальное количество юнитов
        public float SpawnRadius;       // Радиус спауна
        public bool UseZoneSpawning;    // Использовать зоны для спауна
        public TileType PreferredZone;  // Предпочитаемая зона
    }
}