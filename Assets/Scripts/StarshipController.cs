using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody))]
public class StarshipController : MonoBehaviour
{
    public Quaternion rotacionRecibidaCelular = Quaternion.identity;

    // ==========================================
    // --- ¡NUEVO! CONFIGURACIÓN DE VISIÓN ---
    // ==========================================
    [Header("Control por Visión (Python)")]
    [Tooltip("Arrastra aquí el objeto que tiene el script UDPReceiver")]
    public UDPReceiver receptorUDP; 
    
    [Tooltip("Ajusta este valor si la nave va muy lento o muy rápido")]
    public float multiplicadorVelocidadZ = 1.0f; 
    // ==========================================

    [Header("Configuración de Vuelo")]
    public float speed = 40f;
    public float turnSpeed = 90f;
    
    [Header("Zona Muerta")]
    public float deadzoneAngle = 10f; 

    [Header("Calibration")]
    private Quaternion calibrationOffset = Quaternion.identity;
    private bool isCalibrated = false;

    // Referencia al motor de físicas
    private Rigidbody rb;

    void Start()
    {
        // Obtenemos el Rigidbody automáticamente al inicio
        rb = GetComponent<Rigidbody>();
    }

    // CUANDO USAMOS FÍSICAS, SIEMPRE DEBEMOS USAR FixedUpdate EN LUGAR DE Update
    void FixedUpdate()
    {
        if (rotacionRecibidaCelular == Quaternion.identity) return;

        // --- 1. LEER Y CALCULAR EL CELULAR ---
        Quaternion rawRot = rotacionRecibidaCelular;
        Quaternion currentDeviceRot = new Quaternion(rawRot.x, rawRot.z, rawRot.y, rawRot.w);

        if (!isCalibrated && currentDeviceRot != Quaternion.identity) 
        {
            calibrationOffset = currentDeviceRot;
            isCalibrated = true;
        }

        Quaternion relativeRot = Quaternion.Inverse(calibrationOffset) * currentDeviceRot;
        Vector3 tiltAngles = relativeRot.eulerAngles;
        float pitchInput = NormalizeAngle(tiltAngles.x); 
        float rollInput = NormalizeAngle(tiltAngles.z);  

        if (Mathf.Abs(pitchInput) < deadzoneAngle) pitchInput = 0;
        if (Mathf.Abs(rollInput) < deadzoneAngle) rollInput = 0;

        float normalizedPitch = Mathf.Clamp(pitchInput / 60f, -1f, 1f);
        float normalizedRoll = Mathf.Clamp(rollInput / 60f, -1f, 1f);

        // --- 2. MOVIMIENTO (MOTOR DE FÍSICAS) ---
        // rb.MovePosition mueve la nave, pero la DETIENE si hay una pared poligonal
        //Vector3 avance = transform.forward * speed * Time.fixedDeltaTime;
        //rb.MovePosition(rb.position + avance);
        float velocidadActual = 0f;

        if (receptorUDP != null)
        {
            // Leemos accel y brake directamente del receptor
            float accel = receptorUDP.currentData.accel;
            float brake = receptorUDP.currentData.brake;

            // La velocidad neta es aceleración menos frenado, multiplicada por tu factor
            velocidadActual = (accel - brake) * multiplicadorVelocidadZ;
            velocidadActual = Mathf.Max(0f, velocidadActual); // No retroceder
        }

        Vector3 avance = transform.forward * velocidadActual * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + avance);
        

        // --- 3. GIRO (MOTOR DE FÍSICAS) ---
        float targetVisualPitch = normalizedPitch * 60f;
        float targetVisualRoll = normalizedRoll * 60f; 
        
        // El Yaw sigue siendo continuo (como volante)
        float yawTurn = normalizedRoll * turnSpeed * Time.fixedDeltaTime;

        // Extraemos los ángulos actuales. Al usar físicas, usamos rb.rotation
        Vector3 currentAngles = rb.rotation.eulerAngles;

        // Suavizamos el cabeceo y el alabeo (Pitch y Roll)
        float smoothPitch = Mathf.LerpAngle(currentAngles.x, targetVisualPitch, Time.fixedDeltaTime * 10f);
        float smoothRoll = Mathf.LerpAngle(currentAngles.z, targetVisualRoll, Time.fixedDeltaTime * 10f);
        
        // Sumamos (o restamos) el giro del Yaw
        float newYaw = currentAngles.y - yawTurn; 

        // rb.MoveRotation gira la nave de forma segura sin atravesar paredes con las alas
        Quaternion rotacionFisica = Quaternion.Euler(smoothPitch, newYaw, smoothRoll);
        rb.MoveRotation(rotacionFisica);
    }

    void Update()
    {
        // Los inputs de teclado (como calibrar) siempre deben ir en Update, no en FixedUpdate
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) Calibrate();
    }

    public void Calibrate()
    {
        Quaternion rawRot = rotacionRecibidaCelular;
        calibrationOffset = new Quaternion(rawRot.x, rawRot.z, rawRot.y, rawRot.w);
        Debug.Log("Nave Calibrada al centro cómodo actual");
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180) angle -= 360;
        return angle;
    }
}