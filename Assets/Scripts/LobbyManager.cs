using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    // ── Referencias UI ────────────────────────────────────────────────────────
    GameObject panelConexion;
    GameObject panelSeleccion;
    GameObject popup;
    Button     btnConectar;
    TextMeshProUGUI textoEstado;
    Button btnRedShip, btnBlueShip, btnOverlord;
    TextMeshProUGUI textoSala;
    Button btnIniciar;
    TextMeshProUGUI popupMensaje;
    Button btnCerrarPopup;

    // ── Colores botón ELEGIR ──────────────────────────────────────────────────
    readonly Color btnNormal   = Color.white;
    readonly Color btnElegido  = new Color(0.2f, 0.2f, 0.2f, 1f);
    readonly Color btnOcupado  = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    // ── Detección de dispositivo y hardware ───────────────────────────────────
    bool esTablet;
    bool tieneCamara;
    bool giroConectado = false;

    Thread   giroThread;
    UdpClient giroClient;

    const string PROP_ROL = "rol";

    // ── AWAKE: conecta referencias ────────────────────────────────────────────
    void Awake()
    {
        // esTablet = SystemInfo.deviceType == DeviceType.Handheld; // TEMPORAL — re-habilitar antes del APK
        esTablet = false;
        tieneCamara = WebCamTexture.devices.Length > 0;

        var canvas = GameObject.Find("Canvas").transform;

        panelConexion  = canvas.Find("PanelConexion").gameObject;
        panelSeleccion = canvas.Find("PanelSeleccion").gameObject;
        popup          = canvas.Find("Popup").gameObject;

        btnConectar  = panelConexion.transform.Find("BtnConectar").GetComponent<Button>();
        textoEstado  = panelConexion.transform.Find("TextoEstado").GetComponent<TextMeshProUGUI>();

        btnRedShip  = panelSeleccion.transform.Find("CardRed/BtnElegir").GetComponent<Button>();
        btnBlueShip = panelSeleccion.transform.Find("CardBlue/BtnElegir").GetComponent<Button>();
        btnOverlord = panelSeleccion.transform.Find("CardOverlord/BtnElegir").GetComponent<Button>();
        textoSala   = panelSeleccion.transform.Find("TextoSala").GetComponent<TextMeshProUGUI>();
        btnIniciar  = panelSeleccion.transform.Find("BtnIniciar").GetComponent<Button>();

        popupMensaje   = popup.transform.Find("Panel/Mensaje").GetComponent<TextMeshProUGUI>();
        btnCerrarPopup = popup.transform.Find("Panel/BtnCerrar").GetComponent<Button>();

        btnConectar.onClick.AddListener(Conectar);
        btnRedShip.onClick.AddListener(IntentarRedShip);
        btnBlueShip.onClick.AddListener(IntentarBlueShip);
        btnOverlord.onClick.AddListener(IntentarOverlord);
        btnIniciar.onClick.AddListener(IniciarJuego);
        btnCerrarPopup.onClick.AddListener(() => popup.SetActive(false));
    }

    void Start()
    {
        panelConexion.SetActive(true);
        panelSeleccion.SetActive(false);
        popup.SetActive(false);
        btnIniciar.interactable = false;

        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = "1.0_TunelAsimetrico";
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "sa";

        // Empieza a escuchar en el puerto del giroscopio desde el inicio
        IniciarListenerGiroscopio();
    }

    // ── LISTENER GIROSCOPIO (UDP 11011) ───────────────────────────────────────
    void IniciarListenerGiroscopio()
    {
        giroThread = new Thread(() =>
        {
            try
            {
                giroClient = new UdpClient();
                giroClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                giroClient.Client.Bind(new IPEndPoint(IPAddress.Any, 11011));
                IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    byte[] data = giroClient.Receive(ref ep);
                    string text = Encoding.UTF8.GetString(data);
                    if (text.Contains("|"))
                        giroConectado = true;  // App del giroscopio está enviando datos
                }
            }
            catch { }
        });
        giroThread.IsBackground = true;
        giroThread.Start();
    }

    void OnDestroy()
    {
        try { giroClient?.Close(); } catch { }
        try { giroThread?.Abort(); } catch { }
    }

    // ── CONECTAR A PHOTON ────────────────────────────────────────────────────
    public void Conectar()
    {
        btnConectar.interactable = false;
        textoEstado.text = "Conectando...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        textoEstado.text = "Buscando sala...";
        PhotonNetwork.JoinOrCreateRoom("TunelCompetitivo", new RoomOptions { MaxPlayers = 3 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        panelConexion.SetActive(false);
        panelSeleccion.SetActive(true);
        ActualizarCards();
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
        => PhotonNetwork.CreateRoom("TunelCompetitivo", new RoomOptions { MaxPlayers = 3 });

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        btnConectar.interactable = true;
        textoEstado.text = "Error de conexión. Intenta de nuevo.";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        btnConectar.interactable = true;
        textoEstado.text = "Sin conexión. Intenta de nuevo.";
    }

    // ── INTENTOS DE SELECCIÓN CON VALIDACIÓN ─────────────────────────────────
    void IntentarRedShip()
    {
        if (esTablet)
        {
            MostrarPopup("Este rol es solo para PC.\n\nLa tablet debe elegir ATACANTE.");
            return;
        }
        ElegirRol("redship");
    }

    void IntentarBlueShip()
    {
        if (esTablet)
        {
            MostrarPopup("Este rol es solo para PC.\n\nLa tablet debe elegir ATACANTE.");
            return;
        }
        ElegirRol("blueship");
    }

    void IntentarOverlord()
    {
        // TEMPORAL: restricción deshabilitada para pruebas en PC
        // if (!esTablet)
        // {
        //     MostrarPopup("Este rol es solo para tablet Android.\n\nDesde PC elige RED SHIP o BLUE SHIP.");
        //     return;
        // }
        ElegirRol("overlord");
    }

    void MostrarPopup(string mensaje)
    {
        popupMensaje.text = mensaje;
        popup.SetActive(true);
    }

    // ── SELECCIÓN REAL ───────────────────────────────────────────────────────
    void ElegirRol(string rol)
    {
        if (RolTomadoPorOtro(rol)) return;
        Hashtable props = new Hashtable() { { PROP_ROL, rol } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        => ActualizarCards();
    public override void OnPlayerEnteredRoom(Player newPlayer) => ActualizarCards();
    public override void OnPlayerLeftRoom(Player otherPlayer)  => ActualizarCards();

    void ActualizarCards()
    {
        string miRol = MiRol();
        ActualizarBoton(btnRedShip,  "redship",  miRol);
        ActualizarBoton(btnBlueShip, "blueship", miRol);
        ActualizarBoton(btnOverlord, "overlord", miRol);

        int elegidos = ContarElegidos();
        bool yoElegi = !string.IsNullOrEmpty(miRol);
        btnIniciar.interactable = PhotonNetwork.IsMasterClient && yoElegi;
        textoSala.text = elegidos == 0
            ? "Elige tu nave para comenzar"
            : $"{elegidos}/{PhotonNetwork.CurrentRoom.PlayerCount} listos";
    }

    void ActualizarBoton(Button btn, string rol, string miRol)
    {
        bool esMio         = miRol == rol;
        bool tomadoPorOtro = RolTomadoPorOtro(rol);

        var colors = btn.colors;
        var txt    = btn.GetComponentInChildren<TextMeshProUGUI>();

        if (esMio)
        {
            btn.interactable   = false;
            colors.normalColor = btnElegido;
            if (txt) txt.text  = "✓ Elegido";
        }
        else if (tomadoPorOtro)
        {
            btn.interactable   = false;
            colors.normalColor = btnOcupado;
            if (txt) txt.text  = "Ocupado";
        }
        else
        {
            btn.interactable   = true;
            colors.normalColor = btnNormal;
            if (txt) txt.text  = "ELEGIR";
        }

        btn.colors = colors;
    }

    // ── INICIAR ───────────────────────────────────────────────────────────────
    public void IniciarJuego()
    {
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("MAP");
    }

    // ── HELPERS ───────────────────────────────────────────────────────────────
    bool RolTomado(string rol)
    {
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.CustomProperties.ContainsKey(PROP_ROL) && p.CustomProperties[PROP_ROL].ToString() == rol)
                return true;
        return false;
    }

    bool RolTomadoPorOtro(string rol)
    {
        foreach (var p in PhotonNetwork.PlayerList)
            if (!p.IsLocal && p.CustomProperties.ContainsKey(PROP_ROL) && p.CustomProperties[PROP_ROL].ToString() == rol)
                return true;
        return false;
    }

    string MiRol()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(PROP_ROL))
            return PhotonNetwork.LocalPlayer.CustomProperties[PROP_ROL].ToString();
        return "";
    }

    int ContarElegidos()
    {
        int n = 0;
        foreach (var p in PhotonNetwork.PlayerList)
            if (p.CustomProperties.ContainsKey(PROP_ROL)) n++;
        return n;
    }
}
