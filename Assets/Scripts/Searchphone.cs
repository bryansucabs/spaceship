using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Globalization;

public class Searchphone : MonoBehaviour
{
    [Header("Configuración Red")]
    public int port = 11011;
    public float intervaloBroadcast = 2f; // Cada 2 segundos para no saturar

    private UdpClient udpClient;
    private Thread mainThread;
    private bool running = true;
    private bool conectado = false;
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
        int intentos = 0;
        while (running && !conectado && intentos < 10)
        {
            try {
                byte[] data = Encoding.UTF8.GetBytes("PC");
                // Usamos la dirección de broadcast de la red
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(NetworkUtils.GetBroadcastAddress()), port);
                udpClient.Send(data, data.Length, endPoint);
                Debug.Log("Anunciando presencia de PC en la red...");
            } catch { }

            intentos++;
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
                //Debug.Log("Recibido: " + text);

                // Lógica de detección de IP del móvil
                if (text == "END") {
                    ipCelular = remoteEP.Address.ToString();
                    Debug.Log("Móvil detectado en: " + ipCelular);
                    InstanceManagerPC.instance.ip = ipCelular;
                    conectado = true;
                    running = false;   
                }
            } catch { /* Evitamos spam de errores al cerrar */ }
        }
    }


    void OnDestroy()
    {
        running = false;
        try { if (udpClient != null) { udpClient.Close(); udpClient = null; } } catch { }
        try { if (mainThread != null) { mainThread.Abort(); mainThread = null; } } catch { }
        Debug.Log("El juego se ha cerrado");
    }

    void OnApplicationQuit()
    {
        OnDestroy();
    }
}