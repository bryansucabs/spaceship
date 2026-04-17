using UnityEngine;
using UnityEngine.InputSystem; 

// ShipController.cs
// Controla el movimiento de la nave del jugador dentro del tunel.
// AHORA: La nave SOLO avanza cuando recibe aceleración del script de Python.
[RequireComponent(typeof(Rigidbody))] 
public class ShipController : MonoBehaviour
{

    // ==========================================
    // --- ¡NUEVO! CONFIGURACIÓN DE VISIÓN ---
    // ==========================================
    [Header("Control por Visión (Python)")]
    [Tooltip("Arrastra aquí el objeto que tiene el script UDPReceiver")]
    public UDPReceiver receptorUDP; 
    
    [Tooltip("Ajusta este valor si la nave va muy lento o muy rápido")]
    public float multiplicadorVelocidadZ = 1.0f; 
    // ==========================================

    // Velocidad de movimiento lateral (izquierda, derecha, arriba, abajo)
    const float LATERAL_SPEED = 40f;

    // Angulo maximo de inclinacion de las alas al moverse horizontalmente
    const float ROLL_AMOUNT   = 28f;
    const float ROLL_SPEED    = 6f;

    // Angulo maximo de inclinacion de la nariz al moverse verticalmente
    const float PITCH_AMOUNT  = 20f;
    const float PITCH_SPEED   = 6f;

    [HideInInspector] public float xLimit = 47.5f; 
    [HideInInspector] public float yLimit = 53.0f; 

    private Rigidbody _rb;
    private float _roll;  
    private float _pitch; 

    void Awake()
    {
        xLimit = 49.32f; 
        yLimit = 53.26f; 

        _rb = GetComponent<Rigidbody>();
        _rb.useGravity    = false;            
        _rb.isKinematic   = true;             
        _rb.interpolation = RigidbodyInterpolation.Interpolate; 

        // Paso 1 y 2: Limpieza de colliders
        foreach (var col in GetComponentsInChildren<Collider>())
            Destroy(col);

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("Hit"))
                Destroy(child.gameObject);
        }

        // Paso 3: Crear colliders propios de la nave
        AddBox(new Vector3(0f, -0.1f, 0f), new Vector3(2.8f, 1.0f, 3.5f));
        AddBox(new Vector3(-3.0f, 0.5f, -0.5f), new Vector3(4.5f, 0.5f, 2.5f));
        AddBox(new Vector3( 3.0f, 0.5f, -0.5f), new Vector3(4.5f, 0.5f, 2.5f));
        AddBox(new Vector3(-2.3f, -0.5f, -0.8f), new Vector3(3.5f, 0.5f, 2.0f));
        AddBox(new Vector3( 2.3f, -0.5f, -0.8f), new Vector3(3.5f, 0.5f, 2.0f));
    }

    void AddBox(Vector3 center, Vector3 size)
    {
        var bc = gameObject.AddComponent<BoxCollider>();
        bc.center    = center;
        bc.size      = size;
        bc.isTrigger = true; 
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        var kb = Keyboard.current;
        if (kb == null) return; 

        float h = 0f, v = 0f;
        if (kb.leftArrowKey.isPressed  || kb.aKey.isPressed) h = -1f; 
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) h =  1f; 
        if (kb.downArrowKey.isPressed  || kb.sKey.isPressed) v = -1f; 
        if (kb.upArrowKey.isPressed    || kb.wKey.isPressed) v =  1f; 

        Vector3 pos = transform.position;

        // =========================================================
        // --- MODIFICADO: LÓGICA DE MOVIMIENTO HACIA ADELANTE ---
        // =========================================================
        float velocidadAvance = 0f;
        
        // Verificamos si conectaste el receptor en el Inspector
        if (receptorUDP != null)
        {
            // Tomamos la aceleración que viene de Python. 
            // Si el pie está en la zona muerta, Python envía 0, y la nave se detiene.
            velocidadAvance = receptorUDP.currentData.accel * multiplicadorVelocidadZ;
        }

        // Solo aplicamos el movimiento frontal si la velocidad es mayor a 0
        pos.z += velocidadAvance * Time.deltaTime;
        // =========================================================

        // Mover en X y Y limitado por los bordes del tunel
        pos.x = Mathf.Clamp(pos.x + h * LATERAL_SPEED * Time.deltaTime, -xLimit, xLimit);
        pos.y = Mathf.Clamp(pos.y + v * LATERAL_SPEED * Time.deltaTime, -yLimit, yLimit);

        transform.position = pos;

        // Rotación visual (inclinación)
        _roll  = Mathf.Lerp(_roll,  -h * ROLL_AMOUNT,  ROLL_SPEED  * Time.deltaTime);
        _pitch = Mathf.Lerp(_pitch, -v * PITCH_AMOUNT, PITCH_SPEED * Time.deltaTime);

        transform.localEulerAngles = new Vector3(_pitch, 0f, _roll);
    }
}