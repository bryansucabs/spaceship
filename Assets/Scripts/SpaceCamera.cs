using UnityEngine;

public class SpaceCamera : MonoBehaviour
{
    public Transform objetivoNave; 
    
    [Header("Ajustes de Seguimiento Inmersivo")]
    public Vector3 distanciaBase = new Vector3(0, 2.5f, -6.5f); // Un poco más cerca para inmersión
    public float suavizadoPosicion = 8f; // Usaremos SmoothDamp que es mucho más orgánico
    public float suavizadoRotacion = 6f;

    [Header("Efectos Dinámicos de Volante")]
    [Tooltip("Exagera la inclinación de la cámara cuando giras el celular para que se sienta increíble.")]
    public float multiplicadorInclinacionCamara = 1.5f;
    [Tooltip("La cámara mira ligeramente hacia el interior de la curva.")]
    public float anticipacionCurva = 10f;

    // Variables de referencia para el SmoothDamp
    private Vector3 velocidadActualPosicion;

    void FixedUpdate() 
    {
        if (objetivoNave == null) return;

        // 1. POSICIÓN: Efecto de arrastre elástico (SmoothDamp)
        // Esto da la sensación de que la nave tira de la cámara (sensación de peso e inercia).
        Vector3 posicionDeseada = objetivoNave.TransformPoint(distanciaBase);
        transform.position = Vector3.SmoothDamp(transform.position, posicionDeseada, ref velocidadActualPosicion, 1f / suavizadoPosicion);

        // 2. DIRECCIÓN DE MIRADA: Miramos hacia adelante, anticipando la curva.
        Vector3 puntoMire = objetivoNave.position + (objetivoNave.forward * anticipacionCurva);

        // --- SOLUCIÓN DE DIRECCIONES ABSOLUTAS (Horizonte Fijo) ---
        // Extraemos cuánto está inclinada la nave lateralmente para aplicarlo a la cámara
        float rollNave = objetivoNave.eulerAngles.z;
        if (rollNave > 180f) rollNave -= 360f; // Normalizar a -180 a 180

        // En lugar de usar "objetivoNave.up" (que voltea la pantalla y marea),
        // usamos "Vector3.up" (el cielo absoluto del mundo). 
        // Solo aplicamos un micro-giro (multiplicador bajo) para el Game Feel sin perder orientación.
        Quaternion rotacionRollExtra = Quaternion.AngleAxis(rollNave * 0.15f, objetivoNave.forward);
        Vector3 arribaDinamico = rotacionRollExtra * Vector3.up;

        // 4. ROTACIÓN: Aplicamos suavemente manteniendo el mundo derecho
        Quaternion rotacionDeseada = Quaternion.LookRotation(puntoMire - transform.position, arribaDinamico);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, Time.deltaTime * suavizadoRotacion);
    }
}