using UnityEngine;
using Photon.Pun;

public class PlayerObjective : MonoBehaviourPun
{
    [Header("Rol asignado por StarshipControllerPun")]
    public string playerRole = "";

    private ShipObjective currentObjective = ShipObjective.GoToDoor;
    private string _currentMessage = "";
    private float _messageTimer = 0f;
    private float _proximityTimer = 0f;

    private bool _leftDoorDone = false;
    private bool _blueDoorDone = false;
    private bool _openDoorDone = false;

    [Header("Distancias de deteccion")]
    public float doorDetectDistance = 18f;
    public float extractionDetectDistance = 14f;
    public float openDoorDetectDistance = 18f;

    void Start()
    {
        if (!photonView.IsMine) return;
        ShowMessageForCurrentObjective(5f);
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (ObjectiveManager.Instance == null || ObjectiveManager.Instance.isGameOver) return;

        if (_messageTimer > 0f)
            _messageTimer -= Time.deltaTime;

        _proximityTimer += Time.deltaTime;
        if (_proximityTimer >= 0.4f)
        {
            _proximityTimer = 0f;
            CheckProximityZones();
            CheckCircleProgress();
        }
    }

    void CheckProximityZones()
    {
        if (playerRole == "redship")
            CheckRedShipZones();
        else if (playerRole == "blueship")
            CheckBlueShipZones();
    }

    void CheckRedShipZones()
    {
        if (currentObjective == ShipObjective.GoToDoor)
        {
            if (IsNearAny("Door_door_4_J7_D1", doorDetectDistance))
            {
                _leftDoorDone = true;
                SetObjective(ShipObjective.StandInCircle);
                return;
            }
        }
        else if (currentObjective == ShipObjective.GoToExtraction)
        {
            var ex = FindExtraction("redInit");
            if (ex != null)
            {
                float d = Vector3.Distance(transform.position, ex.transform.position);
                if (d < extractionDetectDistance)
                    SetObjective(ShipObjective.Completed);
            }
        }
    }

    void CheckBlueShipZones()
    {
        if (currentObjective == ShipObjective.GoToDoor)
        {
            var door = GameObject.Find("Door_door_3_BLUE");
            if (door != null)
            {
                float d = Vector3.Distance(transform.position, door.transform.position);
                if (d < doorDetectDistance)
                {
                    _blueDoorDone = true;
                }
            }

            var openDoor = GameObject.Find("Open_door_1");
            if (openDoor != null && !_openDoorDone)
            {
                float d = Vector3.Distance(transform.position, openDoor.transform.position);
                if (d < openDoorDetectDistance)
                {
                    _openDoorDone = true;
                    var animator = openDoor.GetComponentInChildren<Animator>();
                    if (animator != null && animator.GetBool("character_nearby"))
                        SetObjective(ShipObjective.StandInCircle);
                    else
                        SetObjective(ShipObjective.WaitForDoor);
                }
            }
        }
        else if (currentObjective == ShipObjective.WaitForDoor)
        {
            var openDoor = GameObject.Find("Open_door_1");
            if (openDoor != null)
            {
                float d = Vector3.Distance(transform.position, openDoor.transform.position);
                if (d < openDoorDetectDistance)
                {
                    var animator = openDoor.GetComponentInChildren<Animator>();
                    if (animator != null && animator.GetBool("character_nearby"))
                        SetObjective(ShipObjective.StandInCircle);
                }
            }
        }
        else if (currentObjective == ShipObjective.GoToExtraction)
        {
            var ex = FindExtraction("blueInit");
            if (ex != null)
            {
                float d = Vector3.Distance(transform.position, ex.transform.position);
                if (d < extractionDetectDistance)
                    SetObjective(ShipObjective.Completed);
            }
        }
    }

    void CheckCircleProgress()
    {
        if (currentObjective != ShipObjective.StandInCircle) return;

        if (playerRole == "redship")
        {
            var circle = GameObject.Find("Open_circle_blue_2");
            if (circle != null)
            {
                var trigger = circle.GetComponent<WaitDoorTrigger>();
                if (trigger != null && trigger.IsDoorOpened())
                    SetObjective(ShipObjective.GoToExtraction);
            }
        }
        else if (playerRole == "blueship")
        {
            var circleL = GameObject.Find("circle_left");
            var circleR = GameObject.Find("circle_right");
            bool leftDone = circleL != null && circleL.GetComponent<WaitDoorTrigger>()?.IsDoorOpened() == true;
            bool rightDone = circleR != null && circleR.GetComponent<WaitDoorTrigger>()?.IsDoorOpened() == true;
            if (leftDone || rightDone)
                SetObjective(ShipObjective.GoToExtraction);
        }
    }

    GameObject FindExtraction(string baseName)
    {
        string name2 = baseName + " (2)";
        string name1 = baseName + " (1)";
        var ex = GameObject.Find(name2);
        if (ex == null) ex = GameObject.Find(name1);
        if (ex == null) ex = GameObject.Find(baseName);
        return ex;
    }

    bool IsNearAny(string baseName, float threshold)
    {
        var go = GameObject.Find(baseName);
        if (go != null && Vector3.Distance(transform.position, go.transform.position) < threshold)
            return true;
        go = GameObject.Find(baseName + " (1)");
        if (go != null && Vector3.Distance(transform.position, go.transform.position) < threshold)
            return true;
        go = GameObject.Find(baseName + " (2)");
        if (go != null && Vector3.Distance(transform.position, go.transform.position) < threshold)
            return true;
        return false;
    }

    void SetObjective(ShipObjective newObjective)
    {
        if (currentObjective == newObjective) return;
        if (currentObjective == ShipObjective.Completed || currentObjective == ShipObjective.Failed) return;

        currentObjective = newObjective;
        ShowMessageForCurrentObjective(6f);

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.UpdateObjective(playerRole, currentObjective);
    }

    void ShowMessageForCurrentObjective(float duration)
    {
        _currentMessage = GetMessageForObjective();
        _messageTimer = duration;
    }

    string GetMessageForObjective()
    {
        if (playerRole == "redship")
        {
            switch (currentObjective)
            {
                case ShipObjective.GoToDoor:      return "Diríjase a puerta izquierda";
                case ShipObjective.StandInCircle: return "Permanezca dentro del círculo para abrir la compuerta";
                case ShipObjective.GoToExtraction: return "Ahora diríjase al punto de extracción";
                case ShipObjective.Completed:     return "Extracción completada";
                default: return "";
            }
        }
        else if (playerRole == "blueship")
        {
            switch (currentObjective)
            {
                case ShipObjective.GoToDoor:      return "Diríjase a la puerta derecha";
                case ShipObjective.WaitForDoor:   return "Esperando que se abra la puerta";
                case ShipObjective.StandInCircle: return "Espere en el círculo para abrir el camino";
                case ShipObjective.GoToExtraction: return "Diríjase al punto de extracción";
                case ShipObjective.Completed:     return "Extracción completada";
                default: return "";
            }
        }
        return "";
    }

    void OnGUI()
    {
        if (!photonView.IsMine) return;
        if (currentObjective == ShipObjective.Completed || currentObjective == ShipObjective.Failed) return;
        if (ObjectiveManager.Instance != null && ObjectiveManager.Instance.isGameOver) return;
        if (_messageTimer <= 0f || string.IsNullOrEmpty(_currentMessage)) return;

        float maxDuration = 6f;
        float fadeInOut = 0.6f;
        float alpha;
        if (_messageTimer > maxDuration - fadeInOut)
            alpha = Mathf.Clamp01((maxDuration - _messageTimer) / fadeInOut);
        else if (_messageTimer < fadeInOut)
            alpha = Mathf.Clamp01(_messageTimer / fadeInOut);
        else
            alpha = 1f;

        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 1f, 0f, alpha) },
            wordWrap = true
        };

        var boxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { textColor = new Color(0f, 0f, 0f, alpha * 0.7f) }
        };

        float w = Mathf.Min(650, Screen.width - 60);
        float h = 50;
        float x = (Screen.width - w) / 2f;
        float y = 20;

        GUI.Box(new Rect(x - 12, y - 8, w + 24, h + 16), "", boxStyle);
        GUI.Label(new Rect(x, y, w, h), _currentMessage, labelStyle);
    }
}
