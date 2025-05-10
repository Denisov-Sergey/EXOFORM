using UnityEngine;
using System.Collections;

public class Chunk : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private int blockSize = 16;
    [SerializeField] private float delayBetweenLayers = 0.5f;
    void Start()
    {
        StartCoroutine(GenerateBlockLayerByLayer());
    }

    IEnumerator GenerateBlockLayerByLayer()
    {
        GameObject blockParent = new GameObject("GeneratedBlock");

        for (int y = 0; y < blockSize; y++)
        {
            for (int x = 0; x < blockSize; x++)
            {
                for (int z = 0; z < blockSize; z++)
                {
                    Vector3 position = new Vector3(x, y, z);
                    Instantiate(cubePrefab, position, Quaternion.identity, blockParent.transform);
                }
            }
            yield return new WaitForSeconds(delayBetweenLayers); // Ждём 0.5 сек
        }
    }
}