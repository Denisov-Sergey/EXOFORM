using UnityEngine;
using UnityEngine.AI;
using Unity.Mathematics;

namespace PandemicWars.Scripts.Ecs.Utils
{
    /// <summary>
    /// Утилиты для работы с NavMesh, совместимые с разными версиями Unity
    /// </summary>
    public static class NavMeshUtilities
    {
        /// <summary>
        /// Проверяет наличие NavMesh в сцене
        /// </summary>
        public static bool HasNavMesh()
        {
            // Метод 1: Проверка через SamplePosition в нескольких точках
            float3[] testPositions = {
                float3.zero,
                new float3(10, 0, 10),
                new float3(-10, 0, -10),
                new float3(10, 0, -10),
                new float3(-10, 0, 10)
            };

            foreach (var pos in testPositions)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(pos, out hit, 1000f, NavMesh.AllAreas))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Получает ближайшую точку на NavMesh
        /// </summary>
        public static bool GetNearestNavMeshPoint(float3 position, out float3 nearestPoint, float maxDistance = 10f)
        {
            nearestPoint = position;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, maxDistance, NavMesh.AllAreas))
            {
                nearestPoint = hit.position;
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Проверяет, находится ли позиция на NavMesh
        /// </summary>
        public static bool IsOnNavMesh(float3 position, float tolerance = 1f)
        {
            NavMeshHit hit;
            return NavMesh.SamplePosition(position, out hit, tolerance, NavMesh.AllAreas);
        }

        /// <summary>
        /// Проверяет, можно ли построить путь между двумя точками
        /// </summary>
        public static bool CanCalculatePath(float3 from, float3 to)
        {
            NavMeshPath path = new NavMeshPath();
            return NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) && 
                   path.status == NavMeshPathStatus.PathComplete;
        }

        /// <summary>
        /// Получает информацию о NavMesh для отладки
        /// </summary>
        public static NavMeshDebugInfo GetNavMeshDebugInfo()
        {
            var info = new NavMeshDebugInfo();
            
            // Тестируем несколько позиций
            float3[] testPositions = {
                float3.zero,
                new float3(5, 0, 5),
                new float3(-5, 0, -5)
            };

            int validPositions = 0;
            foreach (var pos in testPositions)
            {
                if (IsOnNavMesh(pos))
                    validPositions++;
            }

            info.HasNavMesh = validPositions > 0;
            info.ValidTestPositions = validPositions;
            info.TotalTestPositions = testPositions.Length;

            // Проверяем области NavMesh
            info.AllAreasAccessible = NavMesh.AllAreas != 0;
            
            return info;
        }

        /// <summary>
        /// Рисует отладочную информацию о NavMesh
        /// </summary>
        public static void DrawNavMeshDebug(float3 position, float radius = 5f)
        {
            // Рисуем сферу поиска
            Color searchColor = IsOnNavMesh(position) ? Color.green : Color.red;
            DrawWireSphere(position, radius, searchColor);

            // Находим и рисуем ближайшую точку на NavMesh
            if (GetNearestNavMeshPoint(position, out float3 nearestPoint))
            {
                Debug.DrawLine(position, nearestPoint, Color.blue, 1f);
                DrawWireSphere(nearestPoint, 0.5f, Color.blue);
            }
        }

        private static void DrawWireSphere(float3 center, float radius, Color color)
        {
            const int segments = 16;
            float angleStep = 2 * Mathf.PI / segments;

            // Горизонтальный круг
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;
                
                float3 point1 = center + new float3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
                float3 point2 = center + new float3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);
                
                Debug.DrawLine(point1, point2, color, 1f);
            }

            // Вертикальный круг
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep;
                float angle2 = (i + 1) * angleStep;
                
                float3 point1 = center + new float3(0, Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius);
                float3 point2 = center + new float3(0, Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius);
                
                Debug.DrawLine(point1, point2, color, 1f);
            }
        }
    }

    /// <summary>
    /// Структура для хранения отладочной информации о NavMesh
    /// </summary>
    [System.Serializable]
    public struct NavMeshDebugInfo
    {
        public bool HasNavMesh;
        public int ValidTestPositions;
        public int TotalTestPositions;
        public bool AllAreasAccessible;

        public override string ToString()
        {
            return $"NavMesh Debug Info:\n" +
                   $"Has NavMesh: {HasNavMesh}\n" +
                   $"Valid Positions: {ValidTestPositions}/{TotalTestPositions}\n" +
                   $"All Areas Accessible: {AllAreasAccessible}";
        }
    }

    /// <summary>
    /// Компонент для автоматической проверки NavMesh в редакторе
    /// </summary>
    public class NavMeshValidator : MonoBehaviour
    {
        [Header("Validation Settings")]
        public bool autoValidate = true;
        public float validationInterval = 5f;
        public bool showDebugInfo = true;

        private float lastValidationTime;

        private void Update()
        {
            if (!autoValidate) return;

            if (Time.time - lastValidationTime > validationInterval)
            {
                ValidateNavMesh();
                lastValidationTime = Time.time;
            }
        }

        [ContextMenu("Validate NavMesh")]
        public void ValidateNavMesh()
        {
            var debugInfo = NavMeshUtilities.GetNavMeshDebugInfo();
            
            if (showDebugInfo)
            {
                Debug.Log(debugInfo.ToString());
            }

            if (!debugInfo.HasNavMesh)
            {
                Debug.LogError("NavMesh не найден или недоступен! Проверьте настройки Navigation.");
            }

            // Проверяем позицию этого объекта
            bool objectOnNavMesh = NavMeshUtilities.IsOnNavMesh(transform.position);
            if (!objectOnNavMesh)
            {
                Debug.LogWarning($"Объект {gameObject.name} не находится на NavMesh!");
                
                if (NavMeshUtilities.GetNearestNavMeshPoint(transform.position, out float3 nearestPoint))
                {
                    Debug.Log($"Ближайшая точка NavMesh: {nearestPoint}");
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showDebugInfo) return;

            // Рисуем отладочную информацию
            NavMeshUtilities.DrawNavMeshDebug(transform.position);
        }
    }
}