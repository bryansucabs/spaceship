using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool IsPlaying { get; private set; }

    private ShipHealth _shipHealth;
    private GUIStyle _hudStyle;
    private bool _stylesInitialized = false;
    private bool _gameEndedByDestroy = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (ObjectiveManager.Instance == null)
        {
            var go = new GameObject("ObjectiveManager");
            go.AddComponent<ObjectiveManager>();
        }

        IsPlaying = true;
        _shipHealth = FindFirstObjectByType<ShipHealth>();
    }

    void Update()
    {
        if (IsPlaying) return;

        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GameOver(string message)
    {
        IsPlaying = false;
        _gameEndedByDestroy = true;

        // Notificar a TODOS los clientes: atacante gana (winnerResult=1).
        // RpcTarget.All incluye esta misma máquina, así que no hace falta
        // llamar a ObjectiveManager localmente por separado.
        var spawner = FindFirstObjectByType<GameSpawnManager>();
        if (spawner != null && spawner.photonView != null)
        {
            spawner.photonView.RPC(
                nameof(GameSpawnManager.RPC_GameOver), Photon.Pun.RpcTarget.All, 1);
        }
        else if (ObjectiveManager.Instance != null)
        {
            ObjectiveManager.Instance.isGameOver = true;
            ObjectiveManager.Instance.NotifyGameOverExternal("GAME OVER\nUna nave fue destruida.");
        }
    }

    public void NotifyGameOver()
    {
        IsPlaying = false;
    }

    public void NotifyGameOverExternal(string message)
    {
        IsPlaying = false;
    }

    void OnGUI()
    {
        InitStyles();

        int health = _shipHealth != null ? _shipHealth.currentHealth : 0;
        int maxHp = _shipHealth != null ? _shipHealth.maxHealth : 5;

        string hearts = "";
        for (int i = 0; i < maxHp; i++)
            hearts += i < health ? "\u2665 " : "\u2661 ";
        GUI.Label(new Rect(20, Screen.height - 50, 300, 40), hearts, _hudStyle);
    }

    void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        _hudStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 26,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.3f, 0.3f) }
        };
    }
}
