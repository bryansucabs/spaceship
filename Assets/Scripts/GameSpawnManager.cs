using UnityEngine;
using Photon.Pun;

public class GameSpawnManager : MonoBehaviour
{
    [Header("Puntos de Aparición")]
    public Transform spawnPointAzul;
    public Transform spawnPointRoja;

    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            // Verificamos si somos el creador de la sala (Jugador 1)
            if (PhotonNetwork.IsMasterClient)
            {
                // Instanciamos la nave azul (Asegúrate de que el nombre coincida con el prefab en Resources)
                PhotonNetwork.Instantiate("StarSparrow1", spawnPointAzul.position, spawnPointAzul.rotation);
            }
            // Si somos el Jugador 2
            else if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
            {
                // Instanciamos la nave roja
                PhotonNetwork.Instantiate("StarSparrow10", spawnPointRoja.position, spawnPointRoja.rotation);
            }
        }
    }
}