using UnityEngine;

public class SpaceCamera : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target; // Arrastra tu nave aquí

    [Header("Posición")]
    public float distance = 10.0f; 
    public float height = 3.0f;   
    public float positionDamping = 5.0f; 

    [Header("Rotación y Visión")]
    public float rotationDamping = 5.0f; 
    [Tooltip("Distancia hacia adelante de la nave que la cámara intentará mirar (Look Ahead). Te ayuda a ver los obstáculos antes.")]
    public float anticipacionMirada = 0f; 
    
    [Tooltip("Micro-inclinación visual. Si la nave gira 90°, la cámara solo gira un poquito para dar inmersión sin marear.")]
    [Range(0f, 1f)]
    public float multiplicadorInclinacion = 0.15f;

    [Header("Prevención de Choques (Túnel)")]
    public LayerMask obstacleLayers; 
    public float cameraRadius = 0.5f; 

    void FixedUpdate() 
    {
        if (!target) return;

        // 1. POSICIÓN (EL ARREGLO PRINCIPAL)
        // Usamos Vector3.up (El cielo del MUNDO) en lugar de target.up (El techo de la nave).
        // Así, aunque la nave gire a 90 grados, la cámara se queda firme ARRIBA, dándote visibilidad perfecta.
        Vector3 idealPosition = target.position - (target.forward * distance) + (Vector3.up * height);
        Vector3 finalPosition = idealPosition;

        // 2. ANTI-CLIPPING
        Vector3 rayStart = target.position + (Vector3.up * 1f); 
        RaycastHit hit;
        if (Physics.Linecast(rayStart, idealPosition, out hit, obstacleLayers))
        {
            finalPosition = hit.point + (hit.normal * cameraRadius);
        }

        // 3. MOVER LA CÁMARA
        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.fixedDeltaTime * positionDamping);

        // 4. ROTACIÓN (LOOK AHEAD)
        // En lugar de mirar directamente a la nave, miramos un punto más adelante en el túnel.
        Vector3 puntoDeMira = target.position + (target.forward * anticipacionMirada);
        Vector3 lookDirection = puntoDeMira - transform.position;

        if (lookDirection != Vector3.zero)
        {
            // Extraemos cuánto está girada la nave (Roll) y lo aplicamos pero multiplicado por un valor muy bajo (0.15)
            // Esto hace que si la nave gira 90°, la cámara solo gire unos 13°, sintiéndose épico pero sin perder el horizonte.
            float rollNave = target.eulerAngles.z;
            if (rollNave > 180f) rollNave -= 360f; // Normalizar -180 a 180
            
            Quaternion rotacionRollExtra = Quaternion.AngleAxis(rollNave * multiplicadorInclinacion, target.forward);
            Vector3 arribaDinamico = rotacionRollExtra * Vector3.up;

            Quaternion wantedRotation = Quaternion.LookRotation(lookDirection, arribaDinamico);
            transform.rotation = Quaternion.Slerp(transform.rotation, wantedRotation, Time.fixedDeltaTime * rotationDamping);
        }
    }
}