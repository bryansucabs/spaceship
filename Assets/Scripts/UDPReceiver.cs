using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDPReceiver : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    public int port = 5001;

    [Header("Referencias")]
    public EngineController engineController; // Arrastra el script de la nave aquí
    public ControlData currentData;
    // Usamos esto para pasar datos de un hilo a otro de forma segura
    private float lastAccelReceived;
    private readonly object lockObject = new object();

    void Start()
    {
        // Iniciamos el hilo secundario para que no bloquee Unity
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP); // Esta línea espera el paquete
                
                string text = Encoding.UTF8.GetString(data);
                ControlData currentData = JsonUtility.FromJson<ControlData>(text);

                // Guardamos el valor en una variable compartida de forma segura
                lock (lockObject)
                {
                    lastAccelReceived = currentData.accel;
                }
            }
            catch (System.Exception err)
            {
                Debug.LogWarning("Error en UDP: " + err.Message);
            }
        }
    }

    void Update()
    {
        // En el hilo principal (Update), le pasamos el dato a la nave
        lock (lockObject)
        {
            if (engineController != null)
            {
                engineController.ActualizarMotores(lastAccelReceived);
            }
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null) receiveThread.Abort();
        client.Close();
    }
}

[System.Serializable]
public class ControlData
{
    public float accel;
    public float brake;
}