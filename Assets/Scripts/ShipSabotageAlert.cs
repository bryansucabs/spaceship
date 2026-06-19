using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ShipSabotageAlert : MonoBehaviour
{
    GameObject canvasGO;
    Image fondoColor;
    TextMeshProUGUI txtMensaje;
    TextMeshProUGUI txtCuenta;

    void Awake() => CrearUI();

    public void MostrarAlerta(float duracion)
        => MostrarAlertaConColor(duracion, new Color(1f, 0f, 0f), "IMPOSTOR SABOTEANDO PUERTAS", new Color(1f, 0.25f, 0.25f));

    public void MostrarAlertaRelentizar(float duracion)
        => MostrarAlertaConColor(duracion, new Color(0f, 0.45f, 1f), "MOTORES FRENADOS", new Color(0.3f, 0.85f, 1f));

    void MostrarAlertaConColor(float duracion, Color colorOverlay, string mensaje, Color colorTexto)
    {
        StopAllCoroutines();
        txtMensaje.text = mensaje;
        txtMensaje.color = colorTexto;
        canvasGO.SetActive(true);
        StartCoroutine(Corrutina(duracion, colorOverlay));
    }

    IEnumerator Corrutina(float duracion, Color colorBase)
    {
        float restante = duracion;
        while (restante > 0f)
        {
            restante -= Time.deltaTime;
            txtCuenta.text = Mathf.CeilToInt(Mathf.Max(0f, restante)) + "s";
            float a = 0.06f + 0.05f * Mathf.Abs(Mathf.Sin(Time.time * 3f));
            fondoColor.color = new Color(colorBase.r, colorBase.g, colorBase.b, a);
            yield return null;
        }
        canvasGO.SetActive(false);
    }

    void CrearUI()
    {
        canvasGO = new GameObject("AlertaSabotaje");
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Overlay sutil que cubre toda la pantalla (muy transparente)
        var fondoGO = new GameObject("Fondo");
        fondoGO.transform.SetParent(canvasGO.transform, false);
        fondoColor = fondoGO.AddComponent<Image>();
        fondoColor.color = new Color(1f, 0f, 0f, 0.07f);
        Stretch(fondoGO.GetComponent<RectTransform>());

        // Panel pequeño en la parte SUPERIOR (no bloquea la navegación)
        var panelGO = new GameObject("Panel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        panelGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);
        var rtPanel = panelGO.GetComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0.18f, 0.86f);
        rtPanel.anchorMax = new Vector2(0.82f, 0.98f);
        rtPanel.offsetMin = rtPanel.offsetMax = Vector2.zero;

        // Mensaje
        var msgGO = new GameObject("Mensaje");
        msgGO.transform.SetParent(panelGO.transform, false);
        txtMensaje = msgGO.AddComponent<TextMeshProUGUI>();
        txtMensaje.text = "IMPOSTOR SABOTEANDO PUERTAS";
        txtMensaje.fontSize = 20;
        txtMensaje.fontStyle = FontStyles.Bold;
        txtMensaje.color = new Color(1f, 0.25f, 0.25f);
        txtMensaje.alignment = TextAlignmentOptions.MidlineLeft;
        var rtMsg = msgGO.GetComponent<RectTransform>();
        rtMsg.anchorMin = new Vector2(0.03f, 0.1f);
        rtMsg.anchorMax = new Vector2(0.72f, 0.9f);
        rtMsg.offsetMin = rtMsg.offsetMax = Vector2.zero;

        // Cuenta regresiva (a la derecha del mensaje)
        var cntGO = new GameObject("Cuenta");
        cntGO.transform.SetParent(panelGO.transform, false);
        txtCuenta = cntGO.AddComponent<TextMeshProUGUI>();
        txtCuenta.text = "15s";
        txtCuenta.fontSize = 26;
        txtCuenta.fontStyle = FontStyles.Bold;
        txtCuenta.color = Color.yellow;
        txtCuenta.alignment = TextAlignmentOptions.Center;
        var rtCnt = cntGO.GetComponent<RectTransform>();
        rtCnt.anchorMin = new Vector2(0.73f, 0.05f);
        rtCnt.anchorMax = new Vector2(0.97f, 0.95f);
        rtCnt.offsetMin = rtCnt.offsetMax = Vector2.zero;

        canvasGO.SetActive(false);
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
