using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// UDPReceiver.cs
// Escucha los datos que envía Python a través de la red local.
public class UDPReceiver : MonoBehaviour
{
    [Header("Configuración de Red")]
    [Tooltip("Debe ser el mismo puerto que configuraste en Python (UNITY_PORT)")]
    public int port = 5001;

    // Esta clase representa exactamente el formato JSON que envía Python
    [System.Serializable]
    public class ControlData
    {
        public float accel;
        public float brake;
    }

    [Header("Datos Recibidos (Solo Lectura)")]
    // Aquí se guarda la información en tiempo real para que ShipController la lea
    public ControlData currentData = new ControlData();

    // Variables internas para la conexión
    private Thread receiveThread;
    private UdpClient client;

    void Start()
    {
        InitUDP();
    }

    // Inicia el hilo secundario para no congelar el juego
    private void InitUDP()
    {
        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("UDPReceiver: Escuchando en el puerto " + port);
    }

    // Función que corre en bucle infinito escuchando la red
    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (true)
        {
            try
            {
                // Escucha a cualquier IP que le mande mensajes por el puerto 5001
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = client.Receive(ref anyIP);
                
                // Convierte los bytes a texto
                string text = Encoding.UTF8.GetString(data);
                
                // Transforma el texto JSON a nuestras variables de Unity
                currentData = JsonUtility.FromJson<ControlData>(text);
            }
            catch (System.Exception err)
            {
                Debug.LogWarning("Error en UDP: " + err.ToString());
            }
        }
    }

    // Es vital cerrar el puerto cuando apagas el juego, o Python no podrá reconectarse luego
    void OnApplicationQuit()
    {
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Abort();
        }
        if (client != null)
        {
            client.Close();
        }
    }
}