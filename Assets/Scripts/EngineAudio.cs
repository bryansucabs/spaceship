using UnityEngine;

/// <summary>
/// EngineAudio.cs
/// Controla el sonido del motor de la nave en tiempo real.
/// El pitch y el volumen escalan con la aceleración recibida desde Python.
///
/// SETUP:
/// 1. Añade este script al mismo GameObject que tiene StarshipController o ShipController.
/// 2. En el Inspector, arrastra el UDPReceiver al campo "receptorUDP".
/// 3. Crea un AudioSource en el mismo GameObject:
///    - Asigna un clip de motor (loop de jet/engine).
///    - Activa "Loop" = true.
///    - Activa "Play On Awake" = true.
///    - Pon Volume en 0 al inicio (el script lo controlará).
/// 4. Arrastra ese AudioSource al campo "engineAudio".
/// </summary>
public class EngineAudio : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El AudioSource del motor. Debe tener un clip en loop.")]
    public AudioSource engineAudio;

    [Tooltip("El receptor UDP que trae los datos de Python.")]
    public UDPReceiver receptorUDP;

    [Header("Rango de Volumen")]
    [Tooltip("Volumen mínimo cuando no hay aceleración (motor en ralentí).")]
    [Range(0f, 1f)] public float volumenMinimo = 0.1f;

    [Tooltip("Volumen máximo al acelerar a tope.")]
    [Range(0f, 1f)] public float volumenMaximo = 1.0f;

    [Header("Rango de Pitch (velocidad del sonido)")]
    [Tooltip("Pitch mínimo en ralentí. 1.0 = sonido original.")]
    [Range(0.1f, 3f)] public float pitchMinimo = 0.8f;

    [Tooltip("Pitch máximo al acelerar a tope. 3.0 = triple velocidad.")]
    [Range(0.1f, 3f)] public float pitchMaximo = 2.5f;

    [Header("Suavizado")]
    [Tooltip("Qué tan rápido responde el sonido a los cambios (mayor = más rápido).")]
    [Range(1f, 20f)] public float smoothSpeed = 6f;

    [Header("Aceleración de referencia")]
    [Tooltip("Valor de accel de Python que se considera '100% de potencia'. Ajusta según tu setup.")]
    public float accelMaxRef = 100f;

    // -------------------------------------------------------
    // Estado interno
    // -------------------------------------------------------
    private float _targetVolume;
    private float _targetPitch;

    void Start()
    {
        if (engineAudio == null)
            engineAudio = GetComponent<AudioSource>();

        if (engineAudio != null)
        {
            engineAudio.volume = volumenMinimo;
            engineAudio.pitch  = pitchMinimo;

            if (!engineAudio.isPlaying)
                engineAudio.Play();
        }
    }

    void Update()
    {
        if (engineAudio == null) return;

        // ---- 1. Leer aceleración normalizada [0, 1] ----
        float accel = 0f;
        if (receptorUDP != null)
            accel = receptorUDP.currentData.accel;

        float t = Mathf.Clamp01(accel / accelMaxRef);

        // ---- 2. Calcular objetivos ----
        _targetVolume = Mathf.Lerp(volumenMinimo, volumenMaximo, t);
        _targetPitch  = Mathf.Lerp(pitchMinimo,  pitchMaximo,  t);

        // ---- 3. Suavizar para que no sea brusco ----
        engineAudio.volume = Mathf.Lerp(engineAudio.volume, _targetVolume, smoothSpeed * Time.deltaTime);
        engineAudio.pitch  = Mathf.Lerp(engineAudio.pitch,  _targetPitch,  smoothSpeed * Time.deltaTime);
    }
}