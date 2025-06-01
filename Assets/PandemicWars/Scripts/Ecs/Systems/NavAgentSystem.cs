using PandemicWars.Scripts.Ecs.Components;
using UnityEngine;
using Unity.Entities;
using UnityEngine.Experimental.AI;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;

namespace PandemicWars.Scripts.Ecs.Systems
{
    /// <summary>
    /// Высокопроизводительная система навигации для ECS архитектуры Unity DOTS.
    /// Обеспечивает расчет пути через NavMesh и плавное движение агентов к целям.
    /// Полностью совместима с Burst компилятором для максимальной производительности.
    /// </summary>
    [BurstCompile]
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
        private void OnUpdate(ref SystemState state)
        {
            // Кешируем текущее время для производительности
            var currentTime = SystemAPI.Time.ElapsedTime;

            // Итерируемся по всем сущностям с компонентами навигации и трансформации
            foreach(var (navAgent, transform, entity) in SystemAPI.Query<RefRW<NavAgentComponent>, RefRW<LocalTransform>>().WithEntityAccess())
            {
                //  Проверяем наличие буфера waypoint'ов перед использованием
                if (!state.EntityManager.HasBuffer<WaypointBuffer>(entity))
                {
                    continue; // Пропускаем сущности без буфера waypoint'ов
                }

                DynamicBuffer<WaypointBuffer> waypointBuffer = state.EntityManager.GetBuffer<WaypointBuffer>(entity);

                // Проверяем, нужно ли пересчитывать путь (каждую секунду)
                if(navAgent.ValueRO.NextPathCalculatedTime < currentTime)
                {
                    navAgent.ValueRW.NextPathCalculatedTime += 2f;
                    
                    // Сбрасываем состояние пути перед новым расчетом
                    navAgent.ValueRW.PathCalculated = false;
                    navAgent.ValueRW.CurrentWaypoint = 0;
                    
                    // Запускаем расчет нового пути
                    CalculatePath(navAgent, transform, waypointBuffer, ref state);
                }
                //  Движение только если путь успешно рассчитан
                else if (navAgent.ValueRO.PathCalculated)
                {
                    Move(navAgent, transform, waypointBuffer, ref state);
                }
                // Если путь не рассчитан и время еще не пришло - агент ждет
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
            //  Комплексная проверка безопасности перед доступом к буферу
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

            // Проверяем, достиг ли агент текущего waypoint'а (порог 0.2 единицы)
            if (math.distance(currentPosition, targetWaypoint) < 0.2f)
            {
                // Переходим к следующему waypoint'у, если он существует
                if (navAgent.ValueRO.CurrentWaypoint + 1 < waypointBuffer.Length)
                {
                    navAgent.ValueRW.CurrentWaypoint += 1;
                }
                else
                {
                    //  Обработка достижения конца пути
                    navAgent.ValueRW.PathCalculated = false;
                    return; // Путь завершен, останавливаем движение
                }
            }

            // Пересчитываем направление после возможного изменения waypoint'а
            //  Повторная проверка границ после изменения CurrentWaypoint
            if (navAgent.ValueRO.CurrentWaypoint >= waypointBuffer.Length)
            {
                navAgent.ValueRW.PathCalculated = false;
                return;
            }

            targetWaypoint = waypointBuffer[navAgent.ValueRO.CurrentWaypoint].waypoint;
            float3 direction = targetWaypoint - currentPosition;

            // Проверяем, что направление не нулевое (избегаем деления на ноль)
            if (math.lengthsq(direction) < 0.0001f)
            {
                return; // Слишком близко к цели, пропускаем кадр
            }

            //  Правильный расчет поворота для 3D пространства
            float3 normalizedDirection = math.normalize(direction);
            
            // Используем LookRotationSafe для корректного 3D поворота (Burst совместимо)
            var targetRotation = quaternion.LookRotationSafe(normalizedDirection, math.up());
            
            // Плавный поворот со скоростью 5 радиан/секунду
            transform.ValueRW.Rotation = math.slerp(
                transform.ValueRW.Rotation,
                targetRotation,
                SystemAPI.Time.DeltaTime * 5f); // Множитель для контроля скорости поворота

            // Перемещение агента в направлении цели
            transform.ValueRW.Position += normalizedDirection * SystemAPI.Time.DeltaTime * navAgent.ValueRO.MovementSpeed;
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

            //  Проверяем инициализацию NavMeshQuery
            if (!navMeshQueryInitialized)
            {
                return; // NavMeshQuery не готов к использованию
            }

            //  Проверяем существование целевой сущности и её компонентов
            if (!state.EntityManager.Exists(navAgent.ValueRO.TargetEntity) ||
                !state.EntityManager.HasComponent<LocalTransform>(navAgent.ValueRO.TargetEntity))
            {
                return; // Цель не существует или не имеет позиции
            }

            // Получаем позиции начала и конца маршрута
            float3 fromPosition = transform.ValueRO.Position;
            float3 toPosition = state.EntityManager.GetComponentData<LocalTransform>(navAgent.ValueRO.TargetEntity).Position;
            
            // Размер области поиска на NavMesh (1x1x1 единица в каждом направлении)
            float3 extents = new float3(1, 1, 1);

            // Проецируем мировые позиции на ближайшие точки NavMesh
            NavMeshLocation fromLocation = navMeshQuery.MapLocation(fromPosition, extents, 0);
            NavMeshLocation toLocation = navMeshQuery.MapLocation(toPosition, extents, 0);

            // Переменные для отслеживания статуса операций
            PathQueryStatus status;
            PathQueryStatus returningStatus;
            const int maxPathSize = 100; // Константа для максимального размера пути

            // Проверяем, что обе позиции находятся на валидном NavMesh
            if(navMeshQuery.IsValid(fromLocation) && navMeshQuery.IsValid(toLocation))
            {
                // Начинаем процесс поиска пути
                status = navMeshQuery.BeginFindPath(fromLocation, toLocation);
                
                if(status == PathQueryStatus.InProgress)
                {
                    // Выполняем итерации поиска (максимум 100 за кадр для производительности)
                    status = navMeshQuery.UpdateFindPath(100, out int iterationsPerformed);
                    
                    if (status == PathQueryStatus.Success)
                    {
                        // Завершаем поиск и получаем количество полигонов в пути
                        status = navMeshQuery.EndFindPath(out int pathSize);

                        // Проверяем, что путь действительно найден
                        if (pathSize > 0)
                        {
                            // Создаем временные массивы для обработки пути
                            var result = new NativeArray<NavMeshLocation>(pathSize + 1, Allocator.Temp);
                            var straightPathFlag = new NativeArray<StraightPathFlags>(maxPathSize, Allocator.Temp);
                            var vertexSide = new NativeArray<float>(maxPathSize, Allocator.Temp);
                            var polygonIds = new NativeArray<PolygonId>(pathSize + 1, Allocator.Temp);
                            int straightPathCount = 0;

                            // Получаем массив полигонов пути
                            navMeshQuery.GetPathResult(polygonIds);

                            // Преобразуем полигональный путь в прямой путь с waypoint'ами
                            returningStatus = PathUtils.FindStraightPath(
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
                            if(returningStatus == PathQueryStatus.Success && straightPathCount > 0)
                            {
                                //  Используем for вместо foreach для Burst оптимизации
                                for (int i = 0; i < straightPathCount; i++)
                                {
                                    //  Используем float3.zero вместо Vector3.zero для Burst
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
                                }
                            }
                            
                            //  Освобождаем ВСЕ временные массивы для предотвращения утечек памяти
                            result.Dispose();          
                            straightPathFlag.Dispose();
                            polygonIds.Dispose();
                            vertexSide.Dispose();
                        }
                    }
                }
            }
            
            // Используем существующий navMeshQuery вместо создания нового
            // navMeshQuery НЕ освобождается здесь - он переиспользуется для следующих вызовов
            // Освобождение происходит только в OnDestroy()
        }
    }
}
