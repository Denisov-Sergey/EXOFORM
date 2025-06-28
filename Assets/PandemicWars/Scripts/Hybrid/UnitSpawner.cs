using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using PandemicWars.Scripts.Ecs.Components.UnitLogicComponents;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace PandemicWars.Scripts.Hybrid
{
    /// <summary>
    /// MonoBehaviour компонент для управления спавном юнитов в MainScene
    /// Размещается в MainScene как отдельный GameObject
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        [Header("Unit Prefabs")] [Tooltip("Префаб для пехотных юнитов (ваш AlienChef)")]
        public GameObject infantryPrefab;

        [Tooltip("Префаб для транспорта")] public GameObject vehiclePrefab;

        [Tooltip("Префаб для авиации")] public GameObject aircraftPrefab;

        [Header("Spawn Settings")] [Tooltip("Точка спавна юнитов")]
        public Transform spawnPoint;

        [Tooltip("Спавнить тестовый юнит при старте")]
        public bool spawnTestUnitOnStart = true;

        [Tooltip("Количество юнитов для теста")] [Range(1, 20)]
        public int testUnitsCount = 1;

        [Tooltip("Радиус разброса при спавне нескольких юнитов")]
        public float spawnRadius = 5f;

        [Header("Debug")] [Tooltip("Показывать отладочную информацию")]
        public bool debugMode = false;

        // Внутренние переменные
        private World world;
        private EntityManager entityManager;
        private bool isInitialized = false;
        private int nextUnitId = 1; // ИСПРАВЛЕНИЕ: добавлена отсутствующая переменная

        void Start()
        {
            InitializeECS();

            if (spawnTestUnitOnStart && spawnPoint != null)
            {
                // Небольшая задержка чтобы ECS инициализировался
                Invoke(nameof(SpawnTestUnits), 0.5f);
            }

            ValidateSetup();
        }

        void InitializeECS()
        {
            world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                entityManager = world.EntityManager;
                isInitialized = true;
                if (debugMode) Debug.Log("UnitSpawner: ECS initialized successfully");
            }
            else
            {
                Debug.LogError("UnitSpawner: ECS World not initialized!");
                // Попробуем еще раз через секунду
                Invoke(nameof(InitializeECS), 1f);
            }
        }

        void ValidateSetup()
        {
            bool hasErrors = false;

            if (spawnPoint == null)
            {
                Debug.LogError("UnitSpawner: Spawn Point not assigned!");
                hasErrors = true;
            }

            if (infantryPrefab == null)
            {
                Debug.LogError("UnitSpawner: Infantry Prefab not assigned!");
                hasErrors = true;
            }
            else if (infantryPrefab.GetComponent<UnitVisualController>() == null)
            {
                Debug.LogError("UnitSpawner: Infantry Prefab missing UnitVisualController component!");
                hasErrors = true;
            }

            // Проверяем NavMesh
            if (spawnPoint != null)
            {
                UnityEngine.AI.NavMeshHit hit;
                if (!UnityEngine.AI.NavMesh.SamplePosition(spawnPoint.position, out hit, 2f,
                        UnityEngine.AI.NavMesh.AllAreas))
                {
                    Debug.LogWarning("UnitSpawner: Spawn Point is not on NavMesh!");
                }
            }

            if (!hasErrors && debugMode)
            {
                Debug.Log("UnitSpawner: Setup validation passed");
            }
        }

        void SpawnTestUnits()
        {
            if (!isInitialized)
            {
                Debug.LogError("UnitSpawner: Cannot spawn units - ECS not initialized");
                return;
            }

            if (testUnitsCount == 1)
            {
                SpawnUnitAtPosition(spawnPoint.position, UnitType.Infantry);
            }
            else
            {
                for (int i = 0; i < testUnitsCount; i++)
                {
                    Vector3 randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
                    Vector3 spawnPos = spawnPoint.position + new Vector3(randomOffset.x, 0, randomOffset.y);

                    // Корректируем позицию на NavMesh
                    UnityEngine.AI.NavMeshHit hit;
                    if (UnityEngine.AI.NavMesh.SamplePosition(spawnPos, out hit, spawnRadius,
                            UnityEngine.AI.NavMesh.AllAreas))
                    {
                        spawnPos = hit.position;
                    }

                    SpawnUnitAtPosition(spawnPos, UnitType.Infantry);
                }
            }
        }

        /// <summary>
        /// Создает новый юнит в указанной позиции
        /// </summary>
        public Entity SpawnUnitAtPosition(Vector3 position, UnitType unitType = UnitType.Infantry, int teamId = 1)
        {
            if (!isInitialized)
            {
                Debug.LogError("Cannot spawn unit - ECS not initialized");
                return Entity.Null;
            }

            // Корректируем позицию на NavMesh
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
            {
                position = hit.position;
            }
            else
            {
                Debug.LogWarning($"Position {position} is not on NavMesh, spawning anyway");
            }

            // Создаем Entity
            var entity = entityManager.CreateEntity();

            // Базовые компоненты
            entityManager.AddComponentData(entity, LocalTransform.FromPosition(position));

            entityManager.AddComponentData(entity, new UnitLogicComponent
            {
                TeamId = teamId,
                UnitType = unitType,
                AttackRange = 10f,
                AttackDamage = 25f,
                AttackCooldown = 1f,
                LastAttackTime = 0f
            });

            // Навигация
            entityManager.AddComponentData(entity, new NavAgentComponent
            {
                TargetEntity = Entity.Null,
                MovementSpeed = 5f,
                PathCalculated = false,
                CurrentWaypoint = 0,
                NextPathCalculatedTime = 0f,
                MaxPathIterations = 100
            });

            entityManager.AddBuffer<WaypointBuffer>(entity);

            // Игрок
            entityManager.AddComponentData(entity, new PlayerUnitComponent
            {
                IsSelected = false,
                SelectionRadius = 1f
            });

            // Анимация
            entityManager.AddComponentData(entity, new AnimationStateComponent
            {
                CurrentState = UnitAnimationState.Idle,
                PreviousState = UnitAnimationState.Idle,
                StateChangeTime = 0f,
                MovementSpeed = 0f,
                IsSelected = false,
                Health = 100f,
                MaxHealth = 100f,
                TriggerAttack = false,
                TriggerDeath = false
            });

            entityManager.AddComponentData(entity, new AnimationLODComponent
            {
                CurrentLOD = AnimationLODLevel.High,
                DistanceToCamera = 0f,
                ForceHighLOD = false
            });

            // Бой
            entityManager.AddComponentData(entity, new CombatComponent
            {
                Health = 100f,
                MaxHealth = 100f,
                Armor = 0f,
                IsDead = false,
                LastDamageTime = 0f,
                LastAttacker = Entity.Null
            });

            if (debugMode)
            {
                Debug.Log($"Spawned {unitType} Entity {entity.Index} at {position}");
            }

            return entity;
        }

        /// <summary>
        /// Возвращает префаб для указанного типа юнита
        /// </summary>
        public GameObject GetPrefabForUnitType(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Infantry => infantryPrefab,
                UnitType.Vehicle => vehiclePrefab,
                UnitType.Aircraft => aircraftPrefab,
                UnitType.Building => infantryPrefab, // Fallback
                _ => infantryPrefab
            };
        }

        /// <summary>
        /// Спавнит юнита в случайной позиции вокруг spawn point
        /// </summary>
        public Entity SpawnRandomUnit(UnitType unitType = UnitType.Infantry)
        {
            if (spawnPoint == null)
            {
                Debug.LogError("Spawn point not set!");
                return Entity.Null;
            }

            Vector3 randomOffset = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPos = spawnPoint.position + new Vector3(randomOffset.x, 0, randomOffset.y);

            return SpawnUnitAtPosition(spawnPos, unitType);
        }

        /// <summary>
        /// Применяет урон юниту по ID
        /// </summary>
        public void DamageUnit(int unitId, float damage)
        {
            if (!isInitialized) return;

            // ИСПРАВЛЕНИЕ: правильный способ итерации через EntityManager
            var query = entityManager.CreateEntityQuery(
                ComponentType.ReadWrite<CombatComponent>(),
                ComponentType.ReadOnly<UnitProxyComponent>()
            );

            var entities = query.ToEntityArray(Allocator.Temp);
            var combatComponents = query.ToComponentDataArray<CombatComponent>(Allocator.Temp);
            var proxyComponents = query.ToComponentDataArray<UnitProxyComponent>(Allocator.Temp);

            for (int i = 0; i < entities.Length; i++)
            {
                if (proxyComponents[i].UnitId == unitId)
                {
                    var combat = combatComponents[i];
                    combat.Health = Mathf.Max(0, combat.Health - damage);
                    if (combat.Health <= 0)
                    {
                        combat.IsDead = true;
                    }

                    entityManager.SetComponentData(entities[i], combat);

                    if (debugMode)
                    {
                        Debug.Log($"Unit {unitId} took {damage} damage, health: {combat.Health}");
                    }
                    break;
                }
            }

            entities.Dispose();
            combatComponents.Dispose();
            proxyComponents.Dispose();
        }

        /// <summary>
        /// Получает количество живых юнитов
        /// </summary>
        public int GetAliveUnitsCount()
        {
            if (!isInitialized) return 0;

            // ИСПРАВЛЕНИЕ: правильный способ подсчета через EntityManager
            var query = entityManager.CreateEntityQuery(ComponentType.ReadOnly<CombatComponent>());
            var combatComponents = query.ToComponentDataArray<CombatComponent>(Allocator.Temp);

            int count = 0;
            for (int i = 0; i < combatComponents.Length; i++)
            {
                if (!combatComponents[i].IsDead)
                {
                    count++;
                }
            }

            combatComponents.Dispose();
            return count;
        }

        // ===== МЕТОДЫ ДЛЯ ТЕСТИРОВАНИЯ (Context Menu) =====

        [ContextMenu("Spawn Single Unit")]
        public void SpawnSingleUnit()
        {
            if (spawnPoint != null)
            {
                SpawnUnitAtPosition(spawnPoint.position, UnitType.Infantry);
            }
        }

        [ContextMenu("Spawn 5 Units")]
        public void Spawn5Units()
        {
            for (int i = 0; i < 5; i++)
            {
                SpawnRandomUnit(UnitType.Infantry);
            }
        }

        [ContextMenu("Spawn 10 Units")]
        public void Spawn10Units()
        {
            for (int i = 0; i < 10; i++)
            {
                SpawnRandomUnit(UnitType.Infantry);
            }
        }

        [ContextMenu("Damage Random Unit")]
        public void DamageRandomUnit()
        {
            int unitCount = GetAliveUnitsCount();
            if (unitCount > 0)
            {
                int randomUnitId = UnityEngine.Random.Range(1, nextUnitId);
                DamageUnit(randomUnitId, 25f);
            }
        }

        [ContextMenu("Kill Random Unit")]
        public void KillRandomUnit()
        {
            int unitCount = GetAliveUnitsCount();
            if (unitCount > 0)
            {
                int randomUnitId = UnityEngine.Random.Range(1, nextUnitId);
                DamageUnit(randomUnitId, 1000f); // Достаточно урона чтобы убить
            }
        }

        [ContextMenu("Print Stats")]
        public void PrintStats()
        {
            Debug.Log($"Alive units: {GetAliveUnitsCount()}");
            Debug.Log($"ECS initialized: {isInitialized}");
            Debug.Log($"Spawn point set: {spawnPoint != null}");
            Debug.Log($"Infantry prefab set: {infantryPrefab != null}");
            Debug.Log($"Next unit ID: {nextUnitId}");
        }

        // ===== ОТЛАДОЧНАЯ ВИЗУАЛИЗАЦИЯ =====

        void OnDrawGizmosSelected()
        {
            if (spawnPoint == null) return;

            // Рисуем spawn point
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);

            // Рисуем радиус спавна
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPoint.position, spawnRadius);

            // Рисуем направление
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(spawnPoint.position, spawnPoint.forward * 2f);
        }
    }
}