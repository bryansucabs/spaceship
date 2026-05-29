using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("Puntos de Aparición")]
    public Transform spawnPointAzul;      // Posición de salida para Jugador 1 (RedShip)
    public Transform spawnPointRoja;      // Posición de salida para Jugador 2 (BlueShip)
    public Transform spawnPointOverlord;  // Posición aérea para el Overlord táctil
    
    [Header("Prefabs (Nombres exactos en Resources)")]
    public string redShipPrefab = "RedShip";    
    public string blueShipPrefab = "BlueShip";  
    public string overlordPrefab = "Overlord";  
    
    private bool yaNaci = false;
    private UDPManagerPUN udpManager;

    void Start()
    {
        // Al cargar la escena, intentamos spawnear inmediatamente si la sala ya está completa
        VerificarYSpawnear();
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        // Si por lag un jugador tardó en cargar la escena, volvemos a evaluar el nacimiento
        VerificarYSpawnear();
    }

    private void VerificarYSpawnear()
    {
        if (yaNaci || !PhotonNetwork.InRoom) return;

        int conectados = PhotonNetwork.CurrentRoom.PlayerCount;
        int maximos = PhotonNetwork.CurrentRoom.MaxPlayers;

        // Si estamos los 3 completos (o si estás probando tú solo en el editor)
        if (conectados == maximos || conectados == 1)
        {
            yaNaci = true;
            SpawnearMiRolAsimetrico();
        }
    }
    
    void SpawnearMiRolAsimetrico()
    {
        // Nos guiamos por el orden de la lista ordenada de Photon (PlayerList)
        // Índice [0] -> Primer usuario en conectar (PC 1 - Mando Celular UDP)
        // Índice [1] -> Segundo usuario en conectar (PC 2 - Teclado normal)
        // Índice [2] -> Tercer usuario en conectar (Tablet / Celular del Overlord)

        // 1. EVALUAR SI SOY EL JUGADOR 1
        if (PhotonNetwork.LocalPlayer.Equals(PhotonNetwork.PlayerList[0]))
        {
            GameObject miNave = PhotonNetwork.Instantiate(redShipPrefab, spawnPointAzul.position, spawnPointAzul.rotation);
            
            var controller = miNave.GetComponent<StarshipControllerPun>();
            if (controller != null)
            {
                controller.esJugadorTeclado = false;
                controller.autoavance = true;
            }
            
            udpManager = miNave.GetComponent<UDPManagerPUN>();
            if (udpManager != null && controller != null)
            {
                udpManager.nave = controller;
            }
            
            Debug.Log("[SPAWN] ¡Yo soy el Jugador 1! Nací como RedShip (Mando Celular UDP).");
        }
        // 2. EVALUAR SI SOY EL JUGADOR 2
        else if (PhotonNetwork.PlayerList.Length > 1 && PhotonNetwork.LocalPlayer.Equals(PhotonNetwork.PlayerList[1]))
        {
            GameObject miNave = PhotonNetwork.Instantiate(blueShipPrefab, spawnPointRoja.position, spawnPointRoja.rotation);
            
            var controller = miNave.GetComponent<StarshipControllerPun>();
            if (controller != null)
            {
                controller.esJugadorTeclado = true;
                controller.autoavance = true;
            }
            
            Debug.Log("[SPAWN] ¡Yo soy el Jugador 2! Nací como BlueShip (Teclado).");
        }
        // 3. EVALUAR SI SOY EL JUGADOR 3 (El Overlord táctil)
        else if (PhotonNetwork.PlayerList.Length > 2 && PhotonNetwork.LocalPlayer.Equals(PhotonNetwork.PlayerList[2]))
        {
            // El tercer dispositivo crea su cámara táctil e interfaz. 
            // Esto NO se ejecuta en el Host, por lo que el Host jamás perderá su pantalla original.
            PhotonNetwork.Instantiate(overlordPrefab, spawnPointOverlord.position, spawnPointOverlord.rotation);
            Debug.Log("[SPAWN] ¡Yo soy el Jugador 3! Nací como el Overlord Táctil.");

            // Si soy el Host cierro la sala, si no, le pido amablemente al master que lo haga
            if (PhotonNetwork.IsMasterClient) FinalizarLobby();
            else photonView.RPC("RPC_FinalizarLobby", RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    void RPC_FinalizarLobby()
    {
        if (PhotonNetwork.IsMasterClient) FinalizarLobby();
    }

    void FinalizarLobby()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        Debug.Log("[Lobby] Sala cerrada con éxito por seguridad de juego.");
    }
}