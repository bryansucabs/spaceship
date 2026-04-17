using UnityEngine;
using UnityEngine.InputSystem; 

// ShipController.cs
// Controla el movimiento de la nave del jugador dentro del túnel usando un celular.
// El movimiento hacia adelante (eje Z) DEBE ser controlado por otro script.
// El jugador inclina el celular (Pitch y Roll) para moverse en X e Y.
[RequireComponent(typeof(Rigidbody))] 
public class StarshipController : MonoBehaviour
{
    [Header("Conexión con el Celular (Android GameRotationVector)")]
    [HideInInspector] public Quaternion rotacionRecibidaCelular = Quaternion.identity;
    
    [Tooltip("Obligatorio: Activa esto si el usuario sostiene el celular en Horizontal (Volante). Si es Vertical (Control Remoto), desactívalo.")]
    public bool celularEnModoLandscape = true;

    [Tooltip("Grados de inclinación del celular para alcanzar la velocidad máxima (Ej: 45 grados).")]
    public float limiteInclinacion = 40f;
    [Tooltip("Grados en el centro donde la nave no se moverá (evita temblores).")]
    public float zonaMuerta = 5f;
    
    [Tooltip("Invierte los controles si sientes que van al revés al probar en el móvil.")]
    public bool invertirArribaAbajo = false;
    public bool invertirIzquierdaDerecha = false;

    [Header("Ajustes de Movimiento Lateral")]
    // Velocidad de movimiento lateral (izquierda, derecha, arriba, abajo)
    public float LATERAL_SPEED = 40f;

    // Ángulo máximo de inclinación de las alas al moverse horizontalmente
    public float ROLL_AMOUNT   = 28f;
    // Velocidad a la que las alas se inclinan y vuelven al centro
    public float ROLL_SPEED    = 6f;

    // Ángulo máximo de inclinación de la nariz al moverse verticalmente
    public float PITCH_AMOUNT  = 20f;
    // Velocidad a la que la nariz sube/baja y vuelve al centro
    public float PITCH_SPEED   = 6f;

    // Límites de movimiento en X e Y — sobreescritos por TunnelGenerator al iniciar
    [HideInInspector] public float xLimit = 47.5f; 
    [HideInInspector] public float yLimit = 53.0f; 

    private Rigidbody _rb;
    private Quaternion calibracionCentro = Quaternion.identity;
    private bool estaCalibrado = false;

    // Valores actuales de inclinación visual (van interpolando suavemente)
    private float _roll;  // inclinación de alas (eje Z)
    private float _pitch; // inclinación de nariz (eje X)

    void Awake()
    {
        // Siempre corregir los límites al valor correcto del túnel actual
        xLimit = 49.32f; // 54 - ancho real del ala (4.68u)
        yLimit = 53.26f; // 54 - alto real de la nave (0.74u)

        _rb = GetComponent<Rigidbody>();
        _rb.useGravity    = false;            // la nave no cae por gravedad
        
        // ¡MODIFICACIÓN CLAVE! Debe ser false para que el EngineController pueda empujarla
        _rb.isKinematic   = false;            
        
        _rb.interpolation = RigidbodyInterpolation.Interpolate; // evita temblor visual

        // Paso 1: destruir todos los colliders originales del prefab
        foreach (var col in GetComponentsInChildren<Collider>())
            Destroy(col);

        // Paso 2: destruir GameObjects hijo llamados "Hit..." de ejecuciones anteriores
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("Hit"))
                Destroy(child.gameObject);
        }

        // Paso 3: crear colliders propios que coinciden con la geometría real de la nave
        AddBox(new Vector3(0f, -0.1f, 0f), new Vector3(2.8f, 1.0f, 3.5f));
        AddBox(new Vector3(-3.0f, 0.5f, -0.5f), new Vector3(4.5f, 0.5f, 2.5f));
        AddBox(new Vector3( 3.0f, 0.5f, -0.5f), new Vector3(4.5f, 0.5f, 2.5f));
        AddBox(new Vector3(-2.3f, -0.5f, -0.8f), new Vector3(3.5f, 0.5f, 2.0f));
        AddBox(new Vector3( 2.3f, -0.5f, -0.8f), new Vector3(3.5f, 0.5f, 2.0f));
        
        // Autocalibrar a los 0.5 segundos por si el jugador ya está sosteniendo el celular
        Invoke(nameof(Calibrar), 0.5f);
    }

    void AddBox(Vector3 center, Vector3 size)
    {
        var bc = gameObject.AddComponent<BoxCollider>();
        bc.center    = center;
        bc.size      = size;
        bc.isTrigger = true; 
    }

    // Cambiamos Update por FixedUpdate para trabajar de la mano con las físicas del EngineController
    void FixedUpdate()
    {
        // No mover la nave si el juego no está activo
        // if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        // Si aún no hemos recibido datos del celular, no intentamos mover la nave
        if (!estaCalibrado || rotacionRecibidaCelular == Quaternion.identity) return;

        // --- MATEMÁTICA ABSOLUTA: SOLUCIÓN PARA ANDROID GAMEROTATIONVECTOR ---
        
        Quaternion rotacionRelativa = Quaternion.Inverse(calibracionCentro) * rotacionRecibidaCelular;

        float rotX = NormalizarAngulo(rotacionRelativa.eulerAngles.x);
        float rotY = NormalizarAngulo(rotacionRelativa.eulerAngles.y);
        float rotZ = NormalizarAngulo(rotacionRelativa.eulerAngles.z);

        float rollCelular = rotZ; 
        float pitchCelular = celularEnModoLandscape ? rotY : rotX; 

        pitchCelular = AplicarZonaMuerta(pitchCelular, zonaMuerta);
        rollCelular  = AplicarZonaMuerta(rollCelular, zonaMuerta);

        float inputV = Mathf.Clamp(pitchCelular / limiteInclinacion, -1f, 1f);
        float inputH = Mathf.Clamp(rollCelular / limiteInclinacion, -1f, 1f);

        if (invertirArribaAbajo) inputV = -inputV;
        if (invertirIzquierdaDerecha) inputH = -inputH;

        // --- APLICAR MOVIMIENTO FÍSICO Y VISUAL ---

        // Usamos la posición del rigidbody para no sobreescribir el avance en Z
        Vector3 pos = _rb.position;

        // Mover en X y Y limitado por los bordes del túnel
        pos.x = Mathf.Clamp(pos.x + inputH * LATERAL_SPEED * Time.fixedDeltaTime, -xLimit, xLimit);
        pos.y = Mathf.Clamp(pos.y + inputV * LATERAL_SPEED * Time.fixedDeltaTime, -yLimit, yLimit);

        // ¡MODIFICACIÓN CLAVE! MovePosition es compatible con las físicas (AddRelativeForce del motor)
        _rb.MovePosition(pos);

        // Interpolar suavemente hacia el ángulo de inclinación objetivo visual
        _roll  = Mathf.Lerp(_roll,  -inputH * ROLL_AMOUNT,  ROLL_SPEED  * Time.fixedDeltaTime);
        _pitch = Mathf.Lerp(_pitch, -inputV * PITCH_AMOUNT, PITCH_SPEED * Time.fixedDeltaTime);

        // Aplicar la rotación visual (pitch en X, roll en Z, Y siempre 0)
        // Usamos MoveRotation para que el colisionador gire suavemente con la física
        Quaternion rotacionVisual = Quaternion.Euler(_pitch, 0f, _roll);
        _rb.MoveRotation(rotacionVisual);
    }

    void Update()
    {
        // La entrada de teclado (Espacio) se debe leer en Update para no perder el click
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Calibrar();
        }
    }

    public void Calibrar()
    {
        calibracionCentro = rotacionRecibidaCelular;
        estaCalibrado = true;
        Debug.Log("Celular Calibrado: Esta posición es ahora tu centro (0,0).");
    }

    private float NormalizarAngulo(float angulo)
    {
        angulo %= 360f;
        if (angulo > 180f)
            angulo -= 360f;
        else if (angulo < -180f)
            angulo += 360f;
        return angulo;
    }

    private float AplicarZonaMuerta(float valor, float limite)
    {
        if (Mathf.Abs(valor) < limite) return 0f;
        return Mathf.Sign(valor) * (Mathf.Abs(valor) - limite);
    }
}