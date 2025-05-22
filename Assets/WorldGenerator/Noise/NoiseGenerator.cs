using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using WorldGenerator.Factory;
using WorldGenerator.Interface;
using WorldGenerator.Settings;

namespace WorldGenerator.Noise
{
    public class NoiseGenerator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private BaseNoiseSettings baseNoiseSettings;
        [SerializeField] private VoronoiSettings voronoiSettings;
        [SerializeField] private CrackSettings crackSettings;
        [SerializeField] private DepressionSettings depressionSettings;
        [SerializeField] private DomainWarpSettings warpSettings;
        [SerializeField] private MeshSettings meshSettings;
        
        private INoiseGenerator _noiseGenerator;
        private float[,] _cachedNoiseMap;
        private int _settingsHash;

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            throw new NotImplementedException();
        }

        private void Initialize()
        {
            var newHash = baseNoiseSettings.GetHashCode();
            
            if (_noiseGenerator == null && _settingsHash == newHash) return;
            
            _noiseGenerator = NoiseFactory.CreateGenerator(baseNoiseSettings);
            _settingsHash = newHash;
        }
        
        private void HandleNoiseUpdate() {
            if(NeedsRegeneration()) {
                RegenerateTerrain();
            }
        }
        private bool NeedsRegeneration() {
            // Проверка изменений настроек
            return baseNoiseSettings.GetHashCode() != _settingsHash;
        }
        public void RegenerateTerrain() {
            // Использование пула мешей
            // Mesh oldMesh = _meshFilter.sharedMesh;
            // Mesh newMesh = _meshPool.GetMesh();
        
            if(_cachedNoiseMap == null) {
                _cachedNoiseMap = _noiseGenerator.GenerateNoiseMap(meshSettings.width, meshSettings.height);
            }
        
            // ... генерация меша ...
            // _meshFilter.sharedMesh = newMesh;
            // _meshPool.ReturnMesh(oldMesh);
        }
        
    }
}
