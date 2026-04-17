using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class NetworkSend : MonoBehaviour
{
    UdpClient udpClient;
    
    public string remoteIP;
    public int port = 11012;

    //private string lastReceived = "";

    void Start()
    {
        // Configuramos para poder reutilizar y escuchar en el puerto
        remoteIP = "";
        udpClient = new UdpClient();
        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));

    }

    // --- ENVIAR DATOS ---
    public void SendData(string message)
    {
        remoteIP = InstanceManagerPC.instance.ip;
        if (remoteIP == "")
        {
            Debug.LogError("IP no configurada");
            return;
        }
        try {
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, remoteIP, port);
        } catch (System.Exception e) {
            Debug.LogError("Error al enviar: " + e.Message);
        }
    }
    void OnDisable()
    {
        if (udpClient != null) udpClient.Close();
    }
}
