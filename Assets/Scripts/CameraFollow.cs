using UnityEngine;

// CameraFollow.cs
// Hace que la camara siga a la nave desde atras y un poco arriba.
// Usa un clamp circular para que la camara nunca salga del tunel,
// incluso cuando la nave esta en las esquinas diagonales del octagono.
public class CameraFollow : MonoBehaviour
{
    // Objeto que la camara sigue — se asigna en el Inspector o se busca automaticamente
    public Transform target;

    // Distancia detras de la nave en el eje Z (negativo = atras)
    public float offsetZ = -24f;

    // Distancia arriba de la nave en el eje Y
    public float offsetY =  5f;

    // Radio maximo que puede alejarse del centro del tunel en X e Y
    // El apothem del tunel es 55u, pero la camara necesita margen para no salir
    // 46u garantiza que en cualquier diagonal del octagono la camara quede adentro
    const float MAX_RADIUS = 46f;

    // Start() se llama al inicio del modo Play
    void Start()
    {
        // Si no se asigno target en el Inspector, buscar la nave automaticamente
        if (target == null)
        {
            var ship = GameObject.Find("PlayerShip");
            if (ship != null) target = ship.transform;
        }

        // Posicionar la camara inmediatamente (evita que aparezca en (0,0,0) un frame)
        if (target != null)
            transform.position = CalcPos(target.position);
    }

    // LateUpdate() se ejecuta despues de Update() — ideal para camaras
    // Asi la nave ya se movio este frame cuando la camara actualiza su posicion
    void LateUpdate()
    {
        if (target == null) return; // si no hay nave, no hacer nada

        // Actualizar la posicion de la camara siguiendo a la nave
        transform.position = CalcPos(target.position);

        // Calcular el angulo de inclinacion de la camara para que apunte a la nave
        // Atan2 calcula el angulo correcto segun el offset vertical y la distancia
        transform.rotation = Quaternion.Euler(
            Mathf.Atan2(offsetY, -offsetZ) * Mathf.Rad2Deg, 0f, 0f);
    }

    // Calcula la posicion ideal de la camara dada la posicion de la nave
    Vector3 CalcPos(Vector3 shipPos)
    {
        // Posicion deseada de la camara (misma X que la nave, mas arriba en Y)
        float camX = shipPos.x;
        float camY = shipPos.y + offsetY;

        // Clamp circular: si la camara se aleja demasiado del centro del tunel,
        // limitar su distancia al radio maximo
        // Esto es mas correcto que clampar X e Y por separado porque funciona
        // correctamente en las esquinas diagonales del octagono
        Vector2 xy = new Vector2(camX, camY);
        if (xy.magnitude > MAX_RADIUS)
            xy = xy.normalized * MAX_RADIUS; // empujar al borde del circulo

        // Devolver la posicion final: XY clampeado, Z detras de la nave
        return new Vector3(xy.x, xy.y, shipPos.z + offsetZ);
    }
}
