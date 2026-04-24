using UnityEngine;

// TutorialObstacleBuilder.cs
// Genera los obstaculos del modo tutorial en 3 fases progresivas:
//   Fase 1 (obstaculos 1-5):  solo arriba/abajo   — aprende eje vertical
//   Fase 2 (obstaculos 6-10): solo izquierda/derecha — aprende eje horizontal
//   Fase 3 (obstaculos 11-18): mezcla de ambos, huecos mas pequenos
public static class TutorialObstacleBuilder
{
    const float INNER    = 54f;
    const float DEPTH    = 6f;
    const float GAP_F1   = 50f;   // hueco grande fase 1 (muy facil)
    const float GAP_F2   = 50f;   // hueco grande fase 2 (muy facil)
    const float GAP_F3A  = 43f;   // hueco medio fase 3 inicio
    const float GAP_F3B  = 38f;   // hueco menor fase 3 final (un poco mas dificil)
    const float START_Z  = 400f;
    const float SPACING  = 360f;  // mas espacio entre obstaculos para reaccionar

    public static void BuildTutorialObstacles(GameObject root)
    {
        var rng = new System.Random(99);

        // ── FASE 1: 5 obstaculos, solo movimiento vertical (arriba/abajo) ──────
        // El obstaculo bloquea casi todo excepto una franja arriba o abajo
        for (int i = 0; i < 5; i++)
        {
            float z      = START_Z + i * SPACING;
            bool  top    = (i % 2 == 0); // alterna: abertura arriba, abertura abajo
            SpawnVertical(root, z, top, GAP_F1, new Color(0f, 0.75f, 1f)); // cyan
        }

        // ── FASE 2: 5 obstaculos, solo movimiento horizontal (izquierda/derecha) ─
        for (int i = 0; i < 5; i++)
        {
            float z    = START_Z + (5 + i) * SPACING;
            bool  left = (i % 2 == 0); // alterna: abertura izquierda, abertura derecha
            SpawnHorizontal(root, z, left, GAP_F2, new Color(1f, 0.75f, 0f)); // amarillo
        }

        // ── FASE 3: 8 obstaculos mixtos, huecos progresivamente mas pequenos ─────
        // Los primeros 4 mantienen el hueco grande, los ultimos 4 lo reducen
        bool[] topArr  = { true, false, true, false, true, false, true, false };
        bool[] leftArr = { true, false, false, true, false, true, true, false };

        for (int i = 0; i < 8; i++)
        {
            float z   = START_Z + (10 + i) * SPACING;
            float gap = i < 4 ? GAP_F3A : GAP_F3B;

            // Alternar entre obstaculos verticales y horizontales en la fase 3
            if (i % 2 == 0)
                SpawnVertical(root, z, topArr[i], gap, new Color(0.5f, 0f, 1f));   // violeta
            else
                SpawnHorizontal(root, z, leftArr[i], gap, new Color(0f, 0.85f, 0.4f)); // verde
        }
    }

    // Obstaculo vertical: slab desde la pared superior o inferior
    // La abertura queda en el lado opuesto (jugador mueve arriba o abajo)
    static void SpawnVertical(GameObject root, float z, bool aperturaArriba, float gap, Color col)
    {
        // sign positivo = slab desde arriba, sign negativo = slab desde abajo
        float sign  = aperturaArriba ? -1f : 1f;
        float slabH = INNER * 2f - gap;
        float cy    = sign * (INNER - slabH * 0.5f);

        ObstacleBuilder.Slab(root, $"TUT_V_{z:0}",
            new Vector3(0f, cy, z),
            new Vector3(INNER * 2.1f, slabH, DEPTH), col);

        // Luz en la abertura para que el jugador vea donde ir
        ObstacleBuilder.AddLight(root,
            new Vector3(0f, -sign * INNER * 0.45f, z - 100f), col);
    }

    // Obstaculo horizontal: slab desde la pared izquierda o derecha
    // La abertura queda en el lado opuesto (jugador mueve izquierda o derecha)
    static void SpawnHorizontal(GameObject root, float z, bool aperturaIzquierda, float gap, Color col)
    {
        float sign  = aperturaIzquierda ? 1f : -1f;
        float slabW = INNER * 2f - gap;
        float cx    = sign * (-INNER + slabW * 0.5f);

        ObstacleBuilder.Slab(root, $"TUT_H_{z:0}",
            new Vector3(cx, 0f, z),
            new Vector3(slabW, INNER * 2.1f, DEPTH), col);

        ObstacleBuilder.AddLight(root,
            new Vector3(-sign * INNER * 0.45f, 0f, z - 100f), col);
    }
}
