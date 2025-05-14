using UnityEngine;

namespace VoxelEngine.Core
{
    public class Chunk : MonoBehaviour
    {
        public VoxelData[,,] Voxels { get; private set; }
        public Vector3Int ChunkPosition { get; private set; }

        [Header("Настройки")]
        [SerializeField] private int _size = 16;
        [SerializeField] private float _voxelSize = 1f;
        public int Size => _size;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private bool _isDataDirty;

        void Awake() 
        {
            Voxels = new VoxelData[_size, _size, _size];
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        public void Initialize(Vector3Int position) 
        {
            ChunkPosition = position;
            transform.position = ((Vector3)position) * (_size * _voxelSize);;
        }

        public void SetVoxelData(VoxelData[,,] data) 
        {
            Voxels = data;
            _isDataDirty = true;
        }

        void Update() 
        {
            if (_isDataDirty) 
            {
                RebuildMesh();
                _isDataDirty = false;
            }
        }

        private void RebuildMesh() 
        {
            Mesh mesh = VoxelEngineManager.Instance.GreedyMesher.GenerateMesh(Voxels);
            _meshFilter.mesh = mesh;
            _meshRenderer.material = VoxelEngineManager.Instance.VoxelMaterial; 
        }
    }
}