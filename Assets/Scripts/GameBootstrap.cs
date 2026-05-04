using UnityEngine;

// GameBootstrap.cs
// Se ejecuta al inicio de SampleScene y decide que modo activar.
// Esta en el mismo GameObject que GameManager.
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (GameMode.IsTutorial)
        {
            // Tutorial: TutorialManager maneja todo
            gameObject.AddComponent<TutorialManager>();
        }
        else
        {
            // Nivel 1: destruir obstaculos pre-construidos y generar los nuevos
            var existing = GameObject.Find("Obstacles");
            if (existing != null) Destroy(existing);

            var root = new GameObject("Obstacles");
            ObstacleBuilder.BuildObstaclesInto(root);
        }
    }
}
