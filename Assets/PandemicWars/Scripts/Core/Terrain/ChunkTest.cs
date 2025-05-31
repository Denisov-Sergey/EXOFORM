using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using UnityEngine;

public class ChunkTest : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private GameObject voxelPrefab; 
    [SerializeField] private float voxelSize = 1f; 
    [SerializeField] private int mapSize = 64; 
    [SerializeField] private float noiseScale = 20f;
    [SerializeField] private float heightMultiplier = 10f; 

    [Header("Настройки генерации")]
    [SerializeField] private float delayPerColumn = 0.01f;
    
    [Header("Генерация")]
    [SerializeField] private bool autoGenerateOnStart = true;
    [SerializeField] private bool showEditorButton = true;

    private List<GameObject> voxels = new List<GameObject>(); // Список для хранения созданных кубов
    private bool _isGeneratorWork = false;
    
    // mapSize = 64 / 0.1 = 640
    private int WorldSize => Mathf.RoundToInt(mapSize / voxelSize); 
    
    void Start()
    {
        if (autoGenerateOnStart)
            StartCoroutine(GenerateTerrain());

    }

    public IEnumerator GenerateTerrain()
    {
        _isGeneratorWork = true;
        ClearTerrain();
        
        float seed = Random.Range(0f, 1000f); // Добавляем случайное смещение для генерации
        
        for (int x = 0; x < WorldSize; x++)
        {
            for (int z = 0; z < WorldSize; z++)
            {
                // Генерация высоты с использованием 3D-шума
                float height = Mathf.PerlinNoise(
                    (x + seed) / noiseScale, 
                    (z + seed) / noiseScale
                ) * heightMultiplier;

                
                GameObject voxel = Instantiate(
                    voxelPrefab, 
                    new Vector3(x * voxelSize, height * voxelSize, z * voxelSize), 
                    Quaternion.identity, 
                    transform
                );
                voxel.transform.localScale = Vector3.one * voxelSize;
                voxels.Add(voxel);
                
            }
            
            yield return new WaitForSeconds(delayPerColumn);
        }
        
        Debug.Log($"Сгенерировано вокселей: {voxels.Count}");
       
        _isGeneratorWork = false;
    }

    void ClearTerrain()
    {
        foreach (GameObject voxel in voxels)
        {
            if (voxel != null)
                Destroy(voxel);
        }
        voxels.Clear();
    }

    // 1. Кнопка через ContextMenu (правая кнопка мыши на компоненте)
    [ContextMenu("Сгенерировать")]
    public void RegenerateTerrain()
    {
        if (!_isGeneratorWork)
        {
            StartCoroutine(GenerateTerrain());
        }
    }

    // 2. Кнопка в окне инспектора (только в редакторе)
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(ChunkTest))]
    public class ChunkEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            ChunkTest chunk = (ChunkTest)target;
            
            if (chunk.showEditorButton && GUILayout.Button("Перегенерировать"))
            {
                chunk.RegenerateTerrain();
            }
        }
    }
    #endif
}