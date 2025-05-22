using UnityEngine;

namespace WorldGenerator.Settings
{
    [CreateAssetMenu(fileName = "MeshSettings", menuName = "Noise/Mesh Settings")]
    public class MeshSettings : ScriptableObject
    {
        [Tooltip("Ширина генерируемой карты.")]
        public int width = 200;
        
        [Tooltip("Высота генерируемой карты.")]
        public int height = 200;
        
        [Tooltip("Множитель высоты ландшафта.")]
        public float heightMultiplier = 15f;
    }
}