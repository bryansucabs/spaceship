using UnityEngine;

// ObstacleMover.cs
// Hace que un obstaculo se mueva o gire de forma oscilatoria.
// Se agrega automaticamente a los obstaculos moviles desde ObstacleBuilder.
public class ObstacleMover : MonoBehaviour
{
    public enum MoveType { Horizontal, Vertical, Rotation }

    public MoveType moveType  = MoveType.Horizontal;
    public float    amplitude = 14f;   // cuanto se mueve en unidades (o grados si es Rotation)
    public float    speed     = 1.3f;  // velocidad de oscilacion

    Vector3 _startPos;
    float   _startAngleZ;

    void Start()
    {
        _startPos    = transform.position;
        _startAngleZ = transform.eulerAngles.z;
    }

    void Update()
    {
        float t = Mathf.Sin(Time.time * speed) * amplitude;

        switch (moveType)
        {
            case MoveType.Horizontal:
                transform.position = _startPos + Vector3.right * t;
                break;
            case MoveType.Vertical:
                transform.position = _startPos + Vector3.up * t;
                break;
            case MoveType.Rotation:
                transform.eulerAngles = new Vector3(0f, 0f, _startAngleZ + t);
                break;
        }
    }
}
