using UnityEngine;

// ObstacleBuilder.cs
// Clase estatica con la logica de construccion de obstaculos.
// Es usada tanto por el menu del Editor (Tools > Build Full Scene)
// como por ObstacleGenerator.cs en tiempo de ejecucion como fallback.
// Cada tipo de obstaculo tiene un color unico para reconocerlo rapidamente.
// Los obstaculos se distribuyen aleatoriamente por todo el mapa.
public static class ObstacleBuilder
{
    // Radio interior del tunel (cara interna de la pared)
    const float INNER = 54f;

    // Grosor visual de cada obstaculo en el eje Z (profundidad)
    const float DEPTH = 6f;

    // Huecos para que la nave pueda pasar — mas grande = mas facil
    const float GAP_HALF  = 36f;
    const float GAP_3Q    = 30f;
    const float GAP_DIAG  = 38f;
    const float GAP_CROSS = 34f;

    // Posicion Z donde aparece el primer obstaculo
    const float START_Z = 450f;

    // Distancia entre obstaculos consecutivos (280u / 120u/s = 2.3 seg de reaccion)
    const float SPACING = 280f;

    // Total de obstaculos en todo el recorrido
    const int TOTAL = 22;

    // Secuencia FIJA de tipos — garantiza que todos los colores y formas
    // esten distribuidos por todo el mapa sin repetidos seguidos.
    // Los primeros 2 son mitades simples para aprender. Luego mezcla de todo.
    //   0=HalfH(naranja)  1=HalfV(rojo)      2=Diagonal(lima)
    //   3=TresC(cyan)     4=DobleM(violeta)   5=Cruz(amarillo)
    //   6=Zigzag(turq)    7=Puerta(verde)     8=DiagCorr(azul)
    // Secuencia FIJA — nunca dos del mismo tipo seguidos,
    // y el segundo ya es completamente diferente al primero.
    // 0=HalfH(naranja) 1=HalfV(rojo)   2=Diagonal(lima)  3=TresC(cyan)
    // 4=DobleM(violeta) 5=Cruz(amarillo) 6=Zigzag(turquesa)
    // 7=Puerta(verde)   8=DiagCorr(azul claro)
    static readonly int[] TYPE_SEQUENCE = {
        0,              // 1:  naranja  — mitad H (facil, aprende a esquivar)
        2,              // 2:  lima     — diagonal (totalmente diferente al anterior)
        1,              // 3:  rojo     — mitad V
        7,              // 4:  verde    — puerta con hueco al medio
        4,              // 5:  violeta  — doble mitad
        3,              // 6:  cyan     — tres cuartos
        8,              // 7:  azul     — corredor diagonal
        5,              // 8:  amarillo — cruz
        6,              // 9:  turquesa — zigzag
        0,              // 10: naranja  — mitad H
        4,              // 11: violeta  — doble mitad
        2,              // 12: lima     — diagonal
        1,              // 13: rojo     — mitad V
        8,              // 14: azul     — corredor diagonal
        3,              // 15: cyan     — tres cuartos
        7,              // 16: verde    — puerta
        5,              // 17: amarillo — cruz
        6,              // 18: turquesa — zigzag
        4,              // 19: violeta  — doble mitad
        0,              // 20: naranja  — mitad H
        3,              // 21: cyan     — tres cuartos
        8               // 22: azul     — corredor diagonal
    };

    // Punto de entrada: construye los obstaculos dentro de un GameObject padre
    public static void BuildObstaclesInto(GameObject root)
    {
        // rng solo para los DETALLES de cada obstaculo (cual pared, que diagonal, etc.)
        // El TIPO viene de TYPE_SEQUENCE — siempre el mismo orden, bien distribuido
        var rng = new System.Random(77);

        for (int i = 0; i < TOTAL; i++)
        {
            float z    = START_Z + i * SPACING + (float)(rng.NextDouble() - 0.5) * 45f;
            int   type = TYPE_SEQUENCE[i];
            Spawn(root, type, z, rng);
        }
    }

    // Crea el obstaculo del tipo indicado en la posicion Z dada
    static void Spawn(GameObject root, int type, float z, System.Random rng)
    {
        switch (type)
        {
            case 0: SpawnHalfH(root, z, rng);         break;
            case 1: SpawnHalfV(root, z, rng);         break;
            case 2: SpawnDiagonal(root, z, rng);      break;
            case 3: SpawnThreeQuarters(root, z, rng); break;
            case 4: SpawnDoubleMitad(root, z, rng);   break;
            case 5: SpawnCross(root, z);               break;
            case 6: SpawnZigzag(root, z, rng);        break;
            case 7: SpawnDoor(root, z, rng);           break;
            case 8: SpawnDiagCorridor(root, z, rng);  break;
        }
    }

    // ── Tipo 0: MITAD HORIZONTAL — NARANJA ───────────────────────────────────
    // Losa de la pared izquierda o derecha. Hueco en el lado opuesto.
    static void SpawnHalfH(GameObject root, float z, System.Random rng)
    {
        bool  fromLeft = rng.Next(0, 2) == 0;
        float sign     = fromLeft ? -1f : 1f;
        Color col      = new Color(0.95f, 0.45f, 0f); // naranja

        float slabW = INNER * 2f - GAP_HALF;
        float cx    = sign * (-INNER + slabW * 0.5f);

        Slab(root, $"HH_{z:0}", new Vector3(cx, 0f, z),
            new Vector3(slabW, INNER * 2.1f, DEPTH), col);
        AddLight(root, new Vector3(sign * INNER * 0.3f, 0f, z - 80f), col);
    }

    // ── Tipo 1: MITAD VERTICAL — ROJO ────────────────────────────────────────
    // Losa de la pared superior o inferior. Hueco en el lado opuesto.
    static void SpawnHalfV(GameObject root, float z, System.Random rng)
    {
        bool  fromTop = rng.Next(0, 2) == 0;
        float sign    = fromTop ? 1f : -1f;
        Color col     = new Color(0.9f, 0.1f, 0.15f); // rojo

        float slabH = INNER * 2f - GAP_HALF;
        float cy    = sign * (INNER - slabH * 0.5f);

        Slab(root, $"HV_{z:0}", new Vector3(0f, cy, z),
            new Vector3(INNER * 2.1f, slabH, DEPTH), col);
        AddLight(root, new Vector3(0f, sign * INNER * 0.3f, z - 80f), col);
    }

    // ── Tipo 2: DIAGONAL — VERDE LIMA ────────────────────────────────────────
    // Dos barras desde paredes diagonales opuestas. Hueco en el centro.
    static void SpawnDiagonal(GameObject root, float z, System.Random rng)
    {
        bool  slash  = rng.Next(0, 2) == 0;
        float ang    = slash ? 45f : -45f;
        Color col    = new Color(0.4f, 0.95f, 0f); // verde lima

        float barLen = INNER - GAP_DIAG * 0.5f;
        float wx     = (slash ? -1f : 1f) * INNER * 0.707f;
        float wy     = INNER * 0.707f;

        Vector3 inward = new Vector3(-wx, -wy, 0f).normalized;
        Vector3 c1 = new Vector3(wx, wy, z) + inward * (barLen * 0.5f);
        Vector3 c2 = new Vector3(-wx, -wy, z) - inward * (barLen * 0.5f);

        Slab(root, $"DA_{z:0}", c1, new Vector3(barLen * 1.5f, DEPTH * 4f, DEPTH),
            new Vector3(0f, 0f, ang), col);
        Slab(root, $"DB_{z:0}", c2, new Vector3(barLen * 1.5f, DEPTH * 4f, DEPTH),
            new Vector3(0f, 0f, ang), col);
        AddLight(root, new Vector3(0f, 0f, z - 80f), col);
    }

    // ── Tipo 3: TRES CUARTOS — AZUL CYAN ─────────────────────────────────────
    // Bloquea tres cuadrantes. Solo una esquina libre.
    static void SpawnThreeQuarters(GameObject root, float z, System.Random rng)
    {
        int   free = rng.Next(0, 4);
        float fx   = (free == 1 || free == 2) ? 1f : -1f;
        float fy   = (free == 0 || free == 1) ? 1f : -1f;
        Color col  = new Color(0f, 0.65f, 1f); // azul cyan

        float hW = INNER * 2f - GAP_3Q;
        float hX = -fx * (INNER - hW * 0.5f);
        Slab(root, $"3QH_{z:0}", new Vector3(hX, 0f, z),
            new Vector3(hW, INNER * 2.1f, DEPTH), col);

        float vH = INNER * 2f - GAP_3Q;
        float vY = -fy * (INNER - vH * 0.5f);
        float vW = INNER + GAP_3Q * 0.5f;
        float vX = fx * (INNER - vW * 0.5f);
        Slab(root, $"3QV_{z:0}", new Vector3(vX, vY, z),
            new Vector3(vW, vH, DEPTH), col);

        AddLight(root, new Vector3(fx * INNER * 0.55f, fy * INNER * 0.55f, z - 80f), col);
    }

    // ── Tipo 4: DOBLE MITAD — VIOLETA ────────────────────────────────────────
    // Dos losas desde paredes opuestas. Hueco en el centro.
    static void SpawnDoubleMitad(GameObject root, float z, System.Random rng)
    {
        bool  horiz   = rng.Next(0, 2) == 0;
        Color col     = new Color(0.55f, 0f, 0.9f); // violeta
        float slabLen = INNER - GAP_HALF * 0.5f;

        if (horiz)
        {
            Slab(root, $"DML_{z:0}", new Vector3(-INNER + slabLen * 0.5f, 0f, z),
                new Vector3(slabLen, INNER * 2.1f, DEPTH), col);
            Slab(root, $"DMR_{z:0}", new Vector3( INNER - slabLen * 0.5f, 0f, z),
                new Vector3(slabLen, INNER * 2.1f, DEPTH), col);
        }
        else
        {
            Slab(root, $"DMT_{z:0}", new Vector3(0f,  INNER - slabLen * 0.5f, z),
                new Vector3(INNER * 2.1f, slabLen, DEPTH), col);
            Slab(root, $"DMB_{z:0}", new Vector3(0f, -INNER + slabLen * 0.5f, z),
                new Vector3(INNER * 2.1f, slabLen, DEPTH), col);
        }
        AddLight(root, new Vector3(0f, 0f, z - 80f), col);
    }

    // ── Tipo 5: CRUZ — AMARILLO ───────────────────────────────────────────────
    // Cuatro barras desde las 4 paredes cardinales. Hueco en el centro.
    static void SpawnCross(GameObject root, float z)
    {
        float barLen = INNER - GAP_CROSS * 0.5f;
        float barW   = DEPTH * 3.5f;
        Color col    = new Color(0.95f, 0.95f, 0f); // amarillo

        Slab(root, $"CL_{z:0}", new Vector3(-INNER + barLen * 0.5f, 0f, z),
            new Vector3(barLen, barW, DEPTH), col);
        Slab(root, $"CR_{z:0}", new Vector3( INNER - barLen * 0.5f, 0f, z),
            new Vector3(barLen, barW, DEPTH), col);
        Slab(root, $"CT_{z:0}", new Vector3(0f,  INNER - barLen * 0.5f, z),
            new Vector3(barW, barLen, DEPTH), col);
        Slab(root, $"CB_{z:0}", new Vector3(0f, -INNER + barLen * 0.5f, z),
            new Vector3(barW, barLen, DEPTH), col);
        AddLight(root, new Vector3(0f, 0f, z - 80f), col);
    }

    // ── Tipo 6: ZIGZAG — TURQUESA ────────────────────────────────────────────
    // Diagonal seguida de una mitad 60u despues. Doble esquive.
    static void SpawnZigzag(GameObject root, float z, System.Random rng)
    {
        SpawnDiagonal(root, z, rng);

        bool  fromLeft = rng.Next(0, 2) == 0;
        float sign     = fromLeft ? -1f : 1f;
        float slabW    = INNER * 2f - GAP_HALF;
        float cx       = sign * (-INNER + slabW * 0.5f);
        Color col      = new Color(0f, 0.85f, 0.65f); // turquesa

        Slab(root, $"ZZ_{z:0}", new Vector3(cx, 0f, z + 60f),
            new Vector3(slabW, INNER * 2.1f, DEPTH), col);
    }

    // ── Tipo 7: PUERTA — VERDE BRILLANTE ─────────────────────────────────────
    // Barra completa de pared a pared con un hueco en posicion ALEATORIA.
    // El hueco no siempre esta en el borde — hay que leer donde esta y reaccionar.
    static void SpawnDoor(GameObject root, float z, System.Random rng)
    {
        bool  horiz = rng.Next(0, 2) == 0;
        Color col   = new Color(0.1f, 0.9f, 0.3f); // verde brillante
        float gapH  = GAP_HALF;

        // Centro del hueco: aleatorio en la zona media del tunel
        float holeCenter = (float)(rng.NextDouble() * 2.0 - 1.0) * INNER * 0.5f;

        float slabALen = (holeCenter - gapH * 0.5f) + INNER;
        float slabBLen = INNER - (holeCenter + gapH * 0.5f);

        if (horiz)
        {
            if (slabALen > 1f)
                Slab(root, $"PUA_{z:0}",
                    new Vector3(-INNER + slabALen * 0.5f, 0f, z),
                    new Vector3(slabALen, INNER * 2.1f, DEPTH), col);
            if (slabBLen > 1f)
                Slab(root, $"PUB_{z:0}",
                    new Vector3(holeCenter + gapH * 0.5f + slabBLen * 0.5f, 0f, z),
                    new Vector3(slabBLen, INNER * 2.1f, DEPTH), col);
        }
        else
        {
            if (slabALen > 1f)
                Slab(root, $"PUA_{z:0}",
                    new Vector3(0f, -INNER + slabALen * 0.5f, z),
                    new Vector3(INNER * 2.1f, slabALen, DEPTH), col);
            if (slabBLen > 1f)
                Slab(root, $"PUB_{z:0}",
                    new Vector3(0f, holeCenter + gapH * 0.5f + slabBLen * 0.5f, z),
                    new Vector3(INNER * 2.1f, slabBLen, DEPTH), col);
        }

        float lx = horiz ? holeCenter : 0f;
        float ly = horiz ? 0f : holeCenter;
        AddLight(root, new Vector3(lx, ly, z - 80f), col);
    }

    // ── Tipo 8: CORREDOR DIAGONAL — AZUL CLARO ───────────────────────────────
    // Dos barras paralelas diagonales que cruzan todo el tunel.
    // La nave debe volar en diagonal para pasar por el espacio entre ellas.
    static void SpawnDiagCorridor(GameObject root, float z, System.Random rng)
    {
        bool  slash     = rng.Next(0, 2) == 0;
        float ang       = slash ? 45f : -45f;
        Color col       = new Color(0f, 0.7f, 1f); // azul claro

        float barLen    = INNER * 3.0f;
        float barThick  = DEPTH * 3.5f;
        float separation = (GAP_DIAG + barThick) * 0.5f;

        float px = slash ? -1f : 1f;
        float py = 1f;
        float pLen = Mathf.Sqrt(px * px + py * py);
        px /= pLen; py /= pLen;

        Vector3 c1 = new Vector3( px * separation,  py * separation, z);
        Vector3 c2 = new Vector3(-px * separation, -py * separation, z);

        Slab(root, $"DCA_{z:0}", c1, new Vector3(barLen, barThick, DEPTH),
            new Vector3(0f, 0f, ang), col);
        Slab(root, $"DCB_{z:0}", c2, new Vector3(barLen, barThick, DEPTH),
            new Vector3(0f, 0f, ang), col);
        AddLight(root, new Vector3(0f, 0f, z - 80f), col);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Color")
                  ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);
        mat.SetFloat("_Cull", 0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 8f);
        go.GetComponent<Renderer>().material = mat;

        var col = go.GetComponent<BoxCollider>();
        if (col) col.isTrigger = true;
        go.AddComponent<TunnelWall>();
        go.AddComponent<ObstacleGlow>();
        return go;
    }

    public static void AddLight(GameObject parent, Vector3 pos, Color color)
    {
        var go = new GameObject("OLight");
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;
        var lt = go.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.color     = color;
        lt.range     = 100f;
        lt.intensity = 5f;
    }
}
