using UnityEngine;

namespace VoxelEngine.Generation.Noise
{
    /// <summary>
    /// Реализует технику Domain Warping для создания сложных шумовых структур
    /// путем искажения координат выборки шума
    /// </summary>
    public class DomainWarping
    {
        private NoiseGenerator _noise;
        private float _warpStrength;

        /// <summary>
        /// Инициализирует генератор доменных искажений
        /// </summary>
        /// <param name="seed">Сид для генерации шума</param>
        /// <param name="warpStrength">Сила искажения координат (по умолчанию: 50f)</param>
        public DomainWarping(int seed, float warpStrength = 50f)
        {
            _noise = new NoiseGenerator(seed);
            _warpStrength = warpStrength;
        }

        /// <summary>
        /// Применяет доменное искажение к указанным координатам
        /// </summary>
        /// <param name="x">Исходная X-координата</param>
        /// <param name="z">Исходная Z-координата</param>
        /// <returns>Вектор с искаженными координатами</returns>
        /// <remarks>
        /// Реализация использует два независимых шума Перлина с:
        /// - Масштабированием координат (0.1f)
        /// - Сдвигом на 1000 единиц для второго измерения
        /// - Умножением на силу искажения
        /// </remarks>
        public Vector2 Warp(float x, float z)
        {
            float warpX = _noise.GetPerlin(x * 0.1f, z * 0.1f) * _warpStrength;
            float warpZ = _noise.GetPerlin(x * 0.1f + 1000, z * 0.1f + 1000) * _warpStrength;
            return new Vector2(x + warpX, z + warpZ);
        }
    }
}