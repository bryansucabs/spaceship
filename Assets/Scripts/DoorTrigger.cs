using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    private Animator _animator;
    private int _insideCount = 0;

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();

        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody != null)
        {
            _insideCount++;
            if (_insideCount == 1 && _animator != null)
            {
                _animator.SetBool("character_nearby", true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody != null)
        {
            _insideCount = Mathf.Max(0, _insideCount - 1);
            if (_insideCount == 0 && _animator != null)
            {
                _animator.SetBool("character_nearby", false);
            }
        }
    }
}
