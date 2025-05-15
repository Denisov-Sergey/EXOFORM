using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace VoxelEngine.Core
{
    /// <summary>
    /// Управляет созданием, загрузкой и удалением чанков воксельного мира
    /// </summary>
    public class ChunkManager : MonoBehaviour
    {
        /// <summary>
        /// Событие, вызываемое при загрузке нового чанка
        /// </summary>
        public event Action<Chunk> OnChunkLoad;

        [Header("Основные настройки")]
        [Tooltip("Префаб чанка должен содержать компонент Chunk")]
        [SerializeField] private GameObject _chunkPrefab;
        
        [Tooltip("Дистанция загрузки чанков вокруг игрока (в чанках)")]
        [SerializeField] private int _loadDistance = 3;

        [Header("Редакторные настройки")]
        [Tooltip("Показывать кнопку генерации в инспекторе")]
        [SerializeField] private bool showEditorButton = true;
        
        [Tooltip("Центральная позиция для генерации в редакторе")]
        [SerializeField] private Vector3Int _editorChunkPosition;
        
        [Tooltip("Радиус генерации чанков в редакторе")]
        [SerializeField] private int _editorLoadRadius = 10;
        
        [Tooltip("Очищать старые чанки перед генерацией новых")]
        [SerializeField] private bool _clearOnRegenerate = true;

        /// <summary>
        /// Хранилище созданных чанков (позиция -> GameObject)
        /// </summary>
        private Hashtable _chunks = new Hashtable();
        public GameObject ChunkPrefab => _chunkPrefab;
        
        /// <summary>
        /// Генерация чанков в редакторе вокруг указанной позиции
        /// </summary>
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

        /// <summary>
        /// Удаление всех созданных чанков
        /// </summary>
        [ContextMenu("Очистить все чанки")]
        public void ClearAllChunks()
        {
            foreach(DictionaryEntry entry in _chunks)
            {
                DestroyImmediate((GameObject)entry.Value);
            }
            _chunks.Clear();
        }

        /// <summary>
        /// Загружает чанк по указанной позиции
        /// </summary>
        /// <param name="chunkPosition">Позиция чанка в чанковых координатах</param>
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
            
            chunkObj.transform.position = (Vector3)chunkPosition * GetChunkWorldSize();
            chunkObj.transform.SetParent(transform);
            
            Chunk chunk = chunkObj.GetComponent<Chunk>();
            chunk.Initialize(chunkPosition);
            chunkObj.transform.position = chunk.transform.position;

            OnChunkLoad?.Invoke(chunk);
            
            _chunks.Add(chunkPosition, chunkObj);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Кастомный редактор для ChunkManager
        /// </summary>
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

        /// <summary>
        /// Рассчитывает размер чанка в мировых единицах
        /// </summary>
        /// <returns>Размер чанка с учетом размера вокселя</returns>
        private float GetChunkWorldSize()
        {
            return _chunkPrefab.GetComponent<Chunk>().Size * _chunkPrefab.GetComponent<Chunk>().VoxelSize;
        }
        
        // Зарезервировано для будущей реализации динамической подгрузки
        /*
        void Start() 
        {
            _player = GameObject.FindGameObjectWithTag("Player").transform;
        }

        void Update() 
        {
            Vector3Int playerChunkPos = GetPlayerChunkPosition();
            LoadChunksAround(playerChunkPos);
        }*/
    }    
}