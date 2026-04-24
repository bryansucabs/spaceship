using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// TutorialManager.cs
// Controla el modo tutorial:
//   - Reemplaza los obstaculos normales con obstaculos de tutorial
//   - El jugador NO puede morir (vida se resetea al instante)
//   - Muestra un contador de cuantas veces choco
//   - No hay temporizador
// Se activa automaticamente cuando GameMode.IsTutorial = true
public class TutorialManager : MonoBehaviour
{
    ShipHealth _health;
    int        _colisiones = 0;
    int        _prevHealth;

    GUIStyle _hudStyle;
    GUIStyle _labelStyle;
    GUIStyle _phaseStyle;
    bool     _stylesReady = false;

    void Start()
    {
        // Desactivar GameManager para que no haya temporizador ni muerte
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null) gm.enabled = false;

        // Cache de salud de la nave
        _health = FindFirstObjectByType<ShipHealth>();
        if (_health != null) _prevHealth = _health.currentHealth;

        // Reemplazar obstaculos normales con obstaculos de tutorial
        var obstaculosViejos = GameObject.Find("Obstacles");
        if (obstaculosViejos != null) Destroy(obstaculosViejos);

        var root = new GameObject("Obstacles");
        TutorialObstacleBuilder.BuildTutorialObstacles(root);
    }

    void Update()
    {
        if (_health == null) return;

        // Si la vida bajo, contar la colision y resetear la vida
        if (_health.currentHealth < _prevHealth)
        {
            _colisiones += _prevHealth - _health.currentHealth;
            _health.currentHealth = _health.maxHealth; // no puede morir
        }
        _prevHealth = _health.currentHealth;

        // R para reiniciar el tutorial
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        // Escape para volver al menu
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            GameMode.IsTutorial = false;
            SceneManager.LoadScene("MainMenu");
        }
    }

    void OnGUI()
    {
        if (!_stylesReady) BuildStyles();

        // Titulo arriba al centro
        GUI.Label(new Rect(Screen.width / 2f - 120, 8, 240, 45), "TUTORIAL", _phaseStyle);

        // Contador de choques arriba a la derecha
        GUI.Label(new Rect(Screen.width - 230, 12, 220, 40),
            $"Choques: {_colisiones}", _hudStyle);

        // Instrucciones abajo
        GUI.Label(new Rect(Screen.width - 230, 55, 220, 25), "R = Reiniciar", _labelStyle);
        GUI.Label(new Rect(Screen.width - 230, 78, 220, 25), "ESC = Menu", _labelStyle);

        // Indicador de fase abajo a la derecha
        var ship = FindFirstObjectByType<ShipHealth>();
        if (ship != null)
        {
            float z = ship.transform.position.z;
            string fase = z < 400 + 5 * 360 ? "Fase 1: muevete arriba / abajo"
                        : z < 400 + 10 * 360 ? "Fase 2: muevete izquierda / derecha"
                        : "Fase 3: esquiva todo";

            GUI.Label(new Rect(Screen.width - 350, Screen.height - 45, 340, 35), fase, _labelStyle);
        }
    }

    void BuildStyles()
    {
        _hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 26,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.yellow }
        };
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            normal   = { textColor = new Color(0.8f, 0.8f, 0.8f) }
        };
        _phaseStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.3f, 0.9f, 1f) }
        };
        _stylesReady = true;
    }
}
