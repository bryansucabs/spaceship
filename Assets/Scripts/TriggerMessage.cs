using UnityEngine;

public class TriggerMessage : MonoBehaviour
{
    [Header("Mensaje a mostrar")]
    public string mensaje = "Mensaje aqui";

    [Header("Duracion en segundos")]
    public float duracion = 6f;

    [Header("Filtrar por rol (opcional)")]
    public string soloRol = "";

    static string _textoActual = "";
    static float _ocultarTimer = 0f;
    static GUIStyle _estilo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Inicializar()
    {
        var go = new GameObject("TriggerMessageSys");
        go.AddComponent<Mensajero>();
        Object.DontDestroyOnLoad(go);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null) return;

        if (!string.IsNullOrEmpty(soloRol))
        {
            var po = other.attachedRigidbody.GetComponent<PlayerObjective>();
            if (po == null || po.playerRole != soloRol) return;
        }

        Mostrar(mensaje, duracion);
    }

    public static void Mostrar(string msg, float dur)
    {
        _textoActual = msg;
        _ocultarTimer = dur;
    }

    class Mensajero : MonoBehaviour
    {
        void Update()
        {
            if (_ocultarTimer > 0f)
            {
                _ocultarTimer -= Time.deltaTime;
                if (_ocultarTimer <= 0f)
                    _textoActual = "";
            }
        }

        void OnGUI()
        {
            if (_ocultarTimer <= 0f || string.IsNullOrEmpty(_textoActual)) return;

            if (_estilo == null)
            {
                _estilo = new GUIStyle(GUI.skin.label);
                _estilo.fontSize = 28;
                _estilo.fontStyle = FontStyle.Bold;
                _estilo.alignment = TextAnchor.MiddleCenter;
                _estilo.normal.textColor = Color.yellow;
            }

            float ancho = 700;
            float alto = 70;
            float x = (Screen.width - ancho) / 2f;
            float y = 30;

            GUI.Box(new Rect(x - 10, y - 10, ancho + 20, alto + 20), "");
            GUI.Label(new Rect(x, y, ancho, alto), _textoActual, _estilo);
        }
    }
}
