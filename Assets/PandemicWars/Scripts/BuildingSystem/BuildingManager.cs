using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PandemicWars.Scripts.BuildingSystem
{
    
/// Основной класс для системы строительства зданий в RTS игре
/// Обрабатывает выбор, позиционирование и размещение зданий с защитой от искажений трансформации
/// </summary>
public class Building : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Массив префабов")]
    [Tooltip("Массив всех доступных для строительства зданий")]
    public BuildingData[] buildings;
    
    [Header("Материалы")]
    [Tooltip("Материал для отображения валидной позиции (зеленый)")]
    public Material validMaterial;
    
    [Tooltip("Материал для отображения невалидной позиции (красный)")]
    public Material invalidMaterial;
    
    [Header("Настройки")]
    
    [Tooltip("Максимальная дистанция raycast для поиска поверхности")]
    [Range(100f, 1000f)]
    public float maxRaycastDistance = 500f;
    
    [Header("Настройки коллизий")]
    [Tooltip("Слой для определения поверхности земли")]
    public LayerMask groundLayer = 1;

    [Tooltip("Слои которые нужно игнорировать при проверке коллизий")]
    public LayerMask ignoreLayers = 0;

    [Tooltip("Игнорировать триггеры при проверке коллизий")]
    public bool ignoreTriggers = true;

    [Tooltip("Высота проверки коллизий")]
    [Range(0.5f, 10f)]
    public float collisionCheckHeight = 2f;

    
    [Header("Отладка")]
    [Tooltip("Показывать отладочную информацию в консоли")]
    public bool debugMode = false;
    
    #endregion
    
    #region Private Fields
    
    // Кэшированные компоненты для оптимизации
    private Camera mainCamera;
    private Mouse currentMouse;
    private Keyboard currentKeyboard;
    
    // Состояние системы строительства
    private GameObject currentPreview;
    private BuildingGrid currentPreviewGrid;
    private bool isBuildMode = false;
    private bool canPlace = false;
    private Renderer previewRenderer;
    private int selectedBuildingIndex = -1;
    
    // Список размещенных зданий для проверки коллизий
    private List<BuildingGrid> placedBuildings = new List<BuildingGrid>();
    
    // Кэшированные значения для оптимизации
    private Vector2 cachedMousePosition = Vector2.zero;
    private Ray cachedRay;
    private Vector3 lastValidPosition = Vector3.zero;
    private const int MAX_HOTKEY_BUILDINGS = 9;
    
    // Защита от искажений масштаба
    private Vector3 originalPreviewScale = Vector3.one;
    private Quaternion originalPreviewRotation = Quaternion.identity;
    
    #endregion
    
    #region Unity Lifecycle
    
    /// <summary>
    /// Инициализация компонентов и кэширование ссылок
    /// </summary>
    void Start()
    {
        // Кэшируем ссылку на основную камеру
        mainCamera = Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("Building System: Основная камера не найдена! Убедитесь что камера помечена как MainCamera.");
        }
        
        // Кэшируем устройства ввода
        CacheInputDevices();
        
        // Валидируем данные зданий
        ValidateBuildingData();
        
        // Находим существующие здания в сцене
        FindExistingBuildings();
        
        if (debugMode)
        {
            Debug.Log("Building System: Система строительства инициализирована");
        }
    }
    
    /// <summary>
    /// Основной цикл обновления - обрабатывает ввод и логику строительства
    /// </summary>
    void Update()
    {
        // Обновляем устройства ввода если необходимо
        UpdateInputDevicesIfNeeded();
        
        // Обрабатываем пользовательский ввод
        HandleInput();
        
        // Обновляем превью только в режиме строительства
        if (isBuildMode && currentPreview != null)
        {
            UpdateBuildingPreview();
        }
    }
    
    /// <summary>
    /// Очистка при уничтожении объекта
    /// </summary>
    void OnDestroy()
    {
        // Очищаем превью при уничтожении компонента
        if (currentPreview != null)
        {
            DestroyImmediate(currentPreview);
        }
    }
    
    #endregion
    
    #region Input Handling
    
    /// <summary>
    /// Кэширует ссылки на устройства ввода для оптимизации
    /// </summary>
    private void CacheInputDevices()
    {
        currentMouse = Mouse.current;
        currentKeyboard = Keyboard.current;
    }
    
    /// <summary>
    /// Обновляет устройства ввода при необходимости
    /// </summary>
    private void UpdateInputDevicesIfNeeded()
    {
        if (currentMouse == null) currentMouse = Mouse.current;
        if (currentKeyboard == null) currentKeyboard = Keyboard.current;
    }
    
    /// <summary>
    /// Централизованная обработка всех типов ввода
    /// </summary>
    private void HandleInput()
    {
        HandleMouseInput();
        HandleKeyboardInput();
    }
    
    /// <summary>
    /// Обработка ввода мыши для размещения и отмены строительства
    /// </summary>
    private void HandleMouseInput()
    {
        if (currentMouse == null) return;
        
        // Левая кнопка мыши - размещение здания
        if (currentMouse.leftButton.wasPressedThisFrame)
        {
            TryPlaceBuilding();
        }
        
        // Правая кнопка мыши - отмена строительства
        if (currentMouse.rightButton.wasPressedThisFrame)
        {
            if (isBuildMode)
            {
                CancelBuilding();
            }
        }
    }
    
    /// <summary>
    /// Обработка ввода клавиатуры для горячих клавиш
    /// </summary>
    private void HandleKeyboardInput()
    {
        if (currentKeyboard == null) return;
        
        // Горячие клавиши для быстрого выбора зданий (1-9)
        for (int i = 0; i < buildings.Length && i < MAX_HOTKEY_BUILDINGS; i++)
        {
            if (currentKeyboard[(Key)(Key.Digit1 + i)].wasPressedThisFrame)
            {
                StartBuilding(i);
                break; // Выходим после первого совпадения
            }
        }
        
        // Escape для отмены строительства
        if (currentKeyboard.escapeKey.wasPressedThisFrame && isBuildMode)
        {
            CancelBuilding();
        }
    }
    
    #endregion
    
    #region Building System Core
    
    /// <summary>
    /// Попытка разместить здание (с проверками валидности)
    /// </summary>
    private void TryPlaceBuilding()
    {
        if (isBuildMode && canPlace && currentPreview != null && currentPreview.activeInHierarchy)
        {
            PlaceBuilding();
        }
        else if (debugMode)
        {
            Debug.Log($"Building System: Невозможно разместить здание. BuildMode: {isBuildMode}, CanPlace: {canPlace}, Preview Active: {currentPreview?.activeInHierarchy}");
        }
    }
    
    /// <summary>
    /// Обновляет превью здания: позицию и валидность размещения
    /// </summary>
    private void UpdateBuildingPreview()
    {
        UpdatePreviewPosition();
        CheckPlacementValidity();
    }
    
    /// <summary>
    /// Начинает процесс строительства выбранного здания
    /// </summary>
    /// <param name="buildingIndex">Индекс здания в массиве buildings</param>
    public void StartBuilding(int buildingIndex)
    {
        // Валидируем индекс здания
        if (!IsValidBuildingIndex(buildingIndex))
        {
            Debug.LogWarning($"Building System: Неверный индекс здания: {buildingIndex}");
            return;
        }
        
        // Отменяем текущее строительство если активно
        if (isBuildMode)
        {
            CancelBuilding();
        }
        
        // Инициализируем новое строительство
        selectedBuildingIndex = buildingIndex;
        isBuildMode = true;
        CreatePreview();
        
        if (debugMode)
        {
            Debug.Log($"Building System: Выбрано здание: {buildings[buildingIndex].buildingName}");
        }
    }
    
    /// <summary>
    /// Создает превью выбранного здания с защитой от искажений трансформации
    /// </summary>
    private void CreatePreview()
    {
        if (!IsValidBuildingIndex(selectedBuildingIndex)) return;
        
        // Создаем превью объект
        currentPreview = Instantiate(buildings[selectedBuildingIndex].previewPrefab);
        
        // КРИТИЧЕСКИ ВАЖНО: Сохраняем и устанавливаем правильные значения трансформации
        originalPreviewScale = Vector3.one;
        originalPreviewRotation = Quaternion.identity;
        
        // Принудительно устанавливаем правильные значения трансформации
        currentPreview.transform.localScale = originalPreviewScale;
        currentPreview.transform.rotation = originalPreviewRotation;
        
        // Получаем компоненты
        previewRenderer = currentPreview.GetComponent<Renderer>();
        currentPreviewGrid = currentPreview.GetComponent<BuildingGrid>();
        
        // Валидируем компоненты
        if (previewRenderer == null)
        {
            Debug.LogError($"Building System: У превью здания '{buildings[selectedBuildingIndex].buildingName}' отсутствует Renderer!");
        }
        
        if (currentPreviewGrid == null)
        {
            Debug.LogError($"Building System: У превью здания '{buildings[selectedBuildingIndex].buildingName}' отсутствует компонент BuildingGrid!");
        }
        
        // Отключаем коллайдер у превью для избежания самопересечений
        DisablePreviewCollider();
        
        // Устанавливаем начальную позицию вне зоны видимости
        currentPreview.transform.position = new Vector3(0, -1000, 0);
        currentPreview.SetActive(false);
        
        if (debugMode)
        {
            Debug.Log($"Building System: Создано превью для здания '{buildings[selectedBuildingIndex].buildingName}'");
        }
    }
    
    /// <summary>
    /// Отключает коллайдер у превью объекта
    /// </summary>
    private void DisablePreviewCollider()
    {
        Collider previewCollider = currentPreview.GetComponent<Collider>();
        if (previewCollider != null)
        {
            previewCollider.enabled = false;
        }
    }
    
    /// <summary>
    /// Обновляет позицию превью здания по позиции мыши с защитой от искажений
    /// </summary>
    private void UpdatePreviewPosition()
    {
        if (currentMouse == null || mainCamera == null || currentPreviewGrid == null) return;
        
        // Получаем текущую позицию мыши
        Vector2 mousePosition = currentMouse.position.ReadValue();
        
        // Оптимизация: обновляем позицию только если мышь значительно сдвинулась
        if (Vector2.Distance(mousePosition, cachedMousePosition) < 2f) return;
        
        cachedMousePosition = mousePosition;
        cachedRay = mainCamera.ScreenPointToRay(cachedMousePosition);
        
        // Ищем точку пересечения с поверхностью
        if (Physics.Raycast(cachedRay, out RaycastHit hit, maxRaycastDistance, groundLayer))
        {
            // Сохраняем трансформацию перед изменением позиции
            Vector3 savedScale = currentPreview.transform.localScale;
            Quaternion savedRotation = currentPreview.transform.rotation;
            
            // Применяем привязку к сетке используя BuildingGrid
            Vector3 snappedPosition = currentPreviewGrid.SnapToGrid(hit.point);
            
            // Устанавливаем новую позицию
            currentPreview.transform.position = snappedPosition;
            
            // ВАЖНО: Восстанавливаем правильные значения трансформации
            currentPreview.transform.localScale = originalPreviewScale;
            currentPreview.transform.rotation = originalPreviewRotation;
            
            // Сохраняем последнюю валидную позицию
            lastValidPosition = snappedPosition;
            
            // Активируем превью
            currentPreview.SetActive(true);
            
            if (debugMode && Vector3.Distance(hit.point, snappedPosition) > 0.1f)
            {
                Debug.Log($"Building System: Позиция привязана к сетке. Исходная: {hit.point}, Привязанная: {snappedPosition}");
            }
        }
        else
        {
            // Скрываем превью если нет валидной поверхности
            currentPreview.SetActive(false);
        }
    }
    
    /// <summary>
    /// Проверяет возможность размещения здания используя сетки BuildingGrid
    /// </summary>
    private void CheckPlacementValidity()
    {
        if (!currentPreview.activeInHierarchy || currentPreviewGrid == null) return;
        
        bool wasCanPlace = canPlace;
        canPlace = true; // Начинаем с предположения что можно разместить
        
        Vector3 previewPosition = currentPreview.transform.position;
        
        // Проверяем пересечение с каждым размещенным зданием
        foreach (BuildingGrid placedBuilding in placedBuildings)
        {
            if (placedBuilding == null) continue; // Пропускаем удаленные здания
            
            if (CheckGridOverlap(previewPosition, currentPreviewGrid, placedBuilding.transform.position, placedBuilding))
            {
                canPlace = false;
                if (debugMode)
                {
                    Debug.Log($"Building System: Пересечение с зданием на позиции {placedBuilding.transform.position}");
                }
                break;
            }
        }
        
        if (canPlace) // Проверяем только если еще можно размещать
        {
            canPlace = CheckCollisionWithOtherObjects(previewPosition);
        }
        
        // Обновляем материал только при изменении состояния (оптимизация)
        if (wasCanPlace != canPlace)
        {
            UpdatePreviewMaterial();
        }
    }
    
    /// <summary>
    /// Проверяет пересечение здания с любыми другими объектами с настраиваемыми параметрами
    /// </summary>
    private bool CheckCollisionWithOtherObjects(Vector3 position)
    {
        if (currentPreviewGrid == null) return true;
    
        // Получаем размеры здания
        Vector2Int gridSize = currentPreviewGrid.GridSize;
        float cellSize = currentPreviewGrid.CellSize;
    
        // Рассчитываем размеры для проверки
        Vector3 boxSize = new Vector3(
            gridSize.x * cellSize * 0.95f, // Слегка уменьшаем для избежания ложных срабатываний
            collisionCheckHeight,
            gridSize.y * cellSize * 0.95f
        );
    
        // Создаем маску слоев для проверки
        LayerMask checkLayers = ~groundLayer; // Исключаем землю
        checkLayers &= ~ignoreLayers; // Исключаем игнорируемые слои
    
        // Исключаем слой превью
        int previewLayer = currentPreview.layer;
        if (previewLayer != 0)
        {
            checkLayers &= ~(1 << previewLayer);
        }
    
        // Выполняем проверку
        Collider[] overlappingColliders = Physics.OverlapBox(
            position + Vector3.up * (collisionCheckHeight * 0.5f),
            boxSize * 0.5f,
            Quaternion.identity,
            checkLayers
        );
    
        // Анализируем результаты
        foreach (Collider collider in overlappingColliders)
        {
            // Пропускаем триггеры если настроено
            if (ignoreTriggers && collider.isTrigger) continue;
        
            // Пропускаем превью
            if (collider.gameObject == currentPreview) continue;
        
            // Пропускаем здания (они проверяются отдельно)
            if (collider.GetComponent<BuildingGrid>() != null) continue;
        
            // Можно добавить дополнительные проверки по тегам
            // if (collider.CompareTag("IgnoreBuilding")) continue;
        
            if (debugMode)
            {
                Debug.Log($"Building System: Коллизия с '{collider.name}' (слой: {LayerMask.LayerToName(collider.gameObject.layer)})");
            }
        
            return false;
        }
    
        return true;
    }

    
    /// <summary>
    /// Проверяет пересечение двух сеток зданий на основе их GridSize и CellSize
    /// </summary>
    /// <param name="pos1">Позиция первого здания</param>
    /// <param name="grid1">Сетка первого здания</param>
    /// <param name="pos2">Позиция второго здания</param>
    /// <param name="grid2">Сетка второго здания</param>
    /// <returns>true если сетки пересекаются</returns>
    private bool CheckGridOverlap(Vector3 pos1, BuildingGrid grid1, Vector3 pos2, BuildingGrid grid2)
    {
        if (grid1 == null || grid2 == null) return false;
        
        // Получаем размеры первого здания из BuildingGrid
        Vector2Int gridSize1 = grid1.GridSize;
        float cellSize1 = grid1.CellSize;
        float halfWidth1 = (gridSize1.x * cellSize1) * 0.5f;
        float halfDepth1 = (gridSize1.y * cellSize1) * 0.5f;
        
        // Получаем размеры второго здания из BuildingGrid
        Vector2Int gridSize2 = grid2.GridSize;
        float cellSize2 = grid2.CellSize;
        float halfWidth2 = (gridSize2.x * cellSize2) * 0.5f;
        float halfDepth2 = (gridSize2.y * cellSize2) * 0.5f;
        
        // Рассчитываем границы первого здания
        float min1X = pos1.x - halfWidth1;
        float max1X = pos1.x + halfWidth1;
        float min1Z = pos1.z - halfDepth1;
        float max1Z = pos1.z + halfDepth1;
        
        // Рассчитываем границы второго здания
        float min2X = pos2.x - halfWidth2;
        float max2X = pos2.x + halfWidth2;
        float min2Z = pos2.z - halfDepth2;
        float max2Z = pos2.z + halfDepth2;
        
        // Проверяем пересечение по осям X и Z
        bool overlapX = min1X < max2X && max1X > min2X;
        bool overlapZ = min1Z < max2Z && max1Z > min2Z;
        
        return overlapX && overlapZ;
    }
    
    /// <summary>
    /// Обновляет материал превью в зависимости от валидности размещения
    /// </summary>
    private void UpdatePreviewMaterial()
    {
        if (previewRenderer != null)
        {
            previewRenderer.material = canPlace ? validMaterial : invalidMaterial;
        }
    }
    
    /// <summary>
    /// Размещает здание в текущей позиции превью
    /// </summary>
    private void PlaceBuilding()
    {
        if (!IsValidBuildingIndex(selectedBuildingIndex)) return;
        
        // Сохраняем трансформ превью
        Vector3 position = currentPreview.transform.position;
        Quaternion rotation = originalPreviewRotation; // Используем оригинальную ротацию
        
        // Создаем реальное здание
        GameObject newBuilding = Instantiate(buildings[selectedBuildingIndex].buildingPrefab, position, rotation);
        
        // ВАЖНО: Устанавливаем правильный масштаб для нового здания
        newBuilding.transform.localScale = Vector3.one;
        
        // Получаем BuildingGrid нового здания и добавляем в список
        BuildingGrid newBuildingGrid = newBuilding.GetComponent<BuildingGrid>();
        if (newBuildingGrid != null)
        {
            placedBuildings.Add(newBuildingGrid);
        }
        else
        {
            Debug.LogError($"Building System: У размещенного здания '{buildings[selectedBuildingIndex].buildingName}' отсутствует компонент BuildingGrid!");
        }
        
        if (debugMode)
        {
            Debug.Log($"Building System: Построено здание '{buildings[selectedBuildingIndex].buildingName}' в позиции {position}");
        }
        
        // Подготавливаем следующее превью для непрерывного строительства
        PrepareNextPreview();
    }
    
    /// <summary>
    /// Подготавливает новое превью для продолжения строительства
    /// </summary>
    private void PrepareNextPreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
        }
        CreatePreview();
    }
    
    /// <summary>
    /// Отменяет текущее строительство
    /// </summary>
    private void CancelBuilding()
    {
        // Очищаем превью
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
        
        // Сбрасываем состояние
        ResetBuildingState();
        
        if (debugMode)
        {
            Debug.Log("Building System: Строительство отменено");
        }
    }
    
    /// <summary>
    /// Сбрасывает состояние системы строительства
    /// </summary>
    private void ResetBuildingState()
    {
        isBuildMode = false;
        canPlace = false;
        selectedBuildingIndex = -1;
        previewRenderer = null;
        currentPreviewGrid = null;
        lastValidPosition = Vector3.zero;
    }
    
    #endregion
    
    #region Validation & Utility
    
    /// <summary>
    /// Проверяет валидность индекса здания
    /// </summary>
    /// <param name="index">Индекс для проверки</param>
    /// <returns>true если индекс валиден</returns>
    private bool IsValidBuildingIndex(int index)
    {
        return index >= 0 && index < buildings.Length && buildings[index] != null;
    }
    
    /// <summary>
    /// Находит все существующие здания в сцене при запуске
    /// </summary>
    private void FindExistingBuildings()
    {
        BuildingGrid[] existingGrids = FindObjectsOfType<BuildingGrid>();
        
        foreach (BuildingGrid grid in existingGrids)
        {
            // Добавляем только активные здания (исключаем превью и неактивные объекты)
            if (grid.gameObject.activeInHierarchy && 
                !grid.name.ToLower().Contains("preview") && 
                !grid.name.ToLower().Contains("ghost"))
            {
                placedBuildings.Add(grid);
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"Building System: Найдено {placedBuildings.Count} существующих зданий");
        }
    }
    
    /// <summary>
    /// Очищает список от уничтоженных зданий
    /// </summary>
    public void CleanupDestroyedBuildings()
    {
        int originalCount = placedBuildings.Count;
        placedBuildings.RemoveAll(building => building == null);
        
        if (debugMode && placedBuildings.Count != originalCount)
        {
            Debug.Log($"Building System: Удалено {originalCount - placedBuildings.Count} уничтоженных зданий из списка");
        }
    }
    
    /// <summary>
    /// Валидирует данные зданий при запуске
    /// </summary>
    private void ValidateBuildingData()
    {
        if (buildings == null || buildings.Length == 0)
        {
            Debug.LogWarning("Building System: Массив зданий пуст!");
            return;
        }
        
        for (int i = 0; i < buildings.Length; i++)
        {
            if (buildings[i] == null)
            {
                Debug.LogWarning($"Building System: Здание #{i} является null!");
                continue;
            }
            
            ValidateBuildingPrefab(buildings[i].buildingPrefab, buildings[i].buildingName, "buildingPrefab");
            ValidateBuildingPrefab(buildings[i].previewPrefab, buildings[i].buildingName, "previewPrefab");
        }
    }
    
    /// <summary>
    /// Валидирует отдельный префаб здания
    /// </summary>
    /// <param name="prefab">Префаб для валидации</param>
    /// <param name="buildingName">Название здания</param>
    /// <param name="prefabType">Тип префаба (для логирования)</param>
    private void ValidateBuildingPrefab(GameObject prefab, string buildingName, string prefabType)
    {
        if (prefab == null)
        {
            Debug.LogError($"Building System: У здания '{buildingName}' отсутствует {prefabType}!");
            return;
        }
        
        BuildingGrid grid = prefab.GetComponent<BuildingGrid>();
        if (grid == null)
        {
            Debug.LogError($"Building System: У {prefabType} здания '{buildingName}' отсутствует компонент BuildingGrid!");
        }
    }
    
    #endregion
    
    #region Public API для UI кнопок
    
    /// <summary>
    /// Публичные методы для подключения к UI кнопкам (0-9)
    /// Используются в OnClick событиях кнопок интерфейса
    /// </summary>
    public void StartBuilding0() { StartBuilding(0); }
    public void StartBuilding1() { StartBuilding(1); }
    public void StartBuilding2() { StartBuilding(2); }
    public void StartBuilding3() { StartBuilding(3); }
    public void StartBuilding4() { StartBuilding(4); }
    public void StartBuilding5() { StartBuilding(5); }
    public void StartBuilding6() { StartBuilding(6); }
    public void StartBuilding7() { StartBuilding(7); }
    public void StartBuilding8() { StartBuilding(8); }
    public void StartBuilding9() { StartBuilding(9); }
    
    /// <summary>
    /// Получает название текущего выбранного здания
    /// </summary>
    /// <returns>Название здания или "Нет выбора"</returns>
    public string GetCurrentBuildingName()
    {
        return IsValidBuildingIndex(selectedBuildingIndex) 
            ? buildings[selectedBuildingIndex].buildingName 
            : "Нет выбора";
    }
    
    /// <summary>
    /// Проверяет активен ли режим строительства
    /// </summary>
    /// <returns>true если режим строительства активен</returns>
    public bool IsBuildingMode() => isBuildMode;
    
    /// <summary>
    /// Проверяет можно ли разместить здание в текущей позиции
    /// </summary>
    /// <returns>true если размещение возможно</returns>
    public bool CanPlaceBuilding() => canPlace;
    
    /// <summary>
    /// Получает количество доступных типов зданий
    /// </summary>
    /// <returns>Количество зданий</returns>
    public int GetBuildingCount() => buildings?.Length ?? 0;
    
    /// <summary>
    /// Отменяет строительство (публичный метод для UI)
    /// </summary>
    public void CancelBuildingFromUI() => CancelBuilding();
    
    /// <summary>
    /// Получает информацию о текущем здании для отладки
    /// </summary>
    /// <returns>Строка с информацией о здании</returns>
    public string GetCurrentBuildingInfo()
    {
        if (currentPreviewGrid != null)
        {
            return $"Здание: {GetCurrentBuildingName()}, Сетка: {currentPreviewGrid.GridSize}, Клетка: {currentPreviewGrid.CellSize}";
        }
        return "Нет активного здания";
    }
    
    /// <summary>
    /// Получает количество размещенных зданий
    /// </summary>
    /// <returns>Количество размещенных зданий</returns>
    public int GetPlacedBuildingsCount() => placedBuildings.Count;
    
    #endregion
}
}