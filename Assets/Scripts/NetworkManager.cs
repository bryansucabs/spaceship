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

        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = "1.0_TunelAsimetrico";
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = "sa";

        if (textoEstado != null) textoEstado.text = "Conectando al servidor maestro de Photon...";
        Debug.Log($"AppVersion: {PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion} | Región: sa | Dispositivo: {(SystemInfo.deviceType == DeviceType.Handheld ? "Móvil" : "PC")}");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        if (textoEstado != null) textoEstado.text = "Conectado. Buscando sala competitiva...";

        RoomOptions opcionesSala = new RoomOptions() { MaxPlayers = 3 };

        if (SystemInfo.deviceType == DeviceType.Handheld)
        {
            if (textoEstado != null) textoEstado.text = "Buscando la sala de la PC en el celular...";
            PhotonNetwork.JoinRoom("TunelCompetitivo");
        }
        else
        {
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
            if (textoEstado != null) textoEstado.text = $"Conectado. Esperando al Host... ({conectados}/{maximos})";
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        if (textoEstado != null)
            textoEstado.text = "Error: No se encontró la sala. Asegúrate de que la PC abrió el juego primero.";
    }

    public void IniciarJuego()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }
}
