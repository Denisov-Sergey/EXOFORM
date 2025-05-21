using UnityEngine;

namespace WorldGenerator.Settings
{
    [System.Serializable]
    public class MeshSettings
    {
        public int width = 200;
        
        public int height = 200;
        
        public float heightMultiplier = 15f;
        
        [Range(1, 5)] public float sharpness = 5f;
        
        [Range(0, 1)] public float quantizeSteps = 0.1f;
    }
}