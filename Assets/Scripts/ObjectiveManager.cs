using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public enum ShipObjective
{
    GoToDoor,
    WaitForDoor,
    StandInCircle,
    GoToExtraction,
    Completed,
    Failed
}

public class ObjectiveManager : MonoBehaviourPunCallbacks
{
    public static ObjectiveManager Instance;

    [Header("Tiempo de partida (2 minutos)")]
    public float gameDuration = 120f;

    public ShipObjective redShipObjective = ShipObjective.GoToDoor;
    public ShipObjective blueShipObjective = ShipObjective.GoToDoor;

    public bool isGameOver = false;
    public int winner = -1;

    private float _timeLeft;
    private string _endMessage = "";
    private string _mensajeEspera = "";

    private float _lobbyCountdown = 5f;
    private bool _goingToLobby = false;

    private GUIStyle _timerStyle;
    private GUIStyle _endStyle;
    private GUIStyle _countdownStyle;
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
            if (_goingToLobby)
            {
                _lobbyCountdown -= Time.deltaTime;
                if (_lobbyCountdown <= 0f)
                {
                    _goingToLobby = false;
                    PhotonNetwork.LeaveRoom();
                }
            }
            return;
        }

        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f;
            DeclareP3Victory();
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("LobyV2");
    }

    void StartLobbyCountdown()
    {
        _lobbyCountdown = 5f;
        _goingToLobby = true;
    }

    PhotonView GetSpawnerPV()
    {
        var spawner = FindFirstObjectByType<GameSpawnManager>();
        return spawner != null ? spawner.photonView : null;
    }

    public void UpdateObjective(string role, ShipObjective objective)
    {
        ApplyObjectiveChange(role, objective);

        var pv = GetSpawnerPV();
        if (pv != null && pv.IsMine)
            pv.RPC(nameof(GameSpawnManager.RPC_SyncObjective), RpcTarget.Others, role, (int)objective);
    }

    // Aplica el cambio de objetivo y evalúa victoria/espera.
    // Se llama local (desde UpdateObjective) Y en los otros clientes (desde RPC_SyncObjective).
    public void ApplyObjectiveChange(string role, ShipObjective objective)
    {
        if (role == "redship") redShipObjective = objective;
        else if (role == "blueship") blueShipObjective = objective;

        if (isGameOver) return;

        bool redDone  = redShipObjective  == ShipObjective.Completed;
        bool blueDone = blueShipObjective == ShipObjective.Completed;

        if (redDone && blueDone)
        {
            // Solo el master client dispara el RPC de victoria para evitar duplicados
            var pv = GetSpawnerPV();
            if (pv != null && pv.IsMine)
                DeclareP1P2Victory();
            return;
        }

        if (redDone)
            _mensajeEspera = "Nave roja llegó al destino\n¡Esperando nave azul!";
        else if (blueDone)
            _mensajeEspera = "Nave azul llegó al destino\n¡Esperando nave roja!";
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
        StartLobbyCountdown();
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
                _endMessage = "WINNER\n¡Escaparon con éxito!";
            else if (role == "overlord")
                _endMessage = "GAME OVER\nLos jugadores escaparon.";
            else
                _endMessage = "WINNER\n¡Jugadores escaparon!";
        }
        else
        {
            if (role == "overlord")
                _endMessage = "WINNER\n¡El atacante ha ganado!";
            else
                _endMessage = "GAME OVER\nEl atacante ha ganado.";
        }

        StartLobbyCountdown();
        if (GameManager.Instance != null)
            GameManager.Instance.NotifyGameOver();
    }

    void OnGUI()
    {
        InitStyles();

        int mins = Mathf.FloorToInt(_timeLeft / 60f);
        int secs = Mathf.FloorToInt(_timeLeft % 60f);

        _timerStyle.normal.textColor = _timeLeft <= 30f ? Color.red : Color.white;
        GUI.Label(new Rect(Screen.width / 2f - 70, 15, 140, 45), $"{mins:00}:{secs:00}", _timerStyle);

        // Mensaje de espera: una nave llegó pero la otra todavía no
        if (!string.IsNullOrEmpty(_mensajeEspera) && !isGameOver)
        {
            float mw = 540, mh = 90;
            float mx = (Screen.width  - mw) / 2f;
            float my = (Screen.height - mh) / 2f - 100f;
            GUI.Box(new Rect(mx - 10, my - 10, mw + 20, mh + 20), "");
            GUI.Label(new Rect(mx, my, mw, mh), _mensajeEspera, _endStyle);
        }

        if (isGameOver)
        {
            float bw = 520, bh = 220;
            float bx = (Screen.width - bw) / 2f;
            float by = (Screen.height - bh) / 2f;

            GUI.Box(new Rect(bx - 10, by - 10, bw + 20, bh + 20), "");
            GUI.Label(new Rect(bx, by + 20, bw, 120), _endMessage, _endStyle);

            int secsLeft = Mathf.CeilToInt(Mathf.Max(0f, _lobbyCountdown));
            GUI.Label(new Rect(bx, by + 155, bw, 50),
                $"Volviendo al lobby en {secsLeft}...", _countdownStyle);
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
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.yellow }
        };

        _countdownStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.gray }
        };
    }

    public ShipObjective GetObjective(string role)
    {
        return role == "redship" ? redShipObjective : blueShipObjective;
    }
}
