using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
// MainMenuManager.cs
public class MainMenuManager : MonoBehaviour
{
    [Header("Conexión Celular")]
    public Quaternion rotacionRecibidaCelular = Quaternion.identity;
    public Texture2D texturaPuntero; // ¡Arrastra aquí una imagen para tu puntero! (Un círculo o mira cruzada)

    [Header("Configuración Puntero Menú")]
    public float sensibilidad = 40f; 
    public float tiempoParaClic = 10.0f; // Segundos que debes mantener el puntero sobre el botón para hacer clic

    GUIStyle _titleStyle;
    GUIStyle _subtitleStyle;
    GUIStyle _buttonStyle;
    bool     _ready = false;

    // Variables internas del puntero
    private Quaternion calibracionCentro = Quaternion.identity;
    private bool estaCalibrado = false;
    private Vector2 posicionPuntero;

    // Variables de "Mirar para hacer clic"
    private float tiempoMirandoTutorial = 0f;
    private float tiempoMirandoNivel1 = 0f;

    void Update()
    {
        if (rotacionRecibidaCelular == Quaternion.identity) return;

        // 1. CALIBRACIÓN
        if (!estaCalibrado)
        {
            calibracionCentro = rotacionRecibidaCelular;
            estaCalibrado = true;
        }

        // Permitir recalibrar con Espacio
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            calibracionCentro = rotacionRecibidaCelular;
        }

        // 2. MATEMÁTICA DEL CELULAR A LA PANTALLA 2D
        Quaternion rotacionRelativa = Quaternion.Inverse(calibracionCentro) * rotacionRecibidaCelular;
        Vector3 forwardRelativo = rotacionRelativa * Vector3.forward;

        float pitchInput = Mathf.Atan2(forwardRelativo.y, forwardRelativo.z) * Mathf.Rad2Deg;
        Vector3 right = rotacionRelativa * Vector3.right;
        float yawInput = Mathf.Atan2(right.y, right.x) * Mathf.Rad2Deg;

        // Convertir grados a coordenadas de la pantalla (0 a Screen.width)
        float normalX = Mathf.Clamp(yawInput / sensibilidad, -1f, 1f);
        float normalY = Mathf.Clamp(-pitchInput / sensibilidad, -1f, 1f); // Invertimos Y para que arriba sea arriba en 2D

        // Posicionamos el puntero partiendo desde el centro de la pantalla
        posicionPuntero.x = (Screen.width / 2f) + (normalX * (Screen.width / 2f));
        posicionPuntero.y = (Screen.height / 2f) + (normalY * (Screen.height / 2f));
    }

    void OnGUI()
    {
        if (!_ready) BuildStyles();

        float w = Screen.width;
        float h = Screen.height;

        // Fondo semitransparente
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Titulo del juego
        GUI.Label(new Rect(w / 2f - 250, h * 0.22f, 500, 90), "SPACESHIP", _titleStyle);

        // --- DEFINIR LAS ZONAS DE LOS BOTONES ---
        Rect rectTutorial = new Rect(w / 2f - 150, h * 0.46f, 300, 75);
        Rect rectNivel1 = new Rect(w / 2f - 150, h * 0.64f, 300, 75);

        // --- LÓGICA DE INTERACCIÓN (Hover to Click) ---
        bool hoverTutorial = rectTutorial.Contains(posicionPuntero);
        bool hoverNivel1 = rectNivel1.Contains(posicionPuntero);

        // Boton Tutorial (Dibuja el botón y detecta si lo presiona el mouse normal o el celular)
        if (GUI.Button(rectTutorial, hoverTutorial ? $"Cargando... {(tiempoMirandoTutorial/tiempoParaClic):P0}" : "Tutorial", _buttonStyle) 
            || ProcesarHoverClic(hoverTutorial, ref tiempoMirandoTutorial))
        {
            CargarEscena(true);
        }
        else if (!hoverTutorial) tiempoMirandoTutorial = 0f; // Resetear si quita la mira

        // Boton Nivel 1
        if (GUI.Button(rectNivel1, hoverNivel1 ? $"Cargando... {(tiempoMirandoNivel1/tiempoParaClic):P0}" : "Nivel 1", _buttonStyle)
            || ProcesarHoverClic(hoverNivel1, ref tiempoMirandoNivel1))
        {
            CargarEscena(false);
        }
        else if (!hoverNivel1) tiempoMirandoNivel1 = 0f;

        // --- DIBUJAR EL PUNTERO DEL CELULAR ---
        if (texturaPuntero != null)
        {
            float tamanoPuntero = 50f;
            // Lo dibujamos restando la mitad de su tamaño para que el centro de la imagen coincida con la coordenada
            GUI.DrawTexture(new Rect(posicionPuntero.x - (tamanoPuntero/2), posicionPuntero.y - (tamanoPuntero/2), tamanoPuntero, tamanoPuntero), texturaPuntero);
        }
    }

    // Función auxiliar para gestionar el tiempo de mirar el botón
    private bool ProcesarHoverClic(bool estaMirando, ref float temporizador)
    {
        if (estaMirando)
        {
            temporizador += Time.deltaTime;
            if (temporizador >= tiempoParaClic)
            {
                return true; // Se completó el tiempo, simular un Clic
            }
        }
        return false;
    }

    private void CargarEscena(bool esTutorial)
    {
        GameMode.IsTutorial = esTutorial;
        if (InstanceManagerPC.instance.ip == "")
        {
            InstanceManagerPC.instance.ip = PlayerPrefs.GetString("ipJugador", "");
        }
        SceneManager.LoadScene("SampleScene");
    }

    void BuildStyles()
    {
        _titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 58,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };
        //...
        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 30,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };
        _ready = true;
    }
}