using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace PandemicWars.Scripts.Ecs.Systems.UnitLogicSystems
{
    /// <summary>
    /// Система обработки пользовательского ввода для управления юнитами.
    /// Совместима с современными версиями Unity DOTS и использует Baker систему.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InputSystem : SystemBase
    {
        private Camera _mainCamera;
        private EntityQuery _selectedUnitsQuery;
        private EntityQuery _movementTargetsQuery;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerUnitComponent>();
            
            // Создаем queries для оптимизации
            _selectedUnitsQuery = GetEntityQuery(
                ComponentType.ReadWrite<PlayerUnitComponent>(),
                ComponentType.ReadWrite<NavAgentComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
            );
            
            _movementTargetsQuery = GetEntityQuery(ComponentType.ReadOnly<MovementTargetTag>());
        }

        protected override void OnStartRunning()
        {
            _mainCamera = Camera.main ?? Object.FindObjectOfType<Camera>();
            
            if (_mainCamera == null)
                Debug.LogError("Камера не найдена! Система ввода не будет работать.");
        }

        protected override void OnUpdate()
        {
            if (_mainCamera == null) return;

            // Обрабатываем клик правой кнопкой мыши для движения
            if (Mouse.current?.rightButton.wasPressedThisFrame == true)
            {
                HandleMovementInput();
            }
            
            // Обрабатываем клик левой кнопкой мыши для выбора юнитов
            if (Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                HandleUnitSelection();
            }
        }

        /// <summary>
        /// Обрабатывает команду движения по клику правой кнопкой мыши.
        /// </summary>
        private void HandleMovementInput()
        {
            if (!TryGetWorldPosition(out float3 targetPosition))
                return;

            Debug.Log($"Команда движения в позицию: {targetPosition}");
            
            // Проверяем, есть ли выбранные юниты
            if (_selectedUnitsQuery.IsEmpty)
            {
                Debug.LogWarning("Нет выбранных юнитов для движения!");
                return;
            }

            // Создаем целевую сущность-маркер
            Entity targetEntity = CreateMovementTarget(targetPosition);
            
            // Назначаем цель всем выбранным юнитам
            AssignTargetToSelectedUnits(targetEntity, targetPosition);
            
            // Визуальная индикация
            CreateMovementIndicator(targetPosition);
        }

        /// <summary>
        /// Современный способ выбора юнитов
        /// </summary>
        private void HandleUnitSelection()
        {
            if (!TryGetWorldPosition(out float3 clickPosition))
                return;

            bool isShiftPressed = Keyboard.current?.leftShiftKey.isPressed == true;
            Entity targetEntity = Entity.Null;

            // Сначала пытаемся найти через EntityReference
            if (TryGetEntityFromRaycast(out Entity entityFromRaycast))
            {
                targetEntity = entityFromRaycast;
            }
            // Если не найден через raycast, ищем по позиции
            else
            {
                targetEntity = FindEntityByPosition(clickPosition);
            }

            if (targetEntity != Entity.Null && 
                EntityManager.Exists(targetEntity) && 
                EntityManager.HasComponent<PlayerUnitComponent>(targetEntity))
            {
                SelectUnit(targetEntity, !isShiftPressed);
            }
            else if (!isShiftPressed)
            {
                // Снимаем выбор со всех юнитов если кликнули в пустоту
                ClearAllSelection();
            }
        }

        /// <summary>
        /// Пытается получить мировую позицию из клика мышью
        /// </summary>
        private bool TryGetWorldPosition(out float3 worldPosition)
        {
            worldPosition = float3.zero;
            
            if (Mouse.current == null) return false;
            
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);
            
            Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red, 1f);
            
            if (Physics.Raycast(ray, out var hit))
            {
                worldPosition = hit.point;
                
                // ИСПРАВЛЕНИЕ: Корректируем позицию относительно NavMesh
                UnityEngine.AI.NavMeshHit navHit;
                if (UnityEngine.AI.NavMesh.SamplePosition(worldPosition, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    worldPosition = navHit.position;
                    Debug.Log($"Цель скорректирована на NavMesh: {worldPosition}");
                }
                else
                {
                    Debug.LogWarning($"Цель {worldPosition} не на NavMesh!");
                }
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Пытается получить Entity через raycast и EntityReference
        /// </summary>
        private bool TryGetEntityFromRaycast(out Entity entity)
        {
            entity = Entity.Null;
            
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);
            
            if (Physics.Raycast(ray, out var hit))
            {
                var entityReference = hit.collider.GetComponent<EntityReference>();
                if (entityReference != null && entityReference.Entity != Entity.Null)
                {
                    entity = entityReference.Entity;
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Поиск Entity по позиции (альтернатива GameObject-Entity связи)
        /// </summary>
        private Entity FindEntityByPosition(float3 clickPosition)
        {
            Entity closestEntity = Entity.Null;
            float closestDistance = float.MaxValue;
            const float maxSelectionDistance = 2f;
            
            // Ищем ближайшую сущность к точке клика
            Entities
                .WithAll<PlayerUnitComponent>()
                .ForEach((Entity entity, in LocalTransform transform, in PlayerUnitComponent playerUnit) =>
                {
                    float distance = math.distance(transform.Position, clickPosition);
                    if (distance < maxSelectionDistance && distance < closestDistance)
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
            EntityManager.DestroyEntity(_movementTargetsQuery);
            
            // Создаем архетип для новой цели
            var archetype = EntityManager.CreateArchetype(
                typeof(MovementTargetTag),
                typeof(LocalTransform)
            );
            
            // Создаем новую цель с заданным архетипом
            Entity targetEntity = EntityManager.CreateEntity(archetype);
            
            // Устанавливаем данные компонентов
            EntityManager.SetComponentData(targetEntity, new MovementTargetTag 
            { 
                CreationTime = (float)SystemAPI.Time.ElapsedTime 
            });
            EntityManager.SetComponentData(targetEntity, LocalTransform.FromPosition(position));
            
            return targetEntity;
        }

        /// <summary>
        /// Назначает цель всем выбранным юнитам.
        /// </summary>
        private void AssignTargetToSelectedUnits(Entity targetEntity, float3 targetPosition)
        {
            int assignedCount = 0;
            
            Entities
                .WithAll<PlayerUnitComponent>()
                .ForEach((Entity entity, ref NavAgentComponent navAgent, in PlayerUnitComponent playerUnit) =>
                {
                    if (!playerUnit.IsSelected) return;

                    // Устанавливаем цель и сбрасываем состояние пути
                    navAgent.TargetEntity = targetEntity;
                    navAgent.PathCalculated = false;
                    navAgent.NextPathCalculatedTime = 0f; // Пересчитываем путь немедленно
                    navAgent.CurrentWaypoint = 0;
                    
                    // Работаем с буфером waypoints
                    if (EntityManager.HasBuffer<WaypointBuffer>(entity))
                    {
                        var waypointBuffer = EntityManager.GetBuffer<WaypointBuffer>(entity);
                        waypointBuffer.Clear();
                        
                        Debug.Log($"Цель назначена юниту {entity.Index}: {targetPosition}");
                        assignedCount++;
                    }
                    else
                    {
                        Debug.LogError($"У юнита {entity.Index} нет WaypointBuffer!");
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
                ClearAllSelection();
            }
            
            if (EntityManager.HasComponent<PlayerUnitComponent>(entity))
            {
                var playerUnit = EntityManager.GetComponentData<PlayerUnitComponent>(entity);
                playerUnit.IsSelected = !playerUnit.IsSelected;
                EntityManager.SetComponentData(entity, playerUnit);
                
                Debug.Log($"Юнит {entity.Index} " + (playerUnit.IsSelected ? "выбран" : "снят с выбора"));
            }
        }

        /// <summary>
        /// Снимает выбор со всех юнитов
        /// </summary>
        private void ClearAllSelection()
        {
            Entities
                .ForEach((ref PlayerUnitComponent playerUnit) =>
                {
                    playerUnit.IsSelected = false;
                }).Run();
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
            
            // Убираем коллайдер чтобы не мешал raycast'ам
            var collider = indicator.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
            
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