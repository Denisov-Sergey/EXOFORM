using UnityEngine;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Информация о занятой клетке и ориентации объекта
    /// </summary>
    public struct OccupiedCell
    {
        public Vector2Int Cell;
        public int Rotation;

        public OccupiedCell(Vector2Int cell, int rotation)
        {
            Cell = cell;
            Rotation = rotation;
        }
    }
}
