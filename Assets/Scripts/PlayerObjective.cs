using UnityEngine;
using Photon.Pun;

public class PlayerObjective : MonoBehaviourPun
{
    [Header("Rol asignado por StarshipControllerPun")]
    public string playerRole = "";

    private ShipObjective _obj = ShipObjective.GoToDoor;
    private float _pollTimer = 0f;
    private float _startTimer = 0f;
    private bool _triggeredOut = false;
    private bool _triggeredMsg2 = false;

    [Header("Distancias de Detección")]
    public float detectRange = 20f;
    // Eliminamos 'extractRange' porque ahora las Zonas nos avisan directamente

    void Start()
    {
        if (!photonView.IsMine) return;
        TriggerMessage.Mostrar(GetMsg(), 30f);
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (ObjectiveManager.Instance == null || ObjectiveManager.Instance.isGameOver) return;

        if (_startTimer < 8f && _obj == ShipObjective.GoToDoor)
        {
            _startTimer += Time.deltaTime;
            if (_startTimer >= 5f && _startTimer - Time.deltaTime < 5f)
                TriggerMessage.Mostrar(GetMsg(), 8f);
        }

        _pollTimer += Time.deltaTime;
        if (_pollTimer >= 0.3f)
        {
            _pollTimer = 0f;
            PollZones();
        }
    }

    /// <summary>
    /// Este método ahora es llamado por el script ZoneTrigger 
    /// que está en los BoxColliders de la escena.
    /// </summary>
    public void HandleTrigger(string zoneName)
    {
        // Seguridad: Solo procesar si este jugador nos pertenece en red
        if (!photonView.IsMine) return;
        
        Debug.LogWarning("[PlayerObjective] Entrando a la zona: " + zoneName);

        if (playerRole == "redship")
        {
            if (zoneName == "out_red" && !_triggeredOut)
            {
                _triggeredOut = true;
                TriggerMessage.Mostrar("Dir\u00edjase a puerta izquierda", 6f);
            }
            else if (zoneName == "red_message2" && !_triggeredMsg2)
            {
                _triggeredMsg2 = true;
                SetObj(ShipObjective.StandInCircle);
            }
            // ZONA DE EXTRACCIÓN REDSHIP
            else if (_obj == ShipObjective.GoToExtraction && zoneName.Contains("MetaRed"))
            {
                SetObj(ShipObjective.Completed);
            }
        }
        else if (playerRole == "blueship")
        {
            if (zoneName == "out_blue" && !_triggeredOut)
            {
                _triggeredOut = true;
                TriggerMessage.Mostrar("Dir\u00edjase a la puerta derecha", 6f);
            }
            else if (zoneName == "blue_message2" && !_triggeredMsg2)
            {
                _triggeredMsg2 = true;
                if (DoorOpen())
                    SetObj(ShipObjective.StandInCircle);
                else
                    SetObj(ShipObjective.WaitForDoor);
            }
            // ZONA DE EXTRACCIÓN BLUESHIP
            else if (_obj == ShipObjective.GoToExtraction && zoneName.Contains("MetaBlue"))
            {
                SetObj(ShipObjective.Completed);
            }
        }
    }

    void PollZones()
    {
        if (playerRole == "redship") PollRed();
        else PollBlue();
    }

    void PollRed()
    {
        if (_obj == ShipObjective.GoToDoor)
        {
            if (!_triggeredOut && IsNear("out_red", detectRange))
            {
                _triggeredOut = true;
                TriggerMessage.Mostrar("Dir\u00edjase a puerta izquierda", 6f);
            }
            if (!_triggeredMsg2 && IsNear("red_message2", detectRange))
            {
                _triggeredMsg2 = true;
                SetObj(ShipObjective.StandInCircle);
            }
        }
        else if (_obj == ShipObjective.StandInCircle)
        {
            var c = GameObject.Find("Open_circle_blue1");
            if (c != null && c.GetComponent<WaitDoorTrigger>()?.IsDoorOpened() == true)
                SetObj(ShipObjective.GoToExtraction);
        }
    }

    void PollBlue()
    {
        if (_obj == ShipObjective.GoToDoor)
        {
            if (!_triggeredOut && IsNear("out_blue", detectRange))
            {
                _triggeredOut = true;
                TriggerMessage.Mostrar("Dir\u00edjase a la puerta derecha", 6f);
            }
            if (!_triggeredMsg2 && IsNear("blue_message2", detectRange))
            {
                _triggeredMsg2 = true;
                if (DoorOpen())
                    SetObj(ShipObjective.StandInCircle);
                else
                    SetObj(ShipObjective.WaitForDoor);
            }
        }
        else if (_obj == ShipObjective.WaitForDoor)
        {
            if (DoorOpen())
                SetObj(ShipObjective.StandInCircle);
        }
        else if (_obj == ShipObjective.StandInCircle)
        {
            if (GameObject.Find("Open_circle2")?.GetComponent<WaitDoorTrigger>()?.IsDoorOpened() == true ||
                GameObject.Find("circle_left")?.GetComponent<WaitDoorTrigger>()?.IsDoorOpened() == true ||
                GameObject.Find("circle_right")?.GetComponent<WaitDoorTrigger>()?.IsDoorOpened() == true)
                SetObj(ShipObjective.GoToExtraction);
        }
    }

    bool DoorOpen()
    {
        var door = GameObject.Find("Open_door_1");
        if (door == null) return false;
        var anim = door.GetComponentInChildren<Animator>();
        return anim != null && anim.GetBool("character_nearby");
    }

    bool IsNear(string name, float range)
    {
        var go = GameObject.Find(name);
        if (go == null) return false;
        return Vector3.Distance(transform.position, go.transform.position) < range;
    }

    void SetObj(ShipObjective next)
    {
        if (_obj == next) return;
        if (_obj == ShipObjective.Completed || _obj == ShipObjective.Failed) return;
        _obj = next;
        TriggerMessage.Mostrar(GetMsg(), 6f);
        if (ObjectiveManager.Instance != null)
            ObjectiveManager.Instance.UpdateObjective(playerRole, _obj);
    }

    string GetMsg()
    {
        if (playerRole == "redship")
        {
            switch (_obj)
            {
                case ShipObjective.GoToDoor:       return "Dir\u00edjase a puerta izquierda";
                case ShipObjective.StandInCircle:  return "Dir\u00edjase a la puerta izquierda y active el mecanismo";
                case ShipObjective.GoToExtraction: return "Ahora dir\u00edjase al punto de extracci\u00f3n";
                case ShipObjective.Completed:      return "Extracci\u00f3n completada";
            }
        }
        else
        {
            switch (_obj)
            {
                case ShipObjective.GoToDoor:       return "Dir\u00edjase a la puerta derecha";
                case ShipObjective.WaitForDoor:    return "Esperando a que RedShip abra la puerta";
                case ShipObjective.StandInCircle:  return "Espere en el c\u00edrculo para abrir el camino";
                case ShipObjective.GoToExtraction: return "Dir\u00edjase al punto de extracci\u00f3n";
                case ShipObjective.Completed:      return "Extracci\u00f3n completada";
            }
        }
        return "";
    }
}