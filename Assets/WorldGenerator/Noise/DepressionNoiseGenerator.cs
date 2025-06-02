using System;
using UnityEngine;
using VoxelEngine.Generation.Noise;
using WorldGenerator.Abstract;
using WorldGenerator.Factory;
using WorldGenerator.Interface;
using WorldGenerator.Settings;

namespace WorldGenerator.Noise
{
    /// <summary>
    /// Генератор впадин для создания различных типов углублений в рельефе
    /// Поддерживает разные формы, случайные вариации и плавные переходы
    /// </summary>
    public class DepressionNoiseGenerator : INoiseGenerator
    {
        #region Private Fields
        
        /// <summary>Основной генератор шума для создания впадин</summary>
        private FastNoiseLite _noise;
        
        /// <summary>Настройки генератора впадин</summary>
        private DepressionSettings _settings;
        
        /// <summary>Базовый генератор для комбинирования с впадинами (опционально)</summary>
        private readonly INoiseGenerator _baseGenerator;
        
        /// <summary>Кэшированные значения для оптимизации</summary>
        private float _cachedThresholdRange;
        private float _cachedRotationRadians;
        private float _cachedCos, _cachedSin;
        
        #endregion

        #region Constructor
        
        /// <summary>
        /// Инициализирует новый генератор впадин с заданными настройками
        /// </summary>
        /// <param name="settings">Настройки генератора впадин</param>
        /// <exception cref="ArgumentNullException">Если настройки равны null</exception>
        public DepressionNoiseGenerator(DepressionSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings)); // Исправлено: убрана лишняя точка с запятой
            
            // Создаем базовый генератор если он указан в настройках
            if (_settings.baseNoise != null)
            {
                _baseGenerator = NoiseFactory.CreateGenerator(_settings.baseNoise);
            }
            
            ConfigureNoise();
        }
        
        #endregion

        #region Noise Configuration
        
        /// <summary>
        /// Настраивает основной генератор шума и кэширует часто используемые значения
        /// </summary>
        private void ConfigureNoise()
        {
            // Инициализация основного генератора шума
            _noise = new FastNoiseLite(_settings.seed + 1000); // +1000 для избежания конфликтов с другими генераторами
            _noise.SetNoiseType(FastNoiseLite.NoiseType.Cellular); // Cellular noise лучше всего подходит для впадин
            _noise.SetCellularReturnType(FastNoiseLite.CellularReturnType.Distance); // Возвращаем расстояние до ближайшей точки
            _noise.SetFrequency(1f / _settings.depressionScale); // Обратная зависимость: больше масштаб = меньше частота
            _noise.SetFractalOctaves(_settings.octaves); // Количество слоев детализации
            
            // Настраиваем форму впадин
            ConfigureShapeNoise();
            
            // Кэшируем часто используемые значения для оптимизации
            CacheComputedValues();
        }
        
        /// <summary>
        /// Настраивает функцию расстояния в зависимости от типа формы впадин
        /// </summary>
        private void ConfigureShapeNoise()
        {
            switch (_settings.shapeType)
            {
                case DepressionSettings.DepressionShape.Circular:
                    // Euclidean distance создает круглые формы
                    _noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Euclidean);
                    break;
                case DepressionSettings.DepressionShape.Manhattan:
                    // Manhattan distance создает квадратные/ромбовидные формы
                    _noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Manhattan);
                    break;
                case DepressionSettings.DepressionShape.Hybrid:
                    // Hybrid distance комбинирует разные формы
                    _noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Hybrid);
                    break;
                case DepressionSettings.DepressionShape.Oval:
                    // Для овальных форм используем Euclidean + трансформацию координат
                    _noise.SetCellularDistanceFunction(FastNoiseLite.CellularDistanceFunction.Euclidean);
                    break;
            }
        }
        
        /// <summary>
        /// Кэширует вычисляемые значения для повышения производительности
        /// </summary>
        private void CacheComputedValues()
        {
            // Кэшируем диапазон порога для нормализации
            _cachedThresholdRange = 1f - _settings.threshold;
            
            // Кэшируем тригонометрические функции для поворота
            if (_settings.rotation != 0f)
            {
                _cachedRotationRadians = _settings.rotation * Mathf.Deg2Rad;
                _cachedCos = Mathf.Cos(_cachedRotationRadians);
                _cachedSin = Mathf.Sin(_cachedRotationRadians);
            }
        }
        
        #endregion

        #region Shape Processing
        
        /// <summary>
        /// Обрабатывает форму впадины в зависимости от настроек
        /// </summary>
        /// <param name="x">X координата</param>
        /// <param name="y">Y координата</param>
        /// <param name="depressionValue">Исходное значение шума</param>
        /// <returns>Обработанное значение с учетом формы</returns>
        private float ProcessDepressionShape(float x, float y, float depressionValue)
        {
            // Только для овальных форм применяем трансформацию координат
            if (_settings.shapeType == DepressionSettings.DepressionShape.Oval)
            {
                // Трансформируем координаты для создания овальной формы
                Vector2 transformedPos = TransformPosition(x, y);
                
                // Перегенерируем шум с трансформированными координатами
                depressionValue = _noise.GetNoise(transformedPos.x, transformedPos.y);
            }
            
            return depressionValue;
        }
        
        /// <summary>
        /// Трансформирует позицию для создания овальных форм с поворотом
        /// </summary>
        /// <param name="x">Исходная X координата</param>
        /// <param name="y">Исходная Y координата</param>
        /// <returns>Трансформированная позиция</returns>
        private Vector2 TransformPosition(float x, float y)
        {
            // Применяем масштабирование по X для создания овальной формы
            float scaledX = x / _settings.aspectRatio;
            float scaledY = y;
            
            // Применяем поворот если он задан
            if (_settings.rotation != 0f)
            {
                // Используем кэшированные значения для оптимизации
                float rotatedX = scaledX * _cachedCos - scaledY * _cachedSin;
                float rotatedY = scaledX * _cachedSin + scaledY * _cachedCos;
                
                return new Vector2(rotatedX, rotatedY);
            }
            
            return new Vector2(scaledX, scaledY);
        }
        
        #endregion

        #region Public Interface Implementation
        
        /// <summary>
        /// Генерирует карту высот с примененными впадинами
        /// </summary>
        /// <param name="width">Ширина карты</param>
        /// <param name="height">Высота карты</param>
        /// <returns>Двумерный массив значений высот</returns>
        public float[,] GenerateNoiseMap(int width, int height)
        {
            float[,] baseMap;
            
            // Получаем базовую карту от другого генератора или создаем пустую
            if (_baseGenerator != null)
            {
                baseMap = _baseGenerator.GenerateNoiseMap(width, height);
            }
            else
            {
                // Создаем пустую карту (все значения = 0)
                baseMap = new float[width, height];
            }
            
            // Применяем впадины к базовой карте
            return ApplyDepressionsToMap(baseMap, width, height);
        }

        /// <summary>
        /// Применяет впадины к уже существующей карте высот
        /// </summary>
        /// <param name="originalMap">Исходная карта высот</param>
        /// <returns>Карта с примененными впадинами</returns>
        public float[,] ApplyDepressionsToExternalMap(float[,] originalMap)
        {
            int width = originalMap.GetLength(0);
            int height = originalMap.GetLength(1);
            return ApplyDepressionsToMap(originalMap, width, height);
        }
        
        #endregion

        #region Depression Application
        
        /// <summary>
        /// Применяет впадины к карте высот - УЛУЧШЕННАЯ ВЕРСИЯ
        /// </summary>
        /// <param name="originalMap">Исходная карта</param>
        /// <param name="width">Ширина карты</param>
        /// <param name="height">Высота карты</param>
        /// <returns>Карта с примененными впадинами</returns>
        private float[,] ApplyDepressionsToMap(float[,] originalMap, int width, int height)
        {
            float[,] depressedMap = new float[width, height];
            
            // Применяем впадины используя оптимизированный метод для точек
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Используем единую логику из ApplyDepressionToPoint
                    depressedMap[x, y] = ApplyDepressionToPoint(originalMap[x, y], x, y);
                }
            }
            
            return depressedMap;
        }

        /// <summary>
        /// Применяет впадину к конкретной точке с учетом всех настроек
        /// </summary>
        /// <param name="originalValue">Исходное значение высоты</param>
        /// <param name="x">X координата</param>
        /// <param name="y">Y координата</param>
        /// <returns>Новое значение высоты с примененной впадиной</returns>
        public float ApplyDepressionToPoint(float originalValue, int x, int y)
        {
            // Получаем значение шума с учетом формы
            float depressionValue = ProcessDepressionShape(x, y, _noise.GetNoise(x, y));
            
            // Проверяем, превышает ли значение порог активации впадины
            if (depressionValue > _settings.threshold)
            {
                // Нормализуем значение от порога до 1.0
                float normalizedDepression = (depressionValue - _settings.threshold) / _cachedThresholdRange;
                
                // Применяем функцию сглаживания для контроля крутизны склонов
                float smoothedDepression = Mathf.Pow(normalizedDepression, _settings.smoothness);
                
                // Добавляем случайную вариацию глубины
                float variationMultiplier = CalculateVariationMultiplier(x, y);
                
                // Вычисляем итоговую силу впадины
                float depressionStrength = smoothedDepression * _settings.strength * variationMultiplier;
                
                // Применяем впадину (вычитаем из исходного значения)
                return originalValue - depressionStrength;
            }
            
            // Возвращаем исходное значение если впадина не активна
            return originalValue;
        }
        
        /// <summary>
        /// Вычисляет множитель случайной вариации для данной точки
        /// </summary>
        /// <param name="x">X координата</param>
        /// <param name="y">Y координата</param>
        /// <returns>Множитель вариации от (1 - variation) до (1 + variation)</returns>
        private float CalculateVariationMultiplier(int x, int y)
        {
            // Быстрая проверка: если вариация отключена, возвращаем 1
            if (_settings.depthVariation <= 0f)
                return 1f;
            
            // Получаем псевдослучайное значение от 0 до 1
            float randomVariation = GetRandomVariation(x, y);
            
            // Преобразуем в диапазон от -variation до +variation и прибавляем к 1
            return 1f + (randomVariation - 0.5f) * _settings.depthVariation;
        }

        /// <summary>
        /// Генерирует псевдослучайное значение на основе координат (детерминированное)
        /// </summary>
        /// <param name="x">X координата</param>
        /// <param name="y">Y координата</param>
        /// <returns>Псевдослучайное значение от 0 до 1</returns>
        private float GetRandomVariation(int x, int y)
        {
            // Используем простую, но эффективную хеш-функцию
            // Большие простые числа обеспечивают хорошее распределение
            int hash = (x * 73856093) ^ (y * 19349663) ^ (_settings.seed * 83492791);
            
            // Преобразуем в диапазон 0-1
            return ((hash & 0x7fffffff) % 1000) / 1000f;
        }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Создает маску впадин без применения к высотам
        /// </summary>
        /// <param name="width">Ширина маски</param>
        /// <param name="height">Высота маски</param>
        /// <returns>Маска впадин (0 = нет впадины, >0 = есть впадина)</returns>
        public float[,] GetDepressionMask(int width, int height)
        {
            float[,] depressionMask = new float[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float depressionValue = ProcessDepressionShape(x, y, _noise.GetNoise(x, y));
                    
                    // Записываем только значения выше порога
                    depressionMask[x, y] = depressionValue > _settings.threshold ? depressionValue : 0f;
                }
            }
            
            return depressionMask;
        }
        
        #endregion

        #region Settings Management
        
        /// <summary>
        /// Обновляет настройки генератора
        /// </summary>
        /// <param name="settings">Новые настройки</param>
        /// <exception cref="ArgumentException">Если переданы настройки неподходящего типа</exception>
        public void UpdateNoiseMap(object settings)
        {
            if (settings is DepressionSettings newSettings)
            {
                _settings = newSettings;
                ConfigureNoise(); // Перенастраиваем генератор с новыми параметрами
                
                // Обновляем базовый генератор если он есть и в новых настройках тоже есть базовый шум
                if (_baseGenerator != null && newSettings.baseNoise != null)
                {
                    _baseGenerator.UpdateNoiseMap(newSettings.baseNoise);
                }
            }
            else
            {
                throw new ArgumentException($"Settings not supported: {settings?.GetType().Name}");
            }
        }

        /// <summary>
        /// Возвращает текущие настройки генератора
        /// </summary>
        /// <returns>Настройки генератора</returns>
        public NoiseSettings GetSettings() => _settings;
        
        #endregion
    }
}
