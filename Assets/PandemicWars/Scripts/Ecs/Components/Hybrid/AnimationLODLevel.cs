namespace PandemicWars.Scripts.Ecs.Components.Hybrid
{
    public enum AnimationLODLevel : byte
    {
        High = 0,    // Полная анимация 60fps
        Medium = 1,  // Анимация 30fps
        Low = 2,     // Анимация 15fps
        Disabled = 3 // Отключена анимация
    }
}