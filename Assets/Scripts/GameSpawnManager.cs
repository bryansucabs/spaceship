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
        yaNaci = true;
        SpawnearMiRolAsimetrico();
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
            if (controller != null) { controller.esJugadorTeclado = false; controller.autoavance = false; controller.InitPlayerObjective(); }
            udpManager = miNave.GetComponent<UDPManagerPUN>();
            if (udpManager != null && controller != null) udpManager.nave = controller;
            Debug.Log("[SPAWN] RedShip — control por celular UDP.");
        }
        else if (miRol == "blueship")
        {
            GameObject miNave = PhotonNetwork.Instantiate(blueShipPrefab, spawnPointRoja.position, spawnPointRoja.rotation);
            var controller = miNave.GetComponent<StarshipControllerPun>();
            if (controller != null) { controller.esJugadorTeclado = true; controller.autoavance = false; controller.InitPlayerObjective(); }
            Debug.Log("[SPAWN] BlueShip — control por teclado.");
        }
        else if (miRol == "overlord")
        {
            Vector3 posOvl = spawnPointOverlord != null ? spawnPointOverlord.position : new Vector3(0f, 200f, 0f);
            Quaternion rotOvl = spawnPointOverlord != null ? spawnPointOverlord.rotation : Quaternion.identity;
            PhotonNetwork.Instantiate(overlordPrefab, posOvl, rotOvl);
            Debug.Log("[SPAWN] Overlord — vista táctil aérea.");

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

    // ── RPCs de sabotaje del Overlord ─────────────────────────────────────────
    // GameSpawnManager vive en la raíz de la escena MAP con su propio PhotonView.
    // PUN2 encuentra estos métodos correctamente porque el PhotonView está en el
    // mismo GameObject que GameSpawnManager (no en un hijo).

    [PunRPC]
    public void RPC_IniciarSabotaje(float duracion)
    {
        foreach (var door in FindObjectsByType<DoorTrigger>(FindObjectsSortMode.None))
            door.ForzarCerrar();
        foreach (var alerta in FindObjectsByType<ShipSabotageAlert>(FindObjectsSortMode.None))
            alerta.MostrarAlerta(duracion);
        var go = new GameObject("_SabotajeTimer");
        DontDestroyOnLoad(go);
        go.AddComponent<SabotajeTimerAuxiliar>().Iniciar(duracion);
    }

    [PunRPC]
    public void RPC_IniciarFreno(float duracion)
    {
        foreach (var alerta in FindObjectsByType<ShipSabotageAlert>(FindObjectsSortMode.None))
            alerta.MostrarAlertaRelentizar(duracion);
        var go = new GameObject("_FrenosTimer");
        DontDestroyOnLoad(go);
        go.AddComponent<FrenosTimerAuxiliar>().Iniciar(duracion);
    }

    [PunRPC]
    public void RPC_RomperPasadizo(string nombre)
    {
        var go = new GameObject("_RotadorPasadizo");
        go.AddComponent<PasadizoRotadorAuxiliar>().Iniciar(nombre, 80f, 2.5f);
    }

    // ── RPCs del sistema de objetivos (ObjectiveManager) ─────────────────────

    [PunRPC]
    public void RPC_SyncObjective(string role, int objectiveInt)
    {
        if (ObjectiveManager.Instance == null) return;
        ShipObjective obj = (ShipObjective)objectiveInt;
        if (role == "redship")
            ObjectiveManager.Instance.redShipObjective = obj;
        else if (role == "blueship")
            ObjectiveManager.Instance.blueShipObjective = obj;
    }

    [PunRPC]
    public void RPC_GameOver(int winnerResult)
    {
        if (ObjectiveManager.Instance == null) return;
        ObjectiveManager.Instance.isGameOver = true;
        ObjectiveManager.Instance.winner = winnerResult;
        ObjectiveManager.Instance.SetEndMessageForRole(winnerResult);
    }
}