﻿using Unity.Entities;

namespace Exoform.Scripts.Ecs.Components.UnitLogicComponents
{
    /// <summary>
    /// Компонент для контроля LOD анимации
    /// </summary>
    public struct AnimationLODComponent : IComponentData
    {
        public AnimationLODLevel CurrentLOD;
        public float DistanceToCamera;
        public bool ForceHighLOD; // Для выбранных юнитов
    }
}