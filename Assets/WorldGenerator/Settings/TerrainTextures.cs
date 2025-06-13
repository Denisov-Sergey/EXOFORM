using UnityEngine;
using WorldGenerator.Abstract;


namespace WorldGenerator.Settings {
    
    [CreateAssetMenu(fileName = "TerrainTextures", menuName = "WorldGenerator/Terrain Textures")]
    public class TerrainTextures : NoiseSettings 
    {
        [Header("Textures for Different Heights")]
        public Texture2D valleyTexture;    // Впадины (низкие места)
        public Texture2D plainTexture;     // Равнины (средние высоты)
        public Texture2D hillTexture;      // Холмы (высокие места)
        public Texture2D peakTexture;      // Пики (самые высокие места)
    
        [Header("Height Thresholds (0-1)")]
        [Range(0f, 1f)] public float valleyHeight = 0.25f;
        [Range(0f, 1f)] public float plainHeight = 0.5f;
        [Range(0f, 1f)] public float hillHeight = 0.75f;
    
        [Header("Texture Settings")]
        public float textureScale = 1f;
        [Range(0f, 0.3f)] public float blendSmoothness = 0.1f;
        
        [Header("Pixelation Settings")]
        [Range(1f, 100f)] public float pixelationFactor = 16f;
        [Range(0f, 1f)] public float pixelSnap = 0.8f;
    
        [Header("Auto-Setup")]
        [SerializeField] private bool autoConfigureTextures = true;
    
        private void OnValidate()
        {
            base.OnValidate();
            
            if (autoConfigureTextures)
            {
                ConfigureTexturesForPixelArt();
            }
        }
    
        private void ConfigureTexturesForPixelArt()
        {
            ConfigureTexture(valleyTexture);
            ConfigureTexture(plainTexture);
            ConfigureTexture(hillTexture);
            ConfigureTexture(peakTexture);
        }
    
        private void ConfigureTexture(Texture2D texture)
        {
            if (texture == null) return;
        
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(texture);
            var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
        
            if (importer != null)
            {
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 128;
                importer.SaveAndReimport();
            }
#endif
        }
    }
}