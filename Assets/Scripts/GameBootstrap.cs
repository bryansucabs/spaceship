using UnityEngine;

// GameBootstrap.cs
// Se ejecuta al inicio de SampleScene y decide que modo activar.
// Agregar este script al mismo GameObject que tiene GameManager.
// Si viene del menu Tutorial activa TutorialManager.
// Si viene del menu Nivel 1 deja todo como esta.
public class GameBootstrap : MonoBehaviour
{
    void Awake()
    {
        if (GameMode.IsTutorial)
            gameObject.AddComponent<TutorialManager>();
    }
}
