using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDPReceiver : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    public int port = 5002;

    [Header("Datos Recibidos (Solo Lectura)")]
    public ControlData currentData = new ControlData();

    // Lock para pasar datos entre hilos de forma segura
    private readonly object lockObject = new object();
    private ControlData pendingData = new ControlData();

    void Start()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log($"UDPReceiver escuchando en puerto {port}");
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);

                string text = Encoding.UTF8.GetString(data);
                ControlData parsed = JsonUtility.FromJson<ControlData>(text);

                // Guardamos en pendingData (hilo secundario) de forma segura
                lock (lockObject)
                {
                    pendingData = parsed;
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
        // Pasamos los datos al hilo principal de Unity
        lock (lockObject)
        {
            currentData = pendingData;
        }
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}

[System.Serializable]
public class ControlData
{
    public float accel;
    public float brake;
}