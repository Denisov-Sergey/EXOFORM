using UnityEngine;
using VoxelEngine.Generation.Noise;

[ExecuteInEditMode]
public class NoiseVisualizer : MonoBehaviour 
{
    /*[Header("Noise Settings")]
    [SerializeField] private float _noiseScale = 50f;
    [SerializeField] private float _offsetX = 0;
    [SerializeField] private float _offsetZ = 0;
    
    [Header("Visualization")]
    [SerializeField] [Range(128, 2048)] private int _textureResolution = 512;
    [SerializeField] private FilterMode _filterMode = FilterMode.Point;
    [SerializeField] private Gradient _colorMap;

    private Texture2D _noiseTexture;
    private Material _material;
    private NoiseGenerator _noiseGenerator;

    void Start()
    {
        InitializeComponents();
        GenerateNewTexture();
    }

    void Update()
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            GenerateNewTexture();
        }
    }

    private void InitializeComponents()
    {
        _noiseGenerator = FindObjectOfType<VoxelEngine.Generation.Noise.NoiseGenerator>();
        _material = GetComponent<Renderer>().sharedMaterial;
        
        if(_material == null)
        {
            _material = new Material(Shader.Find("Standard"));
            GetComponent<Renderer>().sharedMaterial = _material;
        }
    }

    private void GenerateNewTexture()
    {
        if (_noiseGenerator == null) return;

        _noiseTexture = new Texture2D(_textureResolution, _textureResolution)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = _filterMode
        };

        Vector3 planeScale = transform.localScale;
        float worldScaleX = planeScale.x * 10f;
        float worldScaleZ = planeScale.z * 10f;

        for (int y = 0; y < _textureResolution; y++)
        {
            for (int x = 0; x < _textureResolution; x++)
            {
                float xCoord = _offsetX + x / (float)_textureResolution * worldScaleX;
                float yCoord = _offsetZ + y / (float)_textureResolution * worldScaleZ;
                
                float noiseValue = Mathf.Clamp01(
                    _noiseGenerator.GetPerlin(xCoord / _noiseScale, yCoord / _noiseScale)
                );
                
                _noiseTexture.SetPixel(x, y, _colorMap.Evaluate(noiseValue));
            }
        }
        
        _noiseTexture.Apply();
        _material.mainTexture = _noiseTexture;
    }

    public void RandomizeOffset()
    {
        _offsetX = Random.Range(0f, 1000f);
        _offsetZ = Random.Range(0f, 1000f);
        GenerateNewTexture();
    }*/
}