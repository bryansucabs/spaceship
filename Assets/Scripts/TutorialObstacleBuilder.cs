using UnityEngine;

// TutorialObstacleBuilder.cs
// Obstaculos del tutorial con la misma logica de dos barras + abertura,
// pero en 4 fases progresivas:
//   Fase 1 (obs 1-5):  barras horizontales — aprende a moverse ARRIBA/ABAJO
//   Fase 2 (obs 6-10): barras verticales   — aprende a moverse IZQUIERDA/DERECHA
//   Fase 3 (obs 11-15): diagonales          — aprende angulos intermedios
//   Fase 4 (obs 16-18): obstaculos moviles  — aplica todo lo aprendido
public static class TutorialObstacleBuilder
{
    const float INNER    = 54f;
    const float DEPTH    = 8f;
    const float GAP_F1   = 26f;  // fase 1 y 2: obliga a mover la nave
    const float GAP_F3   = 24f;  // fase 3: diagonales
    const float GAP_F4   = 22f;  // fase 4: moviles, mas ajustado
    const float START_Z  = 400f;
    const float SPACING  = 360f; // mas espacio para reaccionar

    // Colores por fase para reconocerlas visualmente
    static readonly Color COL_F1 = new Color(0.1f, 0.7f, 1.0f);  // azul  — arriba/abajo
    static readonly Color COL_F2 = new Color(0.2f, 0.9f, 0.2f);  // verde — izq/der
    static readonly Color COL_F3 = new Color(1.0f, 0.75f, 0.0f); // dorado — diagonal
    static readonly Color COL_F4 = new Color(1.0f, 0.25f, 0.5f); // rosa  — moviles

    public static void BuildTutorialObstacles(GameObject root)
    {
        int i = 0;

        // ── FASE 1: ARRIBA / ABAJO (angulo 0 = barras horizontales) ──────────
        // La abertura esta centrada — el jugador aprende a ir arriba o abajo
        float[] offsetsF1 = { 0f, +8f, -8f, +5f, -5f };
        foreach (float offset in offsetsF1)
        {
            float z = START_Z + i * SPACING;
            SpawnBandas(root, z, 0f, offset, GAP_F1, COL_F1);
            AddLight(root, new Vector3(0f, offset, z - 100f), COL_F1);
            i++;
        }

        // ── FASE 2: IZQUIERDA / DERECHA (angulo 90 = barras verticales) ──────
        // El jugador debe moverse lateralmente para alinearse
        float[] offsetsF2 = { 0f, +8f, -8f, +5f, -5f };
        foreach (float offset in offsetsF2)
        {
            float z = START_Z + i * SPACING;
            SpawnBandas(root, z, 90f, offset, GAP_F1, COL_F2);
            AddLight(root, new Vector3(offset, 0f, z - 100f), COL_F2);
            i++;
        }

        // ── FASE 3: DIAGONALES (mezcla de 45° y -45°) ────────────────────────
        // El jugador debe combinar movimiento horizontal y vertical
        (float angle, float offset)[] defsF3 = {
            ( 45f,  0f), (-45f,  0f), ( 45f, +6f),
            (-45f, -6f), ( 30f,  0f),
        };
        foreach (var (angle, offset) in defsF3)
        {
            float z = START_Z + i * SPACING;
            SpawnBandas(root, z, angle, offset, GAP_F3, COL_F3);
            AddLight(root, new Vector3(0f, 0f, z - 100f), COL_F3);
            i++;
        }

        // ── FASE 4: OBSTACULOS EN MOVIMIENTO ─────────────────────────────────
        // Aplica todo lo aprendido pero ahora los obstaculos se mueven
        (float angle, float offset, ObstacleMover.MoveType move, float speed)[] defsF4 = {
            (  0f,  0f, ObstacleMover.MoveType.Horizontal, 1.1f),
            ( 90f,  0f, ObstacleMover.MoveType.Vertical,   1.2f),
            ( 45f,  0f, ObstacleMover.MoveType.Rotation,   1.2f),
        };
        foreach (var (angle, offset, move, speed) in defsF4)
        {
            float z = START_Z + i * SPACING;
            SpawnBandasMovil(root, z, angle, offset, GAP_F4, COL_F4, move, speed);
            AddLight(root, new Vector3(0f, 0f, z - 100f), COL_F4);
            i++;
        }
    }

    // Crea dos barras con abertura centrada en gapOffset, rotadas al angulo dado
    static void SpawnBandas(GameObject root, float z, float angleZ,
                             float gapOffset, float gap, Color col)
    {
        var frame = new GameObject($"TUT_{z:0}");
        frame.transform.SetParent(root.transform);
        frame.transform.position    = new Vector3(0f, 0f, z);
        frame.transform.eulerAngles = new Vector3(0f, 0f, angleZ);

        float halfGap = gap / 2f;

        float barABottom = gapOffset + halfGap;
        float barAHeight = INNER - barABottom;
        float barACenter = barABottom + barAHeight / 2f;

        float barBTop    = gapOffset - halfGap;
        float barBHeight = barBTop + INNER;
        float barBCenter = -INNER + barBHeight / 2f;

        if (barAHeight > 0.5f)
            SlabLocal(frame, "BarA",
                new Vector3(0f, barACenter, 0f),
                new Vector3(INNER * 2.1f, barAHeight, DEPTH), col);

        if (barBHeight > 0.5f)
            SlabLocal(frame, "BarB",
                new Vector3(0f, barBCenter, 0f),
                new Vector3(INNER * 2.1f, barBHeight, DEPTH), col);
    }

    // Igual pero con movimiento
    static void SpawnBandasMovil(GameObject root, float z, float angleZ,
                                  float gapOffset, float gap, Color col,
                                  ObstacleMover.MoveType moveType, float speed)
    {
        SpawnBandas(root, z, angleZ, gapOffset, gap, col);

        // Agregar mover al frame recien creado
        var frame = root.transform.Find($"TUT_{z:0}");
        if (frame != null)
        {
            var mover       = frame.gameObject.AddComponent<ObstacleMover>();
            mover.moveType  = moveType;
            mover.speed     = speed;
            mover.amplitude = moveType == ObstacleMover.MoveType.Rotation ? 35f : 10f;
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
        mat.SetFloat("_Metallic",   0.6f);
        mat.SetFloat("_Smoothness", 0.7f);
        mat.SetFloat("_Cull",       0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 2f);
        go.GetComponent<Renderer>().material = mat;

        var col = go.GetComponent<BoxCollider>();
        if (col) col.isTrigger = true;
        go.AddComponent<TunnelWall>();
    }

    static void AddLight(GameObject parent, Vector3 pos, Color color)
    {
        var go       = new GameObject("TLight");
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        var lt       = go.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.color     = color;
        lt.range     = 80f;
        lt.intensity = 3.5f;
    }
}
