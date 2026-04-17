using UnityEngine;

// SparkEffect.cs
// Clase estatica que genera un efecto de chispas en el punto de contacto con una pared.
// No necesita prefab — todo el sistema de particulas se crea desde codigo.
// Se llama desde TunnelWall cada vez que la nave toca una pared u obstaculo.
public static class SparkEffect
{
    // Genera chispas en la posicion dada, orientadas segun la normal de la pared   
    // position: punto exacto donde la nave toco la pared
    // wallNormal: direccion perpendicular a la pared, apuntando hacia adentro del tunel
    public static void Spawn(Vector3 position, Vector3 wallNormal)
    {
        // Crear un nuevo GameObject vacio donde viviran las particulas
        var go = new GameObject("Sparks");
        go.transform.position = position;



        // Agregar el sistema de particulas al objeto
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Rotar el objeto para que las chispas salgan alejandose de la pared
        // LookRotation apunta el eje Z del objeto en la direccion dada
        // Negamos la normal para que las chispas vayan hacia el interior del tunel
        go.transform.rotation = Quaternion.LookRotation(-wallNormal);

        // ── Configurar el modulo principal del sistema de particulas ─────────
        var main = ps.main;
        main.duration        = 0.3f;   // el burst dura 0.3 segundos en total
        main.loop            = false;  // no se repite, es un efecto de una sola vez
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.45f); // cada particula vive entre 0.15 y 0.45s
        main.startSpeed      = new ParticleSystem.MinMaxCurve(4f, 18f);      // velocidad inicial aleatoria
        main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.18f); // tamano aleatorio (pequenitas)
        main.maxParticles    = 40;     // maximo de particulas simultaneas
        main.gravityModifier = 0.3f;   // leve caida por gravedad para mayor realismo
        main.simulationSpace = ParticleSystemSimulationSpace.World; // las particulas se mueven en coordenadas del mundo

        // ── Gradiente de color: amarillo -> naranja -> rojo oscuro ────────────
        // Simula el efecto visual de metal rozando metal a alta velocidad
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.95f, 0.3f), 0f),   // amarillo brillante al nacer
                new GradientColorKey(new Color(1f, 0.45f, 0.0f), 0.4f), // naranja a mitad de vida
                new GradientColorKey(new Color(0.6f, 0.1f, 0.0f), 1f),  // rojo oscuro al apagarse
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),    // completamente opaco al inicio
                new GradientAlphaKey(1f, 0.3f),  // sigue opaco hasta el 30%
                new GradientAlphaKey(0f, 1f),    // completamente transparente al final
            });
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        // ── Modulo de emision: burst (explotan todas a la vez) ────────────────
        var emission = ps.emission;
        emission.enabled = true;
        // Burst: en el tiempo 0, emitir entre 30 y 40 particulas de una sola vez
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 30, 40)
        });

        // ── Forma de emision: cono estrecho pegado a la pared ─────────────────
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone; // particulas salen en forma de cono
        shape.angle     = 35f;  // angulo del cono (35 grados de apertura)
        shape.radius    = 0.1f; // radio de la base del cono (muy pequeno = punto casi exacto)

        // ── Velocidad sobre el tiempo: las chispas frenan rapidamente ─────────
        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        // Curva: empieza a velocidad completa (1.0) y frena hasta 0.05 al final de vida
        vel.speedModifier = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.05f)));

        // ── Renderer: configurar como se ve cada particula ────────────────────
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard; // siempre de frente a la camara

        // Buscar el shader de particulas mas adecuado segun el pipeline de render
        var mat = new Material(Shader.Find("Particles/Standard Unlit") ??
                               Shader.Find("Legacy Shaders/Particles/Additive") ??
                               Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        if (mat != null)
        {
            mat.SetFloat("_Blend", 1f); // modo aditivo: las chispas se suman a los colores del fondo
            renderer.material = mat;
        }

        // Iniciar el sistema de particulas
        ps.Play();

        // Destruir el GameObject automaticamente cuando las particulas terminen
        // Se suma la duracion + la vida maxima de las particulas + un margen de 0.2s
        Object.Destroy(go, main.duration + main.startLifetime.constantMax + 0.2f);
    }
}
