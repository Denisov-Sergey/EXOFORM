using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Exoform.Scripts.Ecs.Components.UnitLogicComponents;
using Exoform.Scripts.Map;

namespace Exoform.Scripts.Spawning
{
    /// <summary>
    /// Конфигурация префабов для системы спауна
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnPrefabConfig", menuName = "EXOFORM/Spawn Prefab Configuration")]
    public class SpawnPrefabConfiguration : ScriptableObject
    {
        [System.Serializable]
        public class UnitPrefabEntry
        {
            [Header("Unit Information")]
            public string unitName;
            public UnitType unitType;
            public GameObject prefab;
            
            [Header("Spawn Settings")]
            [Range(0.1f, 10f)]
            public float spawnWeight = 1f;
            public int teamId = 1;
            
            [Header("Requirements")]
            public bool requiresSpecialZone = false;
            public TileType[] allowedZones;
            
            [Header("Balancing")]
            public int cost = 100;
            public float cooldown = 1f;
            
            [Header("Limits")]
            public int maxSimultaneous = -1; // -1 = безлимитно
            public bool countTowardsTotalLimit = true;
        }

        [Header("Player Units")]
        [Tooltip("Префабы юнитов игроков")]
        public List<UnitPrefabEntry> playerUnits = new List<UnitPrefabEntry>();

        [Header("Enemy Units")]
        [Tooltip("Префабы вражеских юнитов")]
        public List<UnitPrefabEntry> enemyUnits = new List<UnitPrefabEntry>();

        [Header("Boss Units")]
        [Tooltip("Префабы боссов")]
        public List<UnitPrefabEntry> bossUnits = new List<UnitPrefabEntry>();

        [Header("Special Units")]
        [Tooltip("Особые юниты (NPC, нейтральные)")]
        public List<UnitPrefabEntry> specialUnits = new List<UnitPrefabEntry>();

        /// <summary>
        /// Получить все префабы для определенной команды
        /// </summary>
        public List<UnitPrefabEntry> GetUnitsForTeam(int teamId)
        {
            var result = new List<UnitPrefabEntry>();
            
            if (teamId == 1) // Игроки
            {
                result.AddRange(playerUnits);
            }
            else if (teamId == 2) // Враги
            {
                result.AddRange(enemyUnits);
            }
            else if (teamId == 0) // Нейтральные/Боссы
            {
                result.AddRange(bossUnits);
                result.AddRange(specialUnits);
            }

            return result.FindAll(u => u.teamId == teamId);
        }

        /// <summary>
        /// Получить случайный префаб для команды
        /// </summary>
        public UnitPrefabEntry GetRandomUnitForTeam(int teamId, UnitType preferredType = UnitType.Infantry)
        {
            var availableUnits = GetUnitsForTeam(teamId);
            
            // Сначала пытаемся найти предпочитаемый тип
            var preferredUnits = availableUnits.FindAll(u => u.unitType == preferredType);
            if (preferredUnits.Count > 0)
            {
                return GetWeightedRandomUnit(preferredUnits);
            }

            // Если не нашли, берем любой
            if (availableUnits.Count > 0)
            {
                return GetWeightedRandomUnit(availableUnits);
            }

            return null;
        }

        /// <summary>
        /// Получить взвешенный случайный юнит
        /// </summary>
        public UnitPrefabEntry GetWeightedRandomUnit(List<UnitPrefabEntry> units)
        {
            if (units.Count == 0) return null;
            if (units.Count == 1) return units[0];

            float totalWeight = 0f;
            foreach (var unit in units)
            {
                totalWeight += unit.spawnWeight;
            }

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            foreach (var unit in units)
            {
                currentWeight += unit.spawnWeight;
                if (randomValue <= currentWeight)
                {
                    return unit;
                }
            }

            return units[units.Count - 1]; // Fallback
        }

        /// <summary>
        /// Проверить, может ли юнит спауниться в указанной зоне
        /// </summary>
        public bool CanSpawnInZone(UnitPrefabEntry unit, TileType zoneType)
        {
            if (!unit.requiresSpecialZone) return true;
            if (unit.allowedZones == null || unit.allowedZones.Length == 0) return true;

            foreach (var allowedZone in unit.allowedZones)
            {
                if (allowedZone == zoneType) return true;
            }

            return false;
        }

        /// <summary>
        /// Валидация конфигурации
        /// </summary>
        public void ValidateConfiguration()
        {
            var errors = new List<string>();

            ValidateUnitList(playerUnits, "Player Units", errors);
            ValidateUnitList(enemyUnits, "Enemy Units", errors);
            ValidateUnitList(bossUnits, "Boss Units", errors);
            ValidateUnitList(specialUnits, "Special Units", errors);

            if (errors.Count > 0)
            {
                Debug.LogError($"Spawn Prefab Configuration Errors:\n{string.Join("\n", errors)}");
            }
            else
            {
                Debug.Log("✅ Spawn Prefab Configuration validated successfully!");
            }
        }

        private void ValidateUnitList(List<UnitPrefabEntry> units, string listName, List<string> errors)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                
                if (unit.prefab == null)
                {
                    errors.Add($"{listName}[{i}]: Missing prefab");
                }

                if (string.IsNullOrEmpty(unit.unitName))
                {
                    errors.Add($"{listName}[{i}]: Empty unit name");
                }

                if (unit.spawnWeight <= 0f)
                {
                    errors.Add($"{listName}[{i}]: Invalid spawn weight ({unit.spawnWeight})");
                }

                if (unit.cost < 0)
                {
                    errors.Add($"{listName}[{i}]: Negative cost ({unit.cost})");
                }
            }
        }

        /// <summary>
        /// Создать стандартную конфигурацию
        /// </summary>
        [ContextMenu("Create Default Configuration")]
        public void CreateDefaultConfiguration()
        {
            playerUnits.Clear();
            enemyUnits.Clear();
            bossUnits.Clear();
            specialUnits.Clear();

            // Добавляем базовые настройки для игроков
            playerUnits.Add(new UnitPrefabEntry
            {
                unitName = "Player Infantry",
                unitType = UnitType.Infantry,
                spawnWeight = 1f,
                teamId = 1,
                cost = 0,
                cooldown = 5f,
                maxSimultaneous = 4
            });

            // Добавляем базовые настройки для врагов
            enemyUnits.Add(new UnitPrefabEntry
            {
                unitName = "Enemy Scout",
                unitType = UnitType.Infantry,
                spawnWeight = 3f,
                teamId = 2,
                cost = 50,
                cooldown = 2f,
                requiresSpecialZone = true,
                allowedZones = new TileType[] { TileType.CorruptedTrap, TileType.InfestationZone }
            });

            enemyUnits.Add(new UnitPrefabEntry
            {
                unitName = "Enemy Heavy",
                unitType = UnitType.Vehicle,
                spawnWeight = 1f,
                teamId = 2,
                cost = 150,
                cooldown = 10f,
                maxSimultaneous = 3,
                requiresSpecialZone = true,
                allowedZones = new TileType[] { TileType.CorruptedTrap }
            });

            // Добавляем боссов
            bossUnits.Add(new UnitPrefabEntry
            {
                unitName = "Zone Guardian",
                unitType = UnitType.Building,
                spawnWeight = 1f,
                teamId = 0,
                cost = 1000,
                cooldown = 120f,
                maxSimultaneous = 1,
                requiresSpecialZone = true,
                allowedZones = new TileType[] { TileType.BossZone }
            });

            Debug.Log("✅ Default spawn configuration created!");
        }

        void OnValidate()
        {
            // Автоматическая валидация при изменении в инспекторе
            if (Application.isPlaying)
            {
                ValidateConfiguration();
            }
        }
    }

    /// <summary>
    /// Менеджер для работы с конфигурацией префабов
    /// </summary>
    public static class SpawnPrefabManager
    {
        private static SpawnPrefabConfiguration cachedConfig;

        /// <summary>
        /// Загрузить конфигурацию префабов
        /// </summary>
        public static SpawnPrefabConfiguration LoadConfiguration()
        {
            if (cachedConfig == null)
            {
                cachedConfig = Resources.Load<SpawnPrefabConfiguration>("SpawnPrefabConfig");
                
                if (cachedConfig == null)
                {
                    Debug.LogError("❌ SpawnPrefabConfiguration не найдена в Resources! Создайте её через Create > EXOFORM > Spawn Prefab Configuration");
                }
            }

            return cachedConfig;
        }

        /// <summary>
        /// Получить префаб для спауна
        /// </summary>
        public static GameObject GetPrefabForSpawn(int teamId, UnitType preferredType, TileType zoneType)
        {
            var config = LoadConfiguration();
            if (config == null) return null;

            var availableUnits = config.GetUnitsForTeam(teamId);
            
            // Фильтруем по зоне
            var validUnits = new List<SpawnPrefabConfiguration.UnitPrefabEntry>();
            foreach (var unit in availableUnits)
            {
                if (config.CanSpawnInZone(unit, zoneType))
                {
                    validUnits.Add(unit);
                }
            }

            if (validUnits.Count == 0) return null;

            // Выбираем предпочитаемый тип если возможно
            var preferredUnits = validUnits.FindAll(u => u.unitType == preferredType);
            if (preferredUnits.Count > 0)
            {
                var selectedUnit = GetWeightedRandomUnitFromList(config, preferredUnits);
                return selectedUnit?.prefab;
            }

            // Иначе любой подходящий
            var anyUnit = GetWeightedRandomUnitFromList(config, validUnits);
            return anyUnit?.prefab;
        }

        /// <summary>
        /// Вспомогательный метод для получения взвешенного случайного юнита
        /// </summary>
        private static SpawnPrefabConfiguration.UnitPrefabEntry GetWeightedRandomUnitFromList(
            SpawnPrefabConfiguration config, List<SpawnPrefabConfiguration.UnitPrefabEntry> units)
        {
            return config.GetWeightedRandomUnit(units);
        }

        /// <summary>
        /// Проверить, можно ли заспаунить юнита (лимиты)
        /// </summary>
        public static bool CanSpawnUnit(int teamId, UnitType unitType)
        {
            var config = LoadConfiguration();
            if (config == null) return false;

            // Определяем лимит из конфигурации
            int maxAllowed = 0;
            bool hasLimit = false;
            var units = config.GetUnitsForTeam(teamId);
            foreach (var unit in units)
            {
                if (unit.unitType != unitType) continue;
                if (unit.maxSimultaneous > 0)
                {
                    maxAllowed += unit.maxSimultaneous;
                    hasLimit = true;
                }
            }

            if (!hasLimit) return true; // нет ограничений в конфиге

            // Подсчитываем существующие юниты через ECS
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return true;

            var entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<UnitLogicComponent>(),
                ComponentType.ReadOnly<CombatComponent>());

            var logicArray = query.ToComponentDataArray<UnitLogicComponent>(Allocator.Temp);
            var combatArray = query.ToComponentDataArray<CombatComponent>(Allocator.Temp);

            int currentCount = 0;
            for (int i = 0; i < logicArray.Length; i++)
            {
                if (combatArray[i].IsDead) continue;

                if (logicArray[i].TeamId == teamId && logicArray[i].UnitType == unitType)
                {
                    currentCount++;
                }
            }

            logicArray.Dispose();
            combatArray.Dispose();
            query.Dispose();

            return currentCount < maxAllowed;
        }
    }
}