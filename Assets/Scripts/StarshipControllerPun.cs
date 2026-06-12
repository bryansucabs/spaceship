using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun; // Necesario para la red

[RequireComponent(typeof(Rigidbody))]
public class StarshipControllerPun : MonoBehaviourPun 
{
    [Header("Modo de Control")]
    public bool esJugadorTeclado = false; // El GameSpawner cambiará esto

    public NetworkSend networkSend;
    private bool wasInDeadzone = true;
    private bool wasAtMaxAngle = false;

    public Quaternion rotacionRecibidaCelular = Quaternion.identity;

    [Header("Autoavance")]
    public bool autoavance = false;

    [Header("Control por Visión (Python)")]
    public UDPReceiver receptorUDP;
    public float multiplicadorVelocidadZ = 1.0f;

    [Header("Configuración de Vuelo")]
    public float speed = 40f;
    public float yawTurnSpeed = 100f; // Usado solo en teclado ahora
    public float maxYawAngle = 85f;   // Devuelto de tu script original para celular
    public float maxRollAngle = 90f;  // Ajustado a 90f como tu script anterior
    public float maxPitchAngle = 85f;

    [Header("Zona Muerta")]
    public float deadzoneAngle = 10f;

    private Quaternion calibrationOffset = Quaternion.identity;
    private bool isCalibrated = false;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (receptorUDP == null) receptorUDP = FindFirstObjectByType<UDPReceiver>();
        if (networkSend == null) networkSend = FindFirstObjectByType<NetworkSend>();

        // ShipSabotageAlert solo en el cliente dueño, así el RPC del Overlord
        // llega al componente correcto en cada máquina de cada nave.
        if (photonView.IsMine && GetComponent<ShipSabotageAlert>() == null)
            gameObject.AddComponent<ShipSabotageAlert>();
    }

    void Update()
    {
        // Protección de Red: Si la nave no es mía, no leo inputs
        if (!photonView.IsMine) return;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Calibrate();
        }
    }

    void FixedUpdate()
    {
        // Protección de Red: Si la nave no es mía, no muevo las físicas
        if (!photonView.IsMine) return;

        if (esJugadorTeclado)
        {
            LogicaMovimientoTeclado();
        }
        else
        {
            LogicaMovimientoCelular();
        }
    }

    // --- LÓGICA 1: TECLADO ---
    void LogicaMovimientoTeclado()
    {
        float pitchInput = 0f;
        float yawInput = 0f;
        float rollInput = 0f;
        
        float moveZ = autoavance ? 1f : 0f; 

        if (Keyboard.current != null)
        {
            if (Keyboard.current.downArrowKey.isPressed) pitchInput = 1f;
            if (Keyboard.current.upArrowKey.isPressed) pitchInput = -1f;
            
            if (Keyboard.current.dKey.isPressed) yawInput = 1f;
            if (Keyboard.current.aKey.isPressed) yawInput = -1f;

            if (Keyboard.current.wKey.isPressed) moveZ = 1f; 
            if (Keyboard.current.sKey.isPressed) moveZ = -1f; 

            if (Keyboard.current.rightArrowKey.isPressed) rollInput = -1f;
            if (Keyboard.current.leftArrowKey.isPressed) rollInput = 1f;
        }

        float velocidadBase = speed;
        float velocidadActual = moveZ * velocidadBase;
        
        rb.linearVelocity = transform.forward * velocidadActual;

        float targetVisualPitch = pitchInput * maxPitchAngle;
        float targetVisualRoll = rollInput * maxRollAngle;
        float rotacionVelocidadYaw = yawInput * yawTurnSpeed;

        Vector3 currentAngles = rb.rotation.eulerAngles;

        float smoothPitch = Mathf.LerpAngle(currentAngles.x, targetVisualPitch, Time.fixedDeltaTime * 2f);
        float smoothRoll = Mathf.LerpAngle(currentAngles.z, targetVisualRoll, Time.fixedDeltaTime * 2f);
        float newYaw = currentAngles.y + (rotacionVelocidadYaw * Time.fixedDeltaTime);

        rb.MoveRotation(Quaternion.Euler(smoothPitch, newYaw, smoothRoll));
    }

    // --- LÓGICA 2: CELULAR (DEVUELTA A TU CÓDIGO ORIGINAL) ---
    void LogicaMovimientoCelular()
    {
        if (rotacionRecibidaCelular == Quaternion.identity) return;

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

        float normalizedPitch = Mathf.Clamp(pitchInput / 60f, -1f, 1f);
        float normalizedYaw   = Mathf.Clamp(yawInput   / 60f, -1f, 1f);
        float normalizedRoll  = Mathf.Clamp(rollInput  / 60f, -1f, 1f);

        // --- SISTEMA DE VIBRACIÓN HÁPTICA ---
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

        // --- VELOCIDAD DESDE PYTHON ---
        float velocidadActual = 0f;

        if (receptorUDP != null)
        {
            float accel      = receptorUDP.currentData.accel;
            bool  isReverse  = receptorUDP.currentData.reverse == 1;

            float direccion  = isReverse ? -1f : 1f;
            velocidadActual  = accel * direccion * multiplicadorVelocidadZ;
        }

        if (autoavance)
        {
            velocidadActual = speed;
        }

        rb.linearVelocity = transform.forward * velocidadActual;

        // --- GIRO ABSOLUTO INDEPENDIENTE ORIGINAL ---
        float targetVisualPitch = normalizedPitch * maxPitchAngle;
        float targetVisualRoll  = normalizedRoll  * maxRollAngle;
        float targetVisualYaw   = normalizedYaw   * maxYawAngle; // Restaurado al cálculo absoluto directo

        Vector3 currentAngles = rb.rotation.eulerAngles;

        float smoothPitch = Mathf.LerpAngle(currentAngles.x, targetVisualPitch, Time.fixedDeltaTime * 10f);
        float smoothRoll  = Mathf.LerpAngle(currentAngles.z, targetVisualRoll,  Time.fixedDeltaTime * 10f);
        float smoothYaw   = Mathf.LerpAngle(currentAngles.y, targetVisualYaw,   Time.fixedDeltaTime * 10f);

        rb.MoveRotation(Quaternion.Euler(smoothPitch, smoothYaw, smoothRoll));
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