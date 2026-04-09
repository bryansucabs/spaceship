using UnityEngine;

// TunnelGenerator.cs
// Genera el tunel octagonal en tiempo de ejecucion SOLO si no fue pre-construido en el Editor.
// Si el tunel ya existe en la escena (pre-construido con Tools > Build Full Scene),
// este script no hace nada y deja el tunel intacto.
public class TunnelGenerator : MonoBehaviour
{
    // Dimensiones fijas del tunel — deben coincidir con GameSceneBuilder.cs
    const float APOTHEM       = 55f;   // distancia del centro a cada cara del octagono
    const float TUNNEL_LENGTH = 7200f; // largo total del tunel en unidades de Unity
    const float START_Z       = 5f;    // posicion Z donde empieza el tunel
    const float THICKNESS     = 2.0f;  // grosor de cada panel de pared
    const int   SIDES         = 8;     // cantidad de lados del octagono

    // Unity llama Awake() al inicio del modo Play, antes que Start()
    void Awake()
    {
        // Si el tunel ya existe en la escena, no hacer nada
        // Esto permite pre-construirlo en Edit Mode con el menu Tools
        if (GameObject.Find("Tunnel") != null) return;

        // Si no existe, generarlo en runtime como fallback
        GenerateTunnel();
    }

    // Construye todos los paneles, anillos y tiras del tunel
    void GenerateTunnel()
    {
        // Ancho de cada panel: formula geometrica para el octogono
        float panelW = 2f * APOTHEM * Mathf.Tan(Mathf.PI / SIDES) + 0.6f;

        // Centro en Z de los paneles largos (mitad del tunel)
        float midZ = START_Z + TUNNEL_LENGTH / 2f;

        // Objeto raiz que agrupa todo el tunel en la Hierarchy
        var root = new GameObject("Tunnel");

        // Colores oscuros para las paredes — se alternan entre paneles
        Color colWallA = new Color(0.13f, 0.15f, 0.18f); // gris oscuro
        Color colWallB = new Color(0.10f, 0.16f, 0.13f); // verde militar oscuro
        Color colRing  = new Color(0.28f, 0.30f, 0.35f); // gris medio para anillos normales
        Color colGlow  = new Color(0.10f, 0.80f, 0.55f); // verde neon para anillos de referencia
        Color colStrip = new Color(0.10f, 0.90f, 0.35f); // verde brillante para tiras guia

        // ── 1. Crear las 8 paredes largas del tunel ──────────────────────────
        for (int i = 0; i < SIDES; i++)
        {
            // Angulo de este panel en radianes (0, 45, 90... 315 grados)
            float a = i * (360f / SIDES) * Mathf.Deg2Rad;

            // Crear el panel de pared
            var wall = MakeCube(root, $"Wall_{i}",
                new Vector3(Mathf.Sin(a) * APOTHEM, Mathf.Cos(a) * APOTHEM, midZ),
                new Vector3(panelW, THICKNESS, TUNNEL_LENGTH),
                new Vector3(0f, 0f, -i * (360f / SIDES)),
                i % 2 == 0 ? colWallA : colWallB);

            // La pared tiene colision tipo trigger para detectar cuando la nave la toca
            var col = wall.GetComponent<BoxCollider>();
            if (col) col.isTrigger = true;

            // TunnelWall quita vida a la nave cuando esta la toca
            wall.AddComponent<TunnelWall>();
        }

        // ── 2. Anillos decorativos cada 18 unidades ──────────────────────────
        int rings = Mathf.FloorToInt(TUNNEL_LENGTH / 18f);
        for (int r = 0; r <= rings; r++)
        {
            float z    = START_Z + r * 18f; // posicion Z de este anillo
            bool  glow = (r % 5 == 0);      // cada 5 anillos uno brillante de referencia

            for (int i = 0; i < SIDES; i++)
            {
                float a  = i * (360f / SIDES) * Mathf.Deg2Rad;
                var ring = MakeCube(root, $"Ring_{r}_{i}",
                    new Vector3(Mathf.Sin(a) * APOTHEM, Mathf.Cos(a) * APOTHEM, z),
                    new Vector3(panelW + 0.5f, THICKNESS + 1.2f, 1.1f),
                    new Vector3(0f, 0f, -i * (360f / SIDES)),
                    glow ? colGlow : colRing, emissive: glow);

                // Los anillos no tienen colision, son solo decorativos
                Destroy(ring.GetComponent<BoxCollider>());
            }
        }

        // ── 3. Tiras guia en suelo y techo ───────────────────────────────────
        // Ayudan al jugador a orientarse dentro del tunel
        foreach (float yo in new[] { -APOTHEM + 0.3f, APOTHEM - 0.3f })
        {
            for (int s = -1; s <= 1; s += 2)
            {
                var strip = MakeCube(root, $"Strip_{yo}_{s}",
                    new Vector3(s * 6f, yo, midZ),
                    new Vector3(0.5f, 0.15f, TUNNEL_LENGTH),
                    Vector3.zero, colStrip, emissive: true);
                Destroy(strip.GetComponent<BoxCollider>());
            }
        }

        // ── 4. Arco de entrada (3 anillos gruesos al inicio) ─────────────────
        for (int k = 0; k < 3; k++)
        {
            float ez   = START_Z - 1f - k * 2.5f; // un poco antes del tunel
            bool  edge = k == 0; // el primer anillo es el mas visible
            for (int i = 0; i < SIDES; i++)
            {
                float a = i * (360f / SIDES) * Mathf.Deg2Rad;
                var en  = MakeCube(root, $"Entry_{k}_{i}",
                    new Vector3(Mathf.Sin(a) * APOTHEM, Mathf.Cos(a) * APOTHEM, ez),
                    new Vector3(panelW + (edge ? 3f : 2f), THICKNESS + (edge ? 5f : 3f), edge ? 5f : 3f),
                    new Vector3(0f, 0f, -i * (360f / SIDES)),
                    edge ? colGlow : colRing, emissive: edge);
                Destroy(en.GetComponent<BoxCollider>());
            }
        }

        // Actualizar los limites de movimiento de la nave segun el tamanio del tunel
        SetShipBounds();

        Debug.Log($"[Tunnel] Generado en runtime — apothem={APOTHEM}, largo={TUNNEL_LENGTH}");
    }

    // Ajusta los limites X e Y de la nave para que coincidan con la pared interior del tunel
    void SetShipBounds()
    {
        var ship = GameObject.Find("PlayerShip");
        if (ship == null) return;
        var sc = ship.GetComponent<ShipController>();
        if (sc == null) return;

        // Cara interior del tunel = APOTHEM - THICKNESS/2 = 54u
        // xLimit = cara interior - ancho real del ala (4.68u)
        // yLimit = cara interior - alto real de la nave (0.74u)
        sc.xLimit = APOTHEM - THICKNESS * 0.5f - 4.68f;
        sc.yLimit = APOTHEM - THICKNESS * 0.5f - 0.74f;
    }

    // Crea un cubo con material Unlit (color uniforme sin importar la luz)
    GameObject MakeCube(GameObject parent, string name,
        Vector3 pos, Vector3 scale, Vector3 euler, Color color, bool emissive = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position    = pos;
        go.transform.localScale  = scale;
        go.transform.eulerAngles = euler;

        // Shader Unlit: el color se ve igual desde cualquier angulo, sin sombras
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);
        mat.SetFloat("_Cull", 0f); // doble cara: visible desde adentro y afuera

        // Si es emisivo brilla en la oscuridad (anillos de referencia y tiras guia)
        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.5f);
        }

        go.GetComponent<Renderer>().material = mat;
        return go;
    }
}
