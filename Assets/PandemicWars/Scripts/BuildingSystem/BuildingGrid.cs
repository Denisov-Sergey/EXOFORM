using System.Drawing;
using UnityEngine;
using Color = UnityEngine.Color;

namespace PandemicWars.Scripts.BuildingSystem
{
    public class BuildingGrid : MonoBehaviour
    {
        [SerializeField] private Vector2Int _gridSize = Vector2Int.one;
        
        
        private void OnDrawGizmosSelected()
        {
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    Gizmos.color = new Color(0.06f, 1f, 0.33f, 0.5f);
                    if ((x+y) %2 == 0)
                    {
                        Gizmos.color = new Color(0.13f, 0.3f, 1f, 0.5f);
                    }
                    Gizmos.DrawCube(transform.position + new Vector3(x, 0, y), new Vector3(1, 0 ,1));
                }
            }
        }
    }
}