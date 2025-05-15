using UnityEngine;

namespace VoxelEngine.Core
{
    /// <summary>
    /// Класс чанка, управляющий воксельными данными и визуализацией
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        [Header("Chunk Settings")]
        [SerializeField, Tooltip("Размер чанка в вокселях")] 
        private int _size = 16;
        
        [SerializeField, Tooltip("Физический размер одного вокселя")] 
        private float _voxelSize = 0.1f;

        // Автоматические свойства
        public float VoxelSize => _voxelSize;
        public int Size => _size;
        public float WorldSize => _size * _voxelSize;
        public Vector3Int ChunkPosition { get; private set; }
        public VoxelData[,,] Voxels { get; private set; }

        // Ссылки на компоненты
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private bool _isDataDirty;

        #region Initialization
        void Awake()
        {
            ValidateComponents();
            InitializeVoxelArray();
        }
        /// <summary>
        /// Инициализация массива вокселей с заполнением значениями по умолчанию
        /// </summary>
        private void InitializeVoxelArray()
        {
            Voxels = new VoxelData[_size, _size, _size];
            
            // Заполнение массива воздухом
            // for(int x = 0; x < _size; x++)
            // {
            //     for(int y = 0; y < _size; y++)
            //     {
            //         for(int z = 0; z < _size; z++)
            //         {
            //             Voxels[x, y, z] = new VoxelData { Type = VoxelType.Air };
            //         }
            //     }
            // }
        }
        /// <summary>
        /// Проверка и получение необходимых компонентов
        /// </summary>
        private void ValidateComponents()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            
            if (_meshFilter.mesh == null)
                _meshFilter.mesh = new Mesh();
        }

        /// <summary>
        /// Инициализация чанка с указанной позицией
        /// </summary>
        public void Initialize(Vector3Int position)
        {
            ChunkPosition = position;
            transform.position = position.ToVector3() * WorldSize;
            transform.position += new Vector3(WorldSize, WorldSize, WorldSize) * 0.5f;

        }
        #endregion

        #region Data Management
        /// <summary>
        /// Обновление воксельных данных с перестроением меша
        /// </summary>
        public void SetVoxelData(VoxelData[,,] data)
        {
            if (data.GetLength(0) != _size || 
                data.GetLength(1) != _size || 
                data.GetLength(2) != _size)
            {
                Debug.LogError("Invalid voxel data size!");
                return;
            }

            Voxels = data;
            MarkForUpdate();
        }

        /// <summary>
        /// Пометить чанк для обновления меша
        /// </summary>
        public void MarkForUpdate() => _isDataDirty = true;
        #endregion

        #region Mesh Generation
        void Update()
        {
            if (_isDataDirty)
            {
                RebuildMesh();
                _isDataDirty = false;
            }
        }

        /// <summary>
        /// Перестроение меша чанка
        /// </summary>
        private void RebuildMesh()
        {
            var mesh = VoxelEngineManager.Instance.GreedyMesher.GenerateMesh(Voxels);
            ApplyMesh(mesh);
        }

        /// <summary>
        /// Применение сгенерированного меша к компонентам
        /// </summary>
        private void ApplyMesh(Mesh mesh)
        {
            _meshFilter.mesh = mesh;
            _meshCollider.sharedMesh = mesh;
            _meshRenderer.material = VoxelEngineManager.Instance.VoxelMaterial;
            
            // Масштабирование трансформа для коллайдера
            // transform.localScale = Vector3.one * _voxelSize; 
        }
        #endregion

        #region Editor Checks
        void OnValidate()
        {
            // Ограничение минимальных значений
            _size = Mathf.Max(1, _size);
            _voxelSize = Mathf.Max(0.01f, _voxelSize);
        }
        #endregion
    }

    public static class Vector3IntExtensions
    {
        public static Vector3 ToVector3(this Vector3Int v) => 
            new Vector3(v.x, v.y, v.z);
    }
}