using UnityEngine;

public class testRotation : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(0f, 0f, 100f * Time.deltaTime);
    }
}