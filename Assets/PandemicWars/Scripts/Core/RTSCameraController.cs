using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class RTSCameraController : MonoBehaviour
{
    [System.Serializable]
    public struct MovementSettings
    {
        [Tooltip("Скорость перетаскивания камеры")]
        public float dragSpeed;
        
        [Tooltip("Плавность движения")]
        public float smoothSpeed;
        
        [Tooltip("Активировать edge scrolling?")]
        public bool edgeScrollingEnabled;
        
        [Tooltip("Скорость edge scrolling")]
        public float edgeScrollSpeed;
        
        [Tooltip("Размер краевой зоны (0-1)")]
        public float edgeBorderSize;
    }

    [System.Serializable]
    public struct ZoomSettings
    {
        [Tooltip("Скорость зума")]
        public float speed;
        
        [Tooltip("Минимальная высота")]
        public float minHeight;
        
        [Tooltip("Максимальная высота")]
        public float maxHeight;
        
        [Tooltip("Угол при приближении")]
        public float minAngle;
        
        [Tooltip("Угол при отдалении")]
        public float maxAngle;
    }

    [System.Serializable]
    public struct KeyboardSettings
    {
        public float baseSpeed;
        public float maxSpeed;
        public float acceleration;
        public bool enabled;
    }

    [System.Serializable]
    public struct MapBounds
    {
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;
    }

    [Header("Настройки")]
    [SerializeField] private MovementSettings movement = new MovementSettings
    {
        dragSpeed = 25f,
        smoothSpeed = 10f,
        edgeScrollingEnabled = false,
        edgeScrollSpeed = 50f,
        edgeBorderSize = 0.05f
    };

    [SerializeField] private ZoomSettings zoom = new ZoomSettings
    {
        speed = 25f,
        minHeight = 10f,
        maxHeight = 100f,
        minAngle = 70f,
        maxAngle = 30f
    };

    [SerializeField] private KeyboardSettings keyboard = new KeyboardSettings
    {
        baseSpeed = 20f,
        maxSpeed = 50f,
        acceleration = 10f,
        enabled = true
    };

    [SerializeField] private MapBounds bounds = new MapBounds
    {
        minX = -100f,
        maxX = 100f,
        minZ = -100f,
        maxZ = 100f
    };

    // Приватные переменные
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float currentZoom;
    private float currentKeyboardSpeed;
    
    private bool isDragging;
    private Vector2 dragStartPos;
    
    private Mouse mouse;
    private Keyboard keyboardInput;
    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        mouse = Mouse.current;
        keyboardInput = Keyboard.current;

        // Инициализация текущих значений
        currentZoom = Mathf.InverseLerp(zoom.minHeight, zoom.maxHeight, transform.position.y);
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    private void Update()
    {
        if (mouse == null || keyboardInput == null) return;

        HandleInput();
    }

    private void LateUpdate()
    {
        ApplyMovement();
    }

    private void HandleInput()
    {
        HandleDrag();
        HandleZoom();
        
        if (movement.edgeScrollingEnabled)
            HandleEdgeScrolling();
            
        if (keyboard.enabled)
            HandleKeyboard();
    }

    /// <summary> Обработка перетаскивания камеры </summary>
    private void HandleDrag()
    {
        if (mouse.middleButton.wasPressedThisFrame)
        {
            isDragging = true;
            dragStartPos = mouse.position.ReadValue();
            return;
        }

        if (mouse.middleButton.wasReleasedThisFrame)
        {
            isDragging = false;
            return;
        }

        if (!isDragging) return;

        Vector2 currentPos = mouse.position.ReadValue();
        Vector2 delta = (dragStartPos - currentPos) * (movement.dragSpeed * Time.deltaTime);
        
        targetPosition += new Vector3(delta.x, 0, delta.y);
        dragStartPos = currentPos;
    }

    /// <summary> Обработка зума камеры </summary>
    private void HandleZoom()
    {
        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0)) return;

        currentZoom = Mathf.Clamp01(currentZoom - scroll * zoom.speed * 0.01f);
        
        float targetHeight = Mathf.Lerp(zoom.minHeight, zoom.maxHeight, currentZoom);
        float targetAngle = Mathf.Lerp(zoom.maxAngle, zoom.minAngle, currentZoom);
        
        targetPosition.y = targetHeight;
        targetRotation = Quaternion.Euler(targetAngle, transform.eulerAngles.y, 0);
    }

    /// <summary> Edge scrolling (опционально) </summary>
    private void HandleEdgeScrolling()
    {
        if (isDragging) return;

        Vector2 mousePos = mouse.position.ReadValue();
        Vector2 viewportPos = new Vector2(
            mousePos.x / Screen.width,
            mousePos.y / Screen.height
        );

        Vector3 move = Vector3.zero;
        
        if (viewportPos.x < movement.edgeBorderSize)
            move.x = -1;
        else if (viewportPos.x > 1 - movement.edgeBorderSize)
            move.x = 1;
            
        if (viewportPos.y < movement.edgeBorderSize)
            move.z = -1;
        else if (viewportPos.y > 1 - movement.edgeBorderSize)
            move.z = 1;

        if (move != Vector3.zero)
        {
            targetPosition += move.normalized * (movement.edgeScrollSpeed * Time.deltaTime);
        }
    }

    /// <summary> Управление с клавиатуры </summary>
    private void HandleKeyboard()
    {
        Vector3 input = Vector3.zero;
        
        if (keyboardInput.leftArrowKey.isPressed) input.x -= 1;
        if (keyboardInput.rightArrowKey.isPressed) input.x += 1;
        if (keyboardInput.downArrowKey.isPressed) input.z -= 1;
        if (keyboardInput.upArrowKey.isPressed) input.z += 1;

        if (input != Vector3.zero)
        {
            currentKeyboardSpeed = Mathf.Min(
                currentKeyboardSpeed + keyboard.acceleration * Time.deltaTime,
                keyboard.maxSpeed
            );
            
            targetPosition += input.normalized * currentKeyboardSpeed * Time.deltaTime;
        }
        else
        {
            currentKeyboardSpeed = keyboard.baseSpeed;
        }
    }

    /// <summary> Плавное применение движения </summary>
    private void ApplyMovement()
    {
        // Ограничение границ
        targetPosition.x = Mathf.Clamp(targetPosition.x, bounds.minX, bounds.maxX);
        targetPosition.z = Mathf.Clamp(targetPosition.z, bounds.minZ, bounds.maxZ);

        // Плавное перемещение
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            Time.deltaTime * movement.smoothSpeed
        );

        // Плавный поворот
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * movement.smoothSpeed
        );
    }
}