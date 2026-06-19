using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using UnityEngine.SceneManagement;

public enum ShipObjective
{
    GoToDoor,
    WaitForDoor,
    StandInCircle,
    GoToExtraction,
    Completed,
    Failed
}

public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance;

    [Header("Tiempo de partida (5 minutos)")]
    public float gameDuration = 300f;

    public ShipObjective redShipObjective = ShipObjective.GoToDoor;
    public ShipObjective blueShipObjective = ShipObjective.GoToDoor;

    public bool isGameOver = false;
    public int winner = -1;

    private float _timeLeft;
    private string _endMessage = "";

    private GUIStyle _timerStyle;
    private GUIStyle _endStyle;
    private bool _stylesInitialized = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _timeLeft = gameDuration;
    }

    void Update()
    {
        if (isGameOver)
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f;
            DeclareP3Victory();
        }
    }

    PhotonView GetSpawnerPV()
    {
        var spawner = FindFirstObjectByType<GameSpawnManager>();
        return spawner != null ? spawner.photonView : null;
    }

    public void UpdateObjective(string role, ShipObjective objective)
    {
        if (role == "redship")
            redShipObjective = objective;
        else if (role == "blueship")
            blueShipObjective = objective;

        if (redShipObjective == ShipObjective.Completed && blueShipObjective == ShipObjective.Completed)
        {
            DeclareP1P2Victory();
            return;
        }

        var pv = GetSpawnerPV();
        if (pv != null && pv.IsMine)
            pv.RPC(nameof(GameSpawnManager.RPC_SyncObjective), RpcTarget.Others, role, (int)objective);
    }

    void DeclareP1P2Victory()
    {
        isGameOver = true;
        winner = 0;

        var pv = GetSpawnerPV();
        if (pv != null && pv.IsMine)
            pv.RPC(nameof(GameSpawnManager.RPC_GameOver), RpcTarget.All, 0);
        else
            SetEndMessageForRole(0);
    }

    void DeclareP3Victory()
    {
        isGameOver = true;
        winner = 1;

        var pv = GetSpawnerPV();
        if (pv != null && pv.IsMine)
            pv.RPC(nameof(GameSpawnManager.RPC_GameOver), RpcTarget.All, 1);
        else
            SetEndMessageForRole(1);
    }

    public void NotifyGameOverExternal(string message)
    {
        isGameOver = true;
        _endMessage = message;
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyGameOver();
    }

    public void SetEndMessageForRole(int winnerResult)
    {
        string role = "";
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("rol"))
            role = PhotonNetwork.LocalPlayer.CustomProperties["rol"].ToString();

        if (winnerResult == 0)
        {
            if (role == "redship" || role == "blueship")
                _endMessage = "VICTORIA - Han escapado con exito!";
            else if (role == "overlord")
                _endMessage = "DERROTA - Los jugadores escaparon.";
            else
                _endMessage = "VICTORIA - Jugadores 1 y 2 ganan.";
        }
        else
        {
            if (role == "overlord")
                _endMessage = "VICTORIA - Los jugadores no escaparon a tiempo.";
            else if (role == "redship" || role == "blueship")
                _endMessage = "DERROTA - El tiempo se ha agotado.";
            else
                _endMessage = "DERROTA - El tiempo se ha agotado.";
        }

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyGameOver();
    }

    void OnGUI()
    {
        InitStyles();

        int mins = Mathf.FloorToInt(_timeLeft / 60f);
        int secs = Mathf.FloorToInt(_timeLeft % 60f);

        Color timerColor = _timeLeft <= 30f ? Color.red : Color.white;
        _timerStyle.normal.textColor = timerColor;

        GUI.Label(new Rect(Screen.width / 2f - 70, 15, 140, 45), $"{mins:00}:{secs:00}", _timerStyle);

        if (isGameOver)
        {
            float bw = 520, bh = 200;
            float bx = (Screen.width - bw) / 2f;
            float by = (Screen.height - bh) / 2f;

            GUI.Box(new Rect(bx - 10, by - 10, bw + 20, bh + 20), "");
            GUI.Label(new Rect(bx, by + 30, bw, 80), _endMessage, _endStyle);
            GUI.Label(new Rect(bx, by + 130, bw, 40), "Presiona R para reiniciar",
                new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.gray } });
        }
    }

    void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        _timerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        _endStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 30,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.yellow }
        };
    }

    public ShipObjective GetObjective(string role)
    {
        return role == "redship" ? redShipObjective : blueShipObjective;
    }
}
