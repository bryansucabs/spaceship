using UnityEngine;

// TubeSegmentBuilder.cs
// Genera tubos octagonales independientes para el mapa de carrera.
// Cada tubo tiene numero, posicion, rotacion y largo configurables.
// Se construye en runtime al hacer Play si no existe ya en escena.
public class TubeSegmentBuilder : MonoBehaviour
{
    const float APOTHEM   = 55f;
    const float THICKNESS = 2f;
    const int   SIDES     = 8;

    void Awake()
    {
        if (GameObject.Find("TubeRoot") != null) return;
        BuildTestFork();
    }

    void BuildTestFork()
    {
        var root = new GameObject("TubeRoot");

        // Tubo 1 — diagonal derecha (nave roja)
        BuildTube(root, 1,
            startPos:  new Vector3(0f, 0f, 0f),
            yawDeg:    30f,
            length:    400f,
            wallColor: new Color(0.55f, 0.05f, 0.05f),
            ringColor: new Color(1f, 0.15f, 0.15f));

        // Tubo 2 — diagonal izquierda (nave azul)
        BuildTube(root, 2,
            startPos:  new Vector3(0f, 0f, 0f),
            yawDeg:    -30f,
            length:    400f,
            wallColor: new Color(0.05f, 0.05f, 0.55f),
            ringColor: new Color(0.15f, 0.15f, 1f));
    }

    void BuildTube(GameObject root, int number, Vector3 startPos, float yawDeg, float length,
                   Color wallColor, Color ringColor)
    {
        float panelW = 2f * APOTHEM * Mathf.Tan(Mathf.PI / SIDES) + 0.6f;
        float midZ   = length / 2f;

        var tube = new GameObject($"Tube_{number}");
        tube.transform.SetParent(root.transform);
        tube.transform.position    = startPos;
        tube.transform.eulerAngles = new Vector3(0f, yawDeg, 0f);

        // 8 paredes
        for (int i = 0; i < SIDES; i++)
        {
            float a    = i * (360f / SIDES) * Mathf.Deg2Rad;
            var   wall = MakeCube(tube, $"Wall_{i}",
                new Vector3(Mathf.Sin(a) * APOTHEM, Mathf.Cos(a) * APOTHEM, midZ),
                new Vector3(panelW, THICKNESS, length),
                new Vector3(0f, 0f, -i * (360f / SIDES)),
                wallColor);

            var col = wall.GetComponent<BoxCollider>();
            if (col) col.isTrigger = true;
            wall.AddComponent<TunnelWall>();
        }

        // Anillos decorativos cada 18 unidades
        int rings = Mathf.FloorToInt(length / 18f);
        for (int r = 0; r <= rings; r++)
        {
            float z = r * 18f;
            for (int i = 0; i < SIDES; i++)
            {
                float a    = i * (360f / SIDES) * Mathf.Deg2Rad;
                var   ring = MakeCube(tube, $"Ring_{r}_{i}",
                    new Vector3(Mathf.Sin(a) * APOTHEM, Mathf.Cos(a) * APOTHEM, z),
                    new Vector3(panelW + 0.5f, THICKNESS + 1.2f, 1.1f),
                    new Vector3(0f, 0f, -i * (360f / SIDES)),
                    ringColor, emissive: true);
                Destroy(ring.GetComponent<BoxCollider>());
            }
        }

        // Numero en la entrada del tubo (TextMesh 3D)
        var labelGo = new GameObject($"Label_Tube{number}");
        labelGo.transform.SetParent(tube.transform);
        labelGo.transform.localPosition = new Vector3(0f, 0f, 5f);
        labelGo.transform.localRotation = Quaternion.identity;
        labelGo.transform.localScale    = Vector3.one * 8f;

        var tm = labelGo.AddComponent<TextMesh>();
        tm.text      = number.ToString();
        tm.fontSize  = 24;
        tm.color     = ringColor;
        tm.anchor    = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;

        Debug.Log($"[TubeBuilder] Tubo {number} generado — yaw={yawDeg}°, largo={length}u");
    }

    GameObject MakeCube(GameObject parent, string name,
        Vector3 localPos, Vector3 scale, Vector3 localEuler, Color color, bool emissive = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition    = localPos;
        go.transform.localScale       = scale;
        go.transform.localEulerAngles = localEuler;

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);
        mat.SetFloat("_Cull", 0f);

        if (emissive)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.5f);
        }

        go.GetComponent<Renderer>().material = mat;
        return go;
    }
}
