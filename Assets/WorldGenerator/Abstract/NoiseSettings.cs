using UnityEngine;
using VoxelEngine.Generation.Noise;

namespace WorldGenerator.Abstract
{
    public abstract class NoiseSettings : ScriptableObject
    {
        public event System.Action OnSettingsChanged;

        protected virtual void OnValidate()
        {
            OnSettingsChanged?.Invoke();
        }
    
        private void OnEnable()
        {
            OnSettingsChanged = null; // Очищаем при загрузке
        }
        
        [Header("Noise Settings")] 
        [Tooltip("Сид для генерации шума. Измените, чтобы получить другую карту.")]
        public int seed = 12345;
        
        [Tooltip("Масштаб шума. Меньше = более детализировано.")]
        public float scale = 250f;
    }
}