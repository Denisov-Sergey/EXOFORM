using UnityEngine;
using VoxelEngine.Generation.Noise;

namespace WorldGenerator.Abstract
{
    public abstract class NoiseSettings : ScriptableObject
    {
        [Header("Noise Settings")] 
        [Tooltip("Сид для генерации шума. Измените, чтобы получить другую карту.")]
        public int seed = 12345;
        
    }
}