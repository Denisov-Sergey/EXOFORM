using UnityEngine;

namespace VoxelEngine.Generation.Noise
{
    /// <summary>
    /// Генератор различных типов шума для процедурной генерации мира
    /// </summary>
    public class NoiseGenerator
    {
        private FastNoiseLite _noise;
        
        /// <summary>
        /// Инициализирует генератор шума с указанным сидом
        /// </summary>
        /// <param name="seed">Сид для инициализации генератора</param>
        public NoiseGenerator(int seed)
        {
            _noise = new FastNoiseLite(seed);
            _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        }

        /// <summary>
        /// Получить значение шума Перлина в указанных координатах
        /// </summary>
        /// <param name="x">X-координата в шумовом пространстве</param>
        /// <param name="z">Z-координата в шумовом пространстве</param>
        /// <returns>Значение шума в диапазоне [-1..1]</returns>
        public float GetPerlin(float x, float z)
        {
            return _noise.GetNoise(x, z);
        }

        /// <summary>
        /// Получить значение шума OpenSimplex2 в указанных координатах
        /// </summary>
        /// <param name="x">X-координата в шумовом пространстве</param>
        /// <param name="z">Z-координата в шумовом пространстве</param>
        /// <returns>Значение шума в диапазоне [-1..1]</returns>
        public float GetSimplex(float x, float z)
        {
            _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
            return _noise.GetNoise(x, z);
        }

        /// <summary>
        /// Получить риджжен (гребенчатый) шум с фрактальными свойствами
        /// </summary>
        /// <param name="x">X-координата в шумовом пространстве</param>
        /// <param name="z">Z-координата в шумовом пространстве</param>
        /// <returns>Значение шума в диапазоне [-1..1]</returns>
        /// <remarks>
        /// Настройки фрактала:
        /// - 6 октав для детализации
        /// - Коэффициент усиления 0.5 для сглаживания
        /// </remarks>
        public float GetRidged(float x, float z)
        {
            _noise.SetNoiseType(FastNoiseLite.NoiseType.Perlin);
            _noise.SetFractalType(FastNoiseLite.FractalType.Ridged);
            _noise.SetFractalOctaves(6);
            _noise.SetFractalGain(0.5f);
    
            return _noise.GetNoise(x, z);
        }
    }
}