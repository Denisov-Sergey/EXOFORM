using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PandemicWars.Scripts.Map
{
    /// <summary>
    /// Конфигурация префабов с разделением по категориям для удобства в инспекторе
    /// </summary>
    [System.Serializable]
    public class PrefabConfiguration
    {
        [Header("🏢 Здания")]
        [Tooltip("Префабы зданий различных типов")]
        public List<GameObject> buildingPrefabs = new List<GameObject>();

        [Header("🌳 Растительность")]
        [Tooltip("Префабы деревьев, кустов и других растений")]
        public List<GameObject> vegetationPrefabs = new List<GameObject>();

        [Header("⛏️ Ресурсы")]
        [Tooltip("Префабы ресурсных точек")]
        public List<GameObject> resourcePrefabs = new List<GameObject>();

        [Header("🚗 Дорожные объекты")]
        [Tooltip("Префабы объектов на дорогах (машины, блокпосты, обломки)")]
        public List<GameObject> roadObjectPrefabs = new List<GameObject>();

        [Header("📦 Лут")]
        [Tooltip("Префабы ящиков с лутом")]
        public List<GameObject> lootPrefabs = new List<GameObject>();

        [Header("🎨 Декорации")]
        [Tooltip("Декоративные объекты")]
        public List<GameObject> decorationPrefabs = new List<GameObject>();

        /// <summary>
        /// Получить все префабы в едином списке для обратной совместимости
        /// </summary>
        public List<GameObject> GetAllPrefabs()
        {
            var allPrefabs = new List<GameObject>();
            
            allPrefabs.AddRange(buildingPrefabs.Where(p => p != null));
            allPrefabs.AddRange(vegetationPrefabs.Where(p => p != null));
            allPrefabs.AddRange(resourcePrefabs.Where(p => p != null));
            allPrefabs.AddRange(roadObjectPrefabs.Where(p => p != null));
            allPrefabs.AddRange(lootPrefabs.Where(p => p != null));
            allPrefabs.AddRange(decorationPrefabs.Where(p => p != null));

            return allPrefabs;
        }

        /// <summary>
        /// Получить префабы определенной категории
        /// </summary>
        public List<GameObject> GetPrefabsByCategory(PrefabCategory category)
        {
            return category switch
            {
                PrefabCategory.Buildings => buildingPrefabs.Where(p => p != null).ToList(),
                PrefabCategory.Vegetation => vegetationPrefabs.Where(p => p != null).ToList(),
                PrefabCategory.Resources => resourcePrefabs.Where(p => p != null).ToList(),
                PrefabCategory.RoadObjects => roadObjectPrefabs.Where(p => p != null).ToList(),
                PrefabCategory.Loot => lootPrefabs.Where(p => p != null).ToList(),
                PrefabCategory.Decorations => decorationPrefabs.Where(p => p != null).ToList(),
                _ => new List<GameObject>()
            };
        }

        /// <summary>
        /// Получить префабы с компонентом PrefabSettings определенного типа
        /// </summary>
        public List<GameObject> GetPrefabsWithType(TileType tileType)
        {
            var allPrefabs = GetAllPrefabs();
            var matchingPrefabs = new List<GameObject>();

            foreach (var prefab in allPrefabs)
            {
                var settings = prefab.GetComponent<PrefabSettings>();
                if (settings != null && settings.tileType == tileType)
                {
                    matchingPrefabs.Add(prefab);
                }
            }

            return matchingPrefabs;
        }

        /// <summary>
        /// Валидация префабов - проверяет наличие необходимых компонентов
        /// </summary>
        public PrefabValidationResult ValidatePrefabs()
        {
            var result = new PrefabValidationResult();
            var allPrefabs = GetAllPrefabs();

            foreach (var prefab in allPrefabs)
            {
                if (prefab == null)
                {
                    result.AddWarning("Найден null префаб в списке");
                    continue;
                }

                var settings = prefab.GetComponent<PrefabSettings>();
                if (settings == null)
                {
                    result.AddError($"Префаб {prefab.name} не имеет компонента PrefabSettings");
                    continue;
                }

                // Проверяем соответствие категории и типа
                var expectedCategory = GetExpectedCategory(settings.tileType);
                var actualCategory = GetPrefabCategory(prefab);
                
                if (expectedCategory != actualCategory)
                {
                    result.AddWarning($"Префаб {prefab.name} ({settings.tileType}) находится в неправильной категории. " +
                                    $"Ожидается: {expectedCategory}, Текущая: {actualCategory}");
                }

                result.ValidPrefabsCount++;
            }

            result.TotalPrefabsCount = allPrefabs.Count;
            return result;
        }

        private PrefabCategory GetExpectedCategory(TileType tileType)
        {
            return tileType switch
            {
                TileType.Structure or TileType.LargeStructure or TileType.ResearchFacility or 
                TileType.ContainmentUnit or TileType.BioDome or TileType.CommandCenter => PrefabCategory.Buildings,
                
                TileType.Spore or TileType.SporeCluster or 
                TileType.CorruptedVegetation or  TileType.Forest or 
                TileType.AlienGrowth => PrefabCategory.Vegetation,
                
                TileType.WoodResource or TileType.StoneResource or 
                TileType.BiomassResource or TileType.MetalResource => PrefabCategory.Resources,
                
                TileType.BrokenCar or TileType.Roadblock or TileType.Debris => PrefabCategory.RoadObjects,
                
                TileType.SupplyCache => PrefabCategory.Loot,
                
                TileType.Decoration => PrefabCategory.Decorations,
                
                _ => PrefabCategory.Buildings
            };
        }

        private PrefabCategory GetPrefabCategory(GameObject prefab)
        {
            if (buildingPrefabs.Contains(prefab)) return PrefabCategory.Buildings;
            if (vegetationPrefabs.Contains(prefab)) return PrefabCategory.Vegetation;
            if (resourcePrefabs.Contains(prefab)) return PrefabCategory.Resources;
            if (roadObjectPrefabs.Contains(prefab)) return PrefabCategory.RoadObjects;
            if (lootPrefabs.Contains(prefab)) return PrefabCategory.Loot;
            if (decorationPrefabs.Contains(prefab)) return PrefabCategory.Decorations;
            
            return PrefabCategory.Buildings; // по умолчанию
        }

        /// <summary>
        /// Получить статистику по префабам
        /// </summary>
        public string GetPrefabStatistics()
        {
            var stats = $"📊 === СТАТИСТИКА ПРЕФАБОВ ===\n";
            stats += $"🏢 Зданий: {buildingPrefabs.Count(p => p != null)}\n";
            stats += $"🌳 Растительности: {vegetationPrefabs.Count(p => p != null)}\n";
            stats += $"⛏️ Ресурсов: {resourcePrefabs.Count(p => p != null)}\n";
            stats += $"🚗 Дорожных объектов: {roadObjectPrefabs.Count(p => p != null)}\n";
            stats += $"📦 Лута: {lootPrefabs.Count(p => p != null)}\n";
            stats += $"🎨 Декораций: {decorationPrefabs.Count(p => p != null)}\n";
            stats += $"📝 Всего: {GetAllPrefabs().Count}\n";
            
            return stats;
        }

        /// <summary>
        /// Автоматическая сортировка префабов по категориям (полезно для миграции)
        /// </summary>
        public void AutoSortPrefabs(List<GameObject> unsortedPrefabs)
        {
            foreach (var prefab in unsortedPrefabs)
            {
                if (prefab == null) continue;

                var settings = prefab.GetComponent<PrefabSettings>();
                if (settings == null) continue;

                var category = GetExpectedCategory(settings.tileType);
                var targetList = GetPrefabsByCategory(category);

                // Добавляем только если еще нет в списке
                if (!targetList.Contains(prefab))
                {
                    switch (category)
                    {
                        case PrefabCategory.Buildings:
                            buildingPrefabs.Add(prefab);
                            break;
                        case PrefabCategory.Vegetation:
                            vegetationPrefabs.Add(prefab);
                            break;
                        case PrefabCategory.Resources:
                            resourcePrefabs.Add(prefab);
                            break;
                        case PrefabCategory.RoadObjects:
                            roadObjectPrefabs.Add(prefab);
                            break;
                        case PrefabCategory.Loot:
                            lootPrefabs.Add(prefab);
                            break;
                        case PrefabCategory.Decorations:
                            decorationPrefabs.Add(prefab);
                            break;
                    }
                }
            }
        }
    }

    public enum PrefabCategory
    {
        Buildings,
        Vegetation,
        Resources,
        RoadObjects,
        Loot,
        Decorations
    }

    /// <summary>
    /// Результат валидации префабов
    /// </summary>
    public class PrefabValidationResult
    {
        public int TotalPrefabsCount { get; set; }
        public int ValidPrefabsCount { get; set; }
        public List<string> Errors { get; private set; } = new List<string>();
        public List<string> Warnings { get; private set; } = new List<string>();

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool IsValid => !HasErrors;

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);

        public string GetReport()
        {
            var report = $"🔍 === ОТЧЕТ ВАЛИДАЦИИ ПРЕФАБОВ ===\n";
            report += $"📊 Всего префабов: {TotalPrefabsCount}\n";
            report += $"✅ Валидных: {ValidPrefabsCount}\n";
            report += $"❌ Ошибок: {Errors.Count}\n";
            report += $"⚠️ Предупреждений: {Warnings.Count}\n\n";

            if (HasErrors)
            {
                report += "❌ ОШИБКИ:\n";
                foreach (var error in Errors)
                {
                    report += $"  • {error}\n";
                }
                report += "\n";
            }

            if (HasWarnings)
            {
                report += "⚠️ ПРЕДУПРЕЖДЕНИЯ:\n";
                foreach (var warning in Warnings)
                {
                    report += $"  • {warning}\n";
                }
                report += "\n";
            }

            if (IsValid && !HasWarnings)
            {
                report += "🎉 Все префабы настроены корректно!";
            }

            return report;
        }
    }
}