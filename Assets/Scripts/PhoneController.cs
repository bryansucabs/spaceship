using UnityEngine;
using UnityEngine.InputSystem;
// IMPORTANTE: Asegúrate de tener estas dos líneas de librerías táctiles arriba
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

[RequireComponent(typeof(Camera))]
public class PhoneController: MonoBehaviour
{
    [Header("Límites del Laberinto")]
    public float minX = -500f;
    public float maxX = 500f;
    public float minZ = -500f;
    public float maxZ = 500f;

    [Header("Configuración del Zoom")]
    public float zoomMin = 8f;
    public float zoomMax = 300f;
    public float sensibilidadZoomTouch = 0.04f;
    public float sensibilidadZoomMouse = 0.1f;

    private Camera cam;
    private Vector3 puntoInicialMundo;
    private Vector3 posicionInicialCamara;
    private bool isDragging = false;
    private bool isZooming = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        transform.rotation = Quaternion.Euler(90f, 0f, 0f); // vista cenital directa hacia abajo

        if (transform.position.y <= 0.1f)
            transform.position = new Vector3(transform.position.x, 80f, transform.position.z);
    }

    // ========================================================
    // ¡ESTO ES LO QUE TE FALTABA! ACTIVACIÓN DEL INPUT TÁCTIL
    // ========================================================
    void OnEnable()
    {
        // Encendemos el soporte de lectura táctil mejorada del New Input System
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        // Lo apagamos al salir de la escena por seguridad de rendimiento
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        bool hasMouse = Mouse.current != null;
        
        // Ahora que EnhancedTouch está activo, esto contará los dedos REALES en el cristal
        int dedosActivos = Touch.activeTouches.Count;

        // ========================================================
        // 1. LÓGICA DE ZOOM (2 Dedos en Móvil O Rueda en PC)
        // ========================================================
        float deltaZoom = 0f;

        if (dedosActivos >= 2)
        {
            isZooming = true;
            isDragging = false; // Cancelamos el arrastre si el Overlord empieza a pellizcar la pantalla

            var t0 = Touch.activeTouches[0];
            var t1 = Touch.activeTouches[1];

            float distActual = Vector2.Distance(t0.screenPosition, t1.screenPosition);
            float distPrevia = Vector2.Distance(
                t0.screenPosition - t0.delta, 
                t1.screenPosition - t1.delta
            );

            deltaZoom = (distActual - distPrevia) * sensibilidadZoomTouch;
        }
        else
        {
            isZooming = false;
            // Si no hay dedos y estás probando en PC con la rueda del mouse
            if (hasMouse && Mathf.Abs(Mouse.current.scroll.ReadValue().y) > 0.01f)
            {
                deltaZoom = Mouse.current.scroll.ReadValue().y * sensibilidadZoomMouse;
            }
        }

        if (Mathf.Abs(deltaZoom) > 0.001f)
        {
            cam.orthographicSize -= deltaZoom;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, zoomMin, zoomMax);
        }

        // Si el usuario está haciendo zoom, congelamos el mapa para que no se mueva chueco
        if (isZooming) return;

        // ========================================================
        // 2a. PAN CON TECLADO (flechas) — PC
        // ========================================================
        if (Keyboard.current != null)
        {
            float panSpeed = cam.orthographicSize * 1.8f * Time.deltaTime;
            Vector3 panDir = Vector3.zero;
            if (Keyboard.current.upArrowKey.isPressed)    panDir.z += 1f;
            if (Keyboard.current.downArrowKey.isPressed)  panDir.z -= 1f;
            if (Keyboard.current.leftArrowKey.isPressed)  panDir.x -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed) panDir.x += 1f;

            if (panDir != Vector3.zero)
            {
                Vector3 nuevaPos = transform.position + panDir * panSpeed;
                nuevaPos.x = Mathf.Clamp(nuevaPos.x, minX, maxX);
                nuevaPos.z = Mathf.Clamp(nuevaPos.z, minZ, maxZ);
                transform.position = nuevaPos;
            }
        }

        // ========================================================
        // 2b. ARRASTRE / PAN (1 Dedo en Tablet  o  Click Derecho en PC)
        // ========================================================
        bool isClicking = hasMouse && Mouse.current.rightButton.isPressed; // click derecho para pan en PC
        bool isTouching = dedosActivos == 1;

        bool empezoToque = (hasMouse && Mouse.current.rightButton.wasPressedThisFrame) ||
                           (isTouching && Touch.activeTouches[0].phase == TouchPhase.Began);

        bool terminoToque = (hasMouse && Mouse.current.rightButton.wasReleasedThisFrame) ||
                            (isTouching && Touch.activeTouches[0].phase == TouchPhase.Ended) ||
                            dedosActivos == 0;

        if (empezoToque)
        {
            // En tablet (táctil): no arrastrar si el dedo está sobre un botón UI
            if (isTouching && UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Vector2 screenPos = isTouching ? Touch.activeTouches[0].screenPosition : Mouse.current.position.ReadValue();
            if (TryObtenerPuntoEnPlano(screenPos, out Vector3 puntoEnMundo))
            {
                isDragging = true;
                puntoInicialMundo = puntoEnMundo;
                posicionInicialCamara = transform.position;
            }
        }

        if (isDragging && (isClicking || isTouching))
        {
            Vector2 screenPos = isTouching ? Touch.activeTouches[0].screenPosition : Mouse.current.position.ReadValue();
            if (TryObtenerPuntoEnPlano(screenPos, out Vector3 puntoActualMundo))
            {
                Vector3 deltaMundo = puntoInicialMundo - puntoActualMundo;
                Vector3 nuevaPos = posicionInicialCamara + deltaMundo;
                nuevaPos.x = Mathf.Clamp(nuevaPos.x, minX, maxX);
                nuevaPos.z = Mathf.Clamp(nuevaPos.z, minZ, maxZ);
                transform.position = nuevaPos;
            }
        }

        if (terminoToque) isDragging = false;
    }

    // Raycast matemático seguro hacia el piso del laberinto (Y = 0)
    private bool TryObtenerPuntoEnPlano(Vector2 screenPos, out Vector3 puntoEnMundo)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        Plane planoPiso = new Plane(Vector3.up, Vector3.zero);

        if (planoPiso.Raycast(ray, out float distance))
        {
            puntoEnMundo = ray.GetPoint(distance);
            return true;
        }

        puntoEnMundo = Vector3.zero;
        return false;
    }
}