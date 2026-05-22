using UnityEngine;

/// <summary>
/// AccelHUD.cs — v4
/// HUD de cockpit con:
///   - Barra de aceleración (verde → amarillo → rojo)
///   - Indicador RETROCESO con luz azul pulsante
///   - Alerta parpadeante solo cuando el pie DERECHO sale de la cámara
/// </summary>
public class AccelHUD : MonoBehaviour
{
    [Header("Referencias")]
    public UDPReceiver receptorUDP;

    [Header("Posición y Tamaño")]
    public float marginLeft   = 30f;
    public float marginBottom = 40f;
    public float barWidth     = 28f;
    public float barHeight    = 180f;

    [Header("Referencia de escala")]
    [Tooltip("Valor de accel de Python que llena la barra al 100%.")]
    public float accelMaxRef = 100f;

    [Header("Fuente (opcional)")]
    public Font hudFont;

    // -------------------------------------------------------
    // Estado interno
    // -------------------------------------------------------
    private bool  _stylesReady = false;
    private float _blinkTimer  = 0f;
    private bool  _blinkOn     = true;

    private GUIStyle _styleLabel;
    private GUIStyle _styleValue;
    private GUIStyle _styleReverse;
    private GUIStyle _styleAlert;
    void Start()
    {
        // Si la casilla está vacía, busca el objeto automáticamente en la escena
        if (receptorUDP == null) 
        {
            receptorUDP = FindFirstObjectByType<UDPReceiver>();
        }
        
    }
    void Update()
    {
        _blinkTimer += Time.deltaTime;
        if (_blinkTimer >= 0.4f)
        {
            _blinkTimer = 0f;
            _blinkOn = !_blinkOn;
        }
    }

    void OnGUI()
    {
        if (!_stylesReady) BuildStyles();

        float accel      = 0f;
        bool  isReverse  = false;
        bool  rightLost  = false;

        if (receptorUDP != null)
        {
            accel      = receptorUDP.currentData.accel;
            isReverse  = receptorUDP.currentData.reverse == 1;
            rightLost  = receptorUDP.currentData.foot_right_lost == 1;
        }

        float tAccel = Mathf.Clamp01(accel / accelMaxRef);

        float screenH = Screen.height;
        float bx = marginLeft;
        float by = screenH - marginBottom - barHeight;

        // ============================================================
        // PANEL PRINCIPAL
        // ============================================================
        float panelW = barWidth + 105f;
        float panelH = barHeight + 72f;
        DrawRect(new Rect(bx - 12, by - 34, panelW, panelH), new Color(0f, 0f, 0f, 0.65f));

        // Etiqueta ACCEL / REVERSE según modo
        string modeLabel = isReverse ? "REVERSE" : "ACCEL";
        _styleLabel.normal.textColor = isReverse
            ? new Color(0.3f, 0.65f, 1f)
            : new Color(0.55f, 0.88f, 1f);
        GUI.Label(new Rect(bx, by - 28, 90, 20), modeLabel, _styleLabel);

        // Barra de fondo
        DrawRect(new Rect(bx, by, barWidth, barHeight), new Color(0.12f, 0.12f, 0.12f, 1f));

        // Relleno de la barra — azul si retroceso, normal si avance
        float fillH  = barHeight * tAccel;
        float fillY  = by + barHeight - fillH;
        Color barCol = isReverse ? ReverseColor(tAccel) : AccelColor(tAccel);
        DrawRect(new Rect(bx, fillY, barWidth, fillH), barCol);

        // Ticks de referencia
        Color tick = new Color(1f, 1f, 1f, 0.2f);
        foreach (float pct in new float[] { 0.25f, 0.5f, 0.75f })
            DrawRect(new Rect(bx, by + barHeight * (1f - pct), barWidth, 1f), tick);

        // Valor numérico
        GUI.Label(new Rect(bx + barWidth + 8, by + barHeight * 0.5f - 18, 72, 36),
                  $"{accel:F0}", _styleValue);

        // ============================================================
        // INDICADOR DE RETROCESO (panel encima de la barra)
        // ============================================================
        float revPanelY = by - 34 - 72f;
        DrawRect(new Rect(bx - 12, revPanelY, panelW, 66f), new Color(0f, 0f, 0f, 0.65f));

        // Luz circular azul
        float lightAlpha = isReverse ? (_blinkOn ? 1f : 0.5f) : 0.18f;
        Color lightColor = isReverse
            ? new Color(0.1f, 0.45f, 1f, lightAlpha)
            : new Color(0.2f, 0.2f, 0.4f, lightAlpha);

        float lightSize = 18f;
        float lightX    = bx + (barWidth - lightSize) * 0.5f;
        float lightY    = revPanelY + 10f;
        DrawCircle(new Vector2(lightX + lightSize * 0.5f, lightY + lightSize * 0.5f),
                   lightSize * 0.5f, lightColor, 16);

        // Texto RETROCESO
        _styleReverse.normal.textColor = isReverse
            ? new Color(0.3f, 0.7f, 1f, 1f)
            : new Color(0.3f, 0.3f, 0.5f, 1f);
        GUI.Label(new Rect(bx, revPanelY + 32f, panelW - 4, 22), "RETROCESO", _styleReverse);

        // Mini-barra de retroceso
        if (isReverse)
        {
            float miniW = (panelW - 24f) * tAccel;
            DrawRect(new Rect(bx, revPanelY + 54f, panelW - 24f, 5f), new Color(0.1f, 0.15f, 0.25f, 1f));
            DrawRect(new Rect(bx, revPanelY + 54f, miniW,        5f), new Color(0.2f, 0.55f, 1f,   1f));
        }

        // ============================================================
        // ALERTA: solo pie DERECHO fuera de cámara
        // ============================================================
        if (rightLost && _blinkOn)
        {
            float alertW = 320f;
            float alertH = 52f;
            float alertX = (Screen.width  - alertW) * 0.5f;
            float alertY = Screen.height  * 0.12f;

            DrawRect(new Rect(alertX, alertY, alertW, alertH), new Color(0.75f, 0.05f, 0.05f, 0.82f));
            DrawRect(new Rect(alertX,          alertY,          alertW, 2f), new Color(1f, 0.3f, 0.3f, 1f));
            DrawRect(new Rect(alertX,          alertY+alertH-2, alertW, 2f), new Color(1f, 0.3f, 0.3f, 1f));
            DrawRect(new Rect(alertX,          alertY,          2f, alertH), new Color(1f, 0.3f, 0.3f, 1f));
            DrawRect(new Rect(alertX+alertW-2, alertY,          2f, alertH), new Color(1f, 0.3f, 0.3f, 1f));

            GUI.Label(new Rect(alertX, alertY + 10f, alertW, alertH),
                      "⚠  PIE DERECHO FUERA DE CAMARA", _styleAlert);
        }
    }

    // -------------------------------------------------------
    // Helpers
    // ------------------------------tar-------------------------
    private void DrawRect(Rect r, Color c)
    {
        var prev = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = prev;
    }

    private void DrawCircle(Vector2 center, float radius, Color color, int segments)
    {
        float step = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float a = i * step * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(a) * radius - 2f;
            float y = center.y + Mathf.Sin(a) * radius - 2f;
            DrawRect(new Rect(x, y, 4f, 4f), color);
        }
        DrawRect(new Rect(center.x - radius * 0.55f, center.y - radius * 0.55f,
                          radius * 1.1f, radius * 1.1f), color);
    }

    private Color AccelColor(float t)
    {
        if (t < 0.5f)
            return Color.Lerp(new Color(0.1f, 0.85f, 0.3f), new Color(0.95f, 0.85f, 0.1f), t * 2f);
        else
            return Color.Lerp(new Color(0.95f, 0.85f, 0.1f), new Color(0.95f, 0.2f, 0.1f), (t - 0.5f) * 2f);
    }

    private Color ReverseColor(float t)
    {
        // Azul oscuro → azul brillante al aumentar la intensidad del retroceso
        return Color.Lerp(new Color(0.05f, 0.15f, 0.55f), new Color(0.25f, 0.6f, 1f), t);
    }

    private void BuildStyles()
    {
        _styleLabel = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = new Color(0.55f, 0.88f, 1f) }
        };
        if (hudFont != null) _styleLabel.font = hudFont;

        _styleValue = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 22,
            fontStyle = FontStyle.Bold,
            normal    = { textColor = Color.white }
        };
        if (hudFont != null) _styleValue.font = hudFont;

        _styleReverse = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 10,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = new Color(0.3f, 0.3f, 0.5f) }
        };
        if (hudFont != null) _styleReverse.font = hudFont;

        _styleAlert = new GUIStyle(GUI.skin.label)
        {
            fontSize  = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };
        if (hudFont != null) _styleAlert.font = hudFont;

        _stylesReady = true;
    }
}