using System.Collections.Generic;
using UnityEngine;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Система управления зонами EXOFORM
    /// </summary>
    public class ExoformZoneSystem
    {
        private CityGrid cityGrid;
        private Dictionary<Vector2Int, ZoneData> zones;
        private List<Vector2Int> allZonePositions;
        
        [System.Serializable]
        public struct ZoneData
        {
            public TileType zoneType;
            public Vector2Int position;
            public Vector2Int size;
            public float difficultyLevel;      // 1.0 - обычная, 2.0 - сложная
            public bool isCleared;            // Очищена ли зона
            public float corruptionLevel;     // 0-1, уровень заражения
            public List<string> rewards;      // Что можно получить
            public List<string> enemies;      // Типы врагов
            public bool hasSpecialEvent;     // Есть ли особое событие
            public string specialEventType;  // Тип события
        }
        
        public ExoformZoneSystem(CityGrid grid)
        {
            cityGrid = grid;
            zones = new Dictionary<Vector2Int, ZoneData>();
            allZonePositions = new List<Vector2Int>();
        }
        
        /// <summary>
        /// Разметить карту на зоны (вызывается генератором)
        /// </summary>
        public void InitializeZones(int zoneWidth = 10, int zoneHeight = 10)
        {
            zones.Clear();
            allZonePositions.Clear();
            
            // Разбиваем карту на прямоугольные зоны
            for (int x = 0; x < cityGrid.Width; x += zoneWidth)
            {
                for (int y = 0; y < cityGrid.Height; y += zoneHeight)
                {
                    Vector2Int zonePos = new Vector2Int(x, y);
                    Vector2Int actualSize = new Vector2Int(
                        Mathf.Min(zoneWidth, cityGrid.Width - x),
                        Mathf.Min(zoneHeight, cityGrid.Height - y)
                    );
                    
                    // Определяем тип зоны
                    TileType zoneType = DetermineZoneType(zonePos, actualSize);
                    
                    var zoneData = new ZoneData
                    {
                        zoneType = zoneType,
                        position = zonePos,
                        size = actualSize,
                        difficultyLevel = CalculateDifficulty(zoneType, zonePos),
                        isCleared = false,
                        corruptionLevel = CalculateInitialCorruption(zoneType),
                        rewards = GenerateRewards(zoneType),
                        enemies = GenerateEnemies(zoneType),
                        hasSpecialEvent = Random.value < GetSpecialEventChance(zoneType),
                        specialEventType = GenerateSpecialEvent(zoneType)
                    };
                    
                    zones[zonePos] = zoneData;
                    allZonePositions.Add(zonePos);
                }
            }
            
            Debug.Log($"🗺️ Создано {zones.Count} зон EXOFORM");
        }
        
        /// <summary>
        /// Определить тип зоны на основе позиции
        /// </summary>
        private TileType DetermineZoneType(Vector2Int position, Vector2Int size)
        {
            // Расстояние от центра карты
            Vector2 center = new Vector2(cityGrid.Width / 2f, cityGrid.Height / 2f);
            float distanceFromCenter = Vector2.Distance(position, center);
            float maxDistance = Vector2.Distance(Vector2.zero, center);
            float normalizedDistance = distanceFromCenter / maxDistance;
            
            // Чем дальше от центра, тем опаснее
            if (normalizedDistance < 0.3f)
            {
                // Центральные зоны - в основном стандартные
                return Random.value < 0.8f ? TileType.StandardZone : TileType.TechnicalZone;
            }
            else if (normalizedDistance < 0.6f)
            {
                // Средние зоны - микс стандартных и технических
                float chance = Random.value;
                if (chance < 0.5f) return TileType.StandardZone;
                if (chance < 0.8f) return TileType.TechnicalZone;
                return TileType.ArtifactZone;
            }
            else
            {
                // Внешние зоны - опасные
                float chance = Random.value;
                if (chance < 0.3f) return TileType.StandardZone;
                if (chance < 0.5f) return TileType.TechnicalZone;
                if (chance < 0.8f) return TileType.ArtifactZone;
                return TileType.CorruptedTrap;
            }
        }
        
        private float CalculateDifficulty(TileType zoneType, Vector2Int position)
        {
            float baseDifficulty = zoneType switch
            {
                TileType.StandardZone => 1.0f,
                TileType.TechnicalZone => 1.3f,
                TileType.ArtifactZone => 1.7f,
                TileType.CorruptedTrap => 2.5f,
                _ => 1.0f
            };
            
            // Добавляем случайную вариацию ±20%
            float variation = Random.Range(0.8f, 1.2f);
            return baseDifficulty * variation;
        }
        
        private float CalculateInitialCorruption(TileType zoneType)
        {
            return zoneType switch
            {
                TileType.StandardZone => Random.Range(0f, 0.1f),
                TileType.TechnicalZone => Random.Range(0.1f, 0.3f),
                TileType.ArtifactZone => Random.Range(0.3f, 0.5f),
                TileType.CorruptedTrap => Random.Range(0.7f, 1.0f),
                _ => 0f
            };
        }
        
        private List<string> GenerateRewards(TileType zoneType)
        {
            var rewards = new List<string>();
            
            switch (zoneType)
            {
                case TileType.StandardZone:
                    rewards.AddRange(new[] { "Metal", "Biomass", "Basic_Equipment" });
                    break;
                case TileType.TechnicalZone:
                    rewards.AddRange(new[] { "Tech_Salvage", "Energy_Cells", "Repair_Kits" });
                    break;
                case TileType.ArtifactZone:
                    rewards.AddRange(new[] { "Artifacts", "Rare_Materials", "Advanced_Tech" });
                    break;
                case TileType.CorruptedTrap:
                    rewards.AddRange(new[] { "Corrupted_Samples", "Biomass", "Danger_Intel" });
                    break;
            }
            
            return rewards;
        }
        
        private List<string> GenerateEnemies(TileType zoneType)
        {
            var enemies = new List<string>();
            
            switch (zoneType)
            {
                case TileType.StandardZone:
                    enemies.AddRange(new[] { "Pawn_Scout", "Small_Mutant" });
                    break;
                case TileType.TechnicalZone:
                    enemies.AddRange(new[] { "Corrupted_Drone", "Tech_Parasite" });
                    break;
                case TileType.ArtifactZone:
                    enemies.AddRange(new[] { "Guardian_Beast", "Psi_Mutant" });
                    break;
                case TileType.CorruptedTrap:
                    enemies.AddRange(new[] { "Corruption_Mass", "Tentacle_Swarm", "Spore_Cloud" });
                    break;
            }
            
            return enemies;
        }
        
        private float GetSpecialEventChance(TileType zoneType)
        {
            return zoneType switch
            {
                TileType.StandardZone => 0.1f,
                TileType.TechnicalZone => 0.2f,
                TileType.ArtifactZone => 0.4f,
                TileType.CorruptedTrap => 0.6f,
                _ => 0f
            };
        }
        
        private string GenerateSpecialEvent(TileType zoneType)
        {
            if (!zones.ContainsKey(Vector2Int.zero)) return "";
            
            return zoneType switch
            {
                TileType.StandardZone => Random.value < 0.5f ? "Resource_Cache" : "Survivor_Group",
                TileType.TechnicalZone => Random.value < 0.5f ? "Malfunctioning_AI" : "Hidden_Lab",
                TileType.ArtifactZone => Random.value < 0.5f ? "Ancient_Vault" : "Psi_Anomaly",
                TileType.CorruptedTrap => Random.value < 0.5f ? "Corruption_Outbreak" : "Hive_Mind",
                _ => ""
            };
        }
        
        /// <summary>
        /// Получить данные зоны по позиции
        /// </summary>
        public ZoneData? GetZoneAt(Vector2Int position)
        {
            // Находим зону, которая содержит эту позицию
            foreach (var kvp in zones)
            {
                var zonePos = kvp.Key;
                var zoneData = kvp.Value;
                
                if (position.x >= zonePos.x && position.x < zonePos.x + zoneData.size.x &&
                    position.y >= zonePos.y && position.y < zonePos.y + zoneData.size.y)
                {
                    return zoneData;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Отметить зону как очищенную
        /// </summary>
        public void ClearZone(Vector2Int zonePosition)
        {
            if (zones.ContainsKey(zonePosition))
            {
                var zone = zones[zonePosition];
                zone.isCleared = true;
                zone.corruptionLevel = 0f;
                zones[zonePosition] = zone;
                
                Debug.Log($"✅ Зона {zone.zoneType} в {zonePosition} очищена!");
            }
        }
        
        /// <summary>
        /// Заразить зону (увеличить уровень порчи)
        /// </summary>
        public void CorruptZone(Vector2Int zonePosition, float corruptionIncrease)
        {
            if (zones.ContainsKey(zonePosition))
            {
                var zone = zones[zonePosition];
                zone.corruptionLevel = Mathf.Clamp01(zone.corruptionLevel + corruptionIncrease);
                zones[zonePosition] = zone;
                
                if (zone.corruptionLevel >= 1.0f)
                {
                    Debug.Log($"☣️ Зона {zone.zoneType} в {zonePosition} полностью заражена!");
                }
            }
        }
        
        /// <summary>
        /// Получить все зоны определенного типа
        /// </summary>
        public List<ZoneData> GetZonesByType(TileType zoneType)
        {
            var result = new List<ZoneData>();
            foreach (var zone in zones.Values)
            {
                if (zone.zoneType == zoneType)
                {
                    result.Add(zone);
                }
            }
            return result;
        }
        
        /// <summary>
        /// Получить статистику зон
        /// </summary>
        public string GetZoneStatistics()
        {
            var stats = new Dictionary<TileType, int>();
            int clearedCount = 0;
            float totalCorruption = 0f;
            
            foreach (var zone in zones.Values)
            {
                if (!stats.ContainsKey(zone.zoneType))
                    stats[zone.zoneType] = 0;
                stats[zone.zoneType]++;
                
                if (zone.isCleared) clearedCount++;
                totalCorruption += zone.corruptionLevel;
            }
            
            var result = "🗺️ === СТАТИСТИКА ЗОН EXOFORM ===\n";
            foreach (var kvp in stats)
            {
                string emoji = GetZoneEmoji(kvp.Key);
                result += $"{emoji} {GetZoneCategory(kvp.Key)}: {kvp.Value}\n";
            }
            
            float avgCorruption = zones.Count > 0 ? totalCorruption / zones.Count : 0f;
            result += $"\n📊 Очищено зон: {clearedCount}/{zones.Count}\n";
            result += $"☣️ Средний уровень заражения: {avgCorruption:F2}";
            
            return result;
        }
        
        private string GetZoneEmoji(TileType zoneType)
        {
            return zoneType switch
            {
                TileType.StandardZone => "🟢",
                TileType.TechnicalZone => "🔧",
                TileType.ArtifactZone => "🧬",
                TileType.CorruptedTrap => "⚠️",
                _ => "❓"
            };
        }
        
        private string GetZoneCategory(TileType zoneType)
        {
            return zoneType switch
            {
                TileType.StandardZone => "Стандартная",
                TileType.TechnicalZone => "Техническая", 
                TileType.ArtifactZone => "Артефактная",
                TileType.CorruptedTrap => "Заражённая",
                _ => "Неизвестная"
            };
        }
        
        /// <summary>
        /// Экспорт данных для ECS систем
        /// </summary>
        public Dictionary<Vector2Int, ZoneData> ExportZoneData()
        {
            return new Dictionary<Vector2Int, ZoneData>(zones);
        }
    }
}