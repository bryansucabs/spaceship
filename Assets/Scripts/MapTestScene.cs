using UnityEngine;

public class MapTestScene : MonoBehaviour
{
    [Header("Prefabs — Assets/EnvironmentPack/Prefabs/")]
    public GameObject prefabStraight;
    public GameObject prefabLTurn;
    public GameObject prefabTJunction;
    public GameObject prefabEnd;

    [Header("Tamaño del segmento")]
    [Tooltip("Largo de un corredor en unidades Unity. Ajustar hasta que encajen sin huecos.")]
    public float segLen = 100f;

    [Tooltip("Multiplicador de escala sobre la escala base del prefab (200). Con 5 los corredores tienen ancho ~110 u (mismo que el túnel octogonal).")]
    public float corridorScale = 5f;

    [Header("Cantidad de tramos")]
    public int approachCount = 8;
    public int branchCount = 6;

    void Awake()
    {
        if (GameObject.Find("MapRoot") != null) return;

        // Destruir el sistema de túnel viejo
        foreach (var tw in FindObjectsByType<TunnelWall>(FindObjectsSortMode.None))
            Destroy(tw.gameObject);

        var oldTube = GameObject.Find("TubeRoot");
        if (oldTube != null) Destroy(oldTube);

        var gb = FindAnyObjectByType<GameBootstrap>();
        if (gb != null) Destroy(gb.gameObject);

        var ast = FindAnyObjectByType<AsteroidSpawner>();
        if (ast != null) Destroy(ast.gameObject);

        var gm = FindAnyObjectByType<GameManager>();
        if (gm != null) Destroy(gm.gameObject);

        var accel = FindAnyObjectByType<AccelHUD>();
        if (accel != null) Destroy(accel.gameObject);

        if (!ValidarPrefabs()) return;
        BuildMap();
    }

    void BuildMap()
    {
        var root = new GameObject("MapRoot");

        for (int i = 0; i < approachCount; i++)
            Colocar(root, prefabStraight, new Vector3(0f, 0f, i * segLen), 0f);

        float fZ = approachCount * segLen;

        Colocar(root, prefabTJunction, new Vector3(0f, 0f, fZ), 0f);

        Vector3 rBase = new Vector3(segLen, 0f, fZ);
        Colocar(root, prefabLTurn, rBase, 90f);
        for (int i = 1; i <= branchCount; i++)
            Colocar(root, prefabStraight, rBase + new Vector3(0f, 0f, i * segLen), 0f);
        Colocar(root, prefabEnd, rBase + new Vector3(0f, 0f, (branchCount + 1) * segLen), 180f);

        Vector3 lBase = new Vector3(-segLen, 0f, fZ);
        Colocar(root, prefabLTurn, lBase, -90f);
        for (int i = 1; i <= branchCount; i++)
            Colocar(root, prefabStraight, lBase + new Vector3(0f, 0f, i * segLen), 0f);
        Colocar(root, prefabEnd, lBase + new Vector3(0f, 0f, (branchCount + 1) * segLen), 180f);

        Debug.Log($"[MapTest] Mapa construido — {approachCount} tramos, T-fork en Z={fZ}, {branchCount} por rama. Escala={corridorScale} segLen={segLen}");
    }

    void Colocar(GameObject parent, GameObject prefab, Vector3 pos, float yaw)
    {
        var go = Instantiate(prefab, pos, Quaternion.Euler(-90f, yaw, 0f));
        go.transform.localScale *= corridorScale;
        go.transform.SetParent(parent.transform);
        go.name = prefab.name;
    }

    bool ValidarPrefabs()
    {
        bool ok = true;
        if (prefabStraight  == null) { Debug.LogError("[MapTest] Falta: prefabStraight");  ok = false; }
        if (prefabLTurn     == null) { Debug.LogError("[MapTest] Falta: prefabLTurn");      ok = false; }
        if (prefabTJunction == null) { Debug.LogError("[MapTest] Falta: prefabTJunction"); ok = false; }
        if (prefabEnd       == null) { Debug.LogError("[MapTest] Falta: prefabEnd");        ok = false; }
        return ok;
    }
}
