using Unity.Entities;

namespace PandemicWars.Scripts.Ecs.Components
{
    /// <summary>
    /// Компонент для отметки выбранных юнитов, которые будут реагировать на команды движения
    /// </summary>
    public struct SelectedUnitComponent : IComponentData
    {
        public bool IsSelected;
    }
    
}