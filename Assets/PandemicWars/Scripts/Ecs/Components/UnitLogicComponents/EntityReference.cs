using Unity.Entities;
using UnityEngine;

namespace PandemicWars.Scripts.Ecs.Components.UnitComponents
{
    /// <summary>
    /// Компонент для связи GameObject с Entity
    /// </summary>
    public class EntityReference : MonoBehaviour
    {
        [HideInInspector]
        public Entity Entity;
        
        /// <summary>
        /// Устанавливает связь с Entity
        /// </summary>
        public void SetEntity(Entity entity)
        {
            Entity = entity;
        }
    }
}