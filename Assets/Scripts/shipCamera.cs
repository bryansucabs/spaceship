using UnityEngine;

public class shipCamera : MonoBehaviour
{
[Header("Objetivo")]
    public Transform target; // Arrastra tu nave aquí

    [Header("Posición")]
    public float distance = 10.0f; // Qué tan atrás de la nave
    public float height = 3.0f;   // Qué tan arriba de la nave
    public float positionDamping = 5.0f; // Suavizado de movimiento

    [Header("Rotación")]
    public float rotationDamping = 3.0f; // Suavizado de giro
    public bool lookAtTarget = true;

    void LateUpdate() 
    {
        if (!target) return;

        // Calcular la posición deseada basada en la rotación de la nave
        // Queremos que la cámara esté SIEMPRE detrás de donde mira la nave
        Vector3 wantedPosition = target.position - (target.forward * distance) + (target.up * height);

        // Interpolar la posición 
        transform.position = Vector3.Lerp(transform.position, wantedPosition, Time.deltaTime * positionDamping);

        // Manejar la rotación
        if (lookAtTarget)
        {
            // La cámara mira a la nave, pero suavemente
            Quaternion wantedRotation = Quaternion.LookRotation(target.position - transform.position, target.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, wantedRotation, Time.deltaTime * rotationDamping);
        }
    }
}
