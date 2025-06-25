using Unity.Entities;

namespace PandemicWars.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Компонент для настроек навигации
    /// </summary>
    public struct NavMeshAgentSettings : IComponentData
    {
        public float StoppingDistance;    // Дистанция остановки перед целью
        public float Acceleration;        // Ускорение
        public float AngularSpeed;        // Скорость поворота (градусы/сек)
        public int AreaMask;             // Маска областей NavMesh
        public bool AutoRepath;          // Автоматический пересчет пути при препятствиях
    }
}