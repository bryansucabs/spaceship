using UnityEngine;
using UnityEngine.SceneManagement;

// MainMenuManager.cs
// Muestra el menu principal con los botones Tutorial y Nivel 1.
// Agregar este script a un GameObject vacio en la escena MainMenu.
public class MainMenuManager : MonoBehaviour
{
    GUIStyle _titleStyle;
    GUIStyle _subtitleStyle;
    GUIStyle _buttonStyle;
    bool     _ready = false;

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

        // Boton Tutorial
        if (GUI.Button(new Rect(w / 2f - 150, h * 0.46f, 300, 75), "Tutorial", _buttonStyle))
        {
            GameMode.IsTutorial = true;
            SceneManager.LoadScene("SampleScene");
        }

        // Boton Nivel 1
        if (GUI.Button(new Rect(w / 2f - 150, h * 0.64f, 300, 75), "Nivel 1", _buttonStyle))
        {
            GameMode.IsTutorial = false;
            SceneManager.LoadScene("SampleScene");
        }
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
        _subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.4f, 0.8f, 1f) }
        };
        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize  = 30,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };
        _ready = true;
    }
}
