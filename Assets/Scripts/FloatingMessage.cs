using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FloatingMessage : MonoBehaviour
{
    [Header("Mensaje a mostrar")]
    [TextArea(1, 3)]
    public string message = "Mensaje aqui";

    [Header("Duracion en segundos")]
    public float duration = 6f;

    [Header("Solo para un jugador especifico (opcional)")]
    public string targetRole = "";

    private static Canvas _canvas;
    private static TextMeshProUGUI _text;
    private static float _hideTimer = 0f;

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null)
            return;

        if (!string.IsNullOrEmpty(targetRole))
        {
            var obj = other.attachedRigidbody.GetComponent<PlayerObjective>();
            if (obj == null || obj.playerRole != targetRole)
                return;
        }

        Show(message, duration);
    }

    public static void Show(string msg, float dur)
    {
        if (_text == null)
            CreateCanvas();

        if (_text != null)
        {
            _text.text = msg;
            _text.color = Color.yellow;
        }

        _hideTimer = dur;
    }

    static void CreateCanvas()
    {
        var go = new GameObject("FloatingMessageCanvas");
        _canvas = go.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 200;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("MsgPanel");
        var panelRT = panelGO.AddComponent<RectTransform>();
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.7f);
        panelGO.transform.SetParent(go.transform, false);
        panelRT.anchorMin = new Vector2(0.5f, 1f);
        panelRT.anchorMax = new Vector2(0.5f, 1f);
        panelRT.pivot = new Vector2(0.5f, 1f);
        panelRT.anchoredPosition = new Vector2(0, -60);
        panelRT.sizeDelta = new Vector2(800, 70);

        _text = panelGO.AddComponent<TextMeshProUGUI>();

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null && TMP_Settings.instance != null)
            font = TMP_Settings.defaultFontAsset;
        if (font != null) _text.font = font;

        _text.fontSize = 24;
        _text.fontStyle = FontStyles.Bold;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = Color.yellow;
        _text.text = "";

        var updater = go.AddComponent<MessageUpdater>();
    }

    class MessageUpdater : MonoBehaviour
    {
        void Update()
        {
            if (FloatingMessage._hideTimer > 0f)
            {
                FloatingMessage._hideTimer -= Time.deltaTime;
                if (FloatingMessage._hideTimer <= 0f && FloatingMessage._text != null)
                    FloatingMessage._text.text = "";
            }
        }
    }
}
