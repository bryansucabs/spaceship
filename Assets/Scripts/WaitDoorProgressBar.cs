using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaitDoorProgressBar : MonoBehaviour
{
    private Canvas _canvas;
    private Image _fillBar;
    private TMP_Text _labelText;
    private TMP_Text _timerText;
    private WaitDoorTrigger _activeTrigger;

    void Start()
    {
        CreateUI();
    }

    void Update()
    {
        _activeTrigger = FindClosestActiveTrigger();

        bool shouldShow = _activeTrigger != null && !_activeTrigger.IsDoorOpened();

        if (_canvas != null && _canvas.gameObject.activeSelf != shouldShow)
            _canvas.gameObject.SetActive(shouldShow);

        if (!shouldShow)
            return;

        _fillBar.fillAmount = _activeTrigger.GetProgress();

        float remaining = _activeTrigger.GetRemainingTime();
        _timerText.text = remaining > 0 ? remaining.ToString("F1") + "s" : "ABRIR!";
    }

    private void CreateUI()
    {
        var canvasGO = new GameObject("ProgressCanvas");
        canvasGO.transform.SetParent(transform, false);
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        var panelGO = new GameObject("Panel");
        var panelRT = panelGO.AddComponent<RectTransform>();
        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.75f);
        panelGO.transform.SetParent(canvasGO.transform, false);
        panelRT.anchorMin = new Vector2(0.5f, 0);
        panelRT.anchorMax = new Vector2(0.5f, 0);
        panelRT.pivot = new Vector2(0.5f, 0);
        panelRT.anchoredPosition = new Vector2(0, 80);
        panelRT.sizeDelta = new Vector2(360, 80);

        var fillGO = new GameObject("FillBar");
        var fillRT = fillGO.AddComponent<RectTransform>();
        _fillBar = fillGO.AddComponent<Image>();
        _fillBar.type = Image.Type.Filled;
        _fillBar.fillMethod = Image.FillMethod.Horizontal;
        _fillBar.fillAmount = 0f;
        _fillBar.color = new Color(0.2f, 0.8f, 0.2f, 1f);

        Texture2D whiteTex = new Texture2D(1, 1);
        whiteTex.SetPixel(0, 0, Color.white);
        whiteTex.Apply();
        _fillBar.sprite = Sprite.Create(whiteTex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        fillGO.transform.SetParent(panelGO.transform, false);
        fillRT.anchorMin = new Vector2(0.05f, 0.2f);
        fillRT.anchorMax = new Vector2(0.95f, 0.55f);
        fillRT.sizeDelta = Vector2.zero;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font == null && TMP_Settings.instance != null)
            font = TMP_Settings.defaultFontAsset;

        var labelGO = new GameObject("LabelText");
        var labelRT = labelGO.AddComponent<RectTransform>();
        _labelText = labelGO.AddComponent<TextMeshProUGUI>();
        _labelText.text = "ESPERANDO PUERTA...";
        _labelText.fontSize = 18;
        _labelText.fontStyle = FontStyles.Bold;
        _labelText.alignment = TextAlignmentOptions.Center;
        _labelText.color = Color.white;
        if (font != null) _labelText.font = font;
        _labelText.enableAutoSizing = true;
        _labelText.fontSizeMin = 10;
        _labelText.fontSizeMax = 18;
        labelGO.transform.SetParent(panelGO.transform, false);
        labelRT.anchorMin = new Vector2(0, 0.55f);
        labelRT.anchorMax = new Vector2(1, 0.95f);
        labelRT.sizeDelta = Vector2.zero;

        var timerGO = new GameObject("TimerText");
        var timerRT = timerGO.AddComponent<RectTransform>();
        _timerText = timerGO.AddComponent<TextMeshProUGUI>();
        _timerText.text = "15.0s";
        _timerText.fontSize = 16;
        _timerText.alignment = TextAlignmentOptions.Center;
        _timerText.color = Color.white;
        if (font != null) _timerText.font = font;
        _timerText.enableAutoSizing = true;
        _timerText.fontSizeMin = 10;
        _timerText.fontSizeMax = 16;
        timerGO.transform.SetParent(panelGO.transform, false);
        timerRT.anchorMin = new Vector2(0, 0);
        timerRT.anchorMax = new Vector2(1, 0.2f);
        timerRT.sizeDelta = Vector2.zero;

        canvasGO.SetActive(false);
    }

    private WaitDoorTrigger FindClosestActiveTrigger()
    {
        WaitDoorTrigger[] triggers = FindObjectsByType<WaitDoorTrigger>(FindObjectsSortMode.None);
        WaitDoorTrigger closest = null;
        float minDist = float.MaxValue;

        foreach (var trigger in triggers)
        {
            if (trigger.IsDoorOpened())
                continue;

            if (trigger.IsPlayerInside)
            {
                if (closest == null)
                {
                    closest = trigger;
                }
                else
                {
                    float dist = Vector3.Distance(transform.position, trigger.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = trigger;
                    }
                }
            }
        }

        return closest;
    }
}
