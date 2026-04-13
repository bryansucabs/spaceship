using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class StarshipController : MonoBehaviour
{
    [HideInInspector] public Quaternion rotacionRecibidaCelular = Quaternion.identity;

    [Header("Sensibilidad Acrobática (3 Ejes Separados)")]
    [Tooltip("Inclinar adelante/atrás: Sube y baja la nave.")]
    public float fuerzaSubirBajar = 15f;
    [Tooltip("Giro plano (brújula): Apunta la nave a izquierda o derecha.")]
    public float fuerzaGiroLateral = 15f;
    [Tooltip("Volante: Rota las alas sobre el propio eje de la nave (¡Para pasar por rejillas!).")]
    public float fuerzaRotarAlas = 25f;

    [Header("Control en Tuberías (Deslizamiento)")]
    [Tooltip("Al girar a los lados, la nave también se resbala físicamente en esa dirección para esquivar mejor.")]
    public float fuerzaDeslizamientoLateral = 10f;

    [Header("Filtros Humanos")]
    [Tooltip("Grados que puedes mover las manos sin que la nave reaccione (evita temblores).")]
    public float zonaMuerta = 5f;
    [Tooltip("Suavidad de la respuesta. Un valor más alto hace la nave más pesada y cinematográfica.")]
    public float suavizado = 8f;

    private Rigidbody rb;
    private Quaternion calibracionCentro = Quaternion.identity;
    private bool estaCalibrado = false;

    // Variables de inercia
    private float inputPitch = 0f;
    private float inputYaw = 0f;
    private float inputRoll = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Físicas óptimas para vuelo libre acrobático
        rb.mass = 1f;
        rb.useGravity = false;
        rb.linearDamping = 2f;  // Frena el deslizamiento al soltar
        rb.angularDamping = 4f; // Frena la rotación rápidamente para no quedar dando vueltas

        Invoke(nameof(Calibrar), 0.5f);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Calibrar();
        }
    }

    void FixedUpdate()
    {
        if (!estaCalibrado || rotacionRecibidaCelular == Quaternion.identity) return;

        // 1. Obtener la rotación exacta respecto al centro calibrado
        Quaternion rotacionRelativa = Quaternion.Inverse(calibracionCentro) * rotacionRecibidaCelular;
        Vector3 angulos = rotacionRelativa.eulerAngles;

        // 2. Extraer los 3 EJES INDEPENDIENTES
        float rawPitch = NormalizarAngulo(angulos.x); // Adelante/Atrás
        float rawYaw   = NormalizarAngulo(angulos.y); // Giro plano (Brújula)
        float rawRoll  = NormalizarAngulo(angulos.z); // Volante

        // 3. Aplicar Zona Muerta suave y Normalizar a rango de -1 a 1 (Asumiendo 60° como límite humano cómodo)
        float targetPitch = Mathf.Clamp(AplicarZonaMuerta(rawPitch, zonaMuerta) / 60f, -1f, 1f);
        float targetYaw   = Mathf.Clamp(AplicarZonaMuerta(rawYaw, zonaMuerta) / 60f, -1f, 1f);
        float targetRoll  = Mathf.Clamp(AplicarZonaMuerta(rawRoll, zonaMuerta) / 60f, -1f, 1f);

        // 4. Suavizar la entrada (Lerp) para evitar tirones de red o de pulso
        inputPitch = Mathf.Lerp(inputPitch, targetPitch, Time.fixedDeltaTime * suavizado);
        inputYaw   = Mathf.Lerp(inputYaw, targetYaw, Time.fixedDeltaTime * suavizado);
        inputRoll  = Mathf.Lerp(inputRoll, targetRoll, Time.fixedDeltaTime * suavizado);

        // 5. APLICAR ROTACIÓN FÍSICA A LOS 3 EJES
        Vector3 torque = new Vector3(
            inputPitch * fuerzaSubirBajar,
            inputYaw * fuerzaGiroLateral,
            inputRoll * fuerzaRotarAlas // Negativo para que el giro del volante coincida con la pantalla
        );
        rb.AddRelativeTorque(torque, ForceMode.Acceleration);

        // 6. APLICAR DESLIZAMIENTO FÍSICO (Strafe) basado en hacia dónde apuntamos
        // Esto es crucial para tuberías: si apuntas a la derecha, la nave se desliza a la derecha.
        if (fuerzaDeslizamientoLateral > 0)
        {
            Vector3 fuerzaLateral = Vector3.right * (inputYaw * fuerzaDeslizamientoLateral);
            rb.AddRelativeForce(fuerzaLateral, ForceMode.Acceleration);
        }
    }

    public void Calibrar()
    {
        calibracionCentro = rotacionRecibidaCelular;
        estaCalibrado = true;
        Debug.Log("Nave Calibrada: Ahora tienes control independiente de los 3 ejes.");
    }

    private float NormalizarAngulo(float angulo)
    {
        if (angulo > 180f) angulo -= 360f;
        return angulo;
    }

    private float AplicarZonaMuerta(float valor, float limite)
    {
        if (Mathf.Abs(valor) < limite) return 0f;
        return Mathf.Sign(valor) * (Mathf.Abs(valor) - limite);
    }
}