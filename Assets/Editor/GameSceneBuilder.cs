using UnityEngine;
using UnityEditor;                    // Solo disponible en el Editor — necesario para MenuItem y Undo
using UnityEditor.SceneManagement;    // Para marcar la escena como modificada (MarkSceneDirty)
using UnityEngine.SceneManagement;    // Para obtener la escena activa

// GameSceneBuilder.cs
// Script del Editor (NO se incluye en el juego final).
// Agrega la opcion "Tools > Build Full Scene" al menu de Unity.
// Al ejecutarlo, construye el tunel y los obstaculos directamente en Edit Mode,
// sin necesidad de presionar Play. Todo queda guardado en la escena.
// Nota: este archivo debe estar en la carpeta Assets/Editor/
public static class GameSceneBuilder
{
    // Dimensiones del tunel — deben coincidir exactamente con TunnelGenerator.cs
    const float APOTHEM       = 55f;    // distancia del centro a cada cara del octagono
    const float TUNNEL_LENGTH = 7200f;  // largo total del tunel
    const float START_Z       = 5f;     // posicion Z del inicio del tunel
    const float THICKNESS     = 2.0f;   // grosor de cada panel de pared
    const int   SIDES         = 8;      // numero de lados del octagono

    // [MenuItem] registra este metodo como una opcion en el menu de Unity
    // "Tools/Build Full Scene" aparece en la barra de menus superior
    [MenuItem("Tools/Build Full Scene")]
    public static void BuildAll()
    {
        // ── Paso 1: Eliminar objetos anteriores para evitar duplicados ────────
        // Undo.DestroyObjectImmediate permite deshacer con Ctrl+Z si algo sale mal
        Cleanup("Tunnel");
        Cleanup("Obstacles");
        Cleanup("TunnelGenerator");     // eliminar el componente runtime si existe
        Cleanup("ObstacleGenerator");   // eliminar el componente runtime si existe

        // ── Paso 2: Construir el tunel octagonal ──────────────────────────────
        BuildTunnel();

        // ── Paso 3: Construir los obstaculos ──────────────────────────────────
        var obsRoot = new GameObject("Obstacles");
        Undo.RegisterCreatedObjectUndo(obsRoot, "Build Obstacles"); // registrar para Ctrl+Z
        ObstacleBuilder.BuildObstaclesInto(obsRoot); // reutilizar la misma logica que en runtime

        // ── Paso 4: Configurar la camara ──────────────────────────────────────
        var cam = Camera.main;
        if (cam != null)
        {
            // Posicion inicial de la camara (detras y arriba de donde estara la nave)
            cam.transform.position    = new Vector3(0f, 5f, -22f);
            cam.transform.eulerAngles = new Vector3(10f, 0f, 0f); // inclinada hacia abajo

            // Usar el Skybox como fondo en lugar de color solido
            cam.clearFlags = CameraClearFlags.Skybox;

            // Agregar o reutilizar el script CameraFollow
            var cf = cam.GetComponent<CameraFollow>() ?? cam.gameObject.AddComponent<CameraFollow>();

            // Conectar la camara a la nave si ya existe en la escena
            var ship = GameObject.Find("PlayerShip");
            if (ship != null) cf.target = ship.transform;
        }

        // ── Paso 5: Configurar la nave ────────────────────────────────────────
        var shipGo = GameObject.Find("PlayerShip");
        if (shipGo != null)
        {
            // Colocar la nave en el origen al inicio del tunel
            shipGo.transform.position = Vector3.zero;

            // Asegurarse de que tiene Rigidbody configurado correctamente
            var rb = shipGo.GetComponent<Rigidbody>() ?? shipGo.AddComponent<Rigidbody>();
            rb.useGravity  = false; // sin gravedad
            rb.isKinematic = true;  // movida por script, no por fisica
            

            // Remplazado por logica en ShipController:
            // Establecer los limites de movimiento correctos para el tunel actual
            // xLimit = apothem - grosor/2 - ancho real del ala (4.68u)
            // yLimit = apothem - grosor/2 - alto real de la nave (0.74u)
            /*
            var sc = shipGo.GetComponent<ShipController>();
            if (sc != null)
            {
                sc.xLimit = APOTHEM - THICKNESS * 0.5f - 4.68f; // = 49.32u
                sc.yLimit = APOTHEM - THICKNESS * 0.5f - 0.74f; // = 53.26u
                EditorUtility.SetDirty(shipGo); // marcar para que Unity guarde los valores
            }
            */
        }

        // ── Paso 6: Marcar la escena como modificada para que Unity la guarde ─
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[Builder] Escena construida. Guarda con Ctrl+S.");
    }

    // Construye el tunel octagonal completo con paredes, anillos y tiras guia
    static void BuildTunnel()
    {
        // Ancho de cada panel de pared — formula geometrica para el octagono regular
        float panelW = 2f * APOTHEM * Mathf.Tan(Mathf.PI / SIDES) + 0.6f;

        // Posicion Z del centro de los paneles largos (en el medio del tunel)
        float midZ   = START_Z + TUNNEL_LENGTH / 2f;

        // Crear el objeto raiz que agrupa todos los elementos del tunel
        var root = new GameObject("Tunnel");
        Undo.RegisterCreatedObjectUndo(root, "Build Tunnel"); // registrar para Ctrl+Z

        // Colores del tunel
        Color colWallA = new Color(0.13f, 0.15f, 0.18f); // gris oscuro (paneles pares)
        Color colWallB = new Color(0.10f, 0.16f, 0.13f); // verde militar oscuro (paneles impares)
        Color colRing  = new Color(0.28f, 0.30f, 0.35f); // gris medio para anillos normales
        Color colGlow  = new Color(0.10f, 0.80f, 0.55f); // verde neon para anillos de referencia
        Color colStrip = new Color(0.10f, 0.90f, 0.35f); // verde brillante para tiras guia

        // ── 1. Crear las 8 paredes largas del tunel ──────────────────────────
        for (int i = 0; i < SIDES; i++)
        {
            // Angulo de este panel en radianes (0, 45, 90... 315 grados)
            float a  = i * (360f / SIDES) * Mathf.Deg2Rad;

            // Crear el panel de pared
            var wall = MakeEditorCube(root, $"Wall_{i}",
                new Vector3(Mathf.Sin(a) * APOTHEM, Mathf.Cos(a) * APOTHEM, midZ),
                new Vector3(panelW, THICKNESS, TUNNEL_LENGTH),
                new Vector3(0f, 0f, -i * (360f / SIDES)),
                i % 2 == 0 ? colWallA : colWallB); // alternar colores

            // Configurar el collider como trigger para detectar cuando la nave toca la pared
            var col = wall.GetComponent<BoxCollider>();
            if (col) col.isTrigger = true;

            // Agregar TunnelWall para que la pared haga dano al ser tocada
            wall.AddComponent<TunnelWall>();
        }

        // ── 2. Anillos decorativos cada 18 unidades ──────────────────────────
        // Ayudan visualmente a percibir la velocidad de avance
        int rings = Mathf.FloorToInt(TUNNEL_LENGTH / 18f);
        for (int r = 0; r <= rings; r++)
        {
            float z    = START_Z + r * 18f; // posicion Z de cada anillo
            bool  glow = (r % 5 == 0);      // cada 5 anillos uno brilla en verde (referencia visual)

            for (int i = 0; i < SIDES; i++)
            {
                float a   = i * (360f / SIDES) * Mathf.Deg2Rad;
                var ring  = MakeEditorCube(root, $"Ring_{r}_{i}",
                    new Vector3(Mathf.Sin(a) * APOTHEM, Mathf.Cos(a) * APOTHEM, z),
                    new Vector3(panelW + 0.5f, THICKNESS + 1.2f, 1.1f),
                    new Vector3(0f, 0f, -i * (360f / SIDES)),
                    glow ? colGlow : colRing, emissive: glow);

                // Los anillos son solo decorativos — eliminar su collider
                Object.DestroyImmediate(ring.GetComponent<BoxCollider>());
            }
        }

        // ── 3. Tiras guia en suelo y techo ───────────────────────────────────
        // Dos lineas verdes en el suelo y dos en el techo ayudan al jugador a orientarse
        foreach (float yo in new[] { -APOTHEM + 0.3f, APOTHEM - 0.3f })
            for (int s = -1; s <= 1; s += 2) // s = -1 (izquierda) y s = +1 (derecha)
            {
                var strip = MakeEditorCube(root, $"Strip_{yo}_{s}",
                    new Vector3(s * 6f, yo, midZ),
                    new Vector3(0.5f, 0.15f, TUNNEL_LENGTH),
                    Vector3.zero, colStrip, emissive: true);

                // Las tiras no tienen colision
                Object.DestroyImmediate(strip.GetComponent<BoxCollider>());
            }

        // ── 4. Arco de entrada (3 anillos gruesos al inicio del tunel) ────────
        // Indica visualmente donde empieza el tunel
        for (int k = 0; k < 3; k++)
        {
            float ez  = START_Z - 1f - k * 2.5f; // un poco antes del inicio del tunel
            bool  edge = k == 0; // el primer anillo es el mas grande y brillante

            for (int i = 0; i < SIDES; i++)
            {
                float a = i * (360f / SIDES) * Mathf.Deg2Rad;
                var en  = MakeEditorCube(root, $"Entry_{k}_{i}",
                    new Vector3(Mathf.Sin(a) * APOTHEM, Mathf.Cos(a) * APOTHEM, ez),
                    new Vector3(panelW + (edge ? 3f : 2f), THICKNESS + (edge ? 5f : 3f), edge ? 5f : 3f),
                    new Vector3(0f, 0f, -i * (360f / SIDES)),
                    edge ? colGlow : colRing, emissive: edge);

                // El arco de entrada tampoco tiene colision
                Object.DestroyImmediate(en.GetComponent<BoxCollider>());
            }
        }
    }

    // Crea un cubo con material Unlit (color uniforme sin importar la luz)
    // Este metodo es identico a MakeCube() en TunnelGenerator.cs pero funciona en Edit Mode
    // La diferencia clave: usa sharedMaterial en lugar de material para no crear instancias
    static GameObject MakeEditorCube(GameObject parent, string name,
        Vector3 pos, Vector3 scale, Vector3 euler, Color color, bool emissive = false)
    {
        // Crear un cubo primitivo de Unity
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform); // hacerlo hijo del objeto raiz
        go.transform.position    = pos;
        go.transform.localScale  = scale;
        go.transform.eulerAngles = euler;

        // Buscar el shader Unlit de URP, con fallback al Standard
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.SetColor("_BaseColor", color); // color principal en URP
        mat.SetColor("_Color",     color); // color principal en Standard shader
        mat.SetFloat("_Cull", 0f);         // doble cara: visible desde adentro y afuera

        // Si el objeto debe emitir luz (brillar), habilitar la emision
        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.5f); // brillar 2.5 veces el color base
        }

        // Asignar el material — sharedMaterial evita crear copias innecesarias en Edit Mode
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    // Elimina un GameObject de la escena por nombre si existe
    // Usa Undo.DestroyObjectImmediate para poder deshacer con Ctrl+Z
    static void Cleanup(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Undo.DestroyObjectImmediate(go);
    }
}
