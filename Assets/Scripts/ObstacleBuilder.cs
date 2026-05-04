using UnityEngine;

// ObstacleBuilder.cs
// Dos barras ASIMETRICAS con abertura descentrada — una barra es mas grande que la otra.
// El gap varia de posicion en cada obstaculo para que el jugador no pueda memorizar.
// Los ultimos 15 obstaculos se mueven.
public static class ObstacleBuilder
{
    const float INNER  = 54f;
    const float DEPTH  = 8f;
    const float GAP    = 28f;  // abertura mas ajustada — mas desafiante

    const float START_Z = 500f;
    const float SPACING = 300f;
    const int   TOTAL   = 22;

    // Color unico por obstaculo
    static readonly Color[] PALETTE = {
        new Color(0.1f, 0.55f, 1.0f), // azul
        new Color(1.0f, 0.35f, 0.1f), // rojo-naranja
        new Color(0.15f, 0.9f, 0.2f), // verde
        new Color(0.9f, 0.85f, 0.0f), // amarillo
        new Color(0.6f, 0.0f, 1.0f),  // violeta
        new Color(0.0f, 0.85f, 0.85f),// cyan
        new Color(1.0f, 0.15f, 0.55f),// rosa
        new Color(1.0f, 0.6f, 0.0f),  // naranja
        new Color(0.0f, 0.9f, 0.55f), // verde-agua
        new Color(1.0f, 1.0f, 0.3f),  // amarillo claro
        new Color(0.55f, 0.2f, 1.0f), // lila
        new Color(0.9f, 0.3f, 0.0f),  // rojo oscuro
        new Color(0.0f, 0.6f, 1.0f),  // azul claro
        new Color(0.5f, 1.0f, 0.0f),  // lima brillante
        new Color(1.0f, 0.0f, 0.3f),  // rojo-rosa
        new Color(0.2f, 0.8f, 0.6f),  // verde-mar
        new Color(0.9f, 0.5f, 1.0f),  // malva
        new Color(1.0f, 0.8f, 0.0f),  // dorado
        new Color(0.0f, 0.4f, 1.0f),  // azul profundo
        new Color(0.7f, 1.0f, 0.0f),  // chartreuse
        new Color(1.0f, 0.2f, 0.8f),  // magenta
        new Color(0.0f, 0.9f, 1.0f),  // aqua
    };

    // angle  = rotacion del obstaculo completo
    // offset = cuanto se desplaza el centro del gap del centro del tunel
    //          positivo = gap mas arriba, negativo = gap mas abajo
    //          esto hace que una barra sea mas grande que la otra
    struct ObsDef
    {
        public float angle, offset, speed;
        public bool moving;
        public ObstacleMover.MoveType moveType;
        public ObsDef(float a, float o, bool m = false,
                      ObstacleMover.MoveType mt = ObstacleMover.MoveType.Horizontal,
                      float sp = 1.2f)
        { angle=a; offset=o; moving=m; moveType=mt; speed=sp; }
    }

    static readonly ObsDef[] SEQUENCE = {
        // Primeros 7: fijos, aprender el mecanismo
        new ObsDef(  0f,   0f),           // 1  centrado
        new ObsDef( 45f, +10f),           // 2  diagonal, gap arriba
        new ObsDef( 90f,  -8f),           // 3  vertical,  gap abajo
        new ObsDef(-45f, +12f),           // 4  diagonal,  gap arriba
        new ObsDef(  0f, -10f),           // 5  horizontal, gap abajo
        new ObsDef( 67f,  +6f),           // 6  casi vertical, leve
        new ObsDef( 22f, -14f),           // 7  casi horizontal, gap abajo

        // Obstaculos 8-22: en movimiento, dificultad creciente
        new ObsDef( 90f,  -8f, true, ObstacleMover.MoveType.Horizontal, 1.1f),
        new ObsDef(  0f, +10f, true, ObstacleMover.MoveType.Vertical,   1.2f),
        new ObsDef( 45f,  -6f, true, ObstacleMover.MoveType.Rotation,   1.2f),
        new ObsDef(-45f, +12f, true, ObstacleMover.MoveType.Horizontal, 1.4f),
        new ObsDef(  0f, -12f, true, ObstacleMover.MoveType.Vertical,   1.4f),
        new ObsDef( 90f,  +8f, true, ObstacleMover.MoveType.Rotation,   1.5f),
        new ObsDef( 22f, -10f, true, ObstacleMover.MoveType.Horizontal, 1.6f),
        new ObsDef( 67f, +14f, true, ObstacleMover.MoveType.Vertical,   1.6f),
        new ObsDef( 45f,  -8f, true, ObstacleMover.MoveType.Rotation,   1.7f),
        new ObsDef(-45f, +10f, true, ObstacleMover.MoveType.Horizontal, 1.8f),
        new ObsDef(  0f, -14f, true, ObstacleMover.MoveType.Vertical,   1.8f),
        new ObsDef( 90f, +12f, true, ObstacleMover.MoveType.Rotation,   2.0f),
        new ObsDef( 45f,  -6f, true, ObstacleMover.MoveType.Horizontal, 2.0f),
        new ObsDef(-45f, +14f, true, ObstacleMover.MoveType.Vertical,   2.1f),
        new ObsDef(  0f, -10f, true, ObstacleMover.MoveType.Rotation,   2.2f),
    };

    public static void BuildObstaclesInto(GameObject root)
    {
        for (int i = 0; i < TOTAL; i++)
        {
            float z   = START_Z + i * SPACING;
            var   def = SEQUENCE[i];
            Color col = PALETTE[i % PALETTE.Length];
            SpawnDosBandas(root, z, def.angle, def.offset, col,
                           def.moving, def.moveType, def.speed);
        }
    }

    static void SpawnDosBandas(GameObject root, float z, float angleZ, float gapOffset,
                                Color col, bool moving,
                                ObstacleMover.MoveType moveType, float speed)
    {
        var frame = new GameObject($"Obs_{z:0}");
        frame.transform.SetParent(root.transform);
        frame.transform.position    = new Vector3(0f, 0f, z);
        frame.transform.eulerAngles = new Vector3(0f, 0f, angleZ);

        float halfGap = GAP / 2f;

        // Barra A: desde (gapOffset + halfGap) hasta INNER — puede ser mas chica o mas grande
        float barABottom = gapOffset + halfGap;
        float barAHeight = INNER - barABottom;
        float barACenter = barABottom + barAHeight / 2f;

        // Barra B: desde -INNER hasta (gapOffset - halfGap) — la contraparte
        float barBTop    = gapOffset - halfGap;
        float barBHeight = barBTop + INNER;  // = INNER + gapOffset - halfGap
        float barBCenter = -INNER + barBHeight / 2f;

        if (barAHeight > 0.5f)
            SlabLocal(frame, "BarA",
                new Vector3(0f, barACenter, 0f),
                new Vector3(INNER * 2.1f, barAHeight, DEPTH), col);

        if (barBHeight > 0.5f)
            SlabLocal(frame, "BarB",
                new Vector3(0f, barBCenter, 0f),
                new Vector3(INNER * 2.1f, barBHeight, DEPTH), col);

        AddLight(root, new Vector3(0f, gapOffset, z - 90f), col);

        if (moving)
        {
            var mover       = frame.AddComponent<ObstacleMover>();
            mover.moveType  = moveType;
            mover.speed     = speed;
            mover.amplitude = moveType == ObstacleMover.MoveType.Rotation ? 38f : 11f;
        }
    }

    static void SlabLocal(GameObject parent, string name,
                           Vector3 localPos, Vector3 scale, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition    = localPos;
        go.transform.localScale       = scale;
        go.transform.localEulerAngles = Vector3.zero;

        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);
        mat.SetFloat("_Metallic",   0.75f);
        mat.SetFloat("_Smoothness", 0.75f);
        mat.SetFloat("_Cull",       0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 2.5f);
        go.GetComponent<Renderer>().material = mat;

        var col = go.GetComponent<BoxCollider>();
        if (col) col.isTrigger = true;
        go.AddComponent<TunnelWall>();
    }

    public static void AddLight(GameObject parent, Vector3 pos, Color color)
    {
        var go       = new GameObject("OLight");
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        var lt       = go.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.color     = color;
        lt.range     = 90f;
        lt.intensity = 4f;
    }

    public static GameObject Slab(GameObject parent, string name,
                                   Vector3 pos, Vector3 scale, Color color)
        => Slab(parent, name, pos, scale, Vector3.zero, color);

    public static GameObject Slab(GameObject parent, string name,
                                   Vector3 pos, Vector3 scale, Vector3 euler, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position    = pos;
        go.transform.localScale  = scale;
        go.transform.eulerAngles = euler;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);
        mat.SetFloat("_Cull",      0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 2.5f);
        go.GetComponent<Renderer>().material = mat;

        var col = go.GetComponent<BoxCollider>();
        if (col) col.isTrigger = true;
        go.AddComponent<TunnelWall>();
        return go;
    }
}
