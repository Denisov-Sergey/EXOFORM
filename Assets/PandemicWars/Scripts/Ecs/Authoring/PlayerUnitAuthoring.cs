using PandemicWars.Scripts.Ecs.Components;
using PandemicWars.Scripts.Ecs.Components.UnitComponents;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Authoring
{
    /// <summary>
    /// Улучшенный Authoring для управляемых игроком юнитов
    /// </summary>
    public class PlayerUnitAuthoring : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float stoppingDistance = 0.5f;
        [SerializeField] private float acceleration = 8f;
        [SerializeField] private float angularSpeed = 120f; // градусы/сек
        [SerializeField] private int maxPathIterations = 1000;
        
        [Header("Selection Settings")]
        [SerializeField] private bool isSelectedByDefault = false;
        [SerializeField] private float selectionRadius = 1f;
        
        [Header("Group Settings")]
        [SerializeField] private int groupId = 0;
        [SerializeField] private bool isGroupLeader = false;
        [SerializeField] private Vector3 formationOffset = Vector3.zero;
        
        [Header("Debug Settings")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool showPath = true;
        [SerializeField] private Color pathColor = Color.green;
        
        [Header("NavMesh Settings")]
        [SerializeField] private int areaMask = -1; // Все области по умолчанию
        [SerializeField] private bool autoRepath = true;

        class Baker : Baker<PlayerUnitAuthoring>
        {
            public override void Bake(PlayerUnitAuthoring authoring)
            {
                if (authoring == null) return;
                
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                if (entity == Entity.Null) return;
                
                // Базовые компоненты навигации
                AddComponent(entity, new NavAgentComponent
                {
                    TargetEntity = Entity.Null,
                    MovementSpeed = authoring.moveSpeed,
                    PathCalculated = false,
                    CurrentWaypoint = 0,
                    NextPathCalculatedTime = 0f,
                    MaxPathIterations = authoring.maxPathIterations
                });
                
                // Настройки NavMesh агента
                AddComponent(entity, new NavMeshAgentSettings
                {
                    StoppingDistance = authoring.stoppingDistance,
                    Acceleration = authoring.acceleration,
                    AngularSpeed = authoring.angularSpeed,
                    AreaMask = authoring.areaMask,
                    AutoRepath = authoring.autoRepath
                });
                
                // Буфер waypoints
                AddBuffer<WaypointBuffer>(entity);
                
                // Компонент управляемого юнита
                AddComponent(entity, new PlayerUnitComponent
                {
                    IsSelected = authoring.isSelectedByDefault,
                    SelectionRadius = authoring.selectionRadius
                });

                // Компонент состояния юнита
                AddComponent(entity, new UnitStateComponent
                {
                    CurrentState = UnitState.Idle,
                    StateChangeTime = 0f
                });

                // Компонент группы (если указан groupId)
                if (authoring.groupId > 0)
                {
                    AddComponent(entity, new UnitGroupComponent
                    {
                        GroupId = authoring.groupId,
                        IsGroupLeader = authoring.isGroupLeader,
                        FormationOffset = authoring.formationOffset
                    });
                }

                // Компонент отладки (если включен)
                if (authoring.showDebugInfo || authoring.showPath)
                {
                    AddComponent(entity, new NavAgentDebugComponent
                    {
                        ShowDebugInfo = authoring.showDebugInfo,
                        ShowPath = authoring.showPath,
                        PathColor = new float4(authoring.pathColor.r, authoring.pathColor.g, 
                                             authoring.pathColor.b, authoring.pathColor.a)
                    });
                }

                // Устанавливаем связь GameObject-Entity для InputSystem
                var entityReference = authoring.GetComponent<EntityReference>();
                if (entityReference == null)
                {
                    entityReference = authoring.gameObject.AddComponent<EntityReference>();
                }
                entityReference.SetEntity(entity);

                // Добавляем тег для определения коллайдера как выбираемого юнита
                var collider = authoring.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.gameObject.tag = "SelectableUnit";
                }
            }
        }

        /// <summary>
        /// Валидация параметров в инспекторе
        /// </summary>
        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            stoppingDistance = Mathf.Max(0.1f, stoppingDistance);
            acceleration = Mathf.Max(0.1f, acceleration);
            angularSpeed = Mathf.Clamp(angularSpeed, 1f, 720f);
            selectionRadius = Mathf.Max(0.1f, selectionRadius);
            maxPathIterations = Mathf.Clamp(maxPathIterations, 10, 10000);
            groupId = Mathf.Max(0, groupId);
        }

        /// <summary>
        /// Отрисовка гизмо в редакторе
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Рисуем радиус выбора
            Gizmos.color = isSelectedByDefault ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, selectionRadius);

            // Рисуем смещение в строю
            if (groupId > 0 && formationOffset != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position + formationOffset, 0.2f);
                Gizmos.DrawLine(transform.position, transform.position + formationOffset);
            }

            // Показываем направление
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}