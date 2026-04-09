using UnityEngine;

// ShipHealth.cs
// Maneja la vida de la nave del jugador.
// Tiene un cooldown entre golpes para que rozar una pared no quite toda la vida instantaneamente.
// Las chispas visuales las genera TunnelWall via SparkEffect al detectar el contacto.
public class ShipHealth : MonoBehaviour
{
    // Vida maxima de la nave
    public int maxHealth = 5;

    // Vida actual — publica para que GameManager pueda leerla y mostrarla en pantalla
    public int currentHealth;

    // Tiempo minimo entre golpes (en segundos) — evita perder varios corazones de golpe
    const float DAMAGE_COOLDOWN = 0.4f;

    // Marca de tiempo del ultimo dano recibido
    float _lastDamage = -999f;

    // Start() se llama al inicio del modo Play
    void Start()
    {
        // Iniciar con vida completa
        currentHealth = maxHealth;
    }

    // Recibe dano desde TunnelWall u otros obstaculos
    public void TakeDamage(int amount)
    {
        // No quitar vida si el juego termino
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        // No quitar vida si el cooldown no termino (evita dano spam)
        if (Time.time - _lastDamage < DAMAGE_COOLDOWN) return;

        // Registrar el momento del golpe
        _lastDamage = Time.time;

        // Reducir vida
        currentHealth -= amount;

        // Si se quedo sin vida, terminar el juego
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            GameManager.Instance?.GameOver("Ship destroyed!"); // ?. evita error si no hay GameManager
        }
    }
}
