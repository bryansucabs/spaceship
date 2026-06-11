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
        string miRol = "";
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("rol"))
            miRol = PhotonNetwork.LocalPlayer.CustomProperties["rol"].ToString();

        if (miRol == "redship")
        {
            GameObject miNave = PhotonNetwork.Instantiate(redShipPrefab, spawnPointAzul.position, spawnPointAzul.rotation);
            var controller = miNave.GetComponent<StarshipControllerPun>();
            if (controller != null) { controller.esJugadorTeclado = false; controller.autoavance = true; }
            udpManager = miNave.GetComponent<UDPManagerPUN>();
            if (udpManager != null && controller != null) udpManager.nave = controller;
            Debug.Log("[SPAWN] RedShip — control por celular UDP.");
        }
        else if (miRol == "blueship")
        {
            GameObject miNave = PhotonNetwork.Instantiate(blueShipPrefab, spawnPointRoja.position, spawnPointRoja.rotation);
            var controller = miNave.GetComponent<StarshipControllerPun>();
            if (controller != null) { controller.esJugadorTeclado = true; controller.autoavance = false; }
            Debug.Log("[SPAWN] BlueShip — control por teclado.");
        }
        else if (miRol == "overlord")
        {
            Vector3 posOvl = spawnPointOverlord != null ? spawnPointOverlord.position : new Vector3(0f, 200f, 0f);
            Quaternion rotOvl = spawnPointOverlord != null ? spawnPointOverlord.rotation : Quaternion.identity;
            PhotonNetwork.Instantiate(overlordPrefab, posOvl, rotOvl);
            Debug.Log("[SPAWN] Overlord — vista táctil aérea.");

            // PRUEBA TEMPORAL: spawear ambas naves que se mueven solas
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                Vector3 posAzul = spawnPointRoja != null ? spawnPointRoja.position : new Vector3(-15f, 2f, 0f);
                Vector3 posRoja = spawnPointAzul  != null ? spawnPointAzul.position  : new Vector3( 15f, 2f, 0f);

                var naveBlue = PhotonNetwork.Instantiate(blueShipPrefab, posAzul, Quaternion.identity);
                var ctrlBlue = naveBlue.GetComponent<StarshipControllerPun>();
                if (ctrlBlue != null) { ctrlBlue.esJugadorTeclado = true; ctrlBlue.autoavance = true; ctrlBlue.speed = 120f; }
                var alBlue = naveBlue.GetComponentInChildren<AudioListener>();
                if (alBlue != null) alBlue.enabled = false;
                var camBlue = naveBlue.GetComponentInChildren<Camera>();
                if (camBlue != null) camBlue.enabled = false; // el HUD del Overlord la activa cuando se necesita

                var naveRed = PhotonNetwork.Instantiate(redShipPrefab, posRoja, Quaternion.identity);
                var ctrlRed = naveRed.GetComponent<StarshipControllerPun>();
                if (ctrlRed != null) { ctrlRed.esJugadorTeclado = true; ctrlRed.autoavance = true; ctrlRed.speed = 120f; }
                var alRed = naveRed.GetComponentInChildren<AudioListener>();
                if (alRed != null) alRed.enabled = false;
                var camRed = naveRed.GetComponentInChildren<Camera>();
                if (camRed != null) camRed.enabled = false;

                Debug.Log("[SPAWN] PRUEBA: BlueShip y RedShip spawneadas a vel=120.");
            }

            if (PhotonNetwork.IsMasterClient) FinalizarLobby();
            else photonView.RPC("RPC_FinalizarLobby", RpcTarget.MasterClient);
        }
        else
        {
            Debug.LogWarning("[SPAWN] Sin rol asignado — el jugador no eligió nave en el lobby.");
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