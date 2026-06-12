using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections;

public class OverlordHUD : MonoBehaviourPun
{
    [Header("Velocidad de snap a nave")]
    public float velocidadSnap = 8f;

    Camera cam;
    Transform objetivoSnap = null;
    Camera camShipActiva = null;

    GameObject marcadorRojo;
    GameObject marcadorAzul;

    Canvas canvasHUD;
    Button btnRoja, btnAzul, btnGeneral;

    // ── IMPOSTOR ──────────────────────────────────────────────────────────────
    enum ImpostorEstado { Listo, Activo, Enfriamiento }
    ImpostorEstado impostorEstado = ImpostorEstado.Listo;
    float impostorTimer = 0f;
    const float SABOTAJE_DURACION = 15f;
    const float COOLDOWN_DURACION = 45f;

    Button btnImpostor;
    Image imgImpostor;
    TextMeshProUGUI txtImpostorLabel;
    TextMeshProUGUI txtImpostorTimer;

    static readonly Color ColorListo        = new Color(0.85f, 0.15f, 0.05f, 1f);
    static readonly Color ColorActivo       = new Color(0.40f, 0.05f, 0.02f, 1f);
    static readonly Color ColorEnfriamiento = new Color(0.22f, 0.22f, 0.22f, 1f);

    // ── FRENOS ────────────────────────────────────────────────────────────────
    enum FrenosEstado { Listo, Activo, Enfriamiento }
    FrenosEstado frenosEstado = FrenosEstado.Listo;
    float frenosTimer = 0f;
    const float FRENOS_DURACION = 10f;
    const float FRENOS_COOLDOWN = 35f;

    Button btnFreno;
    Image imgFreno;
    TextMeshProUGUI txtFrenoLabel;
    TextMeshProUGUI txtFrenoTimer;

    static readonly Color FrenoListo        = new Color(0f,   0.50f, 0.65f, 1f);
    static readonly Color FrenoActivo       = new Color(0f,   0.20f, 0.35f, 1f);
    static readonly Color FrenoEnfriamiento = new Color(0.22f, 0.22f, 0.22f, 1f);
    // ──────────────────────────────────────────────────────────────────────────

    // ── ROMPER PASADIZO ───────────────────────────────────────────────────────
    bool _pasadizoRoto = false;
    Button btnRomper;
    Image imgRomper;
    TextMeshProUGUI txtRomperLabel;
    GameObject _panelSeleccion;
    GameObject _marcador1;   // disco naranja sobre CorridorOsc (1)
    GameObject _marcador3;   // disco amarillo sobre CorridorOsc (3)

    static readonly Color RomperListo    = new Color(0.55f, 0.18f, 0.05f, 1f);
    static readonly Color RomperUsado    = new Color(0.18f, 0.18f, 0.18f, 1f);
    static readonly Color ColorPasadizo1 = new Color(1f,    0.25f, 0f,    1f); // naranja vivo
    static readonly Color ColorPasadizo3 = new Color(0f,    0.85f, 1f,    1f); // cyan vivo
    System.Collections.Generic.List<Renderer> _rendsCorr1 = new();
    System.Collections.Generic.List<Color>    _colsCorr1  = new();
    System.Collections.Generic.List<Renderer> _rendsCorr3 = new();
    System.Collections.Generic.List<Color>    _colsCorr3  = new();
    // ──────────────────────────────────────────────────────────────────────────

    // Corrutinas de vista (separadas para no cancelar la de sabotaje)
    Coroutine _corView;
    Coroutine _corZoom;
    // ──────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        cam = GetComponent<Camera>();
        // Solo el Overlord dueño crea la UI — en los clientes de naves no se crea nada.
        // El componente sigue vivo para recibir RPCs.
        if (!photonView.IsMine) return;
        CrearCanvasHUD();
        CrearMarcadorNave(ref marcadorRojo, "▲ NAVE ROJA",  new Color(1f, 0.2f, 0.2f));
        CrearMarcadorNave(ref marcadorAzul, "▲ NAVE AZUL",  new Color(0.3f, 0.6f, 1f));
    }

    void Start()
    {
        if (!photonView.IsMine) return;
        CrearBotonImpostor(canvasHUD.transform);
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        if (objetivoSnap != null)
        {
            Vector3 destino = new Vector3(objetivoSnap.position.x,
                                          transform.position.y,
                                          objetivoSnap.position.z - 80f);
            transform.position = Vector3.Lerp(transform.position, destino,
                                              Time.deltaTime * velocidadSnap);
        }

        ActualizarMarcadores();
        ActualizarBotonImpostor();
        ActualizarBotonFreno();
    }

    // ── BOTONES NAVES ─────────────────────────────────────────────────────────
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
        PararCorrutinasVista();
        if (camShipActiva != null) { camShipActiva.enabled = false; camShipActiva = null; }
        cam.enabled = true;
        objetivoSnap = null;
        _corView = StartCoroutine(MoverHaciaVistaGeneral());
        _corZoom = StartCoroutine(AjustarZoom(350f));
    }

    void ActivarVistaShip(Transform nave)
    {
        PararCorrutinasVista();
        objetivoSnap = null;
        // true = incluir inactivos: NetworkSetup desactiva la Camera hija en clientes no-propietarios
        var shipCam = nave.GetComponentInChildren<Camera>(true);
        if (shipCam == null) { Debug.LogWarning("[HUD] La nave " + nave.name + " no tiene cámara hija"); return; }
        if (camShipActiva != null && camShipActiva != shipCam) camShipActiva.enabled = false;
        camShipActiva = shipCam;
        shipCam.enabled = true;
        cam.enabled = false;
        Debug.Log("[HUD] Vista nave: " + nave.name);
    }

    void PararCorrutinasVista()
    {
        if (_corView != null) { StopCoroutine(_corView); _corView = null; }
        if (_corZoom != null) { StopCoroutine(_corZoom); _corZoom = null; }
    }

    // ── IMPOSTOR: BOTÓN ───────────────────────────────────────────────────────
    public void PresionarImpostor()
    {
        if (impostorEstado != ImpostorEstado.Listo) return;

        impostorEstado = ImpostorEstado.Activo;
        impostorTimer = SABOTAJE_DURACION;
        btnImpostor.interactable = false;
        imgImpostor.color = ColorActivo;
        txtImpostorLabel.text = "ACTIVO";
        txtImpostorTimer.text = Mathf.CeilToInt(SABOTAJE_DURACION) + "s";

        // Aplicar efectos inmediatamente en esta máquina
        AplicarEfectosSabotaje(SABOTAJE_DURACION);
        // Enviar al resto — GameSpawnManager tiene PhotonView en root de escena MAP
        var spawner = FindFirstObjectByType<GameSpawnManager>();
        if (spawner != null)
            spawner.photonView.RPC(nameof(GameSpawnManager.RPC_IniciarSabotaje),
                                   RpcTarget.Others, SABOTAJE_DURACION);
    }

    public void AplicarEfectosSabotaje(float duracion)
    {
        foreach (var door in FindObjectsByType<DoorTrigger>(FindObjectsSortMode.None))
            door.ForzarCerrar();
        foreach (var alerta in FindObjectsByType<ShipSabotageAlert>(FindObjectsSortMode.None))
            alerta.MostrarAlerta(duracion);
        var go = new GameObject("_SabotajeTimer");
        DontDestroyOnLoad(go);
        go.AddComponent<SabotajeTimerAuxiliar>().Iniciar(duracion);
    }

    void RPC_IniciarSabotaje(float duracion) => AplicarEfectosSabotaje(duracion);

    void ActualizarBotonImpostor()
    {
        if (btnImpostor == null || impostorEstado == ImpostorEstado.Listo) return;

        impostorTimer -= Time.deltaTime;

        if (impostorEstado == ImpostorEstado.Activo)
        {
            txtImpostorTimer.text = Mathf.CeilToInt(Mathf.Max(0f, impostorTimer)) + "s";
            if (impostorTimer <= 0f)
            {
                impostorEstado = ImpostorEstado.Enfriamiento;
                impostorTimer = COOLDOWN_DURACION;
                imgImpostor.color = ColorEnfriamiento;
                txtImpostorLabel.text = "ESPERA";
            }
        }
        else
        {
            txtImpostorTimer.text = Mathf.CeilToInt(Mathf.Max(0f, impostorTimer)) + "s";
            if (impostorTimer <= 0f)
            {
                impostorEstado = ImpostorEstado.Listo;
                imgImpostor.color = ColorListo;
                txtImpostorLabel.text = "SABOTEAR";
                txtImpostorTimer.text = "";
                btnImpostor.interactable = true;
            }
        }
    }

    // ── ROMPER PASADIZO: MÉTODOS ─────────────────────────────────────────────
    void CrearPanelSeleccion(Transform canvasRoot)
    {
        // Fondo muy sutil — los corredores coloreados son visibles a través
        _panelSeleccion = new GameObject("PanelSeleccionPasadizo");
        _panelSeleccion.transform.SetParent(canvasRoot, false);
        _panelSeleccion.layer = 5;
        var rtBg = _panelSeleccion.AddComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero;
        rtBg.anchorMax = Vector2.one;
        rtBg.offsetMin = rtBg.offsetMax = Vector2.zero;
        _panelSeleccion.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);

        // Panel en la mitad IZQUIERDA (derecha libre para ver el mapa)
        var central = CrearImagen(_panelSeleccion.transform, "Central",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(-350f, 0f), new Vector2(520f, 258f),
            new Color(0.07f, 0.07f, 0.10f, 0.97f));

        // Título
        var titGO = new GameObject("Titulo");
        titGO.transform.SetParent(central.transform, false);
        titGO.layer = 5;
        var titTxt = titGO.AddComponent<TextMeshProUGUI>();
        titTxt.text = "SELECCIONA UN PASADIZO";
        titTxt.fontSize = 20;
        titTxt.fontStyle = FontStyles.Bold;
        titTxt.color = new Color(1f, 0.82f, 0.3f);
        titTxt.alignment = TextAlignmentOptions.Center;
        var rtTit = titGO.GetComponent<RectTransform>();
        rtTit.anchorMin = new Vector2(0f, 0.80f);
        rtTit.anchorMax = new Vector2(1f, 1f);
        rtTit.offsetMin = rtTit.offsetMax = Vector2.zero;

        // Opción 1 — naranja (izquierda)
        var btn1 = CrearOpcionPasadizo(central.transform, "PASADIZO 1\nIZQUIERDA",
            ColorPasadizo1, new Vector2(0.03f, 0.18f), new Vector2(0.48f, 0.79f));
        btn1.onClick.AddListener(() => SeleccionarPasadizo("CorridorOsc (1)"));

        // Opción 2 — amarillo (derecha)
        var btn2 = CrearOpcionPasadizo(central.transform, "PASADIZO 2\nDERECHA",
            ColorPasadizo3, new Vector2(0.52f, 0.18f), new Vector2(0.97f, 0.79f));
        btn2.onClick.AddListener(() => SeleccionarPasadizo("CorridorOsc (3)"));

        // Cancelar
        var btnCancelar = CrearBoton(central.transform, "BtnCancelar", "CANCELAR",
            new Vector2(0f, -95f), new Vector2(160f, 38f), new Color(0.35f, 0.12f, 0.12f));
        btnCancelar.onClick.AddListener(CerrarSeleccionPasadizo);

        _panelSeleccion.SetActive(false);
    }

    Button CrearOpcionPasadizo(Transform parent, string texto, Color color,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject("OpcionBtn");
        go.transform.SetParent(parent, false);
        go.layer = 5;
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var bc = btn.colors;
        bc.highlightedColor = new Color(
            Mathf.Min(color.r + 0.15f, 1f),
            Mathf.Min(color.g + 0.15f, 1f),
            Mathf.Min(color.b + 0.1f,  1f), 1f);
        bc.pressedColor = new Color(color.r * 0.55f, color.g * 0.55f, color.b * 0.55f, 1f);
        btn.colors = bc;

        var txtGO = new GameObject("Txt");
        txtGO.transform.SetParent(go.transform, false);
        txtGO.layer = 5;
        var txt = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text = texto;
        txt.fontSize = 19;
        txt.fontStyle = FontStyles.Bold;
        txt.color = new Color(0.05f, 0.05f, 0.05f);
        txt.alignment = TextAlignmentOptions.Center;
        var rtTxt = txtGO.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.offsetMin = rtTxt.offsetMax = Vector2.zero;

        return btn;
    }

    public void AbrirSeleccionPasadizo()
    {
        if (_pasadizoRoto) return;
        _panelSeleccion.SetActive(true);
        ActualizarMarcadorCorredor(ref _marcador1, "CorridorOsc (1)", ColorPasadizo1);
        ActualizarMarcadorCorredor(ref _marcador3, "CorridorOsc (3)", ColorPasadizo3);
        ColorearCorredor("CorridorOsc (1)", ColorPasadizo1, _rendsCorr1, _colsCorr1);
        ColorearCorredor("CorridorOsc (3)", ColorPasadizo3, _rendsCorr3, _colsCorr3);
    }

    void CerrarSeleccionPasadizo()
    {
        _panelSeleccion.SetActive(false);
        if (_marcador1 != null) _marcador1.SetActive(false);
        if (_marcador3 != null) _marcador3.SetActive(false);
        RestaurarCorredor(_rendsCorr1, _colsCorr1);
        RestaurarCorredor(_rendsCorr3, _colsCorr3);
    }

    void SeleccionarPasadizo(string nombre)
    {
        CerrarSeleccionPasadizo();
        _pasadizoRoto = true;
        btnRomper.interactable = false;
        imgRomper.color = RomperUsado;
        txtRomperLabel.text = "ROTO";
        AplicarRotacionPasadizo(nombre);
        var spawnerR = FindFirstObjectByType<GameSpawnManager>();
        if (spawnerR != null)
            spawnerR.photonView.RPC(nameof(GameSpawnManager.RPC_RomperPasadizo),
                                    RpcTarget.Others, nombre);
    }

    public void AplicarRotacionPasadizo(string nombre)
    {
        var go = new GameObject("_RotadorPasadizo");
        go.AddComponent<PasadizoRotadorAuxiliar>().Iniciar(nombre, 80f, 2.5f);
    }

    void RPC_RomperPasadizo(string nombre) => AplicarRotacionPasadizo(nombre);

    void ColorearCorredor(string nombre, Color color,
        System.Collections.Generic.List<Renderer> rendsLst,
        System.Collections.Generic.List<Color> colsLst)
    {
        rendsLst.Clear(); colsLst.Clear();
        var obj = GameObject.Find(nombre);
        if (obj == null) return;
        foreach (var rend in obj.GetComponentsInChildren<Renderer>())
        {
            rendsLst.Add(rend);
            // Guardar color original (URP usa _BaseColor, built-in usa _Color/color)
            Color orig = rend.material.HasProperty("_BaseColor")
                ? rend.material.GetColor("_BaseColor")
                : rend.material.color;
            colsLst.Add(orig);
            if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", color);
            else rend.material.color = color;
        }
    }

    void RestaurarCorredor(
        System.Collections.Generic.List<Renderer> rendsLst,
        System.Collections.Generic.List<Color> colsLst)
    {
        for (int i = 0; i < rendsLst.Count; i++)
        {
            if (rendsLst[i] == null) continue;
            if (rendsLst[i].material.HasProperty("_BaseColor"))
                rendsLst[i].material.SetColor("_BaseColor", colsLst[i]);
            else
                rendsLst[i].material.color = colsLst[i];
        }
        rendsLst.Clear(); colsLst.Clear();
    }

    void ActualizarMarcadorCorredor(ref GameObject marcador, string nombre, Color color)
    {
        if (marcador == null)
        {
            marcador = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marcador.name = "MarcadorCorredor";
            Object.Destroy(marcador.GetComponent<Collider>());
            marcador.transform.localScale = new Vector3(14f, 0.35f, 14f);
            var rend = marcador.GetComponent<Renderer>();
            var mat = new Material(rend.material);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            rend.material = mat;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var obj = GameObject.Find(nombre);
        if (obj != null)
        {
            marcador.transform.position = obj.transform.position + Vector3.up * 22f;
            marcador.SetActive(true);
        }
    }

    // ── FRENOS: BOTÓN ────────────────────────────────────────────────────────
    public void PresionarFreno()
    {
        if (frenosEstado != FrenosEstado.Listo) return;
        frenosEstado = FrenosEstado.Activo;
        frenosTimer = FRENOS_DURACION;
        btnFreno.interactable = false;
        imgFreno.color = FrenoActivo;
        txtFrenoLabel.text = "ACTIVO";
        txtFrenoTimer.text = Mathf.CeilToInt(FRENOS_DURACION) + "s";
        AplicarEfectosFreno(FRENOS_DURACION);
        var spawnerF = FindFirstObjectByType<GameSpawnManager>();
        if (spawnerF != null)
            spawnerF.photonView.RPC(nameof(GameSpawnManager.RPC_IniciarFreno),
                                    RpcTarget.Others, FRENOS_DURACION);
    }

    public void AplicarEfectosFreno(float duracion)
    {
        foreach (var alerta in FindObjectsByType<ShipSabotageAlert>(FindObjectsSortMode.None))
            alerta.MostrarAlertaRelentizar(duracion);
        var go = new GameObject("_FrenosTimer");
        DontDestroyOnLoad(go);
        go.AddComponent<FrenosTimerAuxiliar>().Iniciar(duracion);
    }

    void RPC_IniciarFreno(float duracion) => AplicarEfectosFreno(duracion);

    void ActualizarBotonFreno()
    {
        if (btnFreno == null || frenosEstado == FrenosEstado.Listo) return;
        frenosTimer -= Time.deltaTime;
        if (frenosEstado == FrenosEstado.Activo)
        {
            txtFrenoTimer.text = Mathf.CeilToInt(Mathf.Max(0f, frenosTimer)) + "s";
            if (frenosTimer <= 0f)
            {
                frenosEstado = FrenosEstado.Enfriamiento;
                frenosTimer = FRENOS_COOLDOWN;
                imgFreno.color = FrenoEnfriamiento;
                txtFrenoLabel.text = "ESPERA";
            }
        }
        else
        {
            txtFrenoTimer.text = Mathf.CeilToInt(Mathf.Max(0f, frenosTimer)) + "s";
            if (frenosTimer <= 0f)
            {
                frenosEstado = FrenosEstado.Listo;
                imgFreno.color = FrenoListo;
                txtFrenoLabel.text = "FRENOS";
                txtFrenoTimer.text = "";
                btnFreno.interactable = true;
            }
        }
    }

    // ── MARCADORES ────────────────────────────────────────────────────────────
    void ActualizarMarcadores()
    {
        // Solo mostrar marcadores en vista general (no en vista de nave)
        if (camShipActiva != null)
        {
            marcadorRojo.SetActive(false);
            marcadorAzul.SetActive(false);
            return;
        }

        var roja = BuscarNave("RedShip");
        var azul = BuscarNave("BlueShip");

        if (roja != null)
        {
            marcadorRojo.SetActive(true);
            marcadorRojo.transform.position = roja.position + Vector3.up * 22f;
        }
        else marcadorRojo.SetActive(false);

        if (azul != null)
        {
            marcadorAzul.SetActive(true);
            marcadorAzul.transform.position = azul.position + Vector3.up * 22f;
        }
        else marcadorAzul.SetActive(false);
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────
    Transform BuscarNave(string nombre)
    {
        var go = GameObject.Find(nombre);
        if (go != null) return go.transform;
        go = GameObject.Find(nombre + "(Clone)");
        if (go != null) return go.transform;
        foreach (var ctrl in FindObjectsByType<StarshipControllerPun>(FindObjectsSortMode.None))
            if (ctrl.gameObject.name.StartsWith(nombre))
                return ctrl.transform;
        return null;
    }

    // ── CREAR UI HUD ──────────────────────────────────────────────────────────
    void CrearCanvasHUD()
    {
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

        var panel = CrearImagen(goCanvas.transform, "Panel",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 0f), new Vector2(780f, 70f),
            new Color(0f, 0f, 0f, 0.6f));

        btnRoja    = CrearBoton(panel.transform, "BtnRoja",    "🔴  Nave Roja",
            new Vector2(-260f, 35f), new Vector2(220f, 55f), new Color(0.55f, 0.05f, 0.05f, 1f));
        btnAzul    = CrearBoton(panel.transform, "BtnAzul",    "🔵  Nave Azul",
            new Vector2(0f,    35f), new Vector2(220f, 55f), new Color(0.1f,  0.2f,  0.65f, 1f));
        btnGeneral = CrearBoton(panel.transform, "BtnGeneral", "🗺  Vista General",
            new Vector2(260f,  35f), new Vector2(220f, 55f), new Color(0.15f, 0.35f, 0.15f, 1f));

        btnRoja.onClick.AddListener(IrNaveRoja);
        btnAzul.onClick.AddListener(IrNaveAzul);
        btnGeneral.onClick.AddListener(VistaGeneral);
    }

    void CrearBotonImpostor(Transform canvasRoot)
    {
        var knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

        // ── Botón PUERTAS (centro-derecha, arriba) ───────────────────────────
        var cont = new GameObject("ImpostorBtn");
        cont.transform.SetParent(canvasRoot, false);
        var rt = cont.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot     = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-15f, 65f);
        rt.sizeDelta = new Vector2(110f, 110f);

        imgImpostor = cont.AddComponent<Image>();
        imgImpostor.color = ColorListo;
        if (knob != null) imgImpostor.sprite = knob;

        btnImpostor = cont.AddComponent<Button>();
        btnImpostor.targetGraphic = imgImpostor;
        var bc = btnImpostor.colors;
        bc.highlightedColor = new Color(1f, 0.35f, 0.1f, 1f);
        bc.pressedColor     = new Color(0.5f, 0.05f, 0.02f, 1f);
        bc.disabledColor    = new Color(0.28f, 0.28f, 0.28f, 1f);
        btnImpostor.colors = bc;
        btnImpostor.onClick.AddListener(PresionarImpostor);

        AgregarTextoBoton(cont.transform, ref txtImpostorLabel, ref txtImpostorTimer, "PUERTAS");

        // ── Botón FRENOS (centro-derecha, abajo) ─────────────────────────────
        var cont2 = new GameObject("FrenoBtn");
        cont2.transform.SetParent(canvasRoot, false);
        var rt2 = cont2.AddComponent<RectTransform>();
        rt2.anchorMin = new Vector2(1f, 0.5f);
        rt2.anchorMax = new Vector2(1f, 0.5f);
        rt2.pivot     = new Vector2(1f, 0.5f);
        rt2.anchoredPosition = new Vector2(-15f, -65f);
        rt2.sizeDelta = new Vector2(110f, 110f);

        imgFreno = cont2.AddComponent<Image>();
        imgFreno.color = FrenoListo;
        if (knob != null) imgFreno.sprite = knob;

        btnFreno = cont2.AddComponent<Button>();
        btnFreno.targetGraphic = imgFreno;
        var bc2 = btnFreno.colors;
        bc2.highlightedColor = new Color(0f, 0.70f, 0.85f, 1f);
        bc2.pressedColor     = new Color(0f, 0.18f, 0.28f, 1f);
        bc2.disabledColor    = new Color(0.28f, 0.28f, 0.28f, 1f);
        btnFreno.colors = bc2;
        btnFreno.onClick.AddListener(PresionarFreno);

        AgregarTextoBoton(cont2.transform, ref txtFrenoLabel, ref txtFrenoTimer, "FRENOS");

        // ── Botón ROMPER PASADIZO (centro-derecha, tercero) ──────────────────
        var cont3 = new GameObject("RomperBtn");
        cont3.transform.SetParent(canvasRoot, false);
        var rt3 = cont3.AddComponent<RectTransform>();
        rt3.anchorMin = new Vector2(1f, 0.5f);
        rt3.anchorMax = new Vector2(1f, 0.5f);
        rt3.pivot     = new Vector2(1f, 0.5f);
        rt3.anchoredPosition = new Vector2(-15f, -195f);
        rt3.sizeDelta = new Vector2(110f, 110f);

        imgRomper = cont3.AddComponent<Image>();
        imgRomper.color = RomperListo;
        if (knob != null) imgRomper.sprite = knob;

        btnRomper = cont3.AddComponent<Button>();
        btnRomper.targetGraphic = imgRomper;
        var bc3 = btnRomper.colors;
        bc3.highlightedColor = new Color(0.75f, 0.28f, 0.05f, 1f);
        bc3.pressedColor     = new Color(0.30f, 0.08f, 0.02f, 1f);
        bc3.disabledColor    = new Color(0.22f, 0.22f, 0.22f, 1f);
        btnRomper.colors = bc3;
        btnRomper.onClick.AddListener(AbrirSeleccionPasadizo);

        TextMeshProUGUI lblRomper = null, _tmrRomper = null;
        AgregarTextoBoton(cont3.transform, ref lblRomper, ref _tmrRomper, "ROMPER");
        txtRomperLabel = lblRomper;

        CrearPanelSeleccion(canvasRoot);
    }

    void AgregarTextoBoton(Transform parent,
        ref TextMeshProUGUI labelRef, ref TextMeshProUGUI timerRef, string labelTxt)
    {
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(parent, false);
        lblGO.layer = 5;
        labelRef = lblGO.AddComponent<TextMeshProUGUI>();
        labelRef.text = labelTxt;
        labelRef.fontSize = 12;
        labelRef.fontStyle = FontStyles.Bold;
        labelRef.color = Color.white;
        labelRef.alignment = TextAlignmentOptions.Center;
        var rtLbl = lblGO.GetComponent<RectTransform>();
        rtLbl.anchorMin = new Vector2(0f, 0.45f);
        rtLbl.anchorMax = new Vector2(1f, 0.82f);
        rtLbl.offsetMin = rtLbl.offsetMax = Vector2.zero;

        var timerGO = new GameObject("Timer");
        timerGO.transform.SetParent(parent, false);
        timerGO.layer = 5;
        timerRef = timerGO.AddComponent<TextMeshProUGUI>();
        timerRef.text = "";
        timerRef.fontSize = 28;
        timerRef.fontStyle = FontStyles.Bold;
        timerRef.color = Color.white;
        timerRef.alignment = TextAlignmentOptions.Center;
        var rtTimer = timerGO.GetComponent<RectTransform>();
        rtTimer.anchorMin = new Vector2(0f, 0.08f);
        rtTimer.anchorMax = new Vector2(1f, 0.48f);
        rtTimer.offsetMin = rtTimer.offsetMax = Vector2.zero;
    }

    void CrearMarcadorNave(ref GameObject marcador, string etiqueta, Color color)
    {
        // Punto circular 3D plano visible desde la cámara cenital
        marcador = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marcador.name = "Marcador_" + etiqueta;
        Object.Destroy(marcador.GetComponent<Collider>());
        marcador.transform.localScale = new Vector3(10f, 0.3f, 10f); // disco plano grande

        var rend = marcador.GetComponent<Renderer>();
        var mat = new Material(rend.material);   // copia para no compartir
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", color * 1.5f);
            mat.EnableKeyword("_EMISSION");
        }
        rend.material = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        marcador.SetActive(false);
    }

    // ── CORRUTINAS VISTA ──────────────────────────────────────────────────────
    IEnumerator MoverHaciaVistaGeneral()
    {
        var roja = BuscarNave("RedShip");
        var azul = BuscarNave("BlueShip");
        Vector3 centro = Vector3.zero;
        if (roja != null && azul != null) centro = (roja.position + azul.position) / 2f;
        else if (roja != null) centro = roja.position;
        else if (azul != null) centro = azul.position;

        Vector3 destino = new Vector3(centro.x, transform.position.y, centro.z - 60f);
        float t = 0f; Vector3 origen = transform.position;
        while (t < 1f) { t += Time.deltaTime * 1.5f; transform.position = Vector3.Lerp(origen, destino, t); yield return null; }
    }

    IEnumerator AjustarZoom(float targetSize)
    {
        float origen = cam.orthographicSize; float t = 0f;
        while (t < 1f) { t += Time.deltaTime * 2f; cam.orthographicSize = Mathf.Lerp(origen, targetSize, t); yield return null; }
    }

    // ── UTILIDADES UI ─────────────────────────────────────────────────────────
    GameObject CrearImagen(Transform parent, string nombre,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(parent, false);
        go.layer = 5;
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return go;
    }

    Button CrearBoton(Transform parent, string nombre, string texto,
        Vector2 pos, Vector2 size, Color color)
    {
        var go = CrearImagen(parent, nombre,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size, color);
        var img = go.GetComponent<Image>();
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var c = btn.colors;
        c.highlightedColor = new Color(color.r + 0.2f, color.g + 0.2f, color.b + 0.2f, 1f);
        c.pressedColor     = new Color(color.r - 0.1f, color.g - 0.1f, color.b - 0.1f, 1f);
        btn.colors = c;
        var goTxt = new GameObject("Text");
        goTxt.transform.SetParent(go.transform, false);
        goTxt.layer = 5;
        var txt = goTxt.AddComponent<TextMeshProUGUI>();
        txt.text = texto; txt.fontSize = 16;
        txt.color = Color.white; txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        var rt = goTxt.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return btn;
    }
}

public class SabotajeTimerAuxiliar : MonoBehaviour
{
    public void Iniciar(float duracion) => StartCoroutine(Esperar(duracion));

    System.Collections.IEnumerator Esperar(float duracion)
    {
        yield return new WaitForSeconds(duracion);
        foreach (var door in FindObjectsByType<DoorTrigger>(FindObjectsSortMode.None))
            door.LiberarCierre();
        Destroy(gameObject);
    }
}

// Anima la rotación de un corredor 80° en Y durante 2.5 s (ease in/out).
public class PasadizoRotadorAuxiliar : MonoBehaviour
{
    public void Iniciar(string nombreCorredor, float angulos, float duracion)
    {
        var corredor = GameObject.Find(nombreCorredor);
        if (corredor != null)
        {
            // Trigger activo que redirige la velocidad de las naves hacia afuera.
            // Necesario porque StarshipControllerPun sobreescribe linearVelocity cada
            // FixedUpdate, anulando la respuesta de colisión de PhysX.
            var muroGO = new GameObject("MuroPasadizo_" + nombreCorredor);
            muroGO.transform.position = corredor.transform.position;
            muroGO.AddComponent<MuroInvisiblePasadizo>();

            StartCoroutine(Rotar(corredor.transform, angulos, duracion));
        }
        else
            Destroy(gameObject);
    }

    System.Collections.IEnumerator Rotar(Transform t, float angulos, float duracion)
    {
        Quaternion inicio = t.rotation;
        Quaternion fin = Quaternion.AngleAxis(angulos, Vector3.up) * inicio;
        float elapsed = 0f;
        while (elapsed < duracion)
        {
            elapsed += Time.deltaTime;
            t.rotation = Quaternion.Slerp(inicio, fin,
                Mathf.SmoothStep(0f, 1f, elapsed / duracion));
            yield return null;
        }
        t.rotation = fin;
        Destroy(gameObject);
    }
}

// Bloquea las naves usando un trigger esférico que redirige su velocidad hacia afuera.
// Un BoxCollider sólido falla porque el controlador de nave sobreescribe linearVelocity
// cada FixedUpdate, anulando la respuesta de colisión normal de PhysX.
public class MuroInvisiblePasadizo : MonoBehaviour
{
    void Awake()
    {
        var col = gameObject.AddComponent<SphereCollider>();
        col.radius = 16f;   // cubre todo el largo del corredor (~28 u) más margen
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        Repeler(other, 18f);   // impulso fuerte al entrar
    }

    void OnTriggerStay(Collider other)
    {
        Repeler(other, 0f);    // mantiene la nave fuera mientras siga presionando W
    }

    void Repeler(Collider other, float velMinima)
    {
        var rb = other.attachedRigidbody;
        if (rb == null) return;
        Vector3 dir = other.transform.position - transform.position;
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.right;
        dir.Normalize();
        float mag = Mathf.Max(rb.linearVelocity.magnitude, velMinima);
        rb.linearVelocity = dir * mag;
    }
}

// Ralentiza todas las naves al activarse y restaura sus velocidades al expirar.
public class FrenosTimerAuxiliar : MonoBehaviour
{
    struct EstadoNave { public StarshipControllerPun ctrl; public float velOriginal; }
    System.Collections.Generic.List<EstadoNave> _estados = new();

    public void Iniciar(float duracion)
    {
        foreach (var ctrl in FindObjectsByType<StarshipControllerPun>(FindObjectsSortMode.None))
        {
            _estados.Add(new EstadoNave { ctrl = ctrl, velOriginal = ctrl.speed });
            ctrl.speed *= 0.30f;
        }
        StartCoroutine(Esperar(duracion));
    }

    System.Collections.IEnumerator Esperar(float duracion)
    {
        yield return new WaitForSeconds(duracion);
        foreach (var e in _estados)
            if (e.ctrl != null) e.ctrl.speed = e.velOriginal;
        Destroy(gameObject);
    }
}
