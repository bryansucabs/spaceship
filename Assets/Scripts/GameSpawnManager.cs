using UnityEngine;
using Photon.Pun;
using Photon.Realtime; // Necesario para acceder a Player

public class GameSpawnManager : MonoBehaviourPunCallbacks
{
    [Header("Puntos de Aparición")]
    public Transform spawnPointAzul;
    public Transform spawnPointRoja;


    void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady)
        {
            GameObject miNave;

            if (PhotonNetwork.IsMasterClient)
            {
                // El creador de la sala usa el celular y aparece en la posición azul
                miNave = PhotonNetwork.Instantiate("RedShip", spawnPointAzul.position, spawnPointAzul.rotation);
                miNave.GetComponent<StarshipControllerPun>().esJugadorTeclado = true;

               // miNave.GetComponent<StarshipControllerPun>().autoavance = true
            }
            else
            {
                // El invitado usa el teclado y aparece en la posición roja
                miNave = PhotonNetwork.Instantiate("BlueShip", spawnPointRoja.position, spawnPointRoja.rotation);
                miNave.GetComponent<StarshipControllerPun>().esJugadorTeclado = true;

                //miNave.GetComponent<StarshipControllerPun>().autoavance = true
            }
        }
    }
}