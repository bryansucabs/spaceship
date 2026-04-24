using UnityEngine;

// ObstacleGlow.cs
// Hace pulsar el brillo de un obstaculo suavemente.
// Se agrega automaticamente a cada cubo desde ObstacleBuilder.
public class ObstacleGlow : MonoBehaviour
{
    Color _baseEmission;
    Material _mat;

    void Start()
    {
        _mat = GetComponent<Renderer>().material;
        _baseEmission = _mat.GetColor("_EmissionColor");
    }

    void Update()
    {
        if (_mat == null) return;
        // Pulso suave entre 90% y 110% — apenas perceptible, sin parpadeo
        float pulse = 1f + 0.1f * Mathf.Sin(Time.time * 1.2f + GetInstanceID() * 0.5f);
        _mat.SetColor("_EmissionColor", _baseEmission * pulse);
    }
}
