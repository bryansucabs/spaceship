using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
public class NetworkManager : MonoBehaviourPunCallbacks
{
    public Button botonEmpezar;
    public TextMeshProUGUI textoEstado;
    public string escenaDestino = "MapTest";

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        botonEmpezar.interactable = false;
        if (textoEstado != null) textoEstado.text = "Conectando...";
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        if (textoEstado != null) textoEstado.text = "Buscando sala...";
        PhotonNetwork.JoinOrCreateRoom("TunelCompetitivo", new RoomOptions() { MaxPlayers = 2 }, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            botonEmpezar.interactable = true;
            if (textoEstado != null) textoEstado.text = "Eres el Host. Esperando rival o pulsa Empezar.";
        }
        else
        {
            botonEmpezar.interactable = false;
            if (textoEstado != null) textoEstado.text = "Conectado. Esperando a que el Host inicie...";
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // Si Photon falla, habilitar el botón para test offline
        botonEmpezar.interactable = true;
        if (textoEstado != null) textoEstado.text = $"Sin red ({cause}). Modo local activado.";
    }

    public void IniciarJuego()
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(escenaDestino);
        }
        else
        {
            // Fallback offline: activa modo simulado de Photon y carga la escena
            PhotonNetwork.OfflineMode = true;
            SceneManager.LoadScene(escenaDestino);
        }
    }
}