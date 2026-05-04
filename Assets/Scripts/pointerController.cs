using UnityEngine;
using UnityEngine.InputSystem;

public class pointerController : MonoBehaviour
{
    [Header("Conexión de Datos")]
    [Tooltip("La rotación que llega desde tu receptor UDP del celular.")]
    public Quaternion rotacionRecibidaCelular = Quaternion.identity;

    [Header("Referencias")]
    [Tooltip("El objeto 3D o Canvas de la mira. IMPORTANTE: NO debe ser hijo de la nave para que no se hereden los giros bruscos.")]
    public Transform reticulaVisual;
    [Tooltip("El punto desde el cual se proyecta el puntero (puede ser la cámara o un punto vacío al frente del túnel).")]
    public Transform centroProyeccion;

    [Header("Configuración del Puntero")]
    [Tooltip("Grados que debes mover el celular para que la mira llegue al borde de la pantalla.")]
    public float sensibilidad = 35f; 
    [Tooltip("Qué tan lejos se dibuja el puntero hacia adelante (Eje Z).")]
    public float distanciaAlFrente = 30f; 
    [Tooltip("Límite máximo horizontal en metros (Eje X).")]
    public float rangoMaximoX = 20f; 
    [Tooltip("Límite máximo vertical en metros (Eje Y).")]
    public float rangoMaximoY = 15f; 
    [Tooltip("Amortiguador de temblores de la mano. Valores altos = más responsivo, menos suave.")]
    public float suavizado = 25f;

    // Variables internas de calibración
    private Quaternion calibracionCentro = Quaternion.identity;
    private bool estaCalibrado = false;
    private Vector3 posicionSuavizada;

    void Start()
    {
        // Inicializamos la posición suavizada para evitar un salto en el primer fotograma
        if (reticulaVisual != null) 
            posicionSuavizada = new Vector3(0, 0, distanciaAlFrente);
    }

    void FixedUpdate()
    {
        // Si no hay datos, o faltan referencias, no hacemos nada
        if (rotacionRecibidaCelular == Quaternion.identity || reticulaVisual == null || centroProyeccion == null) return;

        // 1. AUTO-CALIBRACIÓN INICIAL
        if (!estaCalibrado)
        {
            Calibrar();
        }

        // 2. MATEMÁTICA ABSOLUTA DEL PUNTERO
        // Obtenemos la diferencia entre el centro calibrado y la postura actual
        Quaternion rotacionRelativa = Quaternion.Inverse(calibracionCentro) * rotacionRecibidaCelular;

        // Extraemos hacia dónde apunta físicamente el celular en el mundo real
        Vector3 forwardRelativo = rotacionRelativa * Vector3.forward;
        
        // Convertimos ese vector a grados de inclinación pura (Arriba/Abajo y Lados)
        float pitchInput = Mathf.Atan2(forwardRelativo.y, forwardRelativo.z) * Mathf.Rad2Deg;
        //float yawInput   = Mathf.Atan2(forwardRelativo.x, forwardRelativo.z) * Mathf.Rad2Deg;
        float yawInput = rotacionRelativa.eulerAngles.z;
        // 3. NORMALIZAR AL RANGO DE LA PANTALLA (-1 a 1)
        float normalX = Mathf.Clamp(yawInput / sensibilidad, -1f, 1f);
        float normalY = Mathf.Clamp(pitchInput / sensibilidad, -1f, 1f);

        // 4. CALCULAR POSICIÓN DESTINO
        // Construimos la coordenada local (x, y, z) donde debería estar el puntero
        Vector3 posicionDestinoLocal = new Vector3(
            normalX * rangoMaximoX, 
            normalY * rangoMaximoY, 
            distanciaAlFrente
        );

        // 5. SUAVIZADO (Filtro anti-temblor)
        posicionSuavizada = Vector3.Lerp(posicionSuavizada, posicionDestinoLocal, Time.fixedDeltaTime * suavizado);
        
        // 6. APLICAR POSICIÓN AL MUNDO
        // TransformPoint convierte nuestras coordenadas locales calculadas a coordenadas reales del mapa
        reticulaVisual.position = centroProyeccion.TransformPoint(posicionSuavizada);
        
        // Hacemos que la mira siempre esté perfectamente "plana" mirando hacia adelante
        reticulaVisual.rotation = centroProyeccion.rotation;
    }

    void Update()
    {
        // Leer el teclado debe hacerse siempre en Update
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Calibrar();
        }
    }

    // Método público que puedes llamar desde un botón en la UI o desde otro script
    public void Calibrar()
    {
        calibracionCentro = rotacionRecibidaCelular;
        estaCalibrado = true;
        Debug.Log("Puntero Calibrado: Esta postura es el centro exacto (0,0) de tu mira.");
    }
}