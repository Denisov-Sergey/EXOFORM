using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

namespace PandemicWars.Scripts.Ecs.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InputSystem : SystemBase
    {
        private Camera _mainCamera;

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
            if (Mouse.current.rightButton.wasPressedThisFrame)
                HandleMovementInput();
        }

        private void HandleMovementInput()
        {
            if (_mainCamera == null || Mouse.current == null) return;
            
            var mousePosition = Mouse.current.position.ReadValue();
            var ray = _mainCamera.ScreenPointToRay(mousePosition);
            
            Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 10f);
            
            if (Physics.Raycast(ray, out var hit))
            {
                float3 targetPosition = hit.point;
                Debug.Log($"Raycast hit: {targetPosition}");
                
            }
        }
    }
}