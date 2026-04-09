using UnityEngine;

// ObstacleGenerator.cs
// Componente que actua como FALLBACK en tiempo de ejecucion.
// Si los obstaculos ya fueron pre-construidos en Edit Mode con Tools > Build Full Scene,
// este script no hace nada.
// Si por alguna razon no existen, los genera usando ObstacleBuilder.
public class ObstacleGenerator : MonoBehaviour
{
    // Unity llama Start() al inicio del modo Play, despues de Awake()
    void Start()
    {
        // Si los obstaculos ya estan en la escena (pre-construidos), no hacer nada
        if (GameObject.Find("Obstacles") != null) return;

        // Fallback: construir obstaculos en runtime
        var root = new GameObject("Obstacles");
        ObstacleBuilder.BuildObstaclesInto(root);
    }
}
