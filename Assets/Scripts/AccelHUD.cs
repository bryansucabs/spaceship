using UnityEngine;

/// <summary>
/// AccelHUD.cs
/// Dibuja en pantalla un HUD de acelerómetro estilo cockpit:
///   - Barra vertical de aceleración (verde → amarillo → rojo)
///   - Número de velocidad actual
///   - Indicador de freno
///
/// SETUP:
/// 1. Crea un GameObject vacío llamado "HUD" en la escena.
/// 2. Añade este script a él.
/// 3. Arrastra el UDPReceiver al campo "receptorUDP".
/// 4. (Opcional) Asigna una fuente personalizada al campo "hudFont".
///    Si no, usará la fuente por defecto de Unity.
///
/// No necesita Canvas ni nada extra: usa OnGUI (rápido para prototipos).
/// </summary>
public class AccelHUD : MonoBehaviour
{
    [Header("Referencias")]
    public UDPReceiver receptorUDP;

    [Header("Posición y Tamaño del HUD")]
    [Tooltip("Distancia desde el borde izquierdo (en píxeles).")]
    public float marginLeft = 30f;

    [Tooltip("Distancia desde el borde inferior (en píxeles).")]
    public float marginBottom = 40f;

    [Tooltip("Ancho de la barra.")]
    public float barWidth = 28f;

    [Tooltip("Alto total de la barra.")]
    public float barHeight = 180f;

    [Header("Aceleración de referencia")]
    [Tooltip("Valor de accel de Python que llena la barra al 100%. Ajusta según tu setup.")]
    public float accelMaxRef = 100f;

    [Header("Fuente personalizada (opcional)")]
    public Font hudFont;

    // -------------------------------------------------------
    // Recursos internos
    // -------------------------------------------------------
    private Texture2D _texBlack;
    private Texture2D _texWhite;
    private GUIStyle  _labelStyle;
    private GUIStyle  _valueStyle;
    private GUIStyle  _brakeStyle;
    private bool      _stylesReady = false;

    void Start()
    {
        _texBlack = MakeTex(1, 1, new Color(0f, 0f, 0f, 0.55f));
        _texWhite = MakeTex(1, 1, Color.white);
    }

    void OnGUI()
    {
        if (!_stylesReady) BuildStyles();

        float accel = 0f;
        float brake = 0f;

        if (receptorUDP != null)
        {
            accel = receptorUDP.currentData.accel;
            brake = receptorUDP.currentData.brake;
        }

        float tAccel = Mathf.Clamp01(accel / accelMaxRef);
        float tBrake = Mathf.Clamp01(brake / accelMaxRef);

        // ---- Coordenadas (origen en esquina inferior-izquierda) ----
        float screenH = Screen.height;
        float bx = marginLeft;
        float by = screenH - marginBottom - barHeight;

        // ---- Fondo semi-transparente ----
        float panelW = barWidth + 90f;
        float panelH = barHeight + 60f;
        GUI.DrawTexture(new Rect(bx - 10, by - 30, panelW, panelH), _texBlack);

        // ---- Etiqueta "ACCEL" ----
        GUI.Label(new Rect(bx, by - 26, 100, 22), "ACCEL", _labelStyle);

        // ---- Barra de fondo ----
        GUI.DrawTexture(new Rect(bx, by, barWidth, barHeight), MakeTex(1,1, new Color(0.15f,0.15f,0.15f,1f)));

        // ---- Relleno de la barra (color según nivel) ----
        float fillH  = barHeight * tAccel;
        float fillY  = by + barHeight - fillH;
        Color barCol = GradientColor(tAccel);
        GUI.DrawTexture(new Rect(bx, fillY, barWidth, fillH), MakeTex(1, 1, barCol));

        // ---- Líneas de referencia (25%, 50%, 75%) ----
        Color lineCol = new Color(1f,1f,1f,0.25f);
        foreach (float pct in new float[]{0.25f, 0.5f, 0.75f})
        {
            float ly = by + barHeight * (1f - pct);
            GUI.DrawTexture(new Rect(bx, ly, barWidth, 1f), MakeTex(1,1,lineCol));
        }

        // ---- Valor numérico ----
        GUI.Label(new Rect(bx + barWidth + 8, by + barHeight * 0.5f - 16, 70, 32),
                  $"{accel:F0}", _valueStyle);

        // ---- Indicador de freno (pequeño) ----
        if (tBrake > 0.01f)
        {
            float brakeBarH = 60f * tBrake;
            float brakeBY   = screenH - marginBottom - brakeBarH - barHeight - 18f;
            GUI.DrawTexture(new Rect(bx, brakeBY, barWidth, brakeBarH),
                            MakeTex(1,1, new Color(0.9f, 0.2f, 0.2f, 0.85f)));
            GUI.Label(new Rect(bx, brakeBY - 20, 80, 18), "BRAKE", _brakeStyle);
        }
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    private Color GradientColor(float t)
    {
        // Verde → Amarillo → Rojo
        if (t < 0.5f)
            return Color.Lerp(new Color(0.1f, 0.85f, 0.3f), new Color(0.95f, 0.85f, 0.1f), t * 2f);
        else
            return Color.Lerp(new Color(0.95f, 0.85f, 0.1f), new Color(0.95f, 0.2f, 0.1f), (t - 0.5f) * 2f);
    }

    private Texture2D MakeTex(int w, int h, Color col)
    {
        Texture2D t = new Texture2D(w, h);
        t.SetPixel(0, 0, col);
        t.Apply();
        return t;
    }

    private void BuildStyles()
    {
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.6f, 0.9f, 1f) }
        };
        if (hudFont != null) _labelStyle.font = hudFont;

        _valueStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 22,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };
        if (hudFont != null) _valueStyle.font = hudFont;

        _brakeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 10,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(1f, 0.4f, 0.4f) }
        };
        if (hudFont != null) _brakeStyle.font = hudFont;

        _stylesReady = true;
    }
}