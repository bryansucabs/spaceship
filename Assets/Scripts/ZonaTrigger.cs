using UnityEngine;

public class ZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. Verificamos si el objeto que entró tiene el script PlayerObjective
        // Usamos GetComponentInParent por si el collider del jugador está en un objeto hijo
        PlayerObjective playerObj = other.GetComponentInParent<PlayerObjective>();

        // 2. Si es el jugador, le pasamos el nombre de esta zona
        if (playerObj != null)
        {
            playerObj.HandleTrigger(gameObject.name);
        }
    }
}