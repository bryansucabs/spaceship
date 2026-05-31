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
        transform.rotation = Quaternion.Euler(65f, 45f, 0f);

        // Candado de seguridad para que la cámara no nazca atrapada en el suelo
        if (transform.position.y <= 0.1f)
        {
            transform.position = new Vector3(transform.position.x, 80f, transform.position.z);
        }
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
        // 2. LÓGICA DE ARRASTRE / PAN (1 Dedo en Celular o Clic Izquierdo en PC)
        // ========================================================
        bool isClicking = hasMouse && Mouse.current.leftButton.isPressed;
        bool isTouching = dedosActivos == 1;

        // Detectar el instante en que pones el primer dedo o haces clic
        bool empezoToque = (hasMouse && Mouse.current.leftButton.wasPressedThisFrame) ||
                           (isTouching && Touch.activeTouches[0].phase == TouchPhase.Began);
        
        // Detectar cuando levantas el dedo o sueltas el mouse
        bool terminoToque = (hasMouse && Mouse.current.leftButton.wasReleasedThisFrame) ||
                            (isTouching && Touch.activeTouches[0].phase == TouchPhase.Ended) ||
                            dedosActivos == 0;

        if (empezoToque)
        {
            Vector2 screenPos = isTouching ? Touch.activeTouches[0].screenPosition : Mouse.current.position.ReadValue();
            
            if (TryObtenerPuntoEnPlano(screenPos, out Vector3 puntoEnMundo))
            {
                isDragging = true;
                puntoInicialMundo = puntoEnMundo;
                posicionInicialCamara = transform.position;
            }
        }

        // Mientras arrastras el dedo por la pantalla horizontal de la tablet
        if (isDragging && (isClicking || isTouching))
        {
            Vector2 screenPos = isTouching ? Touch.activeTouches[0].screenPosition : Mouse.current.position.ReadValue();
            
            if (TryObtenerPuntoEnPlano(screenPos, out Vector3 puntoActualMundo))
            {
                Vector3 deltaMundo = puntoInicialMundo - puntoActualMundo;
                Vector3 nuevaPos = posicionInicialCamara + deltaMundo;

                // Clampear para que el Overlord no arrastre la pantalla hacia el vacío oscuro
                nuevaPos.x = Mathf.Clamp(nuevaPos.x, minX, maxX);
                nuevaPos.z = Mathf.Clamp(nuevaPos.z, minZ, maxZ);
                transform.position = nuevaPos;
            }
        }

        if (terminoToque)
        {
            isDragging = false;
        }
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