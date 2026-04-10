using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class UDPGyro : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    public int port = 5065; // Asegúrate que la App del celular use este mismo puerto
    
    // El dato que la nave leerá
    [HideInInspector] public Quaternion receivedRotation = Quaternion.identity;

    void Start()
    {
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
                byte[] data = client.Receive(ref anyIP);
                //string text = Encoding.UTF8.GetString(data);
               // Debug.Log("Received data: " + text);
                


                // Reemplaza esta parte dentro de tu ReceiveData()
                string text = Encoding.UTF8.GetString(data);
                string[] parts = text.Split(',');

                if (parts.Length == 3)
                {
                    float x = float.Parse(parts[0]);
                    float y = float.Parse(parts[1]);
                    float z = float.Parse(parts[2]);

                    // Calcular el componente W para completar el Quaternion
                    float sumSq = x * x + y * y + z * z;
                    float w = 0;
                    
                    if (sumSq < 1.0f) {
                        w = Mathf.Sqrt(1.0f - sumSq);
                    }

                    // Ahora tienes el Quaternion completo (x, y, z, w)
                    receivedRotation = new Quaternion(x, y, z, w);
                }

                /*
                // Asumiendo formato: "x,y,z,w"
                string[] parts = text.Split(',');
                if (parts.Length == 4)
                {
                    receivedRotation = new Quaternion(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3])
                    );
                }
                */
                //Debug.Log("Received rotation: " + receivedRotation);
                //Debug.Log("Received rotation euler: " + receivedRotation.eulerAngles);
            }
            catch (Exception e) { Debug.LogWarning(e.ToString()); }
        }
    }

    void OnDisable() { if (receiveThread != null) receiveThread.Abort(); client.Close(); }
}