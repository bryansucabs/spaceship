using UnityEngine;

// LocalObstacleMove.cs
// Hace que un obstaculo se mueva de forma oscilatoria o rote en su propio centro como en el editor.
public class LocalObstacleMove : MonoBehaviour
{
    public enum MoveType { Horizontal, Vertical, Depth, Rotation }
    public enum RotationAxis { X, Y, Z }

    [Header("Configuración Principal")]
    public MoveType moveType = MoveType.Horizontal;
    public RotationAxis rotationAxis = RotationAxis.Z; 

    [Header("Parámetros de Movimiento (Oscilación)")]
    public float amplitude = 3f;   
    public float speed     = 1.3f;  

    [Header("Parámetros de Rotación (Bucle Infinito)")]
    public float rotationSpeed = 90f; 

    Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        // 1. Lógica de Movimiento Oscilatorio (X, Y, Z)
        if (moveType != MoveType.Rotation)
        {
            float t = Mathf.Sin(Time.time * speed) * amplitude;

            switch (moveType)
            {
                case MoveType.Horizontal:
                    transform.position = _startPos + transform.right * t;
                    break;
                case MoveType.Vertical:
                    transform.position = _startPos + transform.up * t;
                    break;
                case MoveType.Depth:
                    transform.position = _startPos + transform.forward * t;
                    break;
            }
        }
        // 2. Lógica de Rotación Continua en su propio centro
        else
        {
            AplicarRotacionHerramienta();
        }
    }

    void AplicarRotacionHerramienta()
    {
        Vector3 ejeRealLocal = Vector3.zero;

        // IMPORTANTE: Usamos las propiedades del transform que apuntan a sus propios ejes reales instalados en el espacio
        switch (rotationAxis)
        {
            case RotationAxis.X: ejeRealLocal = transform.right; break;   // Eje rojo del editor
            case RotationAxis.Y: ejeRealLocal = transform.up; break;      // Eje verde del editor
            case RotationAxis.Z: ejeRealLocal = transform.forward; break; // Eje azul del editor
        }

        // Giramos alrededor de su propio eje geométrico actual, imitando la herramienta de rotación
        transform.Rotate(ejeRealLocal, rotationSpeed * Time.deltaTime, Space.World);
    }
}
