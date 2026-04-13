using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

public static class NetworkUtils
{
    public static string GetBroadcastAddress()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            // 1. Filtrar solo interfaces activas y que NO sean de retroceso (loopback) ni túneles
            if (ni.OperationalStatus != OperationalStatus.Up || 
                ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || 
                ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
                continue;

            var properties = ni.GetIPProperties();

            foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
            {
                // 2. Solo IPv4 y asegurar que la máscara no sea nula
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork && ip.IPv4Mask != null)
                {
                    byte[] ipBytes = ip.Address.GetAddressBytes();
                    byte[] maskBytes = ip.IPv4Mask.GetAddressBytes();

                    if (ipBytes.Length != maskBytes.Length) continue;

                    byte[] broadcast = new byte[4];
                    for (int i = 0; i < 4; i++)
                    {
                        // Operación: IP OR (NOT MASK)
                        broadcast[i] = (byte)(ipBytes[i] | (maskBytes[i] ^ 255));
                    }

                    return new IPAddress(broadcast).ToString();
                }
            }
        }

        // 3. Fallback universal si todo lo demás falla
        return "255.255.255.255";
    }
}