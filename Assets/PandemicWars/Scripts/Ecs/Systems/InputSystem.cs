using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using PandemicWars.Scripts.Ecs.Components;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace PandemicWars.Scripts.Ecs.Systems
{
    /// <summary>
    /// Система обработки пользовательского ввода для управления юнитами.
    /// Совместима с современными версиями Unity DOTS и использует Baker систему.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InputSystem : SystemBase
    {
        private Camera _mainCamera;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerUnitComponent>();
        }

        protected override void OnStartRunning()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
                _mainCamera = Object.FindObjectOfType<Camera>();
                
            if (_mainCamera == null)
                Debug.LogError("Камера не найдена в OnStartRunning!");
        }

        protected override void OnUpdate()
        {
            // Обрабатываем клик правой кнопкой мыши для движения
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                HandleMovementInput();
            }
            
            // Обрабатываем клик левой кнопкой мыши для выбора юнитов
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleUnitSelection();
            }
        }

        /// <summary>
        /// Обрабатывает команду движения по клику правой кнопкой мыши.
        /// </summary>
        private void HandleMovementInput()
        {
            if (_mainCamera == null || Mouse.current == null) return;
            
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);
            
            Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red, 10f);
            
            if (Physics.Raycast(ray, out var hit))
            {
                float3 targetPosition = hit.point;
                Debug.Log($"Команда движения в позицию: {targetPosition}");
                
                // Создаем целевую сущность-маркер
                Entity targetEntity = CreateMovementTarget(targetPosition);
                
                // Назначаем цель всем выбранным юнитам
                AssignTargetToSelectedUnits(targetEntity);
                
                // Визуальная индикация
                CreateMovementIndicator(targetPosition);
            }
        }

        /// <summary>
        /// Современный способ выбора юнитов
        /// </summary>
        private void HandleUnitSelection()
        {
            if (_mainCamera == null || Mouse.current == null) return;
            
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);
            
            if (Physics.Raycast(ray, out var hit))
            {
                // Используем компонент EntityReference
                var entityReference = hit.collider.GetComponent<EntityReference>();
                bool isShiftPressed = Keyboard.current?.leftShiftKey.isPressed == true;
                
                if (entityReference != null && entityReference.Entity != Entity.Null)
                {
                    Entity entity = entityReference.Entity;
                    
                    if (EntityManager.Exists(entity) && 
                        EntityManager.HasComponent<PlayerUnitComponent>(entity))
                    {
                        SelectUnit(entity, !isShiftPressed);
                    }
                }
                else
                {
                    //  Поиск по позиции
                    Entity foundEntity = FindEntityByPosition(hit.point);
                    if (foundEntity != Entity.Null)
                    {
                        SelectUnit(foundEntity, !isShiftPressed);
                    }
                }
            }
        }

        /// <summary>
        /// Поиск Entity по позиции (альтернатива GameObject-Entity связи)
        /// </summary>
        private Entity FindEntityByPosition(float3 clickPosition)
        {
            Entity closestEntity = Entity.Null;
            float closestDistance = float.MaxValue;
            
            // Ищем ближайшую сущность к точке клика
            Entities
                .WithAll<PlayerUnitComponent>()
                .ForEach((Entity entity, in LocalTransform transform) =>
                {
                    float distance = math.distance(transform.Position, clickPosition);
                    if (distance < 2f && distance < closestDistance) // Радиус выбора 2 единицы
                    {
                        closestDistance = distance;
                        closestEntity = entity;
                    }
                }).WithoutBurst().Run();
            
            return closestEntity;
        }

        /// <summary>
        /// Создает сущность-маркер в целевой позиции.
        /// </summary>
        private Entity CreateMovementTarget(float3 position)
        {
            // Удаляем старые цели
            var oldTargets = EntityManager.CreateEntityQuery(typeof(MovementTargetTag));
            EntityManager.DestroyEntity(oldTargets);
            
            // Создаем новую цель
            Entity targetEntity = EntityManager.CreateEntity();
            EntityManager.AddComponent<MovementTargetTag>(targetEntity);
            EntityManager.AddComponent<LocalTransform>(targetEntity);
            EntityManager.SetComponentData(targetEntity, LocalTransform.FromPosition(position));
            
            return targetEntity;
        }

        /// <summary>
        /// Назначает цель всем выбранным юнитам.
        /// </summary>
        private void AssignTargetToSelectedUnits(Entity targetEntity)
        {
            int assignedCount = 0;
            
            var targetPosition = EntityManager.GetComponentData<LocalTransform>(targetEntity).Position;
            
            Entities
                .WithAll<PlayerUnitComponent>()
                .ForEach((Entity entity, ref NavAgentComponent navAgent, in PlayerUnitComponent selected, in LocalTransform localTransform) =>
                {
                    if (selected.IsSelected)
                    {
                        navAgent.TargetEntity = targetEntity;
                        navAgent.PathCalculated = false;
                        navAgent.NextPathCalculatedTime = 0f;
                        
                        if (EntityManager.HasBuffer<WaypointBuffer>(entity))
                        {
                            var waypointBuffer = EntityManager.GetBuffer<WaypointBuffer>(entity);
                            waypointBuffer.Clear();
                    
                            waypointBuffer.Add(new WaypointBuffer { waypoint = targetPosition });
                    
                            // Устанавливаем путь как готовый
                            navAgent.CurrentWaypoint = 0;
                            navAgent.PathCalculated = true;
                    
                            Debug.Log($"Создано {waypointBuffer.Length} waypoint'ов для юнита {entity.Index}");
                        }
                        else
                        {
                            Debug.LogError($"У юнита {entity.Index} нет WaypointBuffer!");
                        }
                        assignedCount++;
                    }
                }).WithoutBurst().Run();
            
            Debug.Log($"Команда движения назначена {assignedCount} юнитам");
        }

        /// <summary>
        /// Выбирает или снимает выбор с юнита.
        /// </summary>
        private void SelectUnit(Entity entity, bool clearOtherSelection)
        {
            if (clearOtherSelection)
            {
                Entities
                    .ForEach((ref PlayerUnitComponent selected) =>
                    {
                        selected.IsSelected = false;
                    }).Run();
            }
            
            if (EntityManager.HasComponent<PlayerUnitComponent>(entity))
            {
                var selected = EntityManager.GetComponentData<PlayerUnitComponent>(entity);
                selected.IsSelected = !selected.IsSelected;
                EntityManager.SetComponentData(entity, selected);
                
                Debug.Log($"Юнит {entity.Index} " + (selected.IsSelected ? "выбран" : "снят с выбора"));
            }
        }

        /// <summary>
        /// Создает визуальный индикатор цели движения.
        /// </summary>
        private void CreateMovementIndicator(float3 position)
        {
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.transform.position = position;
            indicator.transform.localScale = Vector3.one * 0.5f;
            indicator.name = "MovementIndicator";
            
            var renderer = indicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Standard"));
                material.color = new Color(0, 1, 0, 0.5f);
                material.SetFloat("_Mode", 3);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                renderer.material = material;
            }
            
            Object.Destroy(indicator, 2f);
        }
    }
}
