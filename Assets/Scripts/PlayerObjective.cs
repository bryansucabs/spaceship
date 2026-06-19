using UnityEngine;
using Photon.Pun;

public class PlayerObjective : MonoBehaviourPun
{
    [Header("Rol asignado por StarshipControllerPun")]
    public string playerRole = "";

    private ShipObjective currentObjective = ShipObjective.GoToDoor;
    private string objectiveMessage = "";

    private bool _enteredLeftDoor = false;
    private bool _enteredBlueDoor = false;
    private bool _reachedOpenDoor = false;
    private float _circleCheckTimer = 0f;

    void Start()
    {
        if (!photonView.IsMine) return;
        UpdateMessage();
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (ObjectiveManager.Instance == null || ObjectiveManager.Instance.isGameOver) return;

        _circleCheckTimer += Time.deltaTime;
        if (_circleCheckTimer >= 1f)
        {
            _circleCheckTimer = 0f;
            CheckCircleProgress();
            CheckExtractionProximity();
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
                {
                    SetObjective(ShipObjective.GoToExtraction);
                }
            }
        }
        else if (playerRole == "blueship")
        {
            var circleL = GameObject.Find("circle_left");
            var circleR = GameObject.Find("circle_right");
            bool leftDone = circleL != null && circleL.GetComponent<WaitDoorTrigger>()?.IsDoorOpened() == true;
            bool rightDone = circleR != null && circleR.GetComponent<WaitDoorTrigger>()?.IsDoorOpened() == true;
            if (leftDone || rightDone)
            {
                SetObjective(ShipObjective.GoToExtraction);
            }
        }
    }

    void CheckExtractionProximity()
    {
        if (currentObjective != ShipObjective.GoToExtraction) return;

        string extractionName = playerRole == "redship" ? "redInit (2)" : "blueInit (1)";
        var extraction = GameObject.Find(extractionName);
        if (extraction == null)
        {
            extraction = GameObject.Find(playerRole == "redship" ? "redInit" : "blueInit");
        }
        if (extraction != null)
        {
            float dist = Vector3.Distance(transform.position, extraction.transform.position);
            if (dist < 12f)
            {
                SetObjective(ShipObjective.Completed);
            }
        }
    }

    string ResolveZoneName(Collider col)
    {
        string name = col.gameObject.name;
        if (name == "_TriggerProxy" && col.transform.parent != null)
            return col.transform.parent.name;
        return name;
    }

    GameObject ResolveZoneObject(Collider col)
    {
        string name = col.gameObject.name;
        if (name == "_TriggerProxy" && col.transform.parent != null)
            return col.transform.parent.gameObject;
        return col.gameObject;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;
        if (ObjectiveManager.Instance == null || ObjectiveManager.Instance.isGameOver) return;

        string zoneName = ResolveZoneName(other);
        GameObject zoneObj = ResolveZoneObject(other);

        if (playerRole == "redship")
            HandleRedShipZone(zoneName, zoneObj);
        else if (playerRole == "blueship")
            HandleBlueShipZone(zoneName, zoneObj);
    }

    void HandleRedShipZone(string zoneName, GameObject zoneObj)
    {
        if (zoneName.StartsWith("Door_door_4_J7_D1") && currentObjective == ShipObjective.GoToDoor)
        {
            _enteredLeftDoor = true;
            SetObjective(ShipObjective.StandInCircle);
        }
        else if ((zoneName == "Open_circle_blue_2") && currentObjective == ShipObjective.StandInCircle)
        {
        }
        else if (zoneName.StartsWith("redInit") && currentObjective == ShipObjective.GoToExtraction)
        {
            SetObjective(ShipObjective.Completed);
        }
    }

    void HandleBlueShipZone(string zoneName, GameObject zoneObj)
    {
        if (zoneName == "Door_door_3_BLUE")
        {
            _enteredBlueDoor = true;
        }
        else if (zoneName == "Open_door_1" && !_reachedOpenDoor)
        {
            _reachedOpenDoor = true;
            var animator = zoneObj.GetComponentInChildren<Animator>();
            if (animator != null && animator.GetBool("character_nearby"))
            {
                SetObjective(ShipObjective.StandInCircle);
            }
            else
            {
                SetObjective(ShipObjective.WaitForDoor);
            }
        }
        else if ((zoneName == "circle_left" || zoneName == "circle_right") && currentObjective == ShipObjective.StandInCircle)
        {
        }
        else if (zoneName.StartsWith("blueInit") && currentObjective == ShipObjective.GoToExtraction)
        {
            SetObjective(ShipObjective.Completed);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (!photonView.IsMine) return;
        if (currentObjective != ShipObjective.WaitForDoor) return;
        if (playerRole != "blueship") return;

        string zoneName = ResolveZoneName(other);
        GameObject zoneObj = ResolveZoneObject(other);
        if (zoneName == "Open_door_1")
        {
            var animator = zoneObj.GetComponentInChildren<Animator>();
            if (animator != null && animator.GetBool("character_nearby"))
            {
                SetObjective(ShipObjective.StandInCircle);
            }
        }
    }

    void SetObjective(ShipObjective newObjective)
    {
        if (currentObjective == newObjective) return;
        if (currentObjective == ShipObjective.Completed || currentObjective == ShipObjective.Failed) return;

        currentObjective = newObjective;
        UpdateMessage();

        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.UpdateObjective(playerRole, currentObjective);
    }

    void UpdateMessage()
    {
        if (playerRole == "redship")
        {
            switch (currentObjective)
            {
                case ShipObjective.GoToDoor:
                    objectiveMessage = "Dirijase a puerta izquierda";
                    break;
                case ShipObjective.StandInCircle:
                    objectiveMessage = "Dirijase a la puerta izquierda y permanezca dentro del circulo para abrir la compuerta a su companero";
                    break;
                case ShipObjective.GoToExtraction:
                    objectiveMessage = "Ahora dirijase al punto de extraccion";
                    break;
                case ShipObjective.Completed:
                    objectiveMessage = "Extraccion completada!";
                    break;
                default:
                    objectiveMessage = "";
                    break;
            }
        }
        else if (playerRole == "blueship")
        {
            switch (currentObjective)
            {
                case ShipObjective.GoToDoor:
                    objectiveMessage = "Dirijase a la puerta derecha";
                    break;
                case ShipObjective.WaitForDoor:
                    objectiveMessage = "Esperando que BlueShip abra la puerta";
                    break;
                case ShipObjective.StandInCircle:
                    objectiveMessage = "Espere en alguno de los circulos para abrir el camino";
                    break;
                case ShipObjective.GoToExtraction:
                    objectiveMessage = "Dirijase al punto de extraccion";
                    break;
                case ShipObjective.Completed:
                    objectiveMessage = "Extraccion completada!";
                    break;
                default:
                    objectiveMessage = "";
                    break;
            }
        }
    }

    void OnGUI()
    {
        if (!photonView.IsMine) return;
        if (currentObjective == ShipObjective.Completed || currentObjective == ShipObjective.Failed) return;
        if (ObjectiveManager.Instance != null && ObjectiveManager.Instance.isGameOver) return;
        if (string.IsNullOrEmpty(objectiveMessage)) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.yellow },
            wordWrap = true
        };

        float w = Mathf.Min(700, Screen.width - 40);
        float h = 60;
        float x = (Screen.width - w) / 2f;
        float y = Screen.height - 90;

        float boxH = 50;
        GUI.Box(new Rect(x - 10, y - 5, w + 20, boxH + 10), "");
        GUI.Label(new Rect(x, y, w, h), objectiveMessage, style);
    }
}
