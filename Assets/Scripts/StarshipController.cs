using UnityEngine;

public class StarshipController : MonoBehaviour
{
    [Header("Configuración de Control")]
    public bool modoAviador = true; // True: Hacia atrás sube. False: Hacia atrás baja.
    public float deadzone = 0.05f;  // Punto muerto para evitar temblor (0 a 1)
    
    [Header("Sensibilidad y Velocidad")]
    public float sensibilidadRotacion = 50f;
    public float suavizadoInercia = 5f; // Qué tanto tarda en "alcanzar" al mando

    // Estas variables vendrían de tu receptor UDP
    [HideInInspector] public Quaternion rotacionRecibidaCelular; 
    
    private Quaternion rotacionCalibrada;
    private Vector3 rotacionActualVelo; // Para suavizado

    void Update()
    {
        ProcesarVuelo();
    }

    private void ProcesarVuelo()
    {
        // 1. Corregir la orientación del celular para el espacio de Unity
        // Los ejes suelen venir invertidos dependiendo de cómo sostienes el móvil
        Quaternion attitude = rotacionRecibidaCelular;
        Quaternion fixedAttitude = new Quaternion(attitude.x, attitude.y, -attitude.z, -attitude.w);
        
        // Rotación base para modo Landscape (Celular acostado)
        Quaternion rotacionReferencia = Quaternion.Euler(90, 0, 0) * fixedAttitude;

        // 2. Extraer los ángulos Euler para aplicar Deadzone y Sensibilidad
        Vector3 angulosRaw = rotacionReferencia.eulerAngles;
        
        // Convertir ángulos de 0-360 a -180 a 180 para detectar inclinación negativa
        float pitch = WrapAngle(angulosRaw.x); // Cabeceo (Arriba/Abajo)
        float roll = WrapAngle(angulosRaw.z);  // Alabeo (Giro sobre eje)
        float yaw = WrapAngle(angulosRaw.y);   // Guiñada (Izquierda/Derecha)

        // 3. Aplicar Modo Aviador (Invertir Pitch si es necesario)
        if (modoAviador) pitch = -pitch;

        // 4. Aplicar Punto Muerto (Deadzone)
        // Normalizamos los valores según un rango máximo de inclinación (ej. 45 grados)
        float inputPitch = Mathf.Abs(pitch) > deadzone * 45f ? pitch : 0;
        float inputRoll = Mathf.Abs(roll) > deadzone * 45f ? roll : 0;
        float inputYaw = Mathf.Abs(yaw) > deadzone * 45f ? yaw : 0;

        // 5. Calcular la rotación deseada basada en la velocidad de giro
        // No rotamos la nave a la posición exacta del celular, sino que el celular
        // "empuja" la rotación de la nave.
        float targetPitch = inputPitch * sensibilidadRotacion * Time.deltaTime;
        float targetRoll = -inputRoll * sensibilidadRotacion * Time.deltaTime; // Negativo para giro natural
        float targetYaw = inputYaw * sensibilidadRotacion * Time.deltaTime;

        // 6. Aplicar la rotación con suavizado (Slerp/Lerp) para dar sensación de masa
        Quaternion rotacionObjetivo = transform.rotation * Quaternion.Euler(targetPitch, targetYaw, targetRoll);
        
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionObjetivo, Time.deltaTime * suavizadoInercia);
    }

    // Función auxiliar para convertir ángulos a rango -180 a 180
    private float WrapAngle(float angle)
    {
        angle %= 360;
        if (angle > 180) return angle - 360;
        return angle;
    }

    // Método para calibrar el "Centro" (Llamar desde el receptor UDP al conectar)
    public void CalibrarCentro()
    {
        // Esto resetearía la orientación actual si decides usar modo absoluto
        rotacionCalibrada = rotacionRecibidaCelular;
        Debug.Log("Nave Calibrada");
    }
}