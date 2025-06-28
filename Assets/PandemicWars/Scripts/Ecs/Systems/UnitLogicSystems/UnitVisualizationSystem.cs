using PandemicWars.Scripts.Ecs.Components.UnitLogicComponents;
using PandemicWars.Scripts.Hybrid;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Systems.UnitLogicSystems
{
    /// <summary>
    /// Система для автоматического создания визуальных GameObject'ов для ECS Entities
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class UnitVisualizationSystem : SystemBase
    {
        private UnitSpawner unitSpawner;
        private int nextUnitId = 1;

        protected override void OnCreate()
        {
            RequireForUpdate<UnitLogicComponent>();
        }

        protected override void OnStartRunning()
        {
            // Находим UnitSpawner в сцене
            unitSpawner = Object.FindObjectOfType<UnitSpawner>();
            if (unitSpawner == null)
            {
                Debug.LogError("UnitSpawner not found in scene! Please add UnitSpawner to MainScene.");
            }
        }

        protected override void OnUpdate()
        {
            if (unitSpawner == null) return;

            // Обрабатываем все Entities которые еще не имеют визуального представления
            var query = SystemAPI.QueryBuilder()
                .WithAll<UnitLogicComponent, LocalTransform>()
                .WithNone<UnitProxyComponent>() // Только те что еще не связаны с GameObject
                .Build();

            var entities = query.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                CreateVisualForEntity(entity);
            }

            entities.Dispose();
        }

        private void CreateVisualForEntity(Entity entity)
        {
            if (!EntityManager.Exists(entity)) return;

            var unitLogic = EntityManager.GetComponentData<UnitLogicComponent>(entity);
            var transform = EntityManager.GetComponentData<LocalTransform>(entity);

            // Создаем визуальный GameObject
            var prefab = unitSpawner.GetPrefabForUnitType(unitLogic.UnitType);
            if (prefab == null)
            {
                Debug.LogError($"No prefab found for unit type: {unitLogic.UnitType}");
                return;
            }

            var gameObject = Object.Instantiate(prefab, transform.Position, transform.Rotation);
            var visualController = gameObject.GetComponent<UnitVisualController>();

            if (visualController != null)
            {
                // Назначаем уникальный ID
                int unitId = nextUnitId++;
                visualController.unitId = unitId;

                // Добавляем UnitProxyComponent к Entity
                EntityManager.AddComponentData(entity, new UnitProxyComponent
                {
                    UnitId = unitId,
                    IsSpawned = true,
                    LastSyncPosition = transform.Position,
                    LastSyncRotation = transform.Rotation
                });

                Debug.Log($"Created visual GameObject for Entity {entity.Index} with Unit ID {unitId}");
            }
            else
            {
                Debug.LogError($"Prefab {prefab.name} missing UnitVisualController component!");
                Object.Destroy(gameObject);
            }
        }
    }
}