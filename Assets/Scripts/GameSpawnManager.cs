using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

public class GameSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("Puntos de Aparición")]
    public Transform spawnPointAzul;      // Para nave Roja (Master/Celular)
    public Transform spawnPointRoja;      // Para nave Azul (Cliente/Teclado)
    public Transform spawnPointOverlord;  // Para Overlord
    
    [Header("Prefabs (Nombres exactos en Resources)")]
    public string redShipPrefab = "RedShip";    // Nave del Master (celular)
    public string blueShipPrefab = "BlueShip";  // Nave del Cliente (teclado)
    public string overlordPrefab = "Overlord";  // Overlord
    
    private bool spawned = false;
    private UDPManagerPUN udpManager;

    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            StartCoroutine(SpawnWithDelay());
        }
    }
    
    IEnumerator SpawnWithDelay()
    {
        // Esperar un momento para asegurar que todos estén listos
        yield return new WaitForSeconds(0.5f);
        
        if (!spawned && PhotonNetwork.InRoom)
        {
            SpawnMyPlayer();
            spawned = true;
        }
    }
    
    void SpawnMyPlayer()
    {
        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        bool isMaster = PhotonNetwork.IsMasterClient;
        
        GameObject miNave = null;
        
        // LÓGICA DE SPAWN:
        // MasterClient (jugador 1) -> RedShip (celular)
        // Cliente (jugador 2) -> BlueShip (teclado)
        // Cuando llegue el 3er jugador (MasterClient instancia Overlord)
        
        if (isMaster && currentPlayers == 1)
        {
            // PRIMER JUGADOR (MasterClient) - Usa celular
            miNave = SpawnShip(redShipPrefab, spawnPointAzul.position, spawnPointAzul.rotation);
            var controller = miNave.GetComponent<StarshipControllerPun>();
            if (controller != null)
            {
                controller.esJugadorTeclado = false;
                controller.autoavance = true;
            }
            
            // Configurar UDPManager
            udpManager = miNave.GetComponent<UDPManagerPUN>();
            if (udpManager != null && controller != null)
            {
                udpManager.nave = controller;
            }
            
            Debug.Log($"MasterClient spawn como RedShip (Celular) - Jugador: {actorNumber}");
        }
        else if (!isMaster && currentPlayers == 2)
        {
            // SEGUNDO JUGADOR (Cliente) - Usa teclado
            miNave = SpawnShip(blueShipPrefab, spawnPointRoja.position, spawnPointRoja.rotation);
            var controller = miNave.GetComponent<StarshipControllerPun>();
            if (controller != null)
            {
                controller.esJugadorTeclado = true;
                controller.autoavance = true;
            }
            
            Debug.Log($"Client spawn como BlueShip (Teclado) - Jugador: {actorNumber}");
        }
        
        // El MasterClient instancia el Overlord cuando llegue el 3er jugador
        // Esto se maneja en OnPlayerEnteredRoom
    }
    
    GameObject SpawnShip(string prefabName, Vector3 position, Quaternion rotation)
    {
        if (PhotonNetwork.IsConnected)
        {
            return PhotonNetwork.Instantiate(prefabName, position, rotation);
        }
        else
        {
            Debug.LogError($"No se pudo instanciar {prefabName}: Photon no está conectado");
            return null;
        }
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        
        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
        
        Debug.Log($"Jugador {newPlayer.ActorNumber} entró. Total: {currentPlayers}/{maxPlayers}");
        
        // Solo el MasterClient instancia el Overlord cuando llegue el 3er jugador
        if (PhotonNetwork.IsMasterClient && currentPlayers == maxPlayers && maxPlayers == 3)
        {
            SpawnOverlord();
        }
    }
    
    void SpawnOverlord()
    {
        Debug.Log("Spawneando Overlord...");
        GameObject overlord = PhotonNetwork.Instantiate(overlordPrefab, spawnPointOverlord.position, spawnPointOverlord.rotation);
        
        // Cerrar la sala para que no entren más jugadores
        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;
        
        Debug.Log("Overlord instanciado. Sala cerrada.");
    }
    
    // Para debuggear
    void OnGUI()
    {
        if (PhotonNetwork.InRoom)
        {
            GUILayout.Label($"Jugadores: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
            GUILayout.Label($"MasterClient: {PhotonNetwork.IsMasterClient}");
        }
    }
}