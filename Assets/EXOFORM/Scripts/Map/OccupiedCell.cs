using UnityEngine;

namespace Exoform.Scripts.Map
{
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