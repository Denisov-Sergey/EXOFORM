﻿using Unity.Entities;

namespace Exoform.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Компонент для отметки выбранных юнитов, которые будут реагировать на команды движения
    /// </summary>
    public struct SelectedUnitComponent : IComponentData
    {
        public bool IsSelected;
    }
    
}