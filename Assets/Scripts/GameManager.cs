using UnityEngine;
using UnityEngine.InputSystem;   // Para detectar la tecla R con el nuevo Input System
using UnityEngine.SceneManagement; // Para reiniciar la escena con LoadScene

// GameManager.cs
// Controla el estado global del juego: temporizador, fin de juego, y la UI en pantalla.
// Es un Singleton: solo existe una instancia y se puede acceder desde cualquier script
// usando GameManager.Instance
public class GameManager : MonoBehaviour
{
    // Instancia unica accesible desde cualquier script (patron Singleton)
    public static GameManager Instance;

    // Seccion visible en el Inspector para configurar la duracion del juego
    [Header("Game Settings")]
    public float gameDuration = 120f; // duracion total en segundos (2 minutos)

    // Propiedad de solo lectura publica — otros scripts pueden leerla pero no cambiarla
    // Si IsPlaying es false, la nave no se mueve y no puede recibir dano
    public bool IsPlaying { get; private set; }

    // Tiempo restante en segundos (va bajando cada frame)
    private float   _timeLeft;

    // Mensaje que se muestra cuando termina el juego ("You survived!" o "Ship destroyed!")
    private string  _endMessage = "";

    // Referencia a la vida de la nave (para mostrar los corazones en pantalla)
    private ShipHealth _shipHealth;

    // Estilos de texto para la UI — se inicializan una sola vez en InitStyles()
    private GUIStyle _bigStyle; // temporizador grande arriba al centro
    private GUIStyle _hudStyle; // corazones y texto de reinicio
    private GUIStyle _endStyle; // mensaje de fin de juego

    // Awake() se ejecuta antes que Start() — ideal para configurar el Singleton
    void Awake()
    {
        // Guardar esta instancia para que otros scripts puedan acceder con GameManager.Instance
        Instance = this;
    }

    // Start() se ejecuta al inicio del modo Play, una sola vez
    void Start()
    {
        // Iniciar el temporizador con la duracion configurada en el Inspector
        _timeLeft  = gameDuration;

        // El juego empieza activo
        IsPlaying  = true;

        // Buscar automaticamente el componente ShipHealth en la escena
        _shipHealth = FindFirstObjectByType<ShipHealth>();
    }

    // Update() se ejecuta cada frame
    void Update()
    {
        // Si el juego termino, solo escuchar la tecla R para reiniciar
        if (!IsPlaying)
        {
            // Reiniciar la escena si el jugador presiona R
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return; // no hacer nada mas si el juego termino
        }

        // Reducir el tiempo restante
        _timeLeft -= Time.deltaTime;

        // Si el tiempo llego a 0, el jugador sobrevivio
        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f; // no mostrar tiempo negativo
            GameOver("You survived!"); // mensaje de victoria
        }
    }

    // Llamado por ShipHealth cuando la nave llega a 0 de vida,
    // o por Update() cuando se acaba el tiempo
    public void GameOver(string message)
    {
        IsPlaying   = false;   // detener el juego
        _endMessage = message; // guardar el mensaje para mostrarlo en pantalla
    }

    // OnGUI() dibuja la interfaz de usuario directamente en pantalla cada frame
    // No necesita Canvas ni componentes UI — todo se dibuja con codigo
    void OnGUI()
    {
        // Inicializar los estilos de texto la primera vez
        InitStyles();

        // Calcular minutos y segundos del tiempo restante
        int mins   = Mathf.FloorToInt(_timeLeft / 60f);
        int secs   = Mathf.FloorToInt(_timeLeft % 60f);

        // Leer vida actual y vida maxima de la nave (o 0 si no hay nave)
        int health = _shipHealth != null ? _shipHealth.currentHealth : 0;
        int maxHp  = _shipHealth != null ? _shipHealth.maxHealth     : 5;

        // Dibujar el temporizador centrado en la parte superior de la pantalla
        // Formato: "01:45" (minutos:segundos con 2 digitos cada uno)
        GUI.Label(new Rect(Screen.width / 2f - 70, 15, 140, 45), $"{mins:00}:{secs:00}", _bigStyle);

        // Dibujar los corazones en la esquina inferior izquierda
        // Corazon lleno (?) = vida disponible, corazon vacio (?) = vida perdida
        string hearts = "";
        for (int i = 0; i < maxHp; i++)
            hearts += i < health ? "\u2665 " : "\u2661 ";
        GUI.Label(new Rect(20, Screen.height - 50, 300, 40), hearts, _hudStyle);

        // Mostrar pantalla de fin de juego si el juego termino
        if (!IsPlaying && _endMessage != "")
        {
            // Calcular posicion centrada del cuadro de fin de juego
            float bw = 420, bh = 180;
            float bx = (Screen.width  - bw) / 2f;
            float by = (Screen.height - bh) / 2f;

            // Fondo oscuro del cuadro
            GUI.Box(new Rect(bx - 10, by - 10, bw + 20, bh + 20), "");

            // Mensaje principal: "You survived!" o "Ship destroyed!"
            GUI.Label(new Rect(bx, by + 20, bw, 80),  _endMessage,        _endStyle);

            // Instruccion de reinicio
            GUI.Label(new Rect(bx, by + 110, bw, 40), "Press R to restart", _hudStyle);
        }
    }

    // Inicializa los estilos de texto una sola vez (evita crearlos cada frame)
    void InitStyles()
    {
        // Si ya fueron inicializados, no hacer nada
        if (_bigStyle != null) return;

        // Estilo para el temporizador grande
        _bigStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };

        // Estilo para los corazones y el texto de reinicio
        _hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 26,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(1f, 0.3f, 0.3f) } // rojo-rosado
        };

        // Estilo para el mensaje de fin de juego
        _endStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 38,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.yellow }
        };
    }
}
