using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.Hybrid;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Authoring.UnitLogicAutoring
{
    /// <summary>
    /// MonoBehaviour для управления спавном юнитов
    /// Размещается в Main Scene
    /// </summary>
    public class UnitSpawner : MonoBehaviour
    {
        [Header("Unit Prefabs")]
        public GameObject infantryPrefab; // Ваш AlienChef prefab
        public GameObject vehiclePrefab;
        public GameObject aircraftPrefab;

        [Header("Spawn Settings")]
        public Transform spawnPoint;
        public int maxUnits = 100;

        private World world;
        private EntityManager entityManager;
        private int nextUnitId = 1;

        void Start()
        {
            world = World.DefaultGameObjectInjectionWorld;
            entityManager = world?.EntityManager;
        }

        /// <summary>
        /// Создает новый юнит (ECS Entity + GameObject)
        /// </summary>
        public void SpawnUnit(UnitType unitType, Vector3 position, int teamId = 1)
        {
            if (entityManager == null) return;

            // 1. Создаем ECS Entity
            var entity = entityManager.CreateEntity();
            
            // 2. Добавляем компоненты
            entityManager.AddComponentData(entity, new UnitProxyComponent
            {
                UnitId = nextUnitId++,
                IsSpawned = false,
                LastSyncPosition = position,
                LastSyncRotation = quaternion.identity
            });
            
            entityManager.AddComponentData(entity, LocalTransform.FromPosition(position));
            
            entityManager.AddComponentData(entity, new CombatComponent
            {
                Health = 100f,
                MaxHealth = 100f,
                Armor = 0f,
                IsDead = false
            });
            
            entityManager.AddComponentData(entity, new UnitLogicComponent
            {
                TeamId = teamId,
                UnitType = unitType,
                AttackRange = 10f,
                AttackDamage = 25f,
                AttackCooldown = 1f
            });

            // Добавляем компоненты для гибридной анимации
            entityManager.AddComponentData(entity, new AnimationStateComponent
            {
                CurrentState = HybridUnitAnimationState.Idle,
                Health = 100f,
                MaxHealth = 100f
            });

            entityManager.AddComponentData(entity, new AnimationLODComponent
            {
                CurrentLOD = AnimationLODLevel.High
            });

            entityManager.AddComponentData(entity, new PlayerUnitComponent
            {
                IsSelected = false,
                SelectionRadius = 2f
            });

            // 3. GameObject будет создан автоматически через UnitVisualizationSystem

            Debug.Log($"Spawned {unitType} unit with ID {nextUnitId - 1} at {position}");
        }

        public GameObject GetPrefabForUnitType(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Infantry => infantryPrefab,
                UnitType.Vehicle => vehiclePrefab,
                UnitType.Aircraft => aircraftPrefab,
                _ => infantryPrefab
            };
        }

        // Кнопки для тестирования
        [ContextMenu("Spawn Infantry")]
        public void SpawnInfantry()
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            SpawnUnit(UnitType.Infantry, pos);
        }

        [ContextMenu("Spawn 10 Units")]
        public void Spawn10Units()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector3 pos = (spawnPoint != null ? spawnPoint.position : Vector3.zero) + 
                              new Vector3(UnityEngine.Random.Range(-10f, 10f), 0, UnityEngine.Random.Range(-10f, 10f));
                SpawnUnit(UnitType.Infantry, pos);
            }
        }
    }
}

}