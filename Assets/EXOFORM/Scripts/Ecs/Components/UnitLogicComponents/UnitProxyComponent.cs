using Unity.Entities;
using Unity.Mathematics;

namespace Exoform.Scripts.Ecs.Components.UnitLogicComponents
{
    /// <summary>
    /// компонент для связи ECS ↔ GameObject (упрощенный)
    /// </summary>
    public struct UnitProxyComponent : IComponentData
    {
        public int UnitId; // Уникальный ID для синхронизации
        public bool IsSpawned; // Спавнен ли визуальный GameObject
        public float3 LastSyncPosition; // Последняя синхронизированная позиция
        public quaternion LastSyncRotation;
    }
}