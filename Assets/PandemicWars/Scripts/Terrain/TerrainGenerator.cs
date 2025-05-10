using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private int width = 512;    // Ширина карты
    [SerializeField] private int height = 512;   // Длина карты
    [SerializeField] private float scale = 25f;  // Масштаб шума
    [SerializeField] private float heightMultiplier = 50f; // Высота гор

    void Start()
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, heightMultiplier, height);
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float xCoord = (float)x / width * scale;
                float yCoord = (float)y / height * scale;
                heights[x, y] = Mathf.PerlinNoise(xCoord, yCoord);
            }
        }
        return heights;
    }
}