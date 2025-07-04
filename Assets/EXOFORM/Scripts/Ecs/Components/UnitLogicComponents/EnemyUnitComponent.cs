﻿using Unity.Entities;

namespace Exoform.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Компонент для вражеских юнитов
    /// </summary>
    public struct EnemyUnitComponent : IComponentData
    {
        public Entity DefaultTarget; // Игрок или база для атаки
        public float AggroRange;     // Дистанция обнаружения
    }
}