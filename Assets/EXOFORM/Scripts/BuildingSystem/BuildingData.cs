using UnityEngine;

namespace Exoform.Scripts.BuildingSystem
{
    /// <summary>
    /// Структура данных для описания здания
    /// </summary>
    [System.Serializable]
    public class BuildingData
    {
        [Tooltip("Название здания для отображения в интерфейсе")]
        public string buildingName;

        [Tooltip("Префаб основного здания с коллайдерами и BuildingGrid")]
        public GameObject buildingPrefab;

        [Tooltip("Префаб превью здания (полупрозрачный) с BuildingGrid")]
        public GameObject previewPrefab;

        [Tooltip("Иконка здания для UI кнопок")]
        public Sprite buildingIcon;
    }
}