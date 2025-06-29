using UnityEngine;

namespace Exoform.Scripts.UI
{
    /// <summary>
    /// Простое кольцо выбора
    /// </summary>
    public class SelectionRing : MonoBehaviour
    {
        [Header("Selection Settings")]
        public GameObject ringObject;
        public Renderer ringRenderer;
        public Color selectedColor = Color.green;
        public Color hoveredColor = Color.yellow;
        public float ringScale = 2f;
        public float pulseSpeed = 2f;

        private bool isSelected = false;
        private bool isHovered = false;
        private Vector3 originalScale;
        private Material ringMaterial;

        void Start()
        {
            CreateSelectionRing();
            SetSelected(false);
        }

        void CreateSelectionRing()
        {
            if (ringObject == null)
            {
                // Создаем простой цилиндр как кольцо
                ringObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ringObject.name = "SelectionRing";
                ringObject.transform.SetParent(transform);
                ringObject.transform.localPosition = Vector3.zero;
                ringObject.transform.localScale = new Vector3(ringScale, 0.1f, ringScale);
                
                // Убираем коллайдер
                var collider = ringObject.GetComponent<Collider>();
                if (collider != null)
                    DestroyImmediate(collider);
                
                ringRenderer = ringObject.GetComponent<Renderer>();
            }

            if (ringRenderer != null)
            {
                // Создаем материал
                ringMaterial = new Material(Shader.Find("Standard"));
                ringMaterial.SetFloat("_Mode", 3); // Transparent mode
                ringMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                ringMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                ringMaterial.SetInt("_ZWrite", 0);
                ringMaterial.DisableKeyword("_ALPHATEST_ON");
                ringMaterial.EnableKeyword("_ALPHABLEND_ON");
                ringMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                ringMaterial.renderQueue = 3000;
                
                ringRenderer.material = ringMaterial;
                originalScale = ringObject.transform.localScale;
            }
        }

        void Update()
        {
            if (isSelected && ringObject != null)
            {
                // Простой пульсирующий эффект
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.1f;
                ringObject.transform.localScale = originalScale * pulse;
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (ringObject != null)
                ringObject.SetActive(selected);
                
            if (ringMaterial != null && selected)
            {
                ringMaterial.color = new Color(selectedColor.r, selectedColor.g, selectedColor.b, 0.5f);
            }
        }

        public void SetHovered(bool hovered)
        {
            isHovered = hovered;
            
            if (ringMaterial != null && isSelected)
            {
                Color color = hovered ? hoveredColor : selectedColor;
                ringMaterial.color = new Color(color.r, color.g, color.b, 0.5f);
            }
        }

        void OnDestroy()
        {
            if (ringMaterial != null)
                DestroyImmediate(ringMaterial);
        }
    }
}