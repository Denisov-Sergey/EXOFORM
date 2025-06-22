﻿using UnityEngine;
using System.Collections.Generic;

namespace PandemicWars.Scripts.Map
{
    /// <summary>
    /// Упрощенный настройщик префабов - только размер и основные параметры
    /// </summary>
    public class PrefabSettings : MonoBehaviour
    {
        [Header("Basic Info")]
        [Tooltip("Название объекта")]
        public string objectName = "Building";
        
        [Tooltip("Тип тайла")]
        public TileType tileType = TileType.Building;

        [Header("Size Settings")]
        [Tooltip("Размер объекта в клетках (ширина x высота)")]
        public Vector2Int gridSize = Vector2Int.one;

        [Header("Spawn Settings")]
        [Range(0, 50)]
        [Tooltip("Максимальное количество на карте (-1 = без ограничений)")]
        public int maxCount = -1;
        
        [Range(0.1f, 5f)]
        [Tooltip("Вес для случайного выбора (больше = чаще появляется)")]
        public float spawnWeight = 1f;

        [Header("Placement Rules")]
        [Tooltip("Может размещаться на краю карты")]
        public bool canBeAtEdge = true;
        
        [Range(0, 3)]
        [Tooltip("Минимальное расстояние до дорог (0 = не требуется)")]
        public int minDistanceFromRoad = 1;
        
        [Range(1, 3)]
        [Tooltip("Минимальное расстояние до других объектов того же типа (в клетках)")]
        public int minDistanceFromSameType = 2;

        [Header("Rotation Settings")]
        [Tooltip("Поворачивать здание в сторону ближайшей дороги")]
        public bool rotateTowardsRoad = true;
        
        [Tooltip("Список разрешенных углов поворота (0, 90, 180, 270)")]
        public List<float> allowedRotations = new List<float> { 0f, 90f, 180f, 270f };
        
        [Tooltip("Случайный поворот если дорога не найдена")]
        public bool randomRotationIfNoRoad = false;

        // ====== ВЫЧИСЛЯЕМЫЕ СВОЙСТВА ======
        
        /// <summary>
        /// Площадь объекта в клетках
        /// </summary>
        public int Area => gridSize.x * gridSize.y;
        
        /// <summary>
        /// Является ли объект большим (больше 1x1)
        /// </summary>
        public bool IsLargeObject => Area > 1;

        // ====== МЕТОДЫ ======
        
        /// <summary>
        /// Получить все клетки, которые занимает объект
        /// </summary>
        public List<Vector2Int> GetOccupiedCells(Vector2Int basePosition)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    cells.Add(basePosition + new Vector2Int(x, y));
                }
            }
            
            return cells;
        }
        
        /// <summary>
        /// Проверить, помещается ли объект на карте
        /// </summary>
        public bool CanFitOnMap(Vector2Int position, int mapWidth, int mapHeight)
        {
            return position.x >= 0 && 
                   position.y >= 0 && 
                   position.x + gridSize.x <= mapWidth && 
                   position.y + gridSize.y <= mapHeight;
        }
        
        /// <summary>
        /// Проверить, можно ли разместить объект в указанной позиции (базовая версия)
        /// </summary>
        public bool CanPlaceAt(Vector2Int position, TileType[][] grid, int mapWidth, int mapHeight)
        {
            // Проверяем, помещается ли на карте
            if (!CanFitOnMap(position, mapWidth, mapHeight))
                return false;
            
            // Проверяем край карты
            if (!canBeAtEdge)
            {
                if (position.x == 0 || position.y == 0 || 
                    position.x + gridSize.x >= mapWidth || 
                    position.y + gridSize.y >= mapHeight)
                    return false;
            }
            
            // Проверяем, свободны ли все нужные клетки (только базовая проверка)
            var occupiedCells = GetOccupiedCells(position);
            foreach (var cell in occupiedCells)
            {
                // Проверяем базовую сетку (дороги блокируют размещение)
                if (grid[cell.x][cell.y] == TileType.RoadStraight)
                    return false;
            }
            
            return true;
        }

        /// <summary>
        /// Проверить, можно ли разместить объект в указанной позиции (с проверкой занятости зданиями)
        /// </summary>
        public bool CanPlaceAtWithBuildingCheck(Vector2Int position, TileType[][] grid, int mapWidth, int mapHeight, 
            System.Func<Vector2Int, bool> isCellOccupiedByBuilding)
        {
            // Базовая проверка
            if (!CanPlaceAt(position, grid, mapWidth, mapHeight))
                return false;
            
            // Дополнительная проверка зданий
            if (isCellOccupiedByBuilding != null)
            {
                var occupiedCells = GetOccupiedCells(position);
                foreach (var cell in occupiedCells)
                {
                    if (isCellOccupiedByBuilding(cell))
                        return false;
                }
            }
            
            return true;
        }
        
        // ====== ОТЛАДКА ======
        
        void OnValidate()
        {
            gridSize.x = Mathf.Max(1, gridSize.x);
            gridSize.y = Mathf.Max(1, gridSize.y);
            spawnWeight = Mathf.Max(0.1f, spawnWeight);
            objectName = string.IsNullOrEmpty(objectName) ? gameObject.name : objectName;
        }
        
        void OnDrawGizmosSelected()
        {
            // Рисуем размер объекта
            Gizmos.color = Color.cyan;
            Vector3 size = new Vector3(gridSize.x * 5, 0.2f, gridSize.y * 5);
            Gizmos.DrawWireCube(transform.position + new Vector3(gridSize.x * 0.5f - 0.5f, 0, gridSize.y * 0.5f - 0.5f), size);
            
            // Рисуем центр
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.3f);
            
            // Показываем текст с размером
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up, $"{gridSize.x}x{gridSize.y}");
            #endif
        }
    }
}