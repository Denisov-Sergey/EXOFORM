using Exoform.Scripts.Ecs.Components;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Exoform.Scripts.Ecs.Systems.UnitLogicSystems
{
    /// <summary>
    /// Система для визуализации выбранных юнитов
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class UnitSelectionVisualizationSystem : SystemBase
    {
        private Material selectionMaterial;
        private GameObject selectionRingPrefab;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerUnitComponent>();
            CreateSelectionMaterial();
        }

        protected override void OnUpdate()
        {
            // Обновляем визуализацию для всех юнитов
            Entities
                .ForEach((Entity entity, in PlayerUnitComponent playerUnit, in LocalTransform transform) =>
                {
                    UpdateSelectionVisualization(entity, playerUnit.IsSelected, transform.Position);
                })
                .WithoutBurst()
                .Run();
        }

        private void CreateSelectionMaterial()
        {
            selectionMaterial = new Material(Shader.Find("Sprites/Default"));
            selectionMaterial.color = new Color(0, 1, 0, 0.8f);
        }

        private void UpdateSelectionVisualization(Entity entity, bool isSelected, float3 position)
        {
            string ringName = $"SelectionRing_{entity.Index}";
            GameObject existingRing = GameObject.Find(ringName);

            if (isSelected)
            {
                if (existingRing == null)
                {
                    // Создаем кольцо выбора
                    CreateSelectionRing(ringName, position);
                }
                else
                {
                    // Обновляем позицию существующего кольца
                    existingRing.transform.position = position;
                }
            }
            else if (existingRing != null)
            {
                // Удаляем кольцо если юнит больше не выбран
                Object.Destroy(existingRing);
            }
        }

        private void CreateSelectionRing(string name, float3 position)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = name;
            ring.transform.position = position;
            ring.transform.localScale = new Vector3(2f, 0.1f, 2f);

            // Убираем коллайдер
            var collider = ring.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            // Применяем материал
            var renderer = ring.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material = selectionMaterial;
        }

        protected override void OnDestroy()
        {
            // Очищаем все кольца выбора при уничтожении системы
            var rings = GameObject.FindGameObjectsWithTag("SelectionRing");
            foreach (var ring in rings)
            {
                if (ring != null)
                    Object.Destroy(ring);
            }

            if (selectionMaterial != null)
                Object.Destroy(selectionMaterial);

            base.OnDestroy();
        }
    }
}