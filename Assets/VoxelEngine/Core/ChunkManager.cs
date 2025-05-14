using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine.Core
{
    public class ChunkManager : MonoBehaviour
    {
        public event Action<Chunk> OnChunkLoad;

        [Header("Настройки")]
        [SerializeField] private GameObject _chunkPrefab;
        [SerializeField] private int _loadDistance = 3;
        // [SerializeField] private float _voxelSize = 1f;
       
        [Header("Генерация в редакторе")]
        [SerializeField] private bool showEditorButton = true;
        [SerializeField] private Vector3Int _editorChunkPosition;
        [SerializeField] private int _editorLoadRadius = 3;
        [SerializeField] private float _voxelSize = 1f;
        [SerializeField] private bool _autoGenerateInEditMode;
        [SerializeField] private bool _clearOnRegenerate = true;
        
        // Для хранения созданных чанков
        private Hashtable _chunks = new Hashtable();
        
        private Transform _player;

        private void OnValidate() 
        {
            if(_autoGenerateInEditMode && !Application.isPlaying)
                GenerateInEditor();
        }
        
        [ContextMenu("Сгенерировать чанки")]
        public void GenerateInEditor()
        {
            if(_clearOnRegenerate) ClearAllChunks();
            
            for (int x = -_editorLoadRadius; x <= _editorLoadRadius; x++)
            {
                for (int z = -_editorLoadRadius; z <= _editorLoadRadius; z++)
                {
                    Vector3Int chunkPos = new Vector3Int(
                        _editorChunkPosition.x + x,
                        _editorChunkPosition.y,
                        _editorChunkPosition.z + z
                    );
                    LoadChunk(chunkPos);
                }
            }
        }
        [ContextMenu("Очистить все чанки")]
        public void ClearAllChunks()
        {
            foreach(DictionaryEntry entry in _chunks)
            {
                DestroyImmediate((GameObject)entry.Value);
            }
            _chunks.Clear();
        }

        private void LoadChunk(Vector3Int chunkPosition)
        {
            if (_chunkPrefab == null)
            {
                Debug.LogError("Chunk Prefab не назначен!");
                return;
            }
            
            if(_chunks.Contains(chunkPosition)) return;
            
            // Проверка компонента Chunk на префабе
            Chunk chunkComponent = _chunkPrefab.GetComponent<Chunk>();
            if (chunkComponent == null)
            {
                Debug.LogError("Префаб чанка не содержит компонент Chunk!");
                return;
            }
            
            GameObject chunkObj = PrefabUtility.InstantiatePrefab(_chunkPrefab) as GameObject;
            
            // Преобразуем Vector3Int в Vector3 перед умножением на floatfloat chunkSize = GetChunkWorldSize();
            chunkObj.transform.position = (Vector3)chunkPosition * GetChunkWorldSize();
            chunkObj.transform.SetParent(transform);
            
            Chunk chunk = chunkObj.GetComponent<Chunk>();
            chunk.Initialize(chunkPosition);
            
            // Вызов события после создания чанка
            OnChunkLoad?.Invoke(chunk);
            
            _chunks.Add(chunkPosition, chunkObj);
        }
        
        
        
#if UNITY_EDITOR
        [UnityEditor.CustomEditor(typeof(ChunkManager))]
        public class ChunkEditor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
            
                ChunkManager chunkManager = (ChunkManager)target;
            
                if (chunkManager.showEditorButton && GUILayout.Button("Сгенерировать чанки"))
                {
                    chunkManager.GenerateInEditor();
                }
            }
        }
#endif

        private float GetChunkWorldSize()
        {
            return _chunkPrefab.GetComponent<Chunk>().Size * _voxelSize;
        }
        
        /*
        void Start() 
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        void Update() 
        {
            Vector3Int playerChunkPos = GetPlayerChunkPosition();
            LoadChunksAround(playerChunkPos);
        }

        // Загрузка чанка по координатам
        public void LoadChunk(Vector3Int chunkPosition) 
        {
            GameObject chunkObj = Instantiate(_chunkPrefab);
            Chunk chunk = chunkObj.GetComponent<Chunk>();
            chunk.Initialize(chunkPosition);
            OnChunkLoad?.Invoke(chunk);
        }

        // Выгрузка чанка
        public void UnloadChunk(Vector3Int chunkPosition) 
        {
            // Логика поиска и уничтожения чанка...
        }

        // Получение позиции игрока в чанках
        private Vector3Int GetPlayerChunkPosition() 
        {
            Vector3 playerPos = _player.position;
            return new Vector3Int(
                Mathf.FloorToInt(playerPos.x / (_chunkPrefab.GetComponent<Chunk>().Size * _voxelSize)),
                Mathf.FloorToInt(playerPos.y / (_chunkPrefab.GetComponent<Chunk>().Size * _voxelSize)),
                Mathf.FloorToInt(playerPos.z / (_chunkPrefab.GetComponent<Chunk>().Size * _voxelSize))
            );
        }

        // Загрузка чанков вокруг игрока
        private void LoadChunksAround(Vector3Int center) 
        {
            for (int x = -_loadDistance; x <= _loadDistance; x++) 
            {
                for (int z = -_loadDistance; z <= _loadDistance; z++) 
                {
                    Vector3Int chunkPos = new Vector3Int(center.x + x, 0, center.z + z);
                    LoadChunk(chunkPos);
                }
            }
        } */
    }    
}

