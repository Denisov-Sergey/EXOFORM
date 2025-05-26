using UnityEngine;
using UnityEditor;

namespace WorldGenerator.Core
{
    /// <summary>
    /// Управляет Unity компонентами для рендеринга террейна (MeshFilter, MeshRenderer).
    /// Автоматически создает необходимые компоненты и настраивает материалы.
    /// </summary>
    public class TerrainRenderer
    {
        private readonly GameObject _gameObject;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material _defaultMaterial;

        public TerrainRenderer(GameObject gameObject)
        {
            _gameObject = gameObject ?? throw new System.ArgumentNullException(nameof(gameObject));
            InitializeComponents();
        }

        /// <summary>
        /// Инициализирует необходимые Unity компоненты для рендеринга.
        /// </summary>
        private void InitializeComponents()
        {
            SetupMeshFilter();
            SetupMeshRenderer();
        }

        /// <summary>
        /// Настраивает компонент MeshFilter, создавая его при необходимости.
        /// </summary>
        private void SetupMeshFilter()
        {
            _meshFilter = _gameObject.GetComponent<MeshFilter>();
            if (_meshFilter == null)
            {
                _meshFilter = _gameObject.AddComponent<MeshFilter>();
#if UNITY_EDITOR
                Undo.RecordObject(_gameObject, "Add MeshFilter");
#endif
            }
        }

        /// <summary>
        /// Настраивает компонент MeshRenderer с материалом по умолчанию.
        /// </summary>
        private void SetupMeshRenderer()
        {
            _meshRenderer = _gameObject.GetComponent<MeshRenderer>();
            if (_meshRenderer == null)
            {
                _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
#if UNITY_EDITOR
                Undo.RecordObject(_gameObject, "Add MeshRenderer");
#endif
                
                // Создаем материал по умолчанию
                if (_defaultMaterial == null)
                {
                    _defaultMaterial = new Material(Shader.Find("Standard"))
                    {
                        color = new Color(0.8f, 0.8f, 0.8f, 1f)
                    };
                }

                _meshRenderer.material = _defaultMaterial;
            }
        }

        /// <summary>
        /// Применяет сгенерированный меш к объекту.
        /// </summary>
        /// <param name="mesh">Меш для применения</param>
        public void ApplyMesh(Mesh mesh)
        {
            if (mesh == null)
            {
                Debug.LogError("Cannot apply null mesh!");
                return;
            }

            _meshFilter.mesh = mesh;

#if UNITY_EDITOR
            EditorUtility.SetDirty(_meshFilter);
            SceneView.RepaintAll();
#endif
        }

        /// <summary>
        /// Устанавливает материал для рендеринга террейна.
        /// </summary>
        /// <param name="material">Материал для применения</param>
        public void SetMaterial(Material material)
        {
            if (_meshRenderer != null && material != null)
            {
                _meshRenderer.material = material;
            }
        }

        /// <summary>
        /// Получает текущий меш объекта.
        /// </summary>
        /// <returns>Текущий меш или null</returns>
        public Mesh GetCurrentMesh()
        {
            return _meshFilter?.mesh;
        }
    }
}
