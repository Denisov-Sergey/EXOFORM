using UnityEngine;
using WorldGenerator.Settings;
using WorldGenerator.Noise;

namespace WorldGenerator.Core
{
    /// <summary>
    /// Отвечает за композицию различных типов шума в единую карту высот.
    /// Применяет постобработку (резкость, квантование) и специальные эффекты.
    /// </summary>
    public class NoiseCompositor
    {
        private readonly NoiseSettingsManager _settingsManager;
        private readonly NoiseGeneratorRegistry _generatorRegistry;

        public NoiseCompositor(NoiseSettingsManager settingsManager, NoiseGeneratorRegistry generatorRegistry)
        {
            _settingsManager = settingsManager ?? throw new System.ArgumentNullException(nameof(settingsManager));
            _generatorRegistry = generatorRegistry ?? throw new System.ArgumentNullException(nameof(generatorRegistry));
        }

        /// <summary>
        /// Создает композитную карту шума, объединяя различные типы генераторов.
        /// Применяет все активные эффекты в правильном порядке.
        /// </summary>
        /// <param name="width">Ширина карты</param>
        /// <param name="height">Высота карты</param>
        /// <returns>Двумерный массив значений высот</returns>
        public float[,] GenerateCompositeNoiseMap(int width, int height)
        {
            var noiseMap = new float[width, height];

            // 1. Получаем базовую карту шума
            var baseMap = GenerateBaseNoiseMap(width, height);

            // 2. Применяем Domain Warping к базовой карте (влияет на все последующие эффекты)
            if (_settingsManager.UseDomainWarp && _generatorRegistry.HasGenerator<DomainWarpSettings>())
            {
                baseMap = ApplyDomainWarping(baseMap);
            }

            // 3. Применяем постобработку к каждой точке
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var noiseValue = baseMap[x, y];

                    // Применяем эффекты в правильном порядке
                    noiseValue = ApplySharpness(noiseValue);
                    noiseValue = ApplyDepressions(noiseValue, x, y);
                    noiseValue = ApplyCracks(noiseValue, x, y);
                    noiseValue = ApplyQuantization(noiseValue);

                    noiseMap[x, y] = noiseValue;
                }
            }

            return noiseMap;
        }

        /// <summary>
        /// Генерирует базовую карту шума на основе приоритета типов генераторов.
        /// Приоритет: Voronoi > Base > Combined > Flat map
        /// </summary>
        private float[,] GenerateBaseNoiseMap(int width, int height)
        {
            // Приоритет генераторов согласно оригинальной логике
            if (_settingsManager.UseVoronoiNoise && _generatorRegistry.HasGenerator<VoronoiSettings>())
            {
                return _generatorRegistry.GetGenerator<VoronoiSettings>()
                    .GenerateNoiseMap(width, height);
            }

            if (_settingsManager.UseBaseNoise && _generatorRegistry.HasGenerator<BaseNoiseSettings>())
            {
                return _generatorRegistry.GetGenerator<BaseNoiseSettings>()
                    .GenerateNoiseMap(width, height);
            }

            if (_settingsManager.UseCombinedNoise && _generatorRegistry.HasGenerator<CombinedNoiseSettings>())
            {
                return _generatorRegistry.GetGenerator<CombinedNoiseSettings>()
                    .GenerateNoiseMap(width, height);
            }

            // Возвращаем плоскую карту если нет активных генераторов
            Debug.LogWarning("No active noise generators found, returning flat map");
            return new float[width, height];
        }

        /// <summary>
        /// Применяет Domain Warping к карте шума для искажения координат.
        /// </summary>
        private float[,] ApplyDomainWarping(float[,] baseMap)
        {
            var warpGenerator = _generatorRegistry.GetGenerator<DomainWarpSettings>() as DomainWarpNoiseGenerator;
            return warpGenerator?.ApplyWarpingToExternalMap(baseMap) ?? baseMap;
        }

        /// <summary>
        /// Применяет эффект резкости к значению шума.
        /// Увеличивает контраст между высокими и низкими значениями.
        /// </summary>
        private float ApplySharpness(float noiseValue)
        {
            var sharpness = _settingsManager.GetCurrentSharpness();
            if (sharpness <= 0) return noiseValue;

            var sharpened = Mathf.Pow(Mathf.Abs(noiseValue), sharpness);
            return sharpened * Mathf.Sign(noiseValue);
        }

        /// <summary>
        /// Применяет эффект впадин к конкретной точке.
        /// </summary>
        private float ApplyDepressions(float noiseValue, int x, int y)
        {
            if (!_settingsManager.UseDepressions || !_generatorRegistry.HasGenerator<DepressionSettings>())
                return noiseValue;

            var depressionGenerator = _generatorRegistry.GetGenerator<DepressionSettings>() as DepressionNoiseGenerator;
            return depressionGenerator?.ApplyDepressionToPoint(noiseValue, x, y) ?? noiseValue;
        }

        /// <summary>
        /// Применяет эффект трещин к конкретной точке.
        /// </summary>
        private float ApplyCracks(float noiseValue, int x, int y)
        {
            if (!_settingsManager.UseCracks || !_generatorRegistry.HasGenerator<CrackSettings>())
                return noiseValue;

            var crackGenerator = _generatorRegistry.GetGenerator<CrackSettings>() as CrackNoiseGenerator;
            return crackGenerator?.ApplyCrackToPoint(noiseValue, x, y) ?? noiseValue;
        }

        /// <summary>
        /// Применяет квантование к значению шума (создает ступенчатый эффект).
        /// </summary>
        private float ApplyQuantization(float noiseValue)
        {
            var quantizeSteps = _settingsManager.GetCurrentQuantizeSteps();
            if (quantizeSteps <= 0) return noiseValue;

            return Mathf.Round(noiseValue / quantizeSteps) * quantizeSteps;
        }
    }
}
