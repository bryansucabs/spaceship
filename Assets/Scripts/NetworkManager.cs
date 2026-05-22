using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using TMPro; 
public class NetworkManager : MonoBehaviourPunCallbacks
{
    public Button botonEmpezar; // Arrastra tu botón aquí desde el Inspector
    public TextMeshProUGUI textoEstado; // Opcional: un texto para ver qué pasa

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true; 
        botonEmpezar.interactable = false; // Desactiva el botón al inicio
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinOrCreateRoom("TunelCompetitivo", new RoomOptions() { MaxPlayers = 2 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        // Si somos el Master, encendemos el botón de Start
        if (PhotonNetwork.IsMasterClient)
        {
            botonEmpezar.interactable = true;
            if (textoEstado != null) textoEstado.text = "Eres el Host. Esperando rival o pulsa Empezar.";
        }
        else
        {
            // El jugador 2 no puede pulsar el botón, solo espera
            botonEmpezar.interactable = false;
            if (textoEstado != null) textoEstado.text = "Conectado. Esperando a que el Host inicie...";
        }
    }

    // ESTA ES LA FUNCIÓN DEL BOTÓN
    public void IniciarJuego()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Como AutomaticallySyncScene está en true, esto arrastrará al jugador 2 también
            PhotonNetwork.LoadLevel("GameScene"); // Pon el nombre exacto de tu escena de juego
        }
    }
}