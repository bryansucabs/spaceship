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
                lock (lockObject) { pendingData = parsed; }
            }
            catch (System.Exception err)
            {
                Debug.LogWarning("Error en UDP: " + err.Message);
            }
        }
    }

    void Update()
    {
        lock (lockObject) { currentData = pendingData; }
    }

    void OnDestroy()
    {
        try { if (receiveThread != null) { receiveThread.Abort(); receiveThread = null; } } catch { }
        try { if (client != null) { client.Close(); client = null; } } catch { }
    }

    void OnApplicationQuit() { OnDestroy(); }
}

[System.Serializable]
public class ControlData
{
    public float accel;           // magnitud de velocidad (pie verde)
    public int   reverse;         // 0 = adelante, 1 = atrás (pie azul detectado)
    public int   foot_right_lost; // 1 = pie derecho fuera de cámara
}