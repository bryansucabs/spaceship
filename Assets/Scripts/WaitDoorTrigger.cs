using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaitDoorTrigger : MonoBehaviour
{
    [Header("Door Reference")]
    [SerializeField] private Animator doorAnimator;

    [Header("Settings")]
    [SerializeField] private float waitTime = 5f;

    private float _timer = 0f;
    private bool _doorOpened = false;
    private int _insideCount = 0;

    public bool IsPlayerInside => _insideCount > 0;

    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        if (doorAnimator == null)
        {
            FindDoorAnimator();
        }
    }

    void Update()
    {
        if (_doorOpened || _insideCount <= 0)
            return;

        _timer += Time.deltaTime;

        if (_timer >= waitTime && !_doorOpened)
        {
            _doorOpened = true;
            if (doorAnimator != null)
            {
                doorAnimator.SetBool("character_nearby", true);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null && !_doorOpened)
        {
            _insideCount++;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null && !_doorOpened)
        {
            _insideCount = Mathf.Max(0, _insideCount - 1);

            if (_insideCount == 0)
            {
                _timer = 0f;
            }
        }
    }

    private void FindDoorAnimator()
    {
        string doorName = null;
        if (gameObject.name.StartsWith("Open_circle_blue_"))
        {
            doorName = gameObject.name.Replace("Open_circle_blue_", "Open_door_");
        }
        else if (gameObject.name.StartsWith("circle_"))
        {
            doorName = gameObject.name.Replace("circle_", "Wait_door_");
        }

        if (!string.IsNullOrEmpty(doorName))
        {
            GameObject doorObj = GameObject.Find(doorName);
            if (doorObj != null)
            {
                doorAnimator = doorObj.GetComponentInChildren<Animator>();
            }
        }
    }

    public float GetProgress()
    {
        if (_doorOpened) return 1f;
        return Mathf.Clamp01(_timer / waitTime);
    }

    public float GetRemainingTime()
    {
        if (_doorOpened) return 0f;
        return Mathf.Max(0f, waitTime - _timer);
    }

    public bool IsDoorOpened()
    {
        return _doorOpened;
    }
}
