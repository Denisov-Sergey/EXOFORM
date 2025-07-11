using Unity.Entities;

namespace EXOFORM.Scripts.Ecs.Components.Spawning
{
    /// <summary>
    /// Компонент спаунера (Singleton Entity)
    /// </summary>
    public struct SpawnerStateComponent : IComponentData
    {
        public float LastSpawnTime;
        public int CurrentPlayerCount;
        public int CurrentEnemyCount;
        public int WaveNumber;
        public float NextWaveTime;
        public bool IsActive;
    }
}