using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Класс для размещения техники для восстановления в технических зонах
    /// </summary>
    public class TechSalvagePlacer
    {
        private CityGrid cityGrid;
        private List<PrefabSettings> techSalvagePrefabs;
        private MonoBehaviour coroutineRunner;
        private Dictionary<PrefabSettings, int> spawnedCounts;
        private ExoformZoneSystem zoneSystem;
        public bool PlayerActivated { get; set; }

        public TechSalvagePlacer(CityGrid grid, List<GameObject> prefabs, MonoBehaviour runner, ExoformZoneSystem zoneSystem)
        {
            cityGrid = grid;
            coroutineRunner = runner;
            spawnedCounts = new Dictionary<PrefabSettings, int>();
            this.zoneSystem = zoneSystem;
            PlayerActivated = false;
            LoadTechSalvagePrefabs(prefabs);
        }

        void LoadTechSalvagePrefabs(List<GameObject> prefabs)
        {
            techSalvagePrefabs = new List<PrefabSettings>();

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null && IsTechSalvageType(settings.tileType))
                    {
                        techSalvagePrefabs.Add(settings);
                    }
                }
            }

            Debug.Log($"🔧 Загружено {techSalvagePrefabs.Count} префабов техники для восстановления");
        }

        bool IsTechSalvageType(TileType type)
        {
            return type == TileType.DamagedGenerator ||
                   type == TileType.BrokenRobot ||
                   type == TileType.CorruptedTerminal ||
                   type == TileType.TechSalvageResource;
        }

        public IEnumerator PlaceTechSalvage(float density, float animationSpeed)
        {
            if (techSalvagePrefabs.Count == 0)
            {
                Debug.Log("  ⚠️ Нет префабов техники для размещения");
                yield break;
            }

            if (!PlayerActivated)
            {
                Debug.Log("  ⏸️ Размещение техники отложено до активации игроком");
                yield break;
            }

            Debug.Log("🔧 === РАЗМЕЩЕНИЕ ТЕХНИКИ ДЛЯ ВОССТАНОВЛЕНИЯ ===");

            // Размещаем технику в технических зонах и рядом со структурами
            yield return PlaceTechInTechnicalZones(density, animationSpeed);
            yield return PlaceTechNearStructures(density * 0.5f, animationSpeed);
            
            LogTechSalvageStatistics();
        }

        IEnumerator PlaceTechInTechnicalZones(float density, float animationSpeed)
        {
            Debug.Log("  🏭 Размещение техники в технических зонах...");
            
            // Находим все технические зоны
            List<Vector2Int> technicalZonePositions = FindTechnicalZonePositions();
            
            if (technicalZonePositions.Count == 0)
            {
                Debug.Log("    ⚠️ Технические зоны не найдены");
                yield break;
            }
            
            int techItemsToPlace = Mathf.RoundToInt(technicalZonePositions.Count * density);
            Debug.Log($"    🎯 Размещаем {techItemsToPlace} единиц техники в {technicalZonePositions.Count} позициях технических зон");
            
            for (int i = 0; i < techItemsToPlace && technicalZonePositions.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, technicalZonePositions.Count);
                Vector2Int position = technicalZonePositions[randomIndex];
                
                var techType = ChooseTechTypeForTechnicalZone();
                
                if (TryPlaceTechSalvage(position, techType))
                {
                    RemoveNearbyPositions(technicalZonePositions, position, 3);
                    yield return new WaitForSeconds(animationSpeed * 0.3f);
                }
                else
                {
                    technicalZonePositions.RemoveAt(randomIndex);
                }
            }
        }

        IEnumerator PlaceTechNearStructures(float density, float animationSpeed)
        {
            Debug.Log("  🏢 Размещение техники рядом со структурами...");
            
            List<Vector2Int> structureNearbyPositions = FindPositionsNearStructures();
            
            int techItemsToPlace = Mathf.RoundToInt(structureNearbyPositions.Count * density);
            Debug.Log($"    🎯 Размещаем {techItemsToPlace} единиц техники рядом со структурами");
            
            for (int i = 0; i < techItemsToPlace && structureNearbyPositions.Count > 0; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, structureNearbyPositions.Count);
                Vector2Int position = structureNearbyPositions[randomIndex];
                
                var techType = ChooseTechTypeForStructures();
                
                if (TryPlaceTechSalvage(position, techType))
                {
                    RemoveNearbyPositions(structureNearbyPositions, position, 2);
                    yield return new WaitForSeconds(animationSpeed * 0.2f);
                }
                else
                {
                    structureNearbyPositions.RemoveAt(randomIndex);
                }
            }
        }

        List<Vector2Int> FindTechnicalZonePositions()
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            for (int x = 0; x < cityGrid.Width; x++)
            {
                for (int y = 0; y < cityGrid.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    if (IsGoodTechnicalPosition(pos) && IsTechnicalZone(pos))
                    {
                        positions.Add(pos);
                    }
                }
            }
            
            return positions;
        }

        bool IsGoodTechnicalPosition(Vector2Int pos)
        {
            if (!cityGrid.IsValidPosition(pos)) return false;
            if (cityGrid.Grid[pos.x][pos.y] != TileType.Grass) return false;
            if (cityGrid.IsCellOccupiedByBuilding(pos)) return false;
            
            // Техника должна быть рядом с путями (в пределах 3 клеток)
            if (!HasPathwayNearby(pos, 3)) return false;
            
            // Но не слишком близко к путям (минимум 2 клетки)
            if (HasPathwayNearby(pos, 1)) return false;
            
            // Предпочитаем места рядом со структурами
            return HasStructureNearby(pos, 5);
        }

        List<Vector2Int> FindPositionsNearStructures()
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            // Ищем позиции в радиусе 2-4 клеток от структур
            foreach (var kvp in cityGrid.BuildingOccupancy)
            {
                if (IsStructureType(kvp.Key))
                {
                    foreach (var structureCell in kvp.Value)
                    {
                        var structurePos = structureCell.Cell;
                        for (int dx = -4; dx <= 4; dx++)
                        {
                            for (int dy = -4; dy <= 4; dy++)
                            {
                                if (Mathf.Abs(dx) < 2 && Mathf.Abs(dy) < 2) continue; // Слишком близко
                                
                                Vector2Int pos = structurePos + new Vector2Int(dx, dy);

                                if (CanPlaceTechAt(pos) && !positions.Contains(pos) && IsTechnicalZone(pos))
                                {
                                    positions.Add(pos);
                                }
                            }
                        }
                    }
                }
            }
            
            return positions;
        }

        bool IsStructureType(TileType type)
        {
            return type == TileType.Structure ||
                   type == TileType.LargeStructure ||
                   type == TileType.ResearchFacility ||
                   type == TileType.ProcessingPlant ||
                   type == TileType.CommandCenter ||
                   type == TileType.ContainmentUnit;
        }

        TileType ChooseTechTypeForTechnicalZone()
        {
            // В технических зонах больше шансов на сложную технику
            float chance = Random.value;
            
            if (chance < 0.4f) return TileType.DamagedGenerator;
            if (chance < 0.7f) return TileType.BrokenRobot;
            if (chance < 0.9f) return TileType.CorruptedTerminal;
            return TileType.TechSalvageResource;
        }

        TileType ChooseTechTypeForStructures()
        {
            // Рядом со структурами чаще простая техника и обломки
            float chance = Random.value;
            
            if (chance < 0.6f) return TileType.TechSalvageResource;
            if (chance < 0.8f) return TileType.BrokenRobot;
            if (chance < 0.95f) return TileType.DamagedGenerator;
            return TileType.CorruptedTerminal;
        }

        bool CanPlaceTechAt(Vector2Int pos)
        {
            if (!cityGrid.IsValidPosition(pos)) return false;
            if (cityGrid.Grid[pos.x][pos.y] != TileType.Grass) return false;
            if (cityGrid.IsCellOccupiedByBuilding(pos)) return false;
            
            return true;
        }

        bool TryPlaceTechSalvage(Vector2Int position, TileType techType)
        {
            if (!IsTechnicalZone(position))
            {
                Debug.Log($"[TechSalvagePlacer] Зона несовместима в {position}");
                return false;
            }

            if (!CanPlaceTechAt(position)) return false;
            
            // Проверяем лимиты
            var prefab = techSalvagePrefabs.FirstOrDefault(p => p.tileType == techType);
            if (prefab != null)
            {
                int currentCount = GetSpawnedCount(prefab);
                if (prefab.maxCount > 0 && currentCount >= prefab.maxCount)
                {
                    return false;
                }
            }
            
            // Регистрируем технику
            if (!cityGrid.BuildingOccupancy.ContainsKey(techType))
                cityGrid.BuildingOccupancy[techType] = new List<OccupiedCell>();

            cityGrid.BuildingOccupancy[techType].Add(new OccupiedCell(position, 0));
            
            // Увеличиваем счетчик
            if (prefab != null)
            {
                if (!spawnedCounts.ContainsKey(prefab))
                    spawnedCounts[prefab] = 0;
                spawnedCounts[prefab]++;
            }
            
            Debug.Log($"    🔧 Размещен {GetTechEmoji(techType)} {techType} в {position}");
            return true;
        }

        int GetSpawnedCount(PrefabSettings settings)
        {
            return spawnedCounts.ContainsKey(settings) ? spawnedCounts[settings] : 0;
        }

        bool HasPathwayNearby(Vector2Int pos, int distance)
        {
            for (int dx = -distance; dx <= distance; dx++)
            {
                for (int dy = -distance; dy <= distance; dy++)
                {
                    Vector2Int checkPos = pos + new Vector2Int(dx, dy);
                    if (cityGrid.IsValidPosition(checkPos) && 
                        cityGrid.Grid[checkPos.x][checkPos.y] == TileType.PathwayStraight)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool HasStructureNearby(Vector2Int pos, int distance)
        {
            for (int dx = -distance; dx <= distance; dx++)
            {
                for (int dy = -distance; dy <= distance; dy++)
                {
                    Vector2Int checkPos = pos + new Vector2Int(dx, dy);
                    if (cityGrid.IsValidPosition(checkPos) && 
                        cityGrid.IsCellOccupiedByBuilding(checkPos))
                    {
                        var buildingType = cityGrid.GetBuildingTypeAt(checkPos);
                        if (buildingType.HasValue && IsStructureType(buildingType.Value))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        void RemoveNearbyPositions(List<Vector2Int> positions, Vector2Int center, int distance)
        {
            positions.RemoveAll(pos => 
                Mathf.Abs(pos.x - center.x) <= distance && 
                Mathf.Abs(pos.y - center.y) <= distance);
        }

        void LogTechSalvageStatistics()
        {
            Debug.Log("  📊 === СТАТИСТИКА ТЕХНИКИ ===");
            
            foreach (var kvp in spawnedCounts)
            {
                var settings = kvp.Key;
                int count = kvp.Value;
                string emoji = GetTechEmoji(settings.tileType);
                Debug.Log($"  {emoji} {settings.objectName}: {count}");
            }
            
            int totalTech = spawnedCounts.Values.Sum();
            Debug.Log($"  🔧 Всего техники для восстановления: {totalTech}");
        }

        string GetTechEmoji(TileType type)
        {
            return type switch
            {
                TileType.DamagedGenerator => "⚡",
                TileType.BrokenRobot => "🤖",
                TileType.CorruptedTerminal => "💻",
                TileType.TechSalvageResource => "🔧",
                _ => "⚙️"
            };
        }

        bool IsTechnicalZone(Vector2Int pos)
        {
            var zone = zoneSystem?.GetZoneAt(pos);
            if (zone.HasValue && zone.Value.zoneType == TileType.TechnicalZone)
                return true;
            return false;
        }
    }
}