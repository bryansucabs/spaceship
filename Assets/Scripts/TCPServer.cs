using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

public class TCPServer : MonoBehaviour
{
    TcpListener server;
    TcpClient client;
    Thread serverThread;
    public static TCPServer instance;
    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public int port = 11012;

    private volatile bool running = true;
    private volatile bool connected = false;

    private string lastMessage = null;
    private readonly object lockObj = new object();

    void Start()
    {
        serverThread = new Thread(RunServer);
        serverThread.IsBackground = true;
        serverThread.Start();
    }

    void RunServer()
    {
        try
        {
            server = new TcpListener(IPAddress.Any, port);
            server.Start();

            Debug.Log("Esperando cliente...");

            client = server.AcceptTcpClient(); // bloquea hasta que se conecte
            connected = true;

            Debug.Log("Cliente conectado");

            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);

            while (running && client.Connected)
            {
                string msg = reader.ReadLine(); // requiere \n desde el cliente

                if (msg == null) break;

                lock (lockObj)
                {
                    lastMessage = msg;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error servidor: " + e.Message);
        }
    }

    void Update()
    {
        // Procesar mensaje en hilo principal
        if (lastMessage != null)
        {
            string msg;

            lock (lockObj)
            {
                msg = lastMessage;
                lastMessage = null;
            }

            Debug.Log("Recibido: " + msg);

            // ejemplo: responder
            Send("ACK: " + msg);
        }
    }

    public void Send(string message)
    {
        if (!connected || client == null) return;

        try
        {
            NetworkStream stream = client.GetStream();
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            stream.Write(data, 0, data.Length);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error al enviar: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        running = false;

        if (serverThread != null && serverThread.IsAlive)
            serverThread.Join();

        client?.Close();
        server?.Stop();
    }
}