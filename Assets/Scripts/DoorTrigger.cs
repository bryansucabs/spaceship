using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    Animator _animator;
    int _insideCount = 0;
    bool _saboteado = false;

    void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        var col = GetComponent<Collider>();
        ConfigurarColliderTrigger(col);
    }

    void ConfigurarColliderTrigger(Collider col)
    {
        if (col == null || col.isTrigger) return;

        MeshCollider meshCol = col as MeshCollider;
        if (meshCol != null && !meshCol.convex)
        {
            var triggerGO = new GameObject("_TriggerProxy");
            triggerGO.transform.SetParent(transform, false);
            var box = triggerGO.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = meshCol.bounds.size;
            box.center = transform.InverseTransformPoint(meshCol.bounds.center);
            return;
        }

        col.isTrigger = true;
    }

    public void ForzarCerrar()
    {
        _saboteado = true;
        _insideCount = 0;
        if (_animator != null) _animator.SetBool("character_nearby", false);
    }

    public void LiberarCierre()
    {
        _saboteado = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_saboteado || other.attachedRigidbody == null) return;
        _insideCount++;
        if (_insideCount == 1 && _animator != null)
            _animator.SetBool("character_nearby", true);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.attachedRigidbody == null) return;
        _insideCount = Mathf.Max(0, _insideCount - 1);
        if (_insideCount == 0 && _animator != null)
            _animator.SetBool("character_nearby", false);
    }
}
