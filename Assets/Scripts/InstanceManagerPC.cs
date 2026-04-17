using UnityEngine;

public class InstanceManagerPC : MonoBehaviour
{
    public static InstanceManagerPC instance;

    public string ip;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}