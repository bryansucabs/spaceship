using UnityEngine;
using UnityEngine.InputSystem; // Necesario para usar el nuevo Input System de Unity

// ShipController.cs
// Controla el movimiento de la nave del jugador dentro del tunel.
// La nave avanza automaticamente hacia adelante (eje Z).
// El jugador usa flechas o WASD para moverse en X e Y dentro del tunel.
// Las alas se inclinan visualmente al moverse (roll) y la nariz sube/baja (pitch).
[RequireComponent(typeof(Rigidbody))] // Unity agrega automaticamente un Rigidbody si no hay uno
public class ShipController : MonoBehaviour
{
    // Velocidad constante hacia adelante (no se puede cambiar desde el Inspector)
    const float FORWARD_SPEED = 120f;

    // Velocidad de movimiento lateral (izquierda, derecha, arriba, abajo)
    const float LATERAL_SPEED = 40f;

    // Angulo maximo de inclinacion de las alas al moverse horizontalmente
    const float ROLL_AMOUNT   = 28f;

    // Velocidad a la que las alas se inclinan y vuelven al centro
    const float ROLL_SPEED    = 6f;

    // Angulo maximo de inclinacion de la nariz al moverse verticalmente
    const float PITCH_AMOUNT  = 20f;

    // Velocidad a la que la nariz sube/baja y vuelve al centro
    const float PITCH_SPEED   = 6f;

    // Limites de movimiento en X e Y — sobreescritos por TunnelGenerator al iniciar
    // [HideInInspector] los oculta en el Inspector para evitar confusiones
    [HideInInspector] public float xLimit = 47.5f; // limite lateral
    [HideInInspector] public float yLimit = 53.0f; // limite vertical

    // Referencia al Rigidbody para configurarlo
    private Rigidbody _rb;

    // Valores actuales de inclinacion (van interpolando suavemente)
    private float _roll;  // inclinacion de alas (eje Z)
    private float _pitch; // inclinacion de nariz (eje X)

    // Awake() se llama antes que Start() — ideal para configurar componentes
    void Awake()
    {
        // Siempre corregir los limites al valor correcto del tunel actual
        // Tunel: APOTHEM=55, THICKNESS=2 → cara interior = 54u
        xLimit = 49.32f; // 54 - ancho real del ala (4.68u)
        yLimit = 53.26f; // 54 - alto real de la nave (0.74u)

        _rb = GetComponent<Rigidbody>();
        _rb.useGravity    = false;            // la nave no cae por gravedad
        _rb.isKinematic   = true;             // la fisica no mueve la nave (lo hace el script)
        _rb.interpolation = RigidbodyInterpolation.Interpolate; // evita temblor visual

        // Paso 1: destruir todos los colliders originales del prefab StarSparrow1
        // El prefab tiene muchos colliders que no corresponden exactamente con la geometria visible
        foreach (var col in GetComponentsInChildren<Collider>())
            Destroy(col);

        // Paso 2: destruir GameObjects hijo llamados "Hit..." de ejecuciones anteriores
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("Hit"))
                Destroy(child.gameObject);
        }

        // Paso 3: crear colliders propios que coinciden con la geometria real de la nave
        // Medidos del prefab: escala 0.72, ala max X=6.5*0.72=4.68, max Y=0.89*0.72=0.64

        // Collider del cuerpo central de la nave
        AddBox(new Vector3(0f, -0.1f, 0f), new Vector3(2.8f, 1.0f, 3.5f));

        // Collider del ala superior izquierda (la mas larga, llega hasta X=-4.68, Y=+0.64)
        AddBox(new Vector3(-3.0f, 0.5f, -0.5f), new Vector3(4.5f, 0.5f, 2.5f));

        // Collider del ala superior derecha
        AddBox(new Vector3( 3.0f, 0.5f, -0.5f), new Vector3(4.5f, 0.5f, 2.5f));

        // Collider del ala inferior izquierda (mas corta, llega hasta X=-3.33, Y=-0.73)
        AddBox(new Vector3(-2.3f, -0.5f, -0.8f), new Vector3(3.5f, 0.5f, 2.0f));

        // Collider del ala inferior derecha
        AddBox(new Vector3( 2.3f, -0.5f, -0.8f), new Vector3(3.5f, 0.5f, 2.0f));
    }

    // Agrega un BoxCollider trigger directamente al GameObject de la nave
    void AddBox(Vector3 center, Vector3 size)
    {
        var bc = gameObject.AddComponent<BoxCollider>();
        bc.center    = center;
        bc.size      = size;
        bc.isTrigger = true; // trigger: detecta contacto sin bloquear fisicamente
    }

    // Update() se llama cada frame — aqui va toda la logica de movimiento
    void Update()
    {
        // No mover la nave si el juego no esta activo (fin de juego, pausa)
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        // Obtener el teclado actual
        var kb = Keyboard.current;
        if (kb == null) return; // no hay teclado conectado

        // Leer input de movimiento lateral y vertical
        float h = 0f, v = 0f;
        if (kb.leftArrowKey.isPressed  || kb.aKey.isPressed) h = -1f; // izquierda
        if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) h =  1f; // derecha
        if (kb.downArrowKey.isPressed  || kb.sKey.isPressed) v = -1f; // abajo
        if (kb.upArrowKey.isPressed    || kb.wKey.isPressed) v =  1f; // arriba

        // Calcular nueva posicion
        Vector3 pos = transform.position;

        // Siempre avanza en Z segun la velocidad constante
        pos.z += FORWARD_SPEED * Time.deltaTime;

        // Mover en X y Y limitado por los bordes del tunel
        pos.x = Mathf.Clamp(pos.x + h * LATERAL_SPEED * Time.deltaTime, -xLimit, xLimit);
        pos.y = Mathf.Clamp(pos.y + v * LATERAL_SPEED * Time.deltaTime, -yLimit, yLimit);

        transform.position = pos;

        // Interpolar suavemente hacia el angulo de inclinacion objetivo
        // Lerp: mezcla entre el valor actual y el objetivo segun la velocidad
        _roll  = Mathf.Lerp(_roll,  -h * ROLL_AMOUNT,  ROLL_SPEED  * Time.deltaTime);
        _pitch = Mathf.Lerp(_pitch, -v * PITCH_AMOUNT, PITCH_SPEED * Time.deltaTime);

        // Aplicar la rotacion visual (pitch en X, roll en Z, Y siempre 0)
        transform.localEulerAngles = new Vector3(_pitch, 0f, _roll);
    }
}
