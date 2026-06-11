using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

// Agrega marcadores de naves y botones de navegación a la vista del Atacante.
// Requiere que el GameObject tenga también PhoneController (cámara ortográfica).
public class OverlordHUD : MonoBehaviourPun
{
    [Header("Velocidad de snap a nave")]
    public float velocidadSnap = 8f;

    Camera cam;
    Transform objetivoSnap = null;
    Camera camShipActiva = null;     // cámara de nave actualmente visible

    // Marcadores world-space (canvas World Space encima de cada nave)
    GameObject marcadorRojo;
    GameObject marcadorAzul;

    // Canvas Screen-Space para los botones HUD
    Canvas canvasHUD;
    Button btnRoja, btnAzul, btnGeneral;

    // ── INICIALIZACIÓN ────────────────────────────────────────────────────────
    void Awake()
    {
        cam = GetComponent<Camera>();
        CrearCanvasHUD();
        CrearMarcadorNave(ref marcadorRojo, "▲ NAVE ROJA",  new Color(1f, 0.2f, 0.2f));
        CrearMarcadorNave(ref marcadorAzul, "▲ NAVE AZUL",  new Color(0.3f, 0.6f, 1f));
    }

    // ── UPDATE: seguir nave seleccionada + actualizar marcadores ─────────────
    void Update()
    {
        if (objetivoSnap != null)
        {
            // Seguimiento continuo — la cámara sigue a la nave mientras se mueve
            Vector3 destino = new Vector3(objetivoSnap.position.x,
                                          transform.position.y,
                                          objetivoSnap.position.z - 80f);
            transform.position = Vector3.Lerp(transform.position, destino,
                                              Time.deltaTime * velocidadSnap);
        }

        ActualizarMarcadores();
    }

    // ── BOTONES ───────────────────────────────────────────────────────────────
    public void IrNaveRoja()
    {
        var nave = BuscarNave("RedShip");
        if (nave == null) { Debug.LogWarning("[HUD] RedShip no encontrada"); return; }
        ActivarVistaShip(nave);
    }

    public void IrNaveAzul()
    {
        var nave = BuscarNave("BlueShip");
        if (nave == null) { Debug.LogWarning("[HUD] BlueShip no encontrada"); return; }
        ActivarVistaShip(nave);
    }

    public void VistaGeneral()
    {
        StopAllCoroutines();

        // Apagar la cámara de nave activa
        if (camShipActiva != null)
        {
            camShipActiva.enabled = false;
            camShipActiva = null;
        }

        // Volver a la cámara cenital del Overlord
        cam.enabled = true;
        objetivoSnap = null;

        StartCoroutine(MoverHaciaVistaGeneral());
        StartCoroutine(AjustarZoom(350f));
    }

    void ActivarVistaShip(Transform nave)
    {
        StopAllCoroutines();
        objetivoSnap = null;

        var shipCam = nave.GetComponentInChildren<Camera>();
        if (shipCam == null)
        {
            Debug.LogWarning("[HUD] La nave " + nave.name + " no tiene cámara hija");
            return;
        }

        // Apagar cámara de nave previa si era diferente
        if (camShipActiva != null && camShipActiva != shipCam)
            camShipActiva.enabled = false;

        camShipActiva = shipCam;
        shipCam.enabled = true;

        // Apagar la vista cenital del Overlord
        cam.enabled = false;

        Debug.Log("[HUD] Vista nave: " + nave.name);
    }

    System.Collections.IEnumerator MoverHaciaVistaGeneral()
    {
        // Busca el punto medio entre las dos naves si existen, si no va al origen
        var roja = BuscarNave("RedShip");
        var azul = BuscarNave("BlueShip");
        Vector3 centro = Vector3.zero;
        if (roja != null && azul != null)
            centro = (roja.position + azul.position) / 2f;
        else if (roja != null) centro = roja.position;
        else if (azul != null) centro = azul.position;

        Vector3 destino = new Vector3(centro.x, transform.position.y, centro.z - 60f);
        float tiempo = 0f;
        Vector3 origen = transform.position;
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime * 1.5f;
            transform.position = Vector3.Lerp(origen, destino, tiempo);
            yield return null;
        }
    }

    System.Collections.IEnumerator AjustarZoom(float targetSize)
    {
        float origen = cam.orthographicSize;
        float tiempo = 0f;
        while (tiempo < 1f)
        {
            tiempo += Time.deltaTime * 2f;
            cam.orthographicSize = Mathf.Lerp(origen, targetSize, tiempo);
            yield return null;
        }
    }

    // ── MARCADORES ────────────────────────────────────────────────────────────
    void ActualizarMarcadores()
    {
        var roja = BuscarNave("RedShip");
        var azul = BuscarNave("BlueShip");

        if (roja != null)
        {
            marcadorRojo.SetActive(true);
            marcadorRojo.transform.position = roja.position + Vector3.up * 18f;
            marcadorRojo.transform.rotation = Quaternion.LookRotation(Vector3.down, cam.transform.forward)
                                              * Quaternion.Euler(90f, 0f, 0f);
        }
        else marcadorRojo.SetActive(false);

        if (azul != null)
        {
            marcadorAzul.SetActive(true);
            marcadorAzul.transform.position = azul.position + Vector3.up * 18f;
            marcadorAzul.transform.rotation = Quaternion.LookRotation(Vector3.down, cam.transform.forward)
                                              * Quaternion.Euler(90f, 0f, 0f);
        }
        else marcadorAzul.SetActive(false);
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────
    Transform BuscarNave(string nombre)
    {
        // Búsqueda directa
        var go = GameObject.Find(nombre);
        if (go != null) return go.transform;

        // Photon añade "(Clone)" al instanciar prefabs
        go = GameObject.Find(nombre + "(Clone)");
        if (go != null) return go.transform;

        // Fallback: busca por nombre parcial entre todos los controladores de nave
        foreach (var ctrl in FindObjectsByType<StarshipControllerPun>(FindObjectsSortMode.None))
            if (ctrl.gameObject.name.StartsWith(nombre))
                return ctrl.transform;

        return null;
    }

    // ── CREAR UI HUD ──────────────────────────────────────────────────────────
    void CrearCanvasHUD()
    {
        // Asegurar que haya un EventSystem en la escena
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        var goCanvas = new GameObject("OverlordHUD_Canvas");
        canvasHUD = goCanvas.AddComponent<Canvas>();
        canvasHUD.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasHUD.sortingOrder = 10;
        goCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        goCanvas.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1280, 720);
        goCanvas.AddComponent<GraphicRaycaster>();

        // Panel inferior con los 3 botones
        var panel = CrearImagen(goCanvas.transform, "Panel",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 0f), new Vector2(780f, 70f),
            new Color(0f, 0f, 0f, 0.6f));

        btnRoja    = CrearBoton(panel.transform, "BtnRoja",    "🔴  Nave Roja",
            new Vector2(-260f, 35f), new Vector2(220f, 55f),
            new Color(0.55f, 0.05f, 0.05f, 1f));
        btnAzul    = CrearBoton(panel.transform, "BtnAzul",    "🔵  Nave Azul",
            new Vector2(0f,    35f), new Vector2(220f, 55f),
            new Color(0.1f,  0.2f,  0.65f, 1f));
        btnGeneral = CrearBoton(panel.transform, "BtnGeneral", "🗺  Vista General",
            new Vector2(260f,  35f), new Vector2(220f, 55f),
            new Color(0.15f, 0.35f, 0.15f, 1f));

        btnRoja.onClick.AddListener(IrNaveRoja);
        btnAzul.onClick.AddListener(IrNaveAzul);
        btnGeneral.onClick.AddListener(VistaGeneral);
    }

    void CrearMarcadorNave(ref GameObject marcador, string etiqueta, Color color)
    {
        marcador = new GameObject("Marcador_" + etiqueta);

        // Canvas en World Space para que aparezca encima de la nave en 3D
        var canvas = marcador.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        marcador.AddComponent<GraphicRaycaster>();

        var rt = marcador.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 50f);
        rt.localScale = Vector3.one * 0.1f;

        var goTxt = new GameObject("Texto");
        goTxt.transform.SetParent(marcador.transform, false);
        var txt = goTxt.AddComponent<TextMeshProUGUI>();
        txt.text = etiqueta;
        txt.fontSize = 28;
        txt.fontStyle = FontStyles.Bold;
        txt.color = color;
        txt.alignment = TextAlignmentOptions.Center;
        var rtTxt = goTxt.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = rtTxt.offsetMax = Vector2.zero;

        marcador.SetActive(false);
    }

    // ── UTILIDADES UI ─────────────────────────────────────────────────────────
    GameObject CrearImagen(Transform parent, string nombre,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        go.layer = 5;
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    Button CrearBoton(Transform parent, string nombre, string texto,
        Vector2 pos, Vector2 size, Color color)
    {
        var go = CrearImagen(parent, nombre,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            pos, size, color);

        var img = go.GetComponent<Image>();
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;   // necesario para que reciba clics con mouse y táctil

        // Color de hover para feedback visual
        var colors = btn.colors;
        colors.highlightedColor = new Color(color.r + 0.2f, color.g + 0.2f, color.b + 0.2f, 1f);
        colors.pressedColor     = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f, 1f);
        btn.colors = colors;

        var goTxt = new GameObject("Text");
        goTxt.transform.SetParent(go.transform, false);
        goTxt.layer = 5;
        var txt = goTxt.AddComponent<TextMeshProUGUI>();
        txt.text = texto;
        txt.fontSize = 16;
        txt.color = Color.white;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        var rt = goTxt.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        return btn;
    }
}
