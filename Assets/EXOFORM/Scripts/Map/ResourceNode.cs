using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Exoform.Scripts.Map
{
    public enum ResourceType
    {
        Wood,
        Stone,
        Food,
        Metal
    }

    /// <summary>
    /// Компонент ресурсной точки - MonoBehaviour для статичных ресурсов
    /// Управляет сбором ресурсов, визуализацией и взаимодействием с юнитами
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        [Header("Resource Settings")] [SerializeField]
        private ResourceType resourceType = ResourceType.Wood;

        [SerializeField] private int currentAmount = 1000;
        [SerializeField] private int maxAmount = 1000;
        [SerializeField] private float gatherRate = 1f; // ресурсов в секунду
        [SerializeField] private float gatherRadius = 2f; // радиус сбора
        [SerializeField] private int gatherBatchSize = 5; // за один раз

        [Header("Regeneration")] [SerializeField]
        private bool canRegenerate = false;

        [SerializeField] private float regenerateRate = 0.1f; // ресурсов в секунду
        [SerializeField] private float regenerateDelay = 10f; // задержка после истощения
        [SerializeField] private int maxRegenerationAmount = 500; // макс восстановление

        [Header("Visual")] [SerializeField] private GameObject visual;
        [SerializeField] private TextMesh amountText;
        [SerializeField] private ParticleSystem gatherEffect;
        [SerializeField] private ParticleSystem depletionEffect;
        [SerializeField] private AudioClip gatherSound;
        [SerializeField] private AudioClip depletionSound;

        [Header("Interaction")] [SerializeField]
        private bool requiresTools = false;

        [SerializeField] private string requiredTool = ""; // например "Axe", "Pickaxe"
        [SerializeField] private float durabilityDamage = 1f; // урон инструменту
        [SerializeField] private int maxSimultaneousGatherers = 3;

        [Header("Quality")] [SerializeField] private float qualityMultiplier = 1f; // 0.5-2.0
        [SerializeField] private bool hasRareDrops = false;
        [SerializeField] private float rareDropChance = 0.05f; // 5%
        [SerializeField] private int rareDropAmount = 10;

        // Состояние
        private bool isDepleted = false;
        private bool isRegenerating = false;
        private float lastGatherTime = 0f;
        private float regenerationTimer = 0f;
        private HashSet<GameObject> currentGatherers = new HashSet<GameObject>();
        private AudioSource audioSource;

        // События
        public System.Action<ResourceNode> OnResourceDepleted;
        public System.Action<ResourceNode> OnResourceRegenerated;
        public System.Action<ResourceNode, int> OnResourceGathered;
        public System.Action<ResourceNode, GameObject> OnGathererAdded;
        public System.Action<ResourceNode, GameObject> OnGathererRemoved;

        // Геттеры
        public ResourceType ResourceType => resourceType;
        public int CurrentAmount => currentAmount;
        public int MaxAmount => maxAmount;
        public float GatherRate => gatherRate;
        public float GatherRadius => gatherRadius;
        public bool IsEmpty => currentAmount <= 0;
        public bool IsDepleted => isDepleted;
        public bool IsRegenerating => isRegenerating;
        public bool CanAcceptGatherer => currentGatherers.Count < maxSimultaneousGatherers && !isDepleted;
        public float QualityMultiplier => qualityMultiplier;
        public int CurrentGatherersCount => currentGatherers.Count;

        void Start()
        {
            InitializeComponent();
            UpdateVisual();
        }

        void Update()
        {
            if (canRegenerate && isDepleted && !isRegenerating)
            {
                regenerationTimer += Time.deltaTime;
                if (regenerationTimer >= regenerateDelay)
                {
                    StartRegeneration();
                }
            }

            if (isRegenerating)
            {
                RegenerateResource();
            }
        }

        void InitializeComponent()
        {
            // Получаем или создаем AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // 3D звук
                audioSource.maxDistance = 10f;
            }

            // Автоматически настраиваем визуал по типу ресурса
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = GetResourceColor();
            }

            // Создаем текст с количеством если его нет
            if (amountText == null)
            {
                CreateAmountText();
            }

            // Создаем эффекты если их нет
            CreateParticleEffects();

            // Настройка параметров по типу ресурса
            ConfigureByResourceType();
        }

        void CreateAmountText()
        {
            var textObject = new GameObject("Amount Text");
            textObject.transform.SetParent(transform);
            textObject.transform.localPosition = Vector3.up * 2f;

            amountText = textObject.AddComponent<TextMesh>();
            amountText.fontSize = 20;
            amountText.color = Color.white;
            amountText.anchor = TextAnchor.MiddleCenter;
            amountText.alignment = TextAlignment.Center;

            // Поворачиваем текст к камере
            if (Camera.main != null)
            {
                textObject.transform.LookAt(Camera.main.transform);
                textObject.transform.Rotate(0, 180, 0);
            }
        }

        void CreateParticleEffects()
        {
            if (gatherEffect == null)
            {
                var gatherEffectObj = new GameObject("GatherEffect");
                gatherEffectObj.transform.SetParent(transform);
                gatherEffectObj.transform.localPosition = Vector3.up;

                gatherEffect = gatherEffectObj.AddComponent<ParticleSystem>();
                var main = gatherEffect.main;
                main.startColor = GetResourceColor();
                main.maxParticles = 10;
                main.startLifetime = 1f;
                main.startSpeed = 2f;
                gatherEffect.Stop();
            }

            if (depletionEffect == null)
            {
                var depletionEffectObj = new GameObject("DepletionEffect");
                depletionEffectObj.transform.SetParent(transform);
                depletionEffectObj.transform.localPosition = Vector3.zero;

                depletionEffect = depletionEffectObj.AddComponent<ParticleSystem>();
                var main = depletionEffect.main;
                main.startColor = Color.gray;
                main.maxParticles = 50;
                main.startLifetime = 2f;
                main.startSpeed = 3f;
                depletionEffect.Stop();
            }
        }

        void ConfigureByResourceType()
        {
            switch (resourceType)
            {
                case ResourceType.Wood:
                    gatherRate = 2f;
                    gatherRadius = 2.5f;
                    gatherBatchSize = 5;
                    requiredTool = "Axe";
                    break;
                case ResourceType.Stone:
                    gatherRate = 1.5f;
                    gatherRadius = 2f;
                    gatherBatchSize = 3;
                    requiredTool = "Pickaxe";
                    break;
                case ResourceType.Food:
                    gatherRate = 3f;
                    gatherRadius = 3f;
                    gatherBatchSize = 8;
                    requiredTool = ""; // не требует инструментов
                    break;
                case ResourceType.Metal:
                    gatherRate = 1f;
                    gatherRadius = 1.5f;
                    gatherBatchSize = 2;
                    requiredTool = "Pickaxe";
                    hasRareDrops = true;
                    rareDropChance = 0.1f;
                    break;
            }
        }

        /// <summary>
        /// Инициализация ресурсной точки (вызывается генератором)
        /// </summary>
        public void Initialize(ResourceType type, int amount, float quality = 1f)
        {
            resourceType = type;
            maxAmount = amount;
            currentAmount = amount;
            qualityMultiplier = quality;

            ConfigureByResourceType();
            InitializeComponent();
            UpdateVisual();
        }

        /// <summary>
        /// Попытка добавить сборщика к ресурсу
        /// </summary>
        public bool TryAddGatherer(GameObject gatherer)
        {
            if (!CanAcceptGatherer) return false;

            currentGatherers.Add(gatherer);
            OnGathererAdded?.Invoke(this, gatherer);

            Debug.Log($"⚒️ Сборщик {gatherer.name} начал добычу {resourceType}");
            return true;
        }

        /// <summary>
        /// Удалить сборщика
        /// </summary>
        public void RemoveGatherer(GameObject gatherer)
        {
            if (currentGatherers.Remove(gatherer))
            {
                OnGathererRemoved?.Invoke(this, gatherer);
                Debug.Log($"🚶 Сборщик {gatherer.name} прекратил добычу {resourceType}");
            }
        }

        /// <summary>
        /// Попытка собрать ресурсы (для юнитов)
        /// </summary>
        public bool TryGather(GameObject gatherer, int requestedAmount, out int actualGathered, out bool gotRareDrop)
        {
            actualGathered = 0;
            gotRareDrop = false;

            if (isDepleted || !currentGatherers.Contains(gatherer))
                return false;

            // Проверка инструментов
            if (requiresTools && !string.IsNullOrEmpty(requiredTool))
            {
                // TODO: Проверить наличие нужного инструмента у сборщика
            }

            // Рассчитываем количество с учетом качества
            int baseAmount = Mathf.Min(requestedAmount, currentAmount, gatherBatchSize);
            actualGathered = Mathf.RoundToInt(baseAmount * qualityMultiplier);

            if (actualGathered > 0)
            {
                currentAmount -= baseAmount;
                lastGatherTime = Time.time;

                // Проверяем редкие дропы
                if (hasRareDrops && Random.value < rareDropChance)
                {
                    gotRareDrop = true;
                    actualGathered += rareDropAmount;
                }

                // Эффекты
                PlayGatherEffects();
                UpdateVisual();

                OnResourceGathered?.Invoke(this, actualGathered);

                Debug.Log($"⛏️ {gatherer.name} собрал {actualGathered} {resourceType} (осталось: {currentAmount})");

                if (currentAmount <= 0)
                {
                    HandleDepletion();
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Автоматический сбор (для станций добычи)
        /// </summary>
        public int AutoGather(float deltaTime)
        {
            if (isDepleted) return 0;

            float gathered = gatherRate * deltaTime * qualityMultiplier;
            int actualGathered = Mathf.Min(Mathf.FloorToInt(gathered), currentAmount);

            if (actualGathered > 0)
            {
                currentAmount -= actualGathered;
                lastGatherTime = Time.time;

                UpdateVisual();
                OnResourceGathered?.Invoke(this, actualGathered);

                if (currentAmount <= 0)
                {
                    HandleDepletion();
                }
            }

            return actualGathered;
        }

        /// <summary>
        /// Проверка, может ли юнит собирать из этой точки
        /// </summary>
        public bool CanGatherFrom(Vector3 position)
        {
            if (isDepleted) return false;

            float distance = Vector3.Distance(transform.position, position);
            return distance <= gatherRadius;
        }

        void PlayGatherEffects()
        {
            // Визуальный эффект
            if (gatherEffect != null)
            {
                gatherEffect.Play();
            }

            // Звуковой эффект
            if (audioSource != null && gatherSound != null)
            {
                audioSource.PlayOneShot(gatherSound);
            }
        }

        void UpdateVisual()
        {
            // Обновляем текст
            if (amountText != null)
            {
                amountText.text = $"{currentAmount}";

                // Меняем цвет текста в зависимости от количества
                float percentage = (float)currentAmount / maxAmount;
                if (percentage > 0.7f)
                    amountText.color = Color.green;
                else if (percentage > 0.3f)
                    amountText.color = Color.yellow;
                else if (percentage > 0f)
                    amountText.color = Color.red;
                else
                    amountText.color = Color.gray;

                // Показываем качество
                if (qualityMultiplier != 1f)
                {
                    amountText.text += $"\n★{qualityMultiplier:F1}";
                }
            }

            // Обновляем размер визуала
            if (visual != null)
            {
                float scale = Mathf.Lerp(0.3f, 1f, (float)currentAmount / maxAmount);
                visual.transform.localScale = Vector3.one * scale;
            }

            // Обновляем материал
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                if (isDepleted)
                {
                    renderer.material.color = Color.gray;
                }
                else if (isRegenerating)
                {
                    renderer.material.color = Color.Lerp(Color.gray, GetResourceColor(),
                        currentAmount / (float)maxRegenerationAmount);
                }
                else
                {
                    renderer.material.color = GetResourceColor();
                }
            }
        }

        void HandleDepletion()
        {
            isDepleted = true;
            regenerationTimer = 0f;

            // Убираем всех сборщиков
            var gatherers = new List<GameObject>(currentGatherers);
            foreach (var gatherer in gatherers)
            {
                RemoveGatherer(gatherer);
            }

            // Эффекты истощения
            if (depletionEffect != null)
            {
                depletionEffect.Play();
            }

            if (audioSource != null && depletionSound != null)
            {
                audioSource.PlayOneShot(depletionSound);
            }

            OnResourceDepleted?.Invoke(this);
            Debug.Log($"💀 Ресурс {resourceType} исчерпан в позиции {transform.position}");

            UpdateVisual();

            if (!canRegenerate)
            {
                StartCoroutine(DestroyAfterDelay(3f));
            }
        }

        void StartRegeneration()
        {
            isRegenerating = true;
            isDepleted = false;
            Debug.Log($"🌱 Начинается восстановление ресурса {resourceType}");
        }

        void RegenerateResource()
        {
            if (currentAmount >= maxRegenerationAmount)
            {
                isRegenerating = false;
                OnResourceRegenerated?.Invoke(this);
                Debug.Log($"✅ Ресурс {resourceType} восстановлен до {currentAmount}");
                return;
            }

            float regen = regenerateRate * Time.deltaTime;
            currentAmount += Mathf.RoundToInt(regen);
            currentAmount = Mathf.Min(currentAmount, maxRegenerationAmount);

            UpdateVisual();
        }

        IEnumerator DestroyAfterDelay(float delay)
        {
            Vector3 originalScale = transform.localScale;
            float timer = 0f;

            while (timer < delay)
            {
                timer += Time.deltaTime;
                float t = timer / delay;
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                yield return null;
            }

            Destroy(gameObject);
        }

        Color GetResourceColor()
        {
            return resourceType switch
            {
                ResourceType.Wood => new Color(0.6f, 0.3f, 0.1f), // Коричневый
                ResourceType.Stone => new Color(0.7f, 0.7f, 0.7f), // Серый
                ResourceType.Food => new Color(0.9f, 0.8f, 0.2f), // Золотистый
                ResourceType.Metal => new Color(0.4f, 0.4f, 0.6f), // Стальной
                _ => Color.white
            };
        }

        /// <summary>
        /// Получить иконку ресурса для UI
        /// </summary>
        public string GetResourceIcon()
        {
            return resourceType switch
            {
                ResourceType.Wood => "🪵",
                ResourceType.Stone => "🪨",
                ResourceType.Food => "🌾",
                ResourceType.Metal => "⚡",
                _ => "⛏️"
            };
        }

        /// <summary>
        /// Получить информацию о ресурсе для UI
        /// </summary>
        public string GetResourceInfo()
        {
            string info = $"{GetResourceIcon()} {resourceType}\n";
            info += $"Количество: {currentAmount}/{maxAmount}\n";
            info += $"Качество: ★{qualityMultiplier:F1}\n";
            info += $"Сборщиков: {currentGatherers.Count}/{maxSimultaneousGatherers}\n";

            if (requiresTools && !string.IsNullOrEmpty(requiredTool))
            {
                info += $"Требует: {requiredTool}\n";
            }

            if (hasRareDrops)
            {
                info += $"Редкие дропы: {rareDropChance * 100:F1}%\n";
            }

            if (isDepleted && canRegenerate)
            {
                info += $"Восстановление через: {(regenerateDelay - regenerationTimer):F1}с";
            }

            return info;
        }

        void OnDrawGizmos()
        {
            // Показываем радиус сбора
            Gizmos.color = isDepleted ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, gatherRadius);

            // Показываем тип ресурса
            Gizmos.color = GetResourceColor();
            if (isDepleted)
                Gizmos.color = Color.Lerp(Gizmos.color, Color.gray, 0.7f);

            Gizmos.DrawCube(transform.position, Vector3.one * 0.5f);

            // Показываем сборщиков
            Gizmos.color = Color.cyan;
            foreach (var gatherer in currentGatherers)
            {
                if (gatherer != null)
                    Gizmos.DrawLine(transform.position, gatherer.transform.position);
            }
        }

        void OnDrawGizmosSelected()
        {
            // Детальная информация при выделении
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, gatherRadius);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 3f,
                GetResourceInfo()
            );
#endif
        }

        // Методы для интеграции с ECS
        /// <summary>
        /// Данные для передачи в ECS систему
        /// </summary>
        public struct ResourceNodeData
        {
            public Vector3 position;
            public ResourceType resourceType;
            public int currentAmount;
            public float gatherRate;
            public float gatherRadius;
            public bool isDepleted;
            public int maxGatherers;
            public int currentGatherers;
            public float qualityMultiplier;
        }

        public ResourceNodeData GetNodeData()
        {
            return new ResourceNodeData
            {
                position = transform.position,
                resourceType = resourceType,
                currentAmount = currentAmount,
                gatherRate = gatherRate,
                gatherRadius = gatherRadius,
                isDepleted = isDepleted,
                maxGatherers = maxSimultaneousGatherers,
                currentGatherers = currentGatherers.Count,
                qualityMultiplier = qualityMultiplier
            };
        }
    }
}