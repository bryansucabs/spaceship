using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro; 

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public Button botonEmpezar; 
    public TextMeshProUGUI textoEstado; 

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true; 
        if (botonEmpezar != null) botonEmpezar.interactable = false; 
        
        // Sincronizamos la versión para que PC y móvil no se aíslen en servidores distintos
        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = "1.0_TunelAsimetrico";
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "sa"; 
        
        if (textoEstado != null) textoEstado.text = "Conectando al servidor maestro de Photon...";
        Debug.Log($"=== INFO PHOTON ===");
        Debug.Log($"AppVersion: {PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion}");
        Debug.Log($"Región fija: {PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion}");
        Debug.Log($"Dispositivo: {(SystemInfo.deviceType == DeviceType.Handheld ? "Móvil" : "PC")}");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        if (textoEstado != null) textoEstado.text = "Conectado. Buscando sala competitiva...";
        
        RoomOptions opcionesSala = new RoomOptions() { MaxPlayers = 3 }; // 2 Pilotos + 1 Overlord

        // FILTRO DE PLATAFORMA: Evita que el celular cree salas huérfanas por error
        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            if (textoEstado != null) textoEstado.text = "Buscando la sala de la PC en el celular...";
            PhotonNetwork.JoinRoom("TunelCompetitivo");
        }
        else
        {
            // Las PCs sí pueden crear la sala si no existe
            PhotonNetwork.JoinOrCreateRoom("TunelCompetitivo", opcionesSala, TypedLobby.Default);
        }
    }

    public override void OnJoinedRoom()
    {
        ActualizarEstadoLobby();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        ActualizarEstadoLobby();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        ActualizarEstadoLobby();
    }

    private void ActualizarEstadoLobby()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        int conectados = PhotonNetwork.CurrentRoom.PlayerCount;
        int maximos = PhotonNetwork.CurrentRoom.MaxPlayers;

        if (PhotonNetwork.IsMasterClient)
        {
            // SEGURO DE INICIO: El Host solo puede pulsar "Empezar" si están los 3 dispositivos listos
            if (conectados == maximos)
            {
                if (botonEmpezar != null) botonEmpezar.interactable = true;
                if (textoEstado != null) textoEstado.text = "¡Sala Llena (3/3)! Pulsa Empezar para iniciar la partida.";
            }
            else
            {
                if (botonEmpezar != null) botonEmpezar.interactable = false;
                if (textoEstado != null) textoEstado.text = $"Eres el Host. Esperando jugadores... ({conectados}/{maximos})";
            }
        }
        else
        {
            if (botonEmpezar != null) botonEmpezar.interactable = false;
            if (textoEstado != null) textoEstado.text = $"Conectado con éxito. Esperando al Host... ({conectados}/{maximos})";
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (textoEstado != null) 
            textoEstado.text = "Error: No se encontró la sala. Asegúrate de que la PC abrió el juego primero.";
    }

    // Vincula esta función al botón de Start UI en el inspector
    public void IniciarJuego()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            // Carga la escena en el Host y arrastra a los clientes automáticamente
            PhotonNetwork.LoadLevel("GameScene"); 
        }
    }
}