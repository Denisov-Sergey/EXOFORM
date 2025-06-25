using Unity.Entities;

namespace PandemicWars.Scripts.Ecs.Components
{
    /// <summary>
    /// Компонент для управляемых игроком юнитов
    /// </summary>
    public struct PlayerUnitComponent : IComponentData
    {
        public bool IsSelected;
        public float SelectionRadius; // Радиус для выбора мышью
    }
}