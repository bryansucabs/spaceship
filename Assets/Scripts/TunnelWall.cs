using UnityEngine;
using System.Collections;

public class TunnelWall : MonoBehaviour
{
    public int damage = 1;

    const float HIT_COOLDOWN    = 0.25f;
    const float WALL_DETECT_DIST = 7f;   // distancia maxima del centro de la nave a la pared

    float       _lastHit = -999f;
    Vector3     _inwardNormal;
    BoxCollider _col;
    bool        _isDestructible = false;
    bool        _destroyed      = false;
    ShipHealth  _ship;

    void Start()
    {
        _col = GetComponent<BoxCollider>();

        _inwardNormal = (Vector3.zero - transform.position).normalized;
        if (_inwardNormal == Vector3.zero) _inwardNormal = Vector3.forward;

        // Obstaculos tienen padre "Obstacles", paredes tienen padre "Tunnel"
        _isDestructible = transform.parent != null && transform.parent.name == "Obstacles";

        if (_isDestructible)
            ActualizarMaterial();

        // Cache de la nave para no buscarla cada frame
        _ship = Object.FindFirstObjectByType<ShipHealth>();
    }

    void ActualizarMaterial()
    {
        var rend = GetComponent<Renderer>();
        if (rend == null) return;

        Color col = rend.material.HasProperty("_BaseColor")
            ? rend.material.GetColor("_BaseColor")
            : rend.material.GetColor("_Color");

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", col);
        mat.SetColor("_Color",     col);
        mat.SetFloat("_Metallic",   0.8f);
        mat.SetFloat("_Smoothness", 0.75f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", col * 2.5f);
        rend.material = mat;
    }

    // ── PAREDES: deteccion por distancia usando multiples puntos de la nave ──
    // Se mide desde el centro, las dos puntas de ala, la nariz y la cola
    // para que coincida con la forma visual real de la nave
    void FixedUpdate()
    {
        if (_isDestructible) return;
        if (_destroyed) return;
        if (Time.time - _lastHit < HIT_COOLDOWN) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        if (_ship == null)
        {
            _ship = Object.FindFirstObjectByType<ShipHealth>();
            if (_ship == null) return;
        }

        Transform t   = _ship.transform;
        Vector3   pos = t.position;

        // Puntos que representan los extremos fisicos de la nave en espacio mundial
        // Los valores (5f alas, 3f nariz, 2f cola) coinciden con el StarSparrow a escala 0.72
        Vector3[] puntos = {
            pos,                             // centro
            pos + t.right   *  5f,           // punta ala derecha
            pos - t.right   *  5f,           // punta ala izquierda
            pos + t.forward *  3f,           // nariz
            pos - t.forward *  2f,           // cola
            pos + t.up      *  1f,           // lomo
            pos - t.up      *  1f,           // vientre
        };

        float   minDist     = float.MaxValue;
        Vector3 bestClosest = pos;

        foreach (var pt in puntos)
        {
            Vector3 c = _col != null ? _col.ClosestPoint(pt) : transform.position;
            float   d = Vector3.Distance(c, pt);
            if (d < minDist) { minDist = d; bestClosest = c; }
        }

        if (minDist < WALL_DETECT_DIST)
        {
            _lastHit = Time.time;
            SparkEffect.Spawn(bestClosest, _inwardNormal);
            _ship.TakeDamage(damage);
            SoundBank.Instance?.PlayPared();
        }
    }

    // ── OBSTACULOS: triggers normales (funciona bien en objetos pequenos) ──
    void OnTriggerEnter(Collider other) { TryHitObstaculo(other); }
    void OnTriggerStay(Collider other)  { TryHitObstaculo(other); }

    void TryHitObstaculo(Collider other)
    {
        if (!_isDestructible) return;
        if (_destroyed) return;
        if (Time.time - _lastHit < HIT_COOLDOWN) return;

        var health = other.GetComponentInParent<ShipHealth>();
        if (health == null) return;

        _lastHit = Time.time;

        Vector3 contactPoint = _col != null
            ? _col.ClosestPoint(other.transform.position)
            : transform.position + _inwardNormal;

        SparkEffect.Spawn(contactPoint, _inwardNormal);
        health.TakeDamage(damage);
        SoundBank.Instance?.PlayObstaculo();

        _destroyed = true;
        StartCoroutine(Destruir(contactPoint));
    }

    IEnumerator Destruir(Vector3 puntoContacto)
    {
        if (_col != null) _col.enabled = false;

        Color col = Color.cyan;
        var rend = GetComponent<Renderer>();
        if (rend != null && rend.material.HasProperty("_BaseColor"))
            col = rend.material.GetColor("_BaseColor");

        for (int i = 0; i < 10; i++)
        {
            var frag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frag.transform.position   = puntoContacto + Random.insideUnitSphere * 4f;
            frag.transform.localScale = Vector3.one * Random.Range(1.5f, 4.5f);
            frag.transform.rotation   = Random.rotation;

            var fragShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var fragMat    = new Material(fragShader);
            fragMat.SetColor("_BaseColor", col);
            fragMat.SetFloat("_Metallic",   0.8f);
            fragMat.SetFloat("_Smoothness", 0.7f);
            fragMat.EnableKeyword("_EMISSION");
            fragMat.SetColor("_EmissionColor", col * 3f);
            frag.GetComponent<Renderer>().material = fragMat;

            Destroy(frag.GetComponent<Collider>());

            var rb = frag.AddComponent<Rigidbody>();
            rb.useGravity      = false;
            rb.linearVelocity  = (Random.onUnitSphere + _inwardNormal) * Random.Range(8f, 22f);
            rb.angularVelocity = Random.onUnitSphere * Random.Range(4f, 12f);

            Destroy(frag, 1.5f);
        }

        SparkEffect.Spawn(puntoContacto + Random.insideUnitSphere * 0.5f, _inwardNormal);

        if (rend != null) rend.enabled = false;

        yield return new WaitForSeconds(0.05f);
        Destroy(gameObject);
    }
}
