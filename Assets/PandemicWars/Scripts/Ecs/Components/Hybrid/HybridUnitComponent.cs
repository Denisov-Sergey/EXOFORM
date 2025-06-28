using Unity.Entities;

namespace PandemicWars.Scripts.Ecs.Components.Hybrid
{
    /// <summary>
    /// Компонент связи между Entity и GameObject
    /// </summary>
    public struct HybridUnitComponent : IComponentData
    {
        public Entity LinkedGameObject; // Ссылка на GameObject через Entity
        public bool AnimationEnabled;
        public float LastSyncTime;
        public float SyncInterval; // Как часто синхронизировать (для оптимизации)
    }
}