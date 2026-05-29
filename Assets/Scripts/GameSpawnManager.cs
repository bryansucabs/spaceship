using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("Puntos de Aparición")]
    public Transform spawnPointAzul;      // Posición de salida para Jugador 1 (RedShip)
    public Transform spawnPointRoja;      // Posición de salida para Jugador 2 (BlueShip)
    
    [Header("Prefabs (Nombres exactos en Resources)")]
    public string redShipPrefab = "RedShip";    // Nave que se controla con Celular/UDP
    public string blueShipPrefab = "BlueShip";  // Nave que se controla con Teclado
    
    private bool yaNaci = false;
    private UDPManagerPUN udpManager;

    void Start()
    {
        VerificarYSpawnear();
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        VerificarYSpawnear();
    }

    private void VerificarYSpawnear()
    {
        if (yaNaci || !PhotonNetwork.InRoom) return;

        int conectados = PhotonNetwork.CurrentRoom.PlayerCount;
        int maximos = PhotonNetwork.CurrentRoom.MaxPlayers; // Ahora es 2

        // Spawnea de inmediato si la sala está llena (2/2) o si estás testeando solo en el editor (1)
        if (conectados == maximos || conectados == 1)
        {
            yaNaci = true;
            SpawnearMiNave();
        }
    }
    
    void SpawnearMiNave()
    {
        // Evaluamos el orden estricto de llegada mediante la lista de Photon:
        // Índice [0] -> Primer usuario en entrar (PC 1 - MasterClient) -> RedShip (Celular)
        // Índice [1] -> Segundo usuario en entrar (PC 2 - Client)       -> BlueShip (Teclado)

        // 1. SI SOY EL PRIMER JUGADOR (PC 1)
        if (PhotonNetwork.LocalPlayer.Equals(PhotonNetwork.PlayerList[0]))
        {
            GameObject miNave = PhotonNetwork.Instantiate(redShipPrefab, spawnPointAzul.position, spawnPointAzul.rotation);
            
            var controller = miNave.GetComponent<StarshipControllerPun>();
            if (controller != null)
            {
                controller.esJugadorTeclado = false; // Desactiva teclado, usará giroscopio
                controller.autoavance = true;
            }
            
            // Conectamos el script de Python/UDP con el controlador de esta nave
            udpManager = miNave.GetComponent<UDPManagerPUN>();
            if (udpManager != null && controller != null)
            {
                udpManager.nave = controller;
            }
            
            Debug.Log("[SPAWN] PC 1 detectada. Instanciada RedShip configurada para control por Celular (UDP).");
            
            // Al ser solo 2 jugadores, el Host puede cerrar la sala de inmediato para evitar intrusos
            FinalizarLobby();
        }
        // 2. SI SOY EL SEGUNDO JUGADOR (PC 2)
        else if (PhotonNetwork.PlayerList.Length > 1 && PhotonNetwork.LocalPlayer.Equals(PhotonNetwork.PlayerList[1]))
        {
            GameObject miNave = PhotonNetwork.Instantiate(blueShipPrefab, spawnPointRoja.position, spawnPointRoja.rotation);
            
            var controller = miNave.GetComponent<StarshipControllerPun>();
            if (controller != null)
            {
                controller.esJugadorTeclado = true; // Activa la lectura de flechas/WASD en el teclado
                controller.autoavance = true;
            }
            
            Debug.Log("[SPAWN] PC 2 detectada. Instanciada BlueShip configurada para control por Teclado.");
        }
    }

    void FinalizarLobby()
    {
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        Debug.Log("[Lobby] Sala cerrada. Grupo de 2 pilotos completado.");
    }
}