using UnityEngine;

public class SpaceCamera : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target; // Arrastra tu nave aquí

    [Header("Posición")]
    public float distance = 10.0f; 
    public float height = 3.0f;   
    public float positionDamping = 5.0f; 

    [Header("Rotación")]
    public float rotationDamping = 3.0f; 
    public bool lookAtTarget = true;
    
    [Tooltip("Si es verdadero, la cámara se inclina con la nave. Si es falso, el horizonte se mantiene recto.")]
    public bool rollWithShip = false;

    [Header("Prevención de Choques (Túnel)")]
    public LayerMask obstacleLayers; // IMPORTANTE: Asigna aquí la capa (Layer) de las paredes de tu túnel
    public float cameraRadius = 0.5f; // El grosor de la cámara para que no atraviese la pared

    void FixedUpdate() 
    {
        if (!target) return;

        // 1. Calcular la posición "Ideal" detrás de la nave
        Vector3 idealPosition = target.position - (target.forward * distance) + (target.up * height);
        Vector3 finalPosition = idealPosition;

        // 2. ANTI-CLIPPING: Lanzamos un rayo desde la nave hacia la posición ideal de la cámara
        RaycastHit hit;
        // Usamos la posición de la nave, pero un poco levantada para no chocar con el piso de la nave misma
        Vector3 rayStart = target.position + (Vector3.up * 1f); 
        
        if (Physics.Linecast(rayStart, idealPosition, out hit, obstacleLayers))
        {
            // Si chocamos con una pared del túnel, adelantamos la cámara hasta el punto de choque
            // sumándole un margen de seguridad (cameraRadius) para que no raspe la textura
            finalPosition = hit.point + (hit.normal * cameraRadius);
        }

        // 3. Interpolar la posición suavemente
        transform.position = Vector3.Lerp(transform.position, finalPosition, Time.fixedDeltaTime * positionDamping);

        // 4. Manejar la rotación
        if (lookAtTarget)
        {
            // Vector de dirección hacia donde debe mirar la cámara
            Vector3 lookDirection = target.position - transform.position;
            
            // Definimos cuál será el "Arriba" de la cámara
            Vector3 upDirection = rollWithShip ? target.up : Vector3.up;

            if (lookDirection != Vector3.zero)
            {
                Quaternion wantedRotation = Quaternion.LookRotation(lookDirection, upDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, wantedRotation, Time.fixedDeltaTime * rotationDamping);
            }

            // Si NO queremos que la cámara se incline con la nave, forzamos el Roll (Z) a 0
            if (!rollWithShip)
            {
                Vector3 euler = transform.eulerAngles;
                transform.rotation = Quaternion.Euler(euler.x, euler.y, 0);
            }
        }
    }
}