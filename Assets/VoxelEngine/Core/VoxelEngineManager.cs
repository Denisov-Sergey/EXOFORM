using UnityEngine;
using VoxelEngine.Rendering.Meshing;

namespace VoxelEngine.Core
{
    public class VoxelEngineManager : MonoBehaviour
    {
        public static VoxelEngineManager Instance { get; private set; }
        
        [Header("Модули")]
        [SerializeField] private ChunkManager _chunkManager;
        [SerializeField] private GreedyMesher _greedyMesher;
        
        void Awake() {
            // Реализация Singleton
            // if (Instance == null) Instance = this;
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Опционально, если нужно сохранить между сценами
            }
            else
            {
                Destroy(gameObject); // Уничтожаем дубликаты
            }
        }

        public ChunkManager ChunkManager => _chunkManager;
        
        public GreedyMesher GreedyMesher => _greedyMesher;
    }
}
