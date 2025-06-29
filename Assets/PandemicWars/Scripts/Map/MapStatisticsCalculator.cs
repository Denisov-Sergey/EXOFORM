using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PandemicWars.Scripts.Map
{
    /// <summary>
    /// Калькулятор статистики карты - отвечает за подсчет и анализ размещенных объектов
    /// </summary>
    public class MapStatisticsCalculator
    {
        private readonly CityGrid cityGrid;
        
        public int TotalCells { get; private set; }
        public Dictionary<TileType, int> BaseTileStats { get; private set; }
        public Dictionary<TileType, int> BuildingStats { get; private set; }
        public Dictionary<TileType, int> VegetationStats { get; private set; }
        public Dictionary<TileType, int> ResourceStats { get; private set; }
        public Dictionary<TileType, int> RoadObjectStats { get; private set; }
        public Dictionary<TileType, int> DecorationStats { get; private set; }

        public MapStatisticsCalculator(CityGrid grid)
        {
            cityGrid = grid;
            BaseTileStats = new Dictionary<TileType, int>();
            BuildingStats = new Dictionary<TileType, int>();
            VegetationStats = new Dictionary<TileType, int>();
            ResourceStats = new Dictionary<TileType, int>();
            RoadObjectStats = new Dictionary<TileType, int>();
            DecorationStats = new Dictionary<TileType, int>();
        }

        public MapStatisticsCalculator CalculateStatistics()
        {
            if (cityGrid?.Grid == null)
            {
                Debug.LogWarning("CityGrid не инициализирован для расчета статистики");
                return this;
            }

            TotalCells = cityGrid.Width * cityGrid.Height;
            CalculateBaseTileStatistics();
            CalculateBuildingStatistics();
            return this;
        }

        private void CalculateBaseTileStatistics()
        {
            BaseTileStats.Clear();
            
            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    TileType tileType = cityGrid.Grid[x][y];
                    if (!BaseTileStats.ContainsKey(tileType))
                        BaseTileStats[tileType] = 0;
                    BaseTileStats[tileType]++;
                }
            }
        }

        private void CalculateBuildingStatistics()
        {
            ClearBuildingStatistics();

            foreach (var kvp in cityGrid.BuildingOccupancy)
            {
                TileType buildingType = kvp.Key;
                int cellCount = kvp.Value.Count;

                var targetDict = GetTargetDictionary(buildingType);
                targetDict[buildingType] = cellCount;
            }
        }

        private void ClearBuildingStatistics()
        {
            BuildingStats.Clear();
            VegetationStats.Clear();
            ResourceStats.Clear();
            RoadObjectStats.Clear();
            DecorationStats.Clear();
        }

        private Dictionary<TileType, int> GetTargetDictionary(TileType buildingType)
        {
            if (IsVegetationType(buildingType)) return VegetationStats;
            if (IsRoadObjectType(buildingType)) return RoadObjectStats;
            if (IsResourceType(buildingType)) return ResourceStats;
            if (IsDecorationType(buildingType)) return DecorationStats;
            return BuildingStats;
        }

        /// <summary>
        /// Получить общую статистику в виде строки
        /// </summary>
        public string GetStatisticsSummary()
        {
            var summary = $"📊 === СТАТИСТИКА КАРТЫ ===\n";
            summary += $"📏 Размер карты: {cityGrid.Width}x{cityGrid.Height} = {TotalCells} клеток\n\n";

            // Базовые тайлы
            summary += "🗺️ Базовые тайлы:\n";
            foreach (var kvp in BaseTileStats)
            {
                float percentage = (float)kvp.Value / TotalCells * 100f;
                string emoji = TileTypeHelper.GetTileEmoji(kvp.Key);
                summary += $"{emoji} {kvp.Key}: {kvp.Value} клеток ({percentage:F2}%)\n";
            }

            // Категории объектов
            summary += GetCategoryStatistics("🏢 Здания", BuildingStats);
            summary += GetCategoryStatistics("🌳 Растительность", VegetationStats);
            summary += GetCategoryStatistics("⛏️ Ресурсы", ResourceStats);
            summary += GetCategoryStatistics("🚗 Объекты на дорогах", RoadObjectStats);
            summary += GetCategoryStatistics("🎨 Декорации", DecorationStats);

            summary += "========================";
            return summary;
        }

        private string GetCategoryStatistics(string categoryName, Dictionary<TileType, int> categoryStats)
        {
            if (categoryStats.Count == 0) return "";

            string result = $"\n{categoryName}:\n";
            int totalInCategory = categoryStats.Values.Sum();
            float categoryPercentage = (float)totalInCategory / TotalCells * 100f;
            result += $"  Всего: {totalInCategory} клеток ({categoryPercentage:F2}%)\n";

            foreach (var kvp in categoryStats.OrderByDescending(x => x.Value))
            {
                float percentage = (float)kvp.Value / TotalCells * 100f;
                string emoji = TileTypeHelper.GetObjectEmoji(kvp.Key);
                result += $"  {emoji} {kvp.Key}: {kvp.Value} клеток ({percentage:F2}%)\n";
            }

            return result;
        }

        /// <summary>
        /// Получить процент использования свободного места
        /// </summary>
        public float GetFreeSpaceUsagePercentage()
        {
            int roadCells = BaseTileStats.GetValueOrDefault(TileType.RoadStraight, 0);
            int freeCells = TotalCells - roadCells;
            int usedFreeCells = BuildingStats.Values.Sum() + VegetationStats.Values.Sum() + 
                              ResourceStats.Values.Sum() + DecorationStats.Values.Sum();
            
            return freeCells > 0 ? (float)usedFreeCells / freeCells * 100f : 0f;
        }

        /// <summary>
        /// Получить процент использования дорог
        /// </summary>
        public float GetRoadUsagePercentage()
        {
            int roadCells = BaseTileStats.GetValueOrDefault(TileType.RoadStraight, 0);
            int usedRoadCells = RoadObjectStats.Values.Sum();
            
            return roadCells > 0 ? (float)usedRoadCells / roadCells * 100f : 0f;
        }

        /// <summary>
        /// Проверить, есть ли проблемы с распределением
        /// </summary>
        public List<string> GetDistributionWarnings()
        {
            var warnings = new List<string>();

            float freeSpaceUsage = GetFreeSpaceUsagePercentage();
            if (freeSpaceUsage > 95f)
            {
                warnings.Add($"⚠️ Очень высокая плотность застройки: {freeSpaceUsage:F1}%");
            }

            int lootCount = RoadObjectStats.GetValueOrDefault(TileType.SupplyCache, 0);
            if (lootCount < 5)
            {
                warnings.Add($"⚠️ Мало лута на карте: {lootCount} ящиков");
            }

            if (ResourceStats.Values.Sum() == 0)
            {
                warnings.Add("⚠️ На карте нет ресурсов!");
            }

            return warnings;
        }

        // Методы проверки типов
        private bool IsVegetationType(TileType type) => type switch
        {
            TileType.Spore or TileType.SporeCluster or 
            TileType.CorruptedVegetation or  TileType.Forest or 
            TileType.AlienGrowth => true,
            _ => false
        };

        private bool IsRoadObjectType(TileType type) => type switch
        {
            TileType.BrokenCar or TileType.SupplyCache or TileType.Roadblock or TileType.Debris => true,
            _ => false
        };

        private bool IsResourceType(TileType type) => type switch
        {
            TileType.WoodResource or TileType.StoneResource or 
            TileType.BiomassResource or TileType.MetalResource => true,
            _ => false
        };

        private bool IsDecorationType(TileType type) => type switch
        {
            TileType.Decoration => true,
            _ => false
        };
    }

    /// <summary>
    /// Хелпер класс для работы с эмодзи и иконками тайлов
    /// </summary>
    public static class TileTypeHelper
    {
        public static string GetTileEmoji(TileType tileType) => tileType switch
        {
            TileType.Grass => "🟩",
            TileType.RoadStraight => "🛤️",
            _ => "⬜"
        };

        public static string GetObjectEmoji(TileType objectType) => objectType switch
        {
            // Здания
            TileType.Structure => "🏠",
            TileType.LargeStructure => "🏢",
            TileType.ResearchFacility => "🏬",
            TileType.ContainmentUnit => "🏭",
            TileType.BioDome => "🏞️",
            TileType.CommandCenter => "🏛️",
            
            // Растительность
            TileType.Spore => "🌲",
            TileType.SporeCluster => "🌳",
            TileType.CorruptedVegetation => "🌸",
            TileType.Forest => "🌲🌲",
            TileType.AlienGrowth => "🌺",
            
            // Ресурсы
            TileType.WoodResource => "🪵",
            TileType.StoneResource => "🪨",
            TileType.BiomassResource => "🌾",
            TileType.MetalResource => "⚡",
            
            // Дорожные объекты
            TileType.BrokenCar => "🚗",
            TileType.SupplyCache => "📦",
            TileType.Roadblock => "🚧",
            TileType.Debris => "🗑️",
            TileType.Decoration => "🎨",
            
            _ => "🏗️"
        };

        public static Color GetObjectColor(TileType objectType) => objectType switch
        {
            // Здания
            TileType.Structure => new Color(0.3f, 0.3f, 0.8f, 0.8f),
            TileType.LargeStructure => new Color(0.5f, 0.2f, 0.8f, 0.8f),
            TileType.ResearchFacility => new Color(0.8f, 0.2f, 0.5f, 0.8f),
            TileType.ContainmentUnit => new Color(0.8f, 0.5f, 0.2f, 0.8f),
            TileType.BioDome => new Color(0.2f, 0.8f, 0.4f, 0.8f),
            TileType.CommandCenter => new Color(0.8f, 0.8f, 0.2f, 0.8f),

            // Растительность
            TileType.Spore => new Color(0.1f, 0.6f, 0.1f, 0.9f),
            TileType.SporeCluster => new Color(0.0f, 0.5f, 0.0f, 0.9f),
            TileType.CorruptedVegetation => new Color(0.9f, 0.4f, 0.7f, 0.8f),
            TileType.Forest => new Color(0.0f, 0.4f, 0.0f, 0.9f),
            TileType.AlienGrowth => new Color(0.5f, 0.9f, 0.5f, 0.8f),

            // Ресурсы
            TileType.WoodResource => new Color(0.6f, 0.3f, 0.1f, 0.9f),
            TileType.StoneResource => new Color(0.7f, 0.7f, 0.7f, 0.9f),
            TileType.BiomassResource => new Color(0.9f, 0.8f, 0.2f, 0.9f),
            TileType.MetalResource => new Color(0.4f, 0.4f, 0.6f, 0.9f),

            // Декорации
            TileType.Decoration => new Color(0.7f, 0.5f, 0.8f, 0.7f),

            _ => new Color(0.3f, 0.3f, 0.8f, 0.8f)
        };

        public static Color GetObjectOutlineColor(TileType objectType) => objectType switch
        {
            // Здания
            TileType.Structure => Color.blue,
            TileType.LargeStructure => Color.magenta,
            TileType.ResearchFacility => Color.red,
            TileType.ContainmentUnit => new Color(1f, 0.5f, 0f),
            TileType.BioDome => Color.green,
            TileType.CommandCenter => Color.yellow,

            // Растительность
            TileType.Spore => new Color(0f, 0.8f, 0f),
            TileType.SporeCluster => new Color(0f, 0.6f, 0f),
            TileType.CorruptedVegetation => new Color(1f, 0.5f, 0.8f),
            TileType.Forest => new Color(0f, 0.5f, 0f),
            TileType.AlienGrowth => new Color(0.3f, 1f, 0.3f),

            // Ресурсы
            TileType.WoodResource => new Color(0.8f, 0.4f, 0.1f),
            TileType.StoneResource => new Color(0.9f, 0.9f, 0.9f),
            TileType.BiomassResource => new Color(1f, 0.9f, 0f),
            TileType.MetalResource => new Color(0.6f, 0.6f, 0.8f),

            // Дорожные объекты
            TileType.BrokenCar => new Color(0.6f, 0.4f, 0.2f),
            TileType.Roadblock => Color.red,
            TileType.Debris => new Color(0.7f, 0.7f, 0.7f),
            TileType.SupplyCache => new Color(1f, 0.85f, 0f),
            TileType.Decoration => new Color(0.8f, 0.6f, 0.9f),

            _ => Color.blue
        };
    }
}