using UnityEngine;

public class ShipFlightController : MonoBehaviour
{
    public UDPGyro udpGyro;
    public UDPReceiver udpMedia;

    [Header("Configuración de Vuelo")]
    public float speed = 80f;
    public float turnSpeed = 90f; // Qué tan rápido gira la nave en curvas cerradas
    
    [Header("Zona Muerta y Nivelado")]
    public float deadzoneAngle = 10f; // Grados de inclinación del celular a ignorar (temblor de mano)
    public float autoLevelSpeed = 3f; // Qué tan rápido se endereza el horizonte

    [Header("Calibration")]
    private Quaternion calibrationOffset = Quaternion.identity;
    private bool isCalibrated = false;

    void Update()
    {
        if (udpGyro == null) return;

        // Obtener y convertir rotación cruda del celular
        Quaternion rawRot = udpGyro.receivedRotation;
        Quaternion currentDeviceRot = new Quaternion(rawRot.x, rawRot.y, rawRot.z, -rawRot.w);

        // Calibración inicial 
        if (!isCalibrated && currentDeviceRot != Quaternion.identity) 
        {
            calibrationOffset = currentDeviceRot;
            isCalibrated = true;
        }

        // Calcular inclinación RELATIVA (qué tanto has movido el celular desde el 'Centro')
        Quaternion relativeRot = Quaternion.Inverse(calibrationOffset) * currentDeviceRot;
        
        // Convertir a ángulos para usarlos
        Vector3 tiltAngles = relativeRot.eulerAngles;
        float pitchInput = NormalizeAngle(tiltAngles.x); // Inclinación adelante/atrás (Arriba/Abajo)
        float rollInput = NormalizeAngle(tiltAngles.z);  // Inclinación izq/der (Volante)

        // Aplicar Zona Muerta
        if (Mathf.Abs(pitchInput) < deadzoneAngle) pitchInput = 0;
        if (Mathf.Abs(rollInput) < deadzoneAngle) rollInput = 0;

        // Normalizamos la entrada para que sea un valor entre -1 y 1 (asumiendo que 60 grados es la inclinación máxima cómoda)
        float normalizedPitch = Mathf.Clamp(pitchInput / 60f, -1f, 1f);
        float normalizedRoll = Mathf.Clamp(rollInput / 60f, -1f, 1f);

        // MOVIMIENTO HACIA ADELANTE CONSTANTE
        // AVANCE 
        float velocidadAvance = 0f;
        
        // Verificamos si conectaste el receptor en el Inspector
        if (udpMedia != null)
        {
            // Tomamos la aceleración que viene de Python. 
            // Si el pie está en la zona muerta, Python envía 0, y la nave se detiene.
            velocidadAvance = udpMedia.currentData.accel/2;
        }


        transform.Translate(Vector3.forward * velocidadAvance * Time.deltaTime);

        // APLICAR GIRO 
        // Arriba/Abajo (Pitch)
        float pitchTurn = normalizedPitch * turnSpeed * Time.deltaTime;
        
        // Izquierda/Derecha (Yaw) -> Usamos el roll del celular para hacer que la nave gire a los lados
        float yawTurn = normalizedRoll * turnSpeed * Time.deltaTime;
        
        // Inclinación visual (Roll) -> Hacemos que la nave se incline al girar como un avión
        float rollTurn = -normalizedRoll * (turnSpeed * 0.8f) * Time.deltaTime;

        transform.Rotate(pitchTurn, yawTurn, rollTurn, Space.Self);

        // ENDEREZAR HORIZONTE 
        if (Mathf.Abs(normalizedRoll) == 0 && Mathf.Abs(normalizedPitch) == 0)
        {
            // Creamos un vector que apunta hacia adelante, pero forzamos su altura (Y) a cero.
            // Esto es el equivalente a mirar fijamente al horizonte del piso.
            Vector3 flatForward = new Vector3(transform.forward.x, 0, transform.forward.z);
            
            // Verificamos que no estemos mirando directo al cielo o al suelo para evitar errores
            if (flatForward != Vector3.zero)
            {
                Quaternion targetFlatRot = Quaternion.LookRotation(flatForward, Vector3.up);
                
                // Suavizamos el regreso a la posición 100% horizontal
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, 
                    targetFlatRot, 
                    Time.deltaTime * autoLevelSpeed
                );
            }
        }

        // Tecla para recalibrar tu centro cómodo en cualquier momento
        if (Input.GetKeyDown(KeyCode.Space)) Calibrate();
    }

    public void Calibrate()
    {
        if (udpGyro == null) return;
        Quaternion rawRot = udpGyro.receivedRotation;
        calibrationOffset = new Quaternion(rawRot.x, rawRot.y, rawRot.z, -rawRot.w);
        Debug.Log("Nave Calibrada al centro cómodo actual");
    }

    // Utilidad matemática: Unity da ángulos de 0 a 360. 
    // Esta función los convierte a -180 a 180 para saber si inclinas a la izquierda o derecha.
    private float NormalizeAngle(float angle)
    {
        if (angle > 180) angle -= 360;
        return angle;
    }
}