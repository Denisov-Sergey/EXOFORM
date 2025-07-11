using EXOFORM.Scripts.Ecs.Components.Spawning;
using Exoform.Scripts.Map;
using Unity.Entities;
using UnityEngine;

namespace EXOFORM.Scripts.Ecs.Authoring.Spawing
{
    /// <summary>
    /// Authoring для отдельных точек спауна
    /// </summary>
    public class SpawnPointAuthoring : MonoBehaviour
    {
        [Header("Spawn Point Settings")]
        public SpawnPointType spawnType = SpawnPointType.EnemySpawn;
        public TileType zoneType = TileType.CorruptedTrap;
        public float cooldownTime = 10f;
        public bool isActive = true;

        class Baker : Baker<SpawnPointAuthoring>
        {
            public override void Bake(SpawnPointAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                AddComponent(entity, new SpawnPointComponent
                {
                    Position = authoring.transform.position,
                    PointType = authoring.spawnType,
                    ZoneType = authoring.zoneType,
                    IsActive = authoring.isActive,
                    CooldownTime = authoring.cooldownTime,
                    LastUsedTime = 0f
                });
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = GetSpawnPointColor();
            Gizmos.DrawWireSphere(transform.position, 2f);
            
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(transform.position, 0.5f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, 
                $"{spawnType}\n{zoneType}");
#endif
        }

        Color GetSpawnPointColor()
        {
            return spawnType switch
            {
                SpawnPointType.PlayerSpawn => Color.green,
                SpawnPointType.EnemySpawn => Color.red,
                SpawnPointType.BossSpawn => Color.magenta,
                SpawnPointType.ReinforcementSpawn => Color.yellow,
                _ => Color.white
            };
        }
    }
}