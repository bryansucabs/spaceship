using UnityEngine;

// TunnelWall.cs
// Se agrega a cada pared del tunel y a cada obstaculo.
// Detecta cuando la nave entra en contacto y le quita vida.
// Tiene un cooldown para que rozar la pared no quite toda la vida de golpe.
// Tambien genera chispas visuales en el punto de contacto.
public class TunnelWall : MonoBehaviour
{
    // Cantidad de dano por golpe (configurable en el Inspector)
    public int damage = 1;

    // Tiempo minimo entre golpes consecutivos en segundos
    // Evita que el dano se acumule muy rapido al rozar una pared
    const float HIT_COOLDOWN = 0.25f;

    // Marca de tiempo del ultimo golpe registrado
    // -999 garantiza que el primer golpe siempre se registre
    float _lastHit = -999f;

    // Normal de esta pared apuntando hacia el interior del tunel
    // Se usa para orientar las chispas en la direccion correcta
    Vector3 _inwardNormal;

    // Referencia al collider de esta pared, para calcular el punto de contacto exacto
    BoxCollider _col;

    // Start() se llama al inicio del modo Play
    void Start()
    {
        // Obtener el collider de esta pared
        _col = GetComponent<BoxCollider>();

        // Calcular la direccion hacia el interior del tunel
        // El tunel esta centrado en el origen, entonces la normal hacia adentro
        // es el vector desde la pared al origen (0,0,0)
        _inwardNormal = (Vector3.zero - transform.position).normalized;
    }

    // OnTriggerEnter: se llama cuando la nave entra al trigger de esta pared
    void OnTriggerEnter(Collider other)  { TryHit(other); }

    // OnTriggerStay: se llama cada frame mientras la nave siga tocando la pared
    // Necesario para detectar el dano continuo si la nave roza la pared por mucho tiempo
    void OnTriggerStay(Collider other)   { TryHit(other); }

    // Intenta aplicar dano si el cooldown lo permite
    void TryHit(Collider other)
    {
        // Verificar el cooldown — si no paso suficiente tiempo, ignorar el golpe
        if (Time.time - _lastHit < HIT_COOLDOWN) return;

        // Buscar el componente ShipHealth en la nave o en su padre
        // GetComponentInParent busca en el objeto mismo y en todos sus padres
        var health = other.GetComponentInParent<ShipHealth>();
        if (health == null) return; // si no es la nave, ignorar

        // Registrar el tiempo de este golpe
        _lastHit = Time.time;

        // Calcular el punto de contacto mas cercano entre el collider de la pared
        // y la posicion de la nave — para ubicar las chispas correctamente
        Vector3 contactPoint = _col != null
            ? _col.ClosestPoint(other.transform.position) // punto mas cercano en la pared
            : transform.position + _inwardNormal * 1f;    // fallback si no hay collider

        // Crear efecto de chispas en el punto de contacto, orientadas hacia adentro
        SparkEffect.Spawn(contactPoint, _inwardNormal);

        // Aplicar dano a la nave
        health.TakeDamage(damage);
    }
}
