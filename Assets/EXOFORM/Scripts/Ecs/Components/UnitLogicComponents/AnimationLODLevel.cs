﻿namespace Exoform.Scripts.Ecs.Components.UnitLogicComponents
{
    /// <summary>
    /// Уровни LOD для анимации
    /// </summary>
    public enum AnimationLODLevel : byte
    {
        High = 0,    // Полная анимация 60fps
        Medium = 1,  // Анимация 30fps
        Low = 2,     // Анимация 15fps
        Disabled = 3 // Отключена анимация
    }
}