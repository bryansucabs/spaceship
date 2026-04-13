using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador de nave espacial que utiliza la orientación de un dispositivo móvil como timón.
/// Recibe un Quaternion con la actitud del celular y lo traduce en rotación de la nave,
/// con opciones de calibración, zona muerta, suavizado y mapeo de ejes personalizable.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class StarshipController : MonoBehaviour
{
    [Header("Entrada del Celular")]
    [Tooltip("Quaternion que se actualiza externamente con la orientación del dispositivo.")]
    [HideInInspector] public Quaternion rotacionRecibidaCelular = Quaternion.identity;

    [Header("Calibración")]
    [Tooltip("Calibrar automáticamente al iniciar.")]
    public bool calibrarAlIniciar = true;
    [Tooltip("Si está activo, se recalibra automáticamente al detectar que el dispositivo ha estado quieto por un tiempo.")]
    public bool autoCalibracionPasiva = false;
    [Tooltip("Tiempo en segundos que el dispositivo debe permanecer casi quieto para recalibrar automáticamente.")]
    public float tiempoQuietoParaAutoCalibracion = 2f;

    [Header("Zona Muerta y Sensibilidad")]
    [Tooltip("Ángulo mínimo (en grados) que debe superar el dispositivo para mover la nave.")]
    [Range(0f, 20f)] public float zonaMuertaAngulo = 3f;
    [Tooltip("Multiplicador de sensibilidad general.")]
    [Range(0.1f, 5f)] public float sensibilidad = 1.5f;

    [Header("Suavizado de Movimiento")]
    [Tooltip("Activa el suavizado para evitar temblores.")]
    public bool usarSuavizado = true;
    [Tooltip("Velocidad de suavizado (menor = más suave pero más retraso).")]
    [Range(0.01f, 1f)] public float velocidadSuavizado = 0.15f;

    [Header("Mapeo de Ejes (Timón de Dos Manos)")]
    [Tooltip("Invertir el eje de cabeceo (pitch) – inclinar adelante/atrás.")]
    public bool invertirCabeceo = false;
    [Tooltip("Invertir el eje de alabeo (roll) – inclinar izquierda/derecha.")]
    public bool invertirAlabeo = false;
    [Tooltip("Invertir el eje de guiñada (yaw) – giro sobre el eje vertical.")]
    public bool invertirGuiñada = false;
    [Tooltip("Habilitar control de guiñada (normalmente desactivado para timón de dos manos).")]
    public bool habilitarGuiñada = false;

    [Header("Física (Rigidbody)")]
    [Tooltip("Aplicar torque en lugar de establecer rotación directamente.")]
    public bool usarTorque = true;
    [Tooltip("Fuerza del torque aplicado.")]
    public float fuerzaTorque = 8f;
    [Tooltip("Usar interpolación directa de rotación (ignora torque).")]
    public bool usarRotacionDirecta = false;
    [Tooltip("Velocidad de interpolación para rotación directa.")]
    public float velocidadRotacionDirecta = 8f;

    [Header("Depuración")]
    public bool mostrarInfoDepuracion = false;

    // Componentes internos
    private Rigidbody rb;
    private Quaternion offsetCalibracion = Quaternion.identity;
    private Quaternion rotacionSuavizada = Quaternion.identity;
    private Quaternion rotacionObjetivoNave = Quaternion.identity;
    private Quaternion rotacionReferenciaNave;

    private bool estaCalibrado = false;
    private float tiempoQuietoActual = 0f;
    private Quaternion ultimaRotacionRecibida;

    // Constantes para el manejo de ejes del celular
    private const float ANGULO_MAXIMO_GIRO = 45f; // Límite opcional para evitar volteretas

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("StarshipController requiere un componente Rigidbody.");
            enabled = false;
            return;
        }

        // Configurar Rigidbody recomendado
        rb.mass = 1f;
        rb.linearDamping = 0f;
        rb.angularDamping = 2f; // Valor sugerido para respuesta controlada
        rb.useGravity = false;

        rotacionSuavizada = Quaternion.identity;
        ultimaRotacionRecibida = rotacionRecibidaCelular;

        if (calibrarAlIniciar)
            Calibrar();
        else
            estaCalibrado = false;
    }

    void Update()
    {
        // Calibración manual por tecla space
        if (Keyboard.current.spaceKey.wasPressedThisFrame){

            Calibrar();
        }

        // Auto-calibración pasiva si está habilitada
        if (autoCalibracionPasiva && estaCalibrado)
            VerificarAutoCalibracion();

        if (!estaCalibrado)
            return;

        // Procesar orientación recibida
        ProcesarOrientacionCelular();
    }

    void FixedUpdate()
    {
        if (!estaCalibrado || rb == null)
            return;

        AplicarRotacionANave();
    }

    /// <summary>
    /// Calcula la rotación objetivo de la nave basada en la orientación calibrada del celular.
    /// Aplica zona muerta, sensibilidad, inversiones y suavizado.
    /// </summary>
    private void ProcesarOrientacionCelular()
    {
        // 1. Obtener rotación relativa al offset de calibración
        Quaternion rotacionRelativa = Quaternion.Inverse(offsetCalibracion) * rotacionRecibidaCelular;

        // 2. Suavizado (Slerp con factor dependiente del tiempo)
        if (usarSuavizado)
        {
            rotacionSuavizada = Quaternion.Slerp(rotacionSuavizada, rotacionRelativa, velocidadSuavizado * Time.deltaTime * 60f);
        }
        else
        {
            rotacionSuavizada = rotacionRelativa;
        }

        // 3. Aplicar zona muerta (basada en ángulo total desde identidad)
        float anguloRotacion;
        Vector3 ejeRotacion;
        rotacionSuavizada.ToAngleAxis(out anguloRotacion, out ejeRotacion);
        if (anguloRotacion > 180f) anguloRotacion -= 360f;

        if (Mathf.Abs(anguloRotacion) < zonaMuertaAngulo)
        {
            // Dentro de zona muerta: rotación nula
            rotacionSuavizada = Quaternion.identity;
        }

        // 4. Convertir a ángulos de Euler para aplicar sensibilidad, inversiones y limitaciones
        Vector3 euler = rotacionSuavizada.eulerAngles;
        // Normalizar a rango [-180, 180]
        euler = new Vector3(
            euler.x > 180 ? euler.x - 360 : euler.x,
            euler.y > 180 ? euler.y - 360 : euler.y,
            euler.z > 180 ? euler.z - 360 : euler.z
        );

        // Aplicar sensibilidad e inversiones
        euler.x *= sensibilidad * (invertirCabeceo ? -1 : 1);
        euler.y *= sensibilidad * (invertirGuiñada ? -1 : 1);
        euler.z *= sensibilidad * (invertirAlabeo ? -1 : 1);

        // Desactivar guiñada si no se desea (típico en timón de dos manos)
        if (!habilitarGuiñada)
            euler.y = 0f;

        // Limitar ángulos para evitar volteretas excesivas (opcional pero mejora control)
        euler.x = Mathf.Clamp(euler.x, -ANGULO_MAXIMO_GIRO, ANGULO_MAXIMO_GIRO);
        euler.z = Mathf.Clamp(euler.z, -ANGULO_MAXIMO_GIRO, ANGULO_MAXIMO_GIRO);

        // 5. Convertir de vuelta a Quaternion
        Quaternion rotacionAjustada = Quaternion.Euler(euler);

        // 6. Calcular rotación objetivo de la nave combinando la rotación de referencia
        rotacionObjetivoNave = rotacionReferenciaNave * rotacionAjustada;
    }

    /// <summary>
    /// Aplica la rotación objetivo a la nave, ya sea mediante torque o interpolación directa.
    /// </summary>
    private void AplicarRotacionANave()
    {
        if (usarRotacionDirecta)
        {
            // Interpolación directa (ignora física)
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, rotacionObjetivoNave, velocidadRotacionDirecta * Time.fixedDeltaTime));
        }
        else if (usarTorque)
        {
            // Calcular torque necesario para alcanzar la rotación objetivo
            Quaternion deltaRot = rotacionObjetivoNave * Quaternion.Inverse(rb.rotation);
            deltaRot.ToAngleAxis(out float angulo, out Vector3 eje);
            if (angulo > 180f) angulo -= 360f;

            if (Mathf.Abs(angulo) > 0.1f)
            {
                Vector3 torque = eje.normalized * angulo * fuerzaTorque;
                rb.AddTorque(torque, ForceMode.Force);
            }
        }
    }

    /// <summary>
    /// Realiza la calibración: guarda la orientación actual del celular como referencia neutra
    /// y almacena la rotación actual de la nave como punto de partida.
    /// </summary>
    public void Calibrar()
    {
        offsetCalibracion = rotacionRecibidaCelular;
        rotacionSuavizada = Quaternion.identity;
        rotacionReferenciaNave = transform.rotation;
        estaCalibrado = true;
        tiempoQuietoActual = 0f;
        ultimaRotacionRecibida = rotacionRecibidaCelular;

        if (mostrarInfoDepuracion)
            Debug.Log("[StarshipController] Calibración realizada.");
    }

    /// <summary>
    /// Detecta si el dispositivo ha permanecido casi quieto durante un tiempo para recalibrar automáticamente.
    /// </summary>
    private void VerificarAutoCalibracion()
    {
        float cambioAngular = Quaternion.Angle(rotacionRecibidaCelular, ultimaRotacionRecibida);
        if (cambioAngular < 0.5f) // Umbral de quietud
        {
            tiempoQuietoActual += Time.deltaTime;
            if (tiempoQuietoActual >= tiempoQuietoParaAutoCalibracion)
            {
                Calibrar();
                tiempoQuietoActual = 0f;
            }
        }
        else
        {
            tiempoQuietoActual = 0f;
        }
        ultimaRotacionRecibida = rotacionRecibidaCelular;
    }

    /// <summary>
    /// Método público para actualizar la rotación desde un script externo.
    /// Útil si el valor se recibe por red o desde otro componente.
    /// </summary>
    public void ActualizarRotacionCelular(Quaternion nuevaRotacion)
    {
        rotacionRecibidaCelular = nuevaRotacion;
    }

    /// <summary>
    /// Reinicia la calibración y opcionalmente reestablece la rotación de la nave.
    /// </summary>
    public void ReiniciarCalibracion(bool reestablecerRotacionNave = false)
    {
        Calibrar();
        if (reestablecerRotacionNave && rb != null)
        {
            rb.rotation = rotacionReferenciaNave;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // Información de depuración en la GUI de la escena
    void OnGUI()
    {
        if (!mostrarInfoDepuracion) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== StarshipController Debug ===");
        GUILayout.Label($"Calibrado: {estaCalibrado}");
        if (estaCalibrado)
        {
            Vector3 eulerRelativo = (Quaternion.Inverse(offsetCalibracion) * rotacionRecibidaCelular).eulerAngles;
            GUILayout.Label($"Euler Relativo: {eulerRelativo}");
            GUILayout.Label($"Zona Muerta: {zonaMuertaAngulo}°");
        }
        GUILayout.Label($"Torque Activo: {usarTorque}");
        GUILayout.EndArea();
    }
}