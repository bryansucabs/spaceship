using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    void Start()
    {
        Debug.Log("Conectando al servidor de Photon...");
        // Nos conectamos usando la configuración que pusimos al importar PUN 2
        PhotonNetwork.ConnectUsingSettings(); 
    }

    // Se llama automáticamente cuando nos conectamos al servidor maestro
    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado al servidor. Entrando al Lobby...");
        PhotonNetwork.JoinLobby();
    }

    // Se llama cuando entramos al lobby general
    public override void OnJoinedLobby()
    {
        Debug.Log("En el Lobby. Buscando sala...");
        // Intentamos unirnos a una sala llamada "Tunel1", si no existe, la crea.
        RoomOptions roomOptions = new RoomOptions() { MaxPlayers = 3 };
        PhotonNetwork.JoinOrCreateRoom("Tunel1", roomOptions, TypedLobby.Default);
    }

    // Se llama cuando entramos exitosamente a la sala
    public override void OnJoinedRoom()
    {
        Debug.Log("¡Unido a la sala: " + PhotonNetwork.CurrentRoom.Name + "!");
        Debug.Log("Jugadores en la sala: " + PhotonNetwork.CurrentRoom.PlayerCount);

        // Si somos el primer jugador (el host), cargamos la escena del juego.
        // Los demás jugadores cargarán esta escena automáticamente.
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("SampleScene"); // Pon aquí el nombre exacto de tu escena de juego
        }
    }
}