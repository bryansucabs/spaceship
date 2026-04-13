using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDPManager : MonoBehaviour
{
    [Header("Referencias")]
    public StarshipController nave; 

    [Header("Configuración Red")]
    public int port = 11011;
    public float intervaloBroadcast = 5f; // Cada 5 segundos para no saturar

    private UdpClient udpClient;
    private Thread mainThread;
    private bool running = true;
    private bool conectado = false;
    
    private Quaternion rotationFromMobile;
    private readonly object lockObj = new object();
    private string ipCelular = "";

    void Start()
    {
        // 1. Inicializamos el cliente UDP una sola vez para TODO
        try {
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            udpClient.EnableBroadcast = true;
        } catch (System.Exception e) {
            Debug.LogError("No se pudo abrir el puerto: " + e.Message);
        }

        // 2. Un solo hilo para la recepción (El broadcast lo manejaremos con una Corrutina)
        mainThread = new Thread(ReceiveLoop);
        mainThread.IsBackground = true;
        mainThread.Start();

        // 3. Iniciamos el anuncio de la PC
        StartCoroutine(BroadcastLoop());
    }

    // El Broadcast es mejor como Corrutina: consume menos que un Thread extra
    System.Collections.IEnumerator BroadcastLoop()
    {
        while (running && !conectado)
        {
            try {
                byte[] data = Encoding.UTF8.GetBytes("PC");
                // Usamos la dirección de broadcast de la red
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(NetworkUtils.GetBroadcastAddress()), port);
                udpClient.Send(data, data.Length, endPoint);
                Debug.Log("Anunciando presencia de PC en la red...");
            } catch { }

            yield return new WaitForSeconds(intervaloBroadcast);
        }
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, port);
        while (running)
        {
            try {
                byte[] data = udpClient.Receive(ref remoteEP);
                string text = Encoding.UTF8.GetString(data);

                // Lógica de detección de IP del móvil
                if (text == "END") {
                    ipCelular = remoteEP.Address.ToString();
                    Debug.Log("Móvil detectado en: " + ipCelular);
                    conectado = true;
                }
                // Lógica de Quaternions
                else if (text.Contains("|")) {
                    ParseQuaternion(text);
                }
            } catch { /* Evitamos spam de errores al cerrar */ }
        }
    }

    private void ParseQuaternion(string text)
    {
        string[] v = text.Split('|');
        if (v.Length == 4) {
            lock (lockObj) {
                rotationFromMobile = new Quaternion(
                    float.Parse(v[0]), 
                    float.Parse(v[1]), 
                    float.Parse(v[2]), 
                    float.Parse(v[3])
                );
            }
        }
    }

    void Update()
    {
        if (nave != null) {
            lock (lockObj) {
                nave.rotacionRecibidaCelular = rotationFromMobile;
            }
        }
    }

    void OnApplicationQuit()
    {
        running = false;
        if (udpClient != null) udpClient.Close();
        if (mainThread != null) mainThread.Abort();
    }
}