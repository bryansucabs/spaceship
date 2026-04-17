using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EngineController : MonoBehaviour
{
    [Header("Configuración de Potencia")]
    public float fuerzaImpulso = 1500f; // La fuerza real de los motores
    public float suavizadoEntrada = 10f; // Qué tan rápido reacciona el motor al pie
    public float friccionEspacial = 0.98f; // Resistencia al avance (0.99 = mucha inercia)
    [Header("DEBUG ACELERACION AUTO")]
    public bool debug = false;
    private float empujeDeseado; // El valor crudo que viene de Python
    private float empujeSuavizado; // El valor tras pasar por el filtro Lerp
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Configuración automática para vuelo espacial
        rb.useGravity = false;
        rb.linearDamping = 0; // Manejamos la fricción por código para más control
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // --- ESTE MÉTODO RECIBE EL JSON DE PYTHON ---
    public void ActualizarMotores(float valorPython)
    {
        // Normalizamos el valor (0 a 170 -> 0 a 1)
        // Esto hace que el resto del script sea independiente de si Python manda 150 o 500
        empujeDeseado = Mathf.Clamp01(valorPython / 170f);
    }

    void Update()
    {
        // 1. FILTRO DE SUAVIZADO (Lerp)
        // Esto evita que si Python pierde el color verde por un segundo, 
        // la nave de un tiron brusco. El motor "sube y baja" de revoluciones suavemente.
        
        if (debug)
        {
            empujeDeseado = 0.01f;
            empujeSuavizado = 0.01f;

        }
        else    
        {
            empujeSuavizado = Mathf.Lerp(empujeSuavizado, empujeDeseado, Time.deltaTime * suavizadoEntrada);
        }
    }

    void FixedUpdate()
    {
        // 2. APLICACIÓN DE FÍSICA
        if (empujeSuavizado > 0.001f)
        {
            // Aplicamos fuerza constante hacia adelante
            rb.AddRelativeForce(Vector3.forward * empujeSuavizado * fuerzaImpulso);
        }

        // 3. FRICCIÓN ESPACIAL (Inercia)
        // Esto hace que si quitas el pie, la nave se deslice y se detenga poco a poco
        rb.linearVelocity *= friccionEspacial;
    }
}