using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class StarshipController : MonoBehaviour
{
    public NetworkSend networkSend;

    private bool wasInDeadzone = true;
    private bool wasAtMaxAngle = false;

    public Quaternion rotacionRecibidaCelular = Quaternion.identity;

    [Header("Autoavance")]
    public bool autoavance = true;

    [Header("Control por Visión (Python)")]
    [Tooltip("Arrastra aquí el objeto que tiene el script UDPReceiver")]
    public UDPReceiver receptorUDP;

    [Tooltip("Ajusta este valor si la nave va muy lento o muy rápido")]
    public float multiplicadorVelocidadZ = 1.0f;

    [Header("Configuración de Vuelo")]
    public float speed = 40f;
    public float maxYawAngle = 85f; // 45
    public float maxRollAngle = 20f; // Nuevo: Límite para el alabeo visual
    public float maxPitchAngle = 85f; // Límite para el cabeceo visual

    [Header("Zona Muerta")]
    public float deadzoneAngle = 10f;

    [Header("Calibration")]
    private Quaternion calibrationOffset = Quaternion.identity;
    private bool isCalibrated = false;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // ========================================================
        // --- 1. LECTURA DEL CELULAR (Giroscopio Jugador 1) ---
        // ========================================================
        float normalizedPitch = 0f;
        float normalizedYaw = 0f;
        float normalizedRoll = 0f;

        if (rotacionRecibidaCelular != Quaternion.identity)
        {
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
            float yawInput   = NormalizeAngle(tiltAngles.y); 
            float rollInput  = NormalizeAngle(tiltAngles.z); 

            if (Mathf.Abs(pitchInput) < deadzoneAngle) pitchInput = 0;
            if (Mathf.Abs(yawInput)   < deadzoneAngle) yawInput   = 0;
            if (Mathf.Abs(rollInput)  < deadzoneAngle) rollInput  = 0;

            normalizedPitch = Mathf.Clamp(pitchInput / 60f, -1f, 1f);
            normalizedYaw   = Mathf.Clamp(yawInput   / 60f, -1f, 1f);
            normalizedRoll  = Mathf.Clamp(rollInput  / 60f, -1f, 1f);

            // Vibración (Solo se activa si el celular está conectado)
            bool currentlyInDeadzone = (Mathf.Abs(normalizedRoll) == 0f && Mathf.Abs(normalizedPitch) == 0f && Mathf.Abs(normalizedYaw) == 0f);
            if (currentlyInDeadzone && !wasInDeadzone)
            {
                if (networkSend != null) networkSend.SendData("VIBRATE_CENTER");
                Debug.Log("Tacto: Centro alcanzado");
            }
            wasInDeadzone = currentlyInDeadzone;

            bool currentlyAtMaxAngle = (Mathf.Abs(normalizedRoll) >= 1f || Mathf.Abs(normalizedPitch) >= 1f || Mathf.Abs(normalizedYaw) >= 1f);
            if (currentlyAtMaxAngle && !wasAtMaxAngle)
            {
                if (networkSend != null) networkSend.SendData("VIBRATE_MAX");
                Debug.Log("Tacto: Límite máximo de giro alcanzado");
            }
            wasAtMaxAngle = currentlyAtMaxAngle;
        }

        // ========================================================
        // --- 2. LECTURA DEL TECLADO (Jugador 2) ---
        // ========================================================
        float keyMoveX = 0f; // Izquierda / Derecha
        float keyMoveY = 0f; // Arriba / Abajo
        float keyMoveZ = 0f; // Adelante / Atrás
        bool isUsingKeyYaw = false;
        float targetKeyYaw = 0f;

        if (Keyboard.current != null)
        {
            var kb = Keyboard.current;

            // Movimiento con Letras
            if (kb.aKey.isPressed) keyMoveX -= 1f; // A -> Izquierda
            if (kb.dKey.isPressed) keyMoveX += 1f; // D -> Derecha
            if (kb.wKey.isPressed) keyMoveY += 1f; // W -> Arriba
            if (kb.sKey.isPressed) keyMoveZ -= 1f; // S -> Atrás

            // Movimiento vertical con flechas (Arriba / Abajo)
            if (kb.upArrowKey.isPressed) keyMoveY += 1f;   // Flecha Arriba -> Arriba
            if (kb.downArrowKey.isPressed) keyMoveY -= 1f; // Flecha Abajo -> Abajo

            // Rotación 90° con flechas (Izquierda / Derecha)
            if (kb.leftArrowKey.isPressed) 
            {
                targetKeyYaw = -90f;
                isUsingKeyYaw = true;
            }
            else if (kb.rightArrowKey.isPressed) 
            {
                targetKeyYaw = 90f;
                isUsingKeyYaw = true;
            }
        }

        // ========================================================
        // --- 3. APLICAR VELOCIDAD DE TRASLACIÓN ---
        // ========================================================
        float velocidadAvance = 0f;

        if (receptorUDP != null)
        {
            float accel      = receptorUDP.currentData.accel;
            bool  isReverse  = receptorUDP.currentData.reverse == 1;
            float direccion  = isReverse ? -1f : 1f;
            velocidadAvance  = accel * direccion * multiplicadorVelocidadZ;
        }

        if (autoavance) velocidadAvance = 60f;

        // Combinamos la velocidad base (Python/Auto) con el movimiento del teclado ("S" resta velocidad)
        velocidadAvance += (keyMoveZ * speed);

        // Creamos un vector local: X (lados), Y (arriba/abajo), Z (avance)
        Vector3 localVelocity = new Vector3(keyMoveX * speed, keyMoveY * speed, velocidadAvance);
        
        // TransformDirection convierte nuestro vector local a las coordenadas globales del mundo
        rb.linearVelocity = transform.TransformDirection(localVelocity);

        // ========================================================
        // --- 4. APLICAR GIRO Y ROTACIÓN ---
        // ========================================================
        float targetVisualPitch = normalizedPitch * maxPitchAngle;
        float targetVisualRoll  = normalizedRoll  * maxRollAngle;
        
        // Si estamos tocando las flechas laterales, forzamos los 90 grados. Si no, usamos el celular
        float targetVisualYaw = isUsingKeyYaw ? targetKeyYaw : (normalizedYaw * maxYawAngle);

        Vector3 currentAngles = rb.rotation.eulerAngles;

        float smoothPitch = Mathf.LerpAngle(currentAngles.x, targetVisualPitch, Time.fixedDeltaTime * 10f);
        float smoothRoll  = Mathf.LerpAngle(currentAngles.z, targetVisualRoll,  Time.fixedDeltaTime * 10f);
        float smoothYaw   = Mathf.LerpAngle(currentAngles.y, targetVisualYaw,   Time.fixedDeltaTime * 10f);

        rb.MoveRotation(Quaternion.Euler(smoothPitch, smoothYaw, smoothRoll));
    }

    void Update()
    {
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