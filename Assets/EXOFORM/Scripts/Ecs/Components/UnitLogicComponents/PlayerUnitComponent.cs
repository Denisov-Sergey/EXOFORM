using Unity.Entities;

namespace Exoform.Scripts.Ecs.Components
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