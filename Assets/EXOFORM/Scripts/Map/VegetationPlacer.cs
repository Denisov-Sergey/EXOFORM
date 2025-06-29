using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Exoform.Scripts.Map
{
    /// <summary>
    /// Специальный класс для размещения растительности
    /// </summary>
    public class VegetationPlacer
    {
        private CityGrid cityGrid;
        private List<PrefabSettings> vegetationPrefabs;
        private MonoBehaviour coroutineRunner;

        public VegetationPlacer(CityGrid grid, List<GameObject> prefabs, MonoBehaviour runner)
        {
            cityGrid = grid;
            coroutineRunner = runner;
            LoadVegetationPrefabs(prefabs);
        }

        void LoadVegetationPrefabs(List<GameObject> prefabs)
        {
            vegetationPrefabs = new List<PrefabSettings>();

            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    var settings = prefab.GetComponent<PrefabSettings>();
                    if (settings != null && IsVegetationType(settings.tileType))
                    {
                        vegetationPrefabs.Add(settings);
                    }
                }
            }

            Debug.Log($"Загружено {vegetationPrefabs.Count} префабов растительности");
        }

        bool IsVegetationType(TileType type)
        {
            return type == TileType.Spore || type == TileType.SporeCluster || 
                   type == TileType.CorruptedVegetation || 
                   type == TileType.Forest || 
                   type == TileType.AlienGrowth;
        }

        public IEnumerator PlaceVegetation(float vegetationDensity, float animationSpeed)
        {
            if (vegetationPrefabs.Count == 0)
            {
                Debug.Log("  🌱 Нет префабов растительности для размещения");
                yield break;
            }

            Debug.Log("🌳 Этап: Размещение растительности");

            // Размещаем растительность в несколько этапов
            yield return coroutineRunner.StartCoroutine(PlaceForests(vegetationDensity, animationSpeed));
            yield return coroutineRunner.StartCoroutine(PlaceGardens(vegetationDensity, animationSpeed));
            yield return coroutineRunner.StartCoroutine(PlaceRandomVegetation(vegetationDensity, animationSpeed));
        }

        IEnumerator PlaceForests(float density, float animationSpeed)
        {
            var forestSettings = vegetationPrefabs.Find(v => v.tileType == TileType.Forest);
            if (forestSettings == null) yield break;

            Debug.Log("  🌲 Размещение лесных массивов...");

            List<Vector2Int> forestAreas = FindForestAreas();
            int forestsToPlace = Mathf.RoundToInt(forestAreas.Count * density * 0.1f); // 10% от density

            for (int i = 0; i < forestsToPlace && forestAreas.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, forestAreas.Count);
                Vector2Int position = forestAreas[randomIndex];

                if (TryPlaceForest(position, forestSettings))
                {
                    RemoveNearbyAreas(forestAreas, position, 8); // Леса далеко друг от друга
                    yield return new WaitForSeconds(animationSpeed);
                }
                else
                {
                    forestAreas.RemoveAt(randomIndex);
                }
            }
        }

        IEnumerator PlaceGardens(float density, float animationSpeed)
        {
            var gardenSettings = vegetationPrefabs.Find(v => v.tileType == TileType.AlienGrowth);
            if (gardenSettings == null) yield break;

            Debug.Log("  🌺 Размещение садов...");

            List<Vector2Int> gardenAreas = FindGardenAreas();
            int gardensToPlace = Mathf.RoundToInt(gardenAreas.Count * density * 0.3f); // 30% от density

            for (int i = 0; i < gardensToPlace && gardenAreas.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, gardenAreas.Count);
                Vector2Int position = gardenAreas[randomIndex];

                if (TryPlaceGarden(position, gardenSettings))
                {
                    RemoveNearbyAreas(gardenAreas, position, 4);
                    yield return new WaitForSeconds(animationSpeed * 0.5f);
                }
                else
                {
                    gardenAreas.RemoveAt(randomIndex);
                }
            }
        }

        IEnumerator PlaceRandomVegetation(float density, float animationSpeed)
        {
            Debug.Log("  🌿 Размещение случайной растительности...");

            // Получаем мелкую растительность (деревья, кусты, цветы)
            var smallVegetation = vegetationPrefabs.FindAll(v => 
                v.tileType == TileType.Spore || 
                v.tileType == TileType.CorruptedVegetation);

            foreach (var vegetation in smallVegetation)
            {
                yield return coroutineRunner.StartCoroutine(PlaceVegetationType(vegetation, density, animationSpeed));
            }
        }

        IEnumerator PlaceVegetationType(PrefabSettings settings, float density, float animationSpeed)
        {
            List<Vector2Int> validPositions = FindValidVegetationPositions(settings);
            float adjustedDensity = GetAdjustedDensity(settings.tileType, density);
            int objectsToPlace = Mathf.RoundToInt(validPositions.Count * adjustedDensity * settings.spawnWeight);

            if (settings.maxCount > 0)
                objectsToPlace = Mathf.Min(objectsToPlace, settings.maxCount);

            Debug.Log($"    🌱 {settings.objectName}: размещаем {objectsToPlace} из {validPositions.Count} позиций");

            for (int i = 0; i < objectsToPlace && validPositions.Count > 0; i++)
            {
                int randomIndex = Random.Range(0, validPositions.Count);
                Vector2Int position = validPositions[randomIndex];

                if (TryPlaceVegetation(position, settings))
                {
                    RemoveNearbyAreas(validPositions, position, settings.minDistanceFromSameType);
                    yield return new WaitForSeconds(animationSpeed * 0.3f);
                }
                else
                {
                    validPositions.RemoveAt(randomIndex);
                }
            }
        }

        float GetAdjustedDensity(TileType vegetationType, float baseDensity)
        {
            return vegetationType switch
            {
                TileType.Spore => baseDensity * 0.4f,        // 40% от базовой плотности
                TileType.CorruptedVegetation => baseDensity * 0.8f,      // 80% от базовой плотности
                TileType.SporeCluster => baseDensity * 0.2f, // 20% от базовой плотности
                _ => baseDensity * 0.5f
            };
        }

        List<Vector2Int> FindForestAreas()
        {
            List<Vector2Int> areas = new List<Vector2Int>();

            // Ищем большие свободные области для лесов (подальше от дорог и зданий)
            for (int x = 5; x < cityGrid.Width - 5; x += 3)
            {
                for (int y = 5; y < cityGrid.Height - 5; y += 3)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (IsAreaClearForForest(pos, 4))
                    {
                        areas.Add(pos);
                    }
                }
            }

            return areas;
        }

        List<Vector2Int> FindGardenAreas()
        {
            List<Vector2Int> areas = new List<Vector2Int>();

            // Ищем места рядом с зданиями для садов
            for (int x = 1; x < cityGrid.Width - 1; x++)
            {
                for (int y = 1; y < cityGrid.Height - 1; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (IsGoodForGarden(pos))
                    {
                        areas.Add(pos);
                    }
                }
            }

            return areas;
        }

        List<Vector2Int> FindValidVegetationPositions(PrefabSettings settings)
        {
            List<Vector2Int> positions = new List<Vector2Int>();

            for (int x = 0; x <= cityGrid.Width - settings.gridSize.x; x++)
            {
                for (int y = 0; y <= cityGrid.Height - settings.gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (CanPlaceVegetationAt(pos, settings))
                    {
                        positions.Add(pos);
                    }
                }
            }

            return positions;
        }

        bool IsAreaClearForForest(Vector2Int center, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int checkPos = center + new Vector2Int(dx, dy);
                    if (!cityGrid.IsValidPosition(checkPos)) return false;
                    
                    // Не должно быть дорог и зданий
                    if (cityGrid.Grid[checkPos.x][checkPos.y] != TileType.Grass) return false;
                    if (cityGrid.IsCellOccupiedByBuilding(checkPos)) return false;
                }
            }
            return true;
        }

        bool IsGoodForGarden(Vector2Int pos)
        {
            // Должна быть трава
            if (cityGrid.Grid[pos.x][pos.y] != TileType.Grass) return false;
            if (cityGrid.IsCellOccupiedByBuilding(pos)) return false;

            // Должно быть здание рядом (в радиусе 2 клеток)
            return HasBuildingNearby(pos, 2) && !HasRoadNearby(pos, 1);
        }

        bool CanPlaceVegetationAt(Vector2Int pos, PrefabSettings settings)
        {
            var occupiedCells = settings.GetOccupiedCells(pos);

            foreach (var cell in occupiedCells)
            {
                if (!cityGrid.IsValidPosition(cell)) return false;
                if (cityGrid.Grid[cell.x][cell.y] != TileType.Grass) return false;
                if (cityGrid.IsCellOccupiedByBuilding(cell)) return false;
            }

            // Специальные правила для разных типов
            switch (settings.tileType)
            {
                case TileType.Spore:
                    return !HasRoadNearby(pos, 1); // Деревья не вплотную к дорогам
                    
                case TileType.CorruptedVegetation:
                    return true; // Кусты и цветы могут быть везде
                    
                default:
                    return true;
            }
        }

        bool HasBuildingNearby(Vector2Int pos, int distance)
        {
            for (int dx = -distance; dx <= distance; dx++)
            {
                for (int dy = -distance; dy <= distance; dy++)
                {
                    Vector2Int checkPos = pos + new Vector2Int(dx, dy);
                    if (cityGrid.IsValidPosition(checkPos) && 
                        cityGrid.IsCellOccupiedByBuilding(checkPos))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool HasRoadNearby(Vector2Int pos, int distance)
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

        bool TryPlaceForest(Vector2Int position, PrefabSettings settings)
        {
            // Размещаем лес как группу деревьев
            Vector2Int forestSize = new Vector2Int(
                Random.Range(3, 6), 
                Random.Range(3, 6)
            );

            List<Vector2Int> forestCells = new List<Vector2Int>();
            for (int x = 0; x < forestSize.x; x++)
            {
                for (int y = 0; y < forestSize.y; y++)
                {
                    Vector2Int cell = position + new Vector2Int(x, y);
                    if (cityGrid.IsValidPosition(cell) && 
                        cityGrid.Grid[cell.x][cell.y] == TileType.Grass &&
                        !cityGrid.IsCellOccupiedByBuilding(cell))
                    {
                        forestCells.Add(cell);
                    }
                }
            }

            if (forestCells.Count >= forestSize.x * forestSize.y * 0.7f) // Минимум 70% площади
            {
                RegisterVegetationArea(TileType.Forest, forestCells);
                return true;
            }

            return false;
        }

        bool TryPlaceGarden(Vector2Int position, PrefabSettings settings)
        {
            var gardenCells = settings.GetOccupiedCells(position);
            
            foreach (var cell in gardenCells)
            {
                if (!CanPlaceVegetationAt(cell, settings)) return false;
            }

            RegisterVegetationArea(TileType.AlienGrowth, gardenCells);
            return true;
        }

        bool TryPlaceVegetation(Vector2Int position, PrefabSettings settings)
        {
            if (!CanPlaceVegetationAt(position, settings)) return false;

            var occupiedCells = settings.GetOccupiedCells(position);
            RegisterVegetationArea(settings.tileType, occupiedCells);
            return true;
        }

        void RegisterVegetationArea(TileType vegetationType, List<Vector2Int> cells)
        {
            if (!cityGrid.BuildingOccupancy.ContainsKey(vegetationType))
                cityGrid.BuildingOccupancy[vegetationType] = new List<Vector2Int>();

            foreach (var cell in cells)
            {
                cityGrid.BuildingOccupancy[vegetationType].Add(cell);
            }
        }

        void RemoveNearbyAreas(List<Vector2Int> positions, Vector2Int center, int distance)
        {
            positions.RemoveAll(pos => 
                Mathf.Abs(pos.x - center.x) <= distance && 
                Mathf.Abs(pos.y - center.y) <= distance);
        }
    }
}