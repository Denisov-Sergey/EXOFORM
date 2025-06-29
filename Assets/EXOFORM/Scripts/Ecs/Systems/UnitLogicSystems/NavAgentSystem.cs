using Exoform.Scripts.Ecs.Components.UnitComponents;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Experimental.AI;

namespace Exoform.Scripts.Ecs.Systems.UnitLogicSystems
{
    /// <summary>
    /// Высокопроизводительная система навигации для ECS архитектуры Unity DOTS.
    /// Обеспечивает расчет пути через NavMesh и плавное движение агентов к целям.
    /// Полностью совместима с Burst компилятором для максимальной производительности.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct NavAgentSystem : ISystem
    {
        /// <summary>
        /// NavMeshQuery для расчета путей. Создается один раз в OnCreate и переиспользуется
        /// для всех агентов, что значительно повышает производительность.
        /// </summary>
        private NavMeshQuery navMeshQuery;
        
        /// <summary>
        /// Флаг инициализации NavMeshQuery. Используется вместо IsCreated для Burst совместимости.
        /// </summary>
        private bool navMeshQueryInitialized;

        /// <summary>
        /// Инициализация системы при старте.
        /// Создаем единственный NavMeshQuery с Persistent аллокатором для переиспользования.
        /// </summary>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Проверяем наличие необходимых компонентов
            state.RequireForUpdate<NavAgentComponent>();
            
            // Получаем мир NavMesh по умолчанию
            var navMeshWorld = NavMeshWorld.GetDefaultWorld();
            
            // Создаем NavMeshQuery с:
            // - Persistent аллокатором (существует до OnDestroy)
            // - Буфером на 1000 узлов (достаточно для большинства сценариев)
            navMeshQuery = new NavMeshQuery(navMeshWorld, Allocator.Persistent, 1000);
            navMeshQueryInitialized = true;
        }

        /// <summary>
        /// Освобождение ресурсов при уничтожении системы.
        /// Критически важно для предотвращения утечек памяти.
        /// </summary>
        [BurstCompile] 
        public void OnDestroy(ref SystemState state)
        {
            // Освобождаем NavMeshQuery только если он был инициализирован
            if (navMeshQueryInitialized)
            {
                navMeshQuery.Dispose();
                navMeshQueryInitialized = false;
            }
        }
        
        /// <summary>
        /// Основной цикл обновления системы навигации.
        /// Для каждого агента определяет необходимость пересчета пути или продолжения движения.
        /// </summary>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Проверяем инициализацию NavMeshQuery
            if (!navMeshQueryInitialized)
            {
                return;
            }

            // Кешируем текущее время для производительности
            var currentTime = SystemAPI.Time.ElapsedTime;

            // Итерируемся по всем сущностям с компонентами навигации и трансформации
            foreach(var (navAgent, transform, entity) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                // Проверяем наличие буфера waypoint'ов перед использованием
                if (!state.EntityManager.HasBuffer<WaypointBuffer>(entity))
                {
                    continue; // Пропускаем сущности без буфера waypoint'ов
                }

                DynamicBuffer<WaypointBuffer> waypointBuffer = state.EntityManager.GetBuffer<WaypointBuffer>(entity);

                // : Проверяем, есть ли цель и нужно ли пересчитывать путь
                bool hasValidTarget = navAgent.ValueRO.TargetEntity != Entity.Null && 
                                    state.EntityManager.Exists(navAgent.ValueRO.TargetEntity) &&
                                    state.EntityManager.HasComponent<LocalTransform>(navAgent.ValueRO.TargetEntity);

                // Если нет валидной цели, очищаем состояние
                if (!hasValidTarget)
                {
                    navAgent.ValueRW.PathCalculated = false;
                    navAgent.ValueRW.TargetEntity = Entity.Null;
                    waypointBuffer.Clear();
                    continue;
                }

                //  Принудительный пересчет пути если он был сброшен в InputSystem
                bool shouldRecalculatePath = !navAgent.ValueRO.PathCalculated || 
                                           navAgent.ValueRO.NextPathCalculatedTime <= currentTime;

                if (shouldRecalculatePath)
                {
                    // Устанавливаем следующее время пересчета (через 2 секунды)
                    navAgent.ValueRW.NextPathCalculatedTime = (float)currentTime + 2f;
                    
                    // Сбрасываем состояние пути перед новым расчетом
                    navAgent.ValueRW.PathCalculated = false;
                    navAgent.ValueRW.CurrentWaypoint = 0;
                    
                    #if UNITY_EDITOR && !BURST_COMPILE
                    Debug.Log($"Начинаем расчет пути для Entity {entity.Index}");
                    #endif
                    
                    // Запускаем расчет нового пути
                    CalculatePath(navAgent, transform, waypointBuffer, ref state);
                }
                // Движение только если путь успешно рассчитан
                else if (navAgent.ValueRO.PathCalculated && waypointBuffer.Length > 0)
                {
                    Move(navAgent, transform, waypointBuffer, ref state);
                }
            }
        }

        /// <summary>
        /// Обрабатывает движение агента по рассчитанному пути.
        /// Включает логику переключения между waypoint'ами, поворота и движения.
        /// </summary>
        [BurstCompile]
        private void Move(RefRW<NavAgentComponent> navAgent, RefRW<LocalTransform> transform, 
            DynamicBuffer<WaypointBuffer> waypointBuffer, ref SystemState state)
        {
            // Комплексная проверка безопасности перед доступом к буферу
            if (!navAgent.ValueRO.PathCalculated ||           // Путь не рассчитан
                waypointBuffer.Length == 0 ||                 // Буфер пустой
                navAgent.ValueRO.CurrentWaypoint >= waypointBuffer.Length) // Индекс вне границ
            {
                // Если данные некорректны, останавливаем движение
                navAgent.ValueRW.PathCalculated = false;
                return;
            }

            // Получаем текущую позицию агента и целевой waypoint
            var currentPosition = transform.ValueRO.Position;
            var targetWaypoint = waypointBuffer[navAgent.ValueRO.CurrentWaypoint].waypoint;

            // Динамический порог достижения waypoint'а в зависимости от скорости
            float baseThreshold = 0.3f;
            float speedFactor = navAgent.ValueRO.MovementSpeed * SystemAPI.Time.DeltaTime;
            float waypointReachThreshold = math.max(baseThreshold, speedFactor * 2f);

            float distanceToWaypoint = math.distance(currentPosition, targetWaypoint);

            #if UNITY_EDITOR && !BURST_COMPILE
            if (navAgent.ValueRO.CurrentWaypoint == 0) // Логируем только первый waypoint
            {
                Debug.Log($"Движение: дистанция до цели {distanceToWaypoint:F3}, порог {waypointReachThreshold:F3}");
            }
            #endif

            if (distanceToWaypoint < waypointReachThreshold)
            {
                // Переходим к следующему waypoint'у, если он существует
                if (navAgent.ValueRO.CurrentWaypoint + 1 < waypointBuffer.Length)
                {
                    navAgent.ValueRW.CurrentWaypoint += 1;
                    #if UNITY_EDITOR && !BURST_COMPILE
                    Debug.Log($"Переход к waypoint {navAgent.ValueRO.CurrentWaypoint} из {waypointBuffer.Length}");
                    #endif
                    return; // Выходим, чтобы на следующем кадре использовать новый waypoint
                }
                else
                {
                    //  Улучшенная проверка достижения финальной цели
                    if (navAgent.ValueRO.TargetEntity != Entity.Null && 
                        state.EntityManager.Exists(navAgent.ValueRO.TargetEntity) &&
                        state.EntityManager.HasComponent<LocalTransform>(navAgent.ValueRO.TargetEntity))
                    {
                        var targetTransform = state.EntityManager.GetComponentData<LocalTransform>(navAgent.ValueRO.TargetEntity);
                        float distanceToFinalTarget = math.distance(currentPosition, targetTransform.Position);
                        
                        #if UNITY_EDITOR && !BURST_COMPILE
                        Debug.Log($"Проверка финальной цели: дистанция {distanceToFinalTarget:F3}");
                        #endif
                        
                        // Если юнит действительно близко к финальной цели
                        if (distanceToFinalTarget < 1.0f) // Порог для финальной цели
                        {
                            navAgent.ValueRW.PathCalculated = false;
                            navAgent.ValueRW.TargetEntity = Entity.Null;
                            waypointBuffer.Clear();
                            #if UNITY_EDITOR && !BURST_COMPILE
                            Debug.Log("Юнит ДЕЙСТВИТЕЛЬНО достиг финальной цели!");
                            #endif
                            return;
                        }
                        else
                        {
                            // Юнит не у цели - пересчитываем путь
                            #if UNITY_EDITOR && !BURST_COMPILE
                            Debug.LogWarning($"Юнит НЕ у цели (дистанция {distanceToFinalTarget:F3}), пересчитываем путь");
                            #endif
                            navAgent.ValueRW.PathCalculated = false;
                            // Сбрасываем время для немедленного пересчета
                            navAgent.ValueRW.NextPathCalculatedTime = 0f;
                            return;
                        }
                    }
                    else
                    {
                        // Нет валидной цели
                        navAgent.ValueRW.PathCalculated = false;
                        navAgent.ValueRW.TargetEntity = Entity.Null;
                        waypointBuffer.Clear();
                        #if UNITY_EDITOR && !BURST_COMPILE
                        Debug.Log("Нет валидной цели, останавливаем движение");
                        #endif
                        return;
                    }
                }
            }

            // Вычисляем направление движения
            float3 direction = targetWaypoint - currentPosition;

            // Проверяем, что направление не нулевое (избегаем деления на ноль)
            if (math.lengthsq(direction) < 0.0001f)
            {
                return; // Слишком близко к цели, пропускаем кадр
            }

            // Нормализуем направление
            float3 normalizedDirection = math.normalize(direction);
            
            // Рассчитываем новую позицию
            float deltaTime = SystemAPI.Time.DeltaTime;
            float moveDistance = navAgent.ValueRO.MovementSpeed * deltaTime;
            var newPosition = currentPosition + normalizedDirection * moveDistance;

            //  Корректируем Y координату относительно NavMesh (только если не в Burst)
            #if !BURST_COMPILE
            UnityEngine.AI.NavMeshHit hit;
            if (UnityEngine.AI.NavMesh.SamplePosition(newPosition, out hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            {
                newPosition.y = hit.position.y;
            }
            #endif

            // Плавный поворот к цели
            if (math.lengthsq(normalizedDirection) > 0.001f)
            {
                var targetRotation = quaternion.LookRotationSafe(normalizedDirection, math.up());
                const float rotationSpeed = 5f; // радиан/секунду
                transform.ValueRW.Rotation = math.slerp(
                    transform.ValueRW.Rotation,
                    targetRotation,
                    deltaTime * rotationSpeed);
            }

            // Применяем новую позицию
            transform.ValueRW.Position = newPosition;

            // Debug информация (только в редакторе и не в Burst)
            #if UNITY_EDITOR && !BURST_COMPILE
            if (navAgent.ValueRO.CurrentWaypoint == 0) // Выводим только для первого waypoint'а чтобы не спамить
            {
                Debug.DrawLine(currentPosition, targetWaypoint, Color.green, 0.1f);
            }
            #endif
        }

        /// <summary>
        /// Рассчитывает оптимальный путь от текущей позиции агента до целевой сущности.
        /// Использует NavMesh для поиска пути и создает массив waypoint'ов для движения.
        /// </summary>
        [BurstCompile]
        private void CalculatePath(RefRW<NavAgentComponent> navAgent, RefRW<LocalTransform> transform, 
            DynamicBuffer<WaypointBuffer> waypointBuffer, ref SystemState state)
        {
            // Очищаем предыдущий путь и сбрасываем состояние
            waypointBuffer.Clear();
            navAgent.ValueRW.PathCalculated = false;
            navAgent.ValueRW.CurrentWaypoint = 0;

            // Проверяем существование целевой сущности и её компонентов
            if (navAgent.ValueRO.TargetEntity == Entity.Null ||
                !state.EntityManager.Exists(navAgent.ValueRO.TargetEntity) ||
                !state.EntityManager.HasComponent<LocalTransform>(navAgent.ValueRO.TargetEntity))
            {
                #if UNITY_EDITOR && !BURST_COMPILE
                Debug.LogWarning("Цель не существует или не имеет позиции");
                #endif
                return; // Цель не существует или не имеет позиции
            }

            // Получаем позиции начала и конца маршрута
            float3 fromPosition = transform.ValueRO.Position;
            float3 toPosition = state.EntityManager.GetComponentData<LocalTransform>(navAgent.ValueRO.TargetEntity).Position;
            
            // Уменьшаем минимальную дистанцию для коротких перемещений
            float totalDistance = math.distance(fromPosition, toPosition);
            const float minPathDistance = 0.5f; // Было 1.0f, стало 0.5f
            
            if (totalDistance < minPathDistance)
            {
                #if UNITY_EDITOR && !BURST_COMPILE
                Debug.Log($"Дистанция слишком мала ({totalDistance:F3}), создаем прямой путь");
                #endif
                
                // Создаем простой путь для коротких дистанций
                waypointBuffer.Add(new WaypointBuffer { waypoint = toPosition });
                navAgent.ValueRW.PathCalculated = true;
                navAgent.ValueRW.CurrentWaypoint = 0;
                return;
            }
            
            #if UNITY_EDITOR && !BURST_COMPILE
            Debug.Log($"Расчет пути от {fromPosition} до {toPosition}, дистанция: {totalDistance:F3}");
            #endif

            // Увеличиваем размер области поиска для лучшего поиска
            float3 extents = new float3(10, 10, 10); 

            // Проецируем мировые позиции на ближайшие точки NavMesh
            NavMeshLocation fromLocation = navMeshQuery.MapLocation(fromPosition, extents, 0);
            NavMeshLocation toLocation = navMeshQuery.MapLocation(toPosition, extents, 0);

            // Проверяем, что обе позиции находятся на валидном NavMesh
            if (!navMeshQuery.IsValid(fromLocation))
            {
                #if UNITY_EDITOR && !BURST_COMPILE
                Debug.LogWarning($"Стартовая позиция {fromPosition} не на NavMesh! Пытаемся найти ближайшую точку.");
                #endif
                
                //  Пытаемся найти ближайшую валидную точку
                fromLocation = navMeshQuery.MapLocation(fromPosition, new float3(20, 20, 20), 0);
                if (!navMeshQuery.IsValid(fromLocation))
                {
                    #if UNITY_EDITOR && !BURST_COMPILE
                    Debug.LogError($"Не удалось найти валидную стартовую позицию для {fromPosition}");
                    #endif
                    return;
                }
            }

            if (!navMeshQuery.IsValid(toLocation))
            {
                #if UNITY_EDITOR && !BURST_COMPILE
                Debug.LogWarning($"Целевая позиция {toPosition} не на NavMesh! Пытаемся найти ближайшую точку.");
                #endif
                
                //  Пытаемся найти ближайшую валидную точку
                toLocation = navMeshQuery.MapLocation(toPosition, new float3(20, 20, 20), 0);
                if (!navMeshQuery.IsValid(toLocation))
                {
                    #if UNITY_EDITOR && !BURST_COMPILE
                    Debug.LogError($"Не удалось найти валидную целевую позицию для {toPosition}");
                    #endif
                    return;
                }
            }

            // Переменные для отслеживания статуса операций
            PathQueryStatus status;
            const int maxPathSize = 100; // Константа для максимального размера пути

            // Начинаем процесс поиска пути
            status = navMeshQuery.BeginFindPath(fromLocation, toLocation);
            
            if (status == PathQueryStatus.InProgress)
            {
                // Выполняем итерации поиска (максимум за кадр для производительности)
                int maxIterations = navAgent.ValueRO.MaxPathIterations > 0 ? 
                    navAgent.ValueRO.MaxPathIterations : 100;
                status = navMeshQuery.UpdateFindPath(maxIterations, out int iterationsPerformed);
                
                if (status == PathQueryStatus.Success)
                {
                    // Завершаем поиск и получаем количество полигонов в пути
                    status = navMeshQuery.EndFindPath(out int pathSize);

                    // Проверяем, что путь действительно найден
                    if (pathSize > 0)
                    {
                        // Создаем временные массивы для обработки пути
                        var result = new NativeArray<NavMeshLocation>(maxPathSize, Allocator.Temp);
                        var straightPathFlag = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
                        var vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
                        var polygonIds = new NativeArray<PolygonId>(pathSize, Allocator.Temp);
                        int straightPathCount = 0;

                        try
                        {
                            // Получаем массив полигонов пути
                            navMeshQuery.GetPathResult(polygonIds);

                            // Преобразуем полигональный путь в прямой путь с waypoint'ами
                            PathQueryStatus straightPathStatus = PathUtils.FindStraightPath(
                                navMeshQuery,
                                fromPosition,
                                toPosition,
                                polygonIds,
                                pathSize,
                                ref result,
                                ref straightPathFlag,
                                ref vertexSide,
                                ref straightPathCount,
                                maxPathSize);

                            // Проверяем успешность создания прямого пути
                            if (straightPathStatus == PathQueryStatus.Success && straightPathCount > 0)
                            {
                                // Добавляем waypoint'ы в буфер
                                for (int i = 0; i < straightPathCount; i++)
                                {
                                    // Используем float3.zero вместо Vector3.zero для Burst
                                    if (!result[i].position.Equals(float3.zero))
                                    {
                                        // Добавляем валидный waypoint в буфер
                                        waypointBuffer.Add(new WaypointBuffer { waypoint = result[i].position });
                                    }
                                }

                                // Устанавливаем флаги готовности пути только если waypoint'ы добавлены
                                if (waypointBuffer.Length > 0)
                                {
                                    navAgent.ValueRW.CurrentWaypoint = 0;
                                    navAgent.ValueRW.PathCalculated = true;
                                    #if UNITY_EDITOR && !BURST_COMPILE
                                    Debug.Log($"Путь рассчитан успешно! Waypoints: {waypointBuffer.Length}");
                                    #endif
                                }
                                else
                                {
                                    #if UNITY_EDITOR && !BURST_COMPILE
                                    Debug.LogWarning("Путь рассчитан, но waypoint'ы не добавлены!");
                                    #endif
                                    
                                    //  Fallback - создаем простой путь к цели
                                    waypointBuffer.Add(new WaypointBuffer { waypoint = toPosition });
                                    navAgent.ValueRW.CurrentWaypoint = 0;
                                    navAgent.ValueRW.PathCalculated = true;
                                }
                            }
                            else
                            {
                                #if UNITY_EDITOR && !BURST_COMPILE
                                Debug.LogWarning($"Не удалось создать прямой путь. Статус: {straightPathStatus}, Count: {straightPathCount}. Создаем простой путь.");
                                #endif
                                
                                //  Fallback - создаем простой путь к цели
                                waypointBuffer.Add(new WaypointBuffer { waypoint = toPosition });
                                navAgent.ValueRW.CurrentWaypoint = 0;
                                navAgent.ValueRW.PathCalculated = true;
                            }
                        }
                        finally
                        {
                            // Освобождаем ВСЕ временные массивы для предотвращения утечек памяти
                            if (result.IsCreated) result.Dispose();
                            if (straightPathFlag.IsCreated) straightPathFlag.Dispose();
                            if (polygonIds.IsCreated) polygonIds.Dispose();
                            if (vertexSide.IsCreated) vertexSide.Dispose();
                        }
                    }
                    else
                    {
                        #if UNITY_EDITOR && !BURST_COMPILE
                        Debug.LogWarning("Путь не найден - pathSize = 0. Создаем прямой путь.");
                        #endif
                        
                        //  Fallback - создаем простой путь к цели
                        waypointBuffer.Add(new WaypointBuffer { waypoint = toPosition });
                        navAgent.ValueRW.CurrentWaypoint = 0;
                        navAgent.ValueRW.PathCalculated = true;
                    }
                }
                else
                {
                    #if UNITY_EDITOR && !BURST_COMPILE
                    Debug.LogWarning($"Поиск пути не завершен успешно. Статус: {status}. Создаем прямой путь.");
                    #endif
                    
                    //  Fallback - создаем простой путь к цели
                    waypointBuffer.Add(new WaypointBuffer { waypoint = toPosition });
                    navAgent.ValueRW.CurrentWaypoint = 0;
                    navAgent.ValueRW.PathCalculated = true;
                }
            }
            else
            {
                #if UNITY_EDITOR && !BURST_COMPILE
                Debug.LogWarning($"Не удалось начать поиск пути. Статус: {status}. Создаем прямой путь.");
                #endif
                
                // Fallback - создаем простой путь к цели
                waypointBuffer.Add(new WaypointBuffer { waypoint = toPosition });
                navAgent.ValueRW.CurrentWaypoint = 0;
                navAgent.ValueRW.PathCalculated = true;
            }
        }
    }
}