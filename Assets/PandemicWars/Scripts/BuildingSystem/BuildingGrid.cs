using UnityEngine;

namespace PandemicWars.Scripts.BuildingSystem
{
    /// <summary>
    /// Компонент для определения размера сетки здания
    /// </summary>
    public class BuildingGrid : MonoBehaviour
    {
        [Header("Размер сетки")]
        [Tooltip("Размер здания в клетках сетки (X - ширина, Y - длина)")]
        [SerializeField] private Vector2Int _gridSize = Vector2Int.one;
        
        [Header("Настройки")]
        [Tooltip("Размер одной клетки сетки в мировых единицах")]
        [SerializeField] private float _cellSize = 1f;
        
        [Header("Визуализация (только в редакторе)")]
        [Tooltip("Показывать сетку в редакторе")]
        [SerializeField] private bool _showGridInEditor = true;
        
        [Tooltip("Высота отображения сетки")]
        [SerializeField] private float _gizmoHeight = 0.1f;
        
        [Tooltip("Цвет для четных клеток")]
        [SerializeField] private Color _evenCellColor = new Color(0.06f, 1f, 0.33f, 0.5f);
        
        [Tooltip("Цвет для нечетных клеток")]
        [SerializeField] private Color _oddCellColor = new Color(0.13f, 0.3f, 1f, 0.5f);

        #region Public Properties
        
        /// <summary>
        /// Размер сетки здания
        /// </summary>
        public Vector2Int GridSize => _gridSize;
        
        /// <summary>
        /// Размер одной клетки
        /// </summary>
        public float CellSize => _cellSize;
        
        /// <summary>
        /// Общий размер здания в мировых координатах
        /// </summary>
        public Vector3 WorldSize => new Vector3(_gridSize.x * _cellSize, 0, _gridSize.y * _cellSize);
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Привязывает позицию к сетке с учетом размера здания
        /// </summary>
        /// <param name="worldPosition">Позиция в мировых координатах</param>
        /// <returns>Привязанная к сетке позиция</returns>
        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            Vector3 snappedPosition = worldPosition;
            
            // Привязываем к сетке с учетом размера клетки
            snappedPosition.x = Mathf.Round(worldPosition.x / _cellSize) * _cellSize;
            snappedPosition.z = Mathf.Round(worldPosition.z / _cellSize) * _cellSize;
            snappedPosition.y = worldPosition.y;
            
            return snappedPosition;
        }
        
        #endregion
        
        #region Validation
        
        /// <summary>
        /// Валидация компонента при изменении значений в инспекторе
        /// </summary>
        private void OnValidate()
        {
            // Ограничиваем минимальные значения
            if (_gridSize.x < 1) _gridSize.x = 1;
            if (_gridSize.y < 1) _gridSize.y = 1;
            if (_cellSize < 0.1f) _cellSize = 0.1f;
        }
        
        #endregion
        
        #region Gizmos
        
        /// <summary>
        /// Отображает сетку в редакторе при выборе объекта
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_showGridInEditor)
            {
                DrawGrid();
            }
        }
        
        /// <summary>
        /// Рисует сетку здания
        /// </summary>
        private void DrawGrid()
        {
            Vector3 worldSize = WorldSize;
            Vector3 startPos = transform.position - worldSize * 0.5f;
            
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    // Альтернативный цвет для шахматного паттерна
                    Gizmos.color = (x + y) % 2 == 0 ? _evenCellColor : _oddCellColor;
                    
                    Vector3 cellPosition = startPos + new Vector3(
                        (x + 0.5f) * _cellSize,
                        _gizmoHeight * 0.5f,
                        (y + 0.5f) * _cellSize
                    );
                    
                    Vector3 cellSize = new Vector3(_cellSize * 0.9f, _gizmoHeight, _cellSize * 0.9f);
                    
                    Gizmos.DrawCube(cellPosition, cellSize);
                }
            }
            
            // Рисуем границы здания
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(transform.position, worldSize + Vector3.up * _gizmoHeight);
        }
        
        #endregion
    }
}
