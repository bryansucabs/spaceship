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

        // Extraer los 3 ejes de forma independiente
        float pitchInput = NormalizeAngle(tiltAngles.x); // Inclinación adelante/atrás
        float yawInput   = NormalizeAngle(tiltAngles.y); // Giro sobre el propio eje (como un volante)
        float rollInput  = NormalizeAngle(tiltAngles.z); // Inclinación lateral (como un avión)

        // Aplicar zonas muertas a los 3 ejes
        if (Mathf.Abs(pitchInput) < deadzoneAngle) pitchInput = 0;
        if (Mathf.Abs(yawInput)   < deadzoneAngle) yawInput   = 0;
        if (Mathf.Abs(rollInput)  < deadzoneAngle) rollInput  = 0;

        // Normalizar los inputs (-1 a 1). Asumimos +-60 grados como el máximo movimiento físico del jugador
        float normalizedPitch = Mathf.Clamp(pitchInput / 60f, -1f, 1f);
        float normalizedYaw   = Mathf.Clamp(yawInput   / 60f, -1f, 1f);
        float normalizedRoll  = Mathf.Clamp(rollInput  / 60f, -1f, 1f);

        // ========================================================
        // --- LÓGICA DE VIBRACIÓN HÁPTICA ---
        // ========================================================
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
        // ========================================================

        // --- 2. VELOCIDAD DESDE PYTHON ---
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
            velocidadActual = 60f;
        }

        rb.linearVelocity = transform.forward * velocidadActual;

        // --- 3. GIRO ABSOLUTO INDEPENDIENTE ---
        // Ahora calculamos los objetivos visuales basándonos en sus respectivos inputs normalizados
        float targetVisualPitch = normalizedPitch * maxPitchAngle;
        float targetVisualRoll  = normalizedRoll  * maxRollAngle;
        float targetVisualYaw   = normalizedYaw   * maxYawAngle; // El Yaw ahora depende del YawInput del celular

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