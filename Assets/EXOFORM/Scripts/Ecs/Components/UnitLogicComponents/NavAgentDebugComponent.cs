using Unity.Entities;
using Unity.Mathematics;

namespace Exoform.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Компонент для отладки и визуализации состояния навигации
    /// </summary>
    public struct NavAgentDebugComponent : IComponentData
    {
        public bool ShowDebugInfo;
        public bool ShowPath;
        public float4 PathColor;
    }
}