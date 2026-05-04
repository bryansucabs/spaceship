using UnityEngine;

// ObstacleGenerator.cs
// Para el Nivel 1: siempre destruye los obstaculos pre-construidos y genera los nuevos.
// Para el Tutorial: no hace nada (TutorialManager maneja sus propios obstaculos).
public class ObstacleGenerator : MonoBehaviour
{
    void Start()
    {
        // El tutorial tiene sus propios obstaculos — no interferir
        if (GameMode.IsTutorial) return;

        // Destruir obstaculos pre-construidos de la escena y regenerar con el diseno actual
        var existing = GameObject.Find("Obstacles");
        if (existing != null) Destroy(existing);

        var root = new GameObject("Obstacles");
        ObstacleBuilder.BuildObstaclesInto(root);
    }
}
