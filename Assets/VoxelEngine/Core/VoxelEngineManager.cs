using UnityEngine;
using VoxelEngine.Rendering.Meshing;

namespace VoxelEngine.Core
{
    /// <summary>
    /// Главный управляющий класс ядра Voxel Engine.
    /// Обеспечивает централизованный доступ к основным системам движка,
    /// реализует паттерн Singleton для глобальной доступности.
    /// </summary>
    public class VoxelEngineManager : MonoBehaviour
    {
        /// <summary>
        /// Статическая ссылка на единственный экземпляр менеджера (Singleton pattern)
        /// </summary>
        public static VoxelEngineManager Instance { get; private set; }
        
        [Header("Модули")]
        [Tooltip("Менеджер чанков, отвечает за их создание, обновление и удаление")]
        [SerializeField] private ChunkManager _chunkManager;
        
        [Tooltip("Мешер, реализующий алгоритм Greedy Meshing для оптимизации мешей")]
        [SerializeField] private GreedyMesher _greedyMesher;
        
        /// <summary>
        /// Инициализация Singleton при загрузке объекта
        /// </summary>
        void Awake() {
            // Реализация паттерна Singleton
            if (Instance == null)
            {
                Instance = this;
                // Сохраняем объект между сценами при необходимости
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // Уничтожаем дублирующие экземпляры
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Менеджер чанков (только для чтения)
        /// </summary>
        public ChunkManager ChunkManager => _chunkManager;

        /// <summary>
        /// Оптимизированный мешер для генерации мешей вокселей (только для чтения)
        /// </summary>
        public GreedyMesher GreedyMesher => _greedyMesher;
    }
}