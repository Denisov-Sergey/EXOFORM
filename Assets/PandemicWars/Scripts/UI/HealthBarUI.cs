using UnityEngine;
using UnityEngine.UI;

namespace PandemicWars.Scripts.UI
{
    /// <summary>
    /// Простой компонент для отображения здоровья
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [Header("Health Bar Settings")] public Canvas healthCanvas;
        public Image healthFill;
        public Image backgroundImage;
        public bool hideWhenFullHealth = true;

        private Camera mainCamera;

        void Start()
        {
            mainCamera = Camera.main;

            // Создаем UI элементы если их нет
            if (healthCanvas == null)
                CreateHealthBar();

            // Скрываем по умолчанию
            SetVisible(false);
        }

        void CreateHealthBar()
        {
            // Создаем Canvas
            var canvasGO = new GameObject("HealthCanvas");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = Vector3.up * 2f;

            healthCanvas = canvasGO.AddComponent<Canvas>();
            healthCanvas.renderMode = RenderMode.WorldSpace;
            healthCanvas.worldCamera = Camera.main;
            healthCanvas.sortingOrder = 10;

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 100f;

            // Размер canvas
            var rectTransform = healthCanvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 20);
            rectTransform.localScale = Vector3.one * 0.01f;

            // Background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform);
            bgGO.transform.localPosition = Vector3.zero;
            bgGO.transform.localScale = Vector3.one;

            backgroundImage = bgGO.AddComponent<Image>();
            backgroundImage.color = Color.black;
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;

            // Health Fill
            var fillGO = new GameObject("HealthFill");
            fillGO.transform.SetParent(canvasGO.transform);
            fillGO.transform.localPosition = Vector3.zero;
            fillGO.transform.localScale = Vector3.one;

            healthFill = fillGO.AddComponent<Image>();
            healthFill.color = Color.green;
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;

            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
        }

        void LateUpdate()
        {
            // Поворачиваем к камере
            if (mainCamera != null && healthCanvas != null)
            {
                healthCanvas.transform.LookAt(mainCamera.transform);
                healthCanvas.transform.Rotate(0, 180, 0);
            }
        }

        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (healthFill == null) return;

            float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0f;
            healthFill.fillAmount = healthPercent;

            // Меняем цвет в зависимости от здоровья
            if (healthPercent > 0.6f)
                healthFill.color = Color.green;
            else if (healthPercent > 0.3f)
                healthFill.color = Color.yellow;
            else
                healthFill.color = Color.red;
        }

        public void SetVisible(bool visible)
        {
            if (healthCanvas != null)
                healthCanvas.gameObject.SetActive(visible);
        }
    }
}