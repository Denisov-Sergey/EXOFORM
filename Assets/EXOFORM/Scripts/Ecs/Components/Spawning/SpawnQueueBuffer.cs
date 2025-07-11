using Unity.Entities;

namespace EXOFORM.Scripts.Ecs.Components.Spawning
{
    /// <summary>
    /// Буфер для очереди спауна
    /// </summary>
    public struct SpawnQueueBuffer : IBufferElementData
    {
        public SpawnRequestComponent Request;
        public float ScheduledTime;
        public int Priority;
    }
}