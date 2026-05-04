using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    ShipHealth _health;
    int        _colisiones = 0;
    int        _prevHealth;

    // Fin del tutorial: despues del ultimo obstaculo (400 + 17*360 + margen)
    const float TUTORIAL_END_Z = 7000f;
    bool  _completado  = false;
    float _countdown   = 5f;

    GUIStyle _hudStyle;
    GUIStyle _phaseStyle;
    GUIStyle _bigStyle;
    GUIStyle _countStyle;
    bool     _stylesReady = false;

    void Start()
    {
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null) gm.enabled = false;

        _health = FindFirstObjectByType<ShipHealth>();
        if (_health != null) _prevHealth = _health.currentHealth;

        var obstaculosViejos = GameObject.Find("Obstacles");
        if (obstaculosViejos != null) Destroy(obstaculosViejos);

        var root = new GameObject("Obstacles");
        TutorialObstacleBuilder.BuildTutorialObstacles(root);
    }

    void Update()
    {
        if (_completado)
        {
            _countdown -= Time.deltaTime;
            if (_countdown <= 0f)
            {
                GameMode.IsTutorial = false;
                SceneManager.LoadScene("SampleScene");
            }
            return;
        }

        // Detectar si la nave termino el tutorial
        if (_health != null && _health.transform.position.z >= TUTORIAL_END_Z)
        {
            _completado = true;
            return;
        }

        // Contar colisiones y evitar muerte
        if (_health != null && _health.currentHealth < _prevHealth)
        {
            _colisiones += _prevHealth - _health.currentHealth;
            _health.currentHealth = _health.maxHealth;
        }
        if (_health != null) _prevHealth = _health.currentHealth;
    }

    void OnGUI()
    {
        if (!_stylesReady) BuildStyles();

        if (_completado)
        {
            // Pantalla de felicitaciones
            float w = Screen.width;
            float h = Screen.height;

            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(w/2f - 400, h * 0.25f, 800, 120),
                "Felicitaciones!", _bigStyle);

            GUI.Label(new Rect(w/2f - 400, h * 0.48f, 800, 70),
                "Completaste el Tutorial", _hudStyle);

            GUI.Label(new Rect(w/2f - 400, h * 0.62f, 800, 100),
                $"Nivel 1 comienza en  {Mathf.CeilToInt(_countdown)}", _countStyle);

            return;
        }

        // HUD normal del tutorial
        GUI.Label(new Rect(Screen.width / 2f - 120, 8, 240, 45), "TUTORIAL", _phaseStyle);
        GUI.Label(new Rect(Screen.width - 230, 12, 220, 40), $"Choques: {_colisiones}", _hudStyle);

        // Indicador de fase — centrado abajo, con aviso previo al cambiar de fase
        if (_health != null)
        {
            float z = _health.transform.position.z;

            // Zonas de transicion: despues del ultimo obstaculo de cada fase
            // muestra el aviso de lo que viene ANTES de que empiece
            float f1End = 400 + 4 * 360 + 200f; // despues del obs 5
            float f2End = 400 + 9 * 360 + 200f; // despues del obs 10
            float f3End = 400 + 14 * 360 + 200f; // despues del obs 15

            string fase;
            Color faseColor;

            if      (z >= f1End && z < 400 + 5 * 360)
            { fase = "Siguiente: Izquierda / Derecha"; faseColor = new Color(0.2f, 0.9f, 0.2f); }
            else if (z >= f2End && z < 400 + 10 * 360)
            { fase = "Siguiente: Diagonales"; faseColor = new Color(1f, 0.75f, 0f); }
            else if (z >= f3End && z < 400 + 15 * 360)
            { fase = "Siguiente: Con movimiento"; faseColor = new Color(1f, 0.25f, 0.5f); }
            else if (z < 400 + 5 * 360)
            { fase = "Arriba / Abajo"; faseColor = new Color(0.3f, 0.9f, 1f); }
            else if (z < 400 + 10 * 360)
            { fase = "Izquierda / Derecha"; faseColor = new Color(0.2f, 0.9f, 0.2f); }
            else if (z < 400 + 15 * 360)
            { fase = "Diagonales"; faseColor = new Color(1f, 0.75f, 0f); }
            else
            { fase = "Con movimiento"; faseColor = new Color(1f, 0.25f, 0.5f); }

            _phaseStyle.normal.textColor = faseColor;
            GUI.Label(new Rect(Screen.width / 2f - 250, Screen.height - 48, 500, 40),
                fase, _phaseStyle);
        }
    }

    void BuildStyles()
    {
        _hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.yellow }
        };
        _phaseStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.3f, 0.9f, 1f) }
        };
        _bigStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 72,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };
        _countStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 52,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.3f, 1f, 0.4f) }
        };
        _stylesReady = true;
    }
}
