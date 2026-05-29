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
        
        // Sincronizamos la versión para este modo específico de 2 jugadores
        PhotonNetwork.PhotonServerSettings.AppSettings.AppVersion = "1.0_Tunel2Jugadores";
        
        if (textoEstado != null) textoEstado.text = "Conectando al servidor maestro de Photon...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        if (textoEstado != null) textoEstado.text = "Conectado. Buscando sala de 2 pilotos...";
        
        // CONFIGURACIÓN VITAL: Máximo 2 jugadores (Nave Celular + Nave Teclado)
        RoomOptions opcionesSala = new RoomOptions() { MaxPlayers = 2 }; 
        PhotonNetwork.JoinOrCreateRoom("TunelCompetitivo2P", opcionesSala, TypedLobby.Default);
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
        int maximos = PhotonNetwork.CurrentRoom.MaxPlayers; // Será 2

        if (PhotonNetwork.IsMasterClient)
        {
            // El Host (PC 1) solo puede pulsar "Empezar" si ya se conectó su rival (PC 2)
            if (conectados == maximos)
            {
                if (botonEmpezar != null) botonEmpezar.interactable = true;
                if (textoEstado != null) textoEstado.text = "¡Rival conectado (2/2)! Pulsa Empezar para iniciar el vuelo.";
            }
            else
            {
                if (botonEmpezar != null) botonEmpezar.interactable = false;
                if (textoEstado != null) textoEstado.text = $"Eres el Host. Esperando al segundo piloto... ({conectados}/{maximos})";
            }
        }
        else
        {
            if (botonEmpezar != null) botonEmpezar.interactable = false;
            if (textoEstado != null) textoEstado.text = $"Conectado con éxito. Esperando que el Host inicie... ({conectados}/{maximos})";
        }
    }

    public void IniciarJuego()
    {
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            if (textoEstado != null) textoEstado.text = "Cargando el túnel para ambos pilotos...";
            PhotonNetwork.LoadLevel("GameScene"); 
        }
    }
}