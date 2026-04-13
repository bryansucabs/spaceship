using UnityEngine;

public class SpaceCamera : MonoBehaviour
{
    public Transform objetivoNave; // La nave
    
    [Header("Ajustes de Seguimiento")]
    public Vector3 distanciaBase = new Vector3(0, 2, -7); // Posición tras la nave
    public float suavizadoPosicion = 5f;
    public float suavizadoRotacion = 4f;

    [Header("Efectos de Movimiento")]
    public float inclinacionExtra = 2f; // Cámara se inclina un poco más que la nave
    public float anticipacionGiro = 0.5f; // Mirada hacia el giro

    void LateUpdate() // Importante: LateUpdate para evitar tiritoneos
    {
        if (objetivoNave == null) return;

        // 1. Calcular la posición deseada en el espacio local de la nave
        Vector3 posicionDeseada = objetivoNave.TransformPoint(distanciaBase);
        
        // 2. Aplicar suavizado a la posición
        transform.position = Vector3.Lerp(transform.position, posicionDeseada, Time.deltaTime * suavizadoPosicion);

        // 3. Calcular la rotación deseada
        // Miramos hacia un punto un poco más adelante de la nave para el "Lead Look"
        Vector3 puntoAnticipacion = objetivoNave.TransformPoint(Vector3.forward * 10f);
        Quaternion rotacionDeseada = Quaternion.LookRotation(puntoAnticipacion - transform.position, objetivoNave.up);

        // 4. Aplicar suavizado a la rotación
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, Time.deltaTime * suavizadoRotacion);
    }
}