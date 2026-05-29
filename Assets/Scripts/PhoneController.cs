using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class PhoneControllerSimple : MonoBehaviour
{
    public float minX = -500f, maxX = 500f, minZ = -500f, maxZ = 500f;
    public float zoomMin = 8f, zoomMax = 300f;
    public float sensibilidadZoomTouch = 0.04f;
    public float sensibilidadZoomMouse = 0.1f;

    private Camera cam;
    private Vector3 puntoInicialMundo;
    private Vector3 posicionInicialCamara;
    private bool dragging = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        transform.rotation = Quaternion.Euler(65f, 45f, 0f);
    }

    void Update()
    {
        // PAN con Mouse o Touch
        if (Mouse.current.leftButton.wasPressedThisFrame || 
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame))
        {
            dragging = true;
            Vector2 pos = ObtenerPosicionToque();
            puntoInicialMundo = ObtenerPuntoEnPlano(pos);
            posicionInicialCamara = transform.position;
        }
        
        if (dragging && (Mouse.current.leftButton.isPressed || 
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)))
        {
            Vector2 posActual = ObtenerPosicionToque();
            Vector3 puntoActualMundo = ObtenerPuntoEnPlano(posActual);
            Vector3 delta = puntoInicialMundo - puntoActualMundo;
            
            Vector3 nuevaPos = posicionInicialCamara + delta;
            nuevaPos.x = Mathf.Clamp(nuevaPos.x, minX, maxX);
            nuevaPos.z = Mathf.Clamp(nuevaPos.z, minZ, maxZ);
            transform.position = nuevaPos;
        }
        
        if (Mouse.current.leftButton.wasReleasedThisFrame || 
            (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
        {
            dragging = false;
        }
        
        // ZOOM
        float zoom = Mouse.current.scroll.ReadValue().y * sensibilidadZoomMouse;
        if (Touchscreen.current != null && Touchscreen.current.touches.Count == 2)
        {
            // Zoom táctil simple
            var t0 = Touchscreen.current.touches[0];
            var t1 = Touchscreen.current.touches[1];
            if (t0.press.isPressed && t1.press.isPressed)
            {
                float distanciaActual = Vector2.Distance(t0.position.ReadValue(), t1.position.ReadValue());
                float distanciaPrevia = Vector2.Distance(t0.position.ReadValue() - t0.delta.ReadValue(), 
                                                          t1.position.ReadValue() - t1.delta.ReadValue());
                zoom = (distanciaPrevia - distanciaActual) * sensibilidadZoomTouch;
            }
        }
        
        cam.orthographicSize -= zoom;
        cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, zoomMin, zoomMax);
    }
    
    private Vector2 ObtenerPosicionToque()
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            return Touchscreen.current.primaryTouch.position.ReadValue();
        return Mouse.current.position.ReadValue();
    }
    
    private Vector3 ObtenerPuntoEnPlano(Vector2 screenPos)
    {
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float distance))
            return ray.GetPoint(distance);
        return Vector3.zero;
    }
}