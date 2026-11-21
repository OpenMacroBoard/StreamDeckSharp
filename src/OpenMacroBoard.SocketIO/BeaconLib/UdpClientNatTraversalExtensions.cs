using System;
using System.Diagnostics;
using System.Net.Sockets;

#pragma warning disable AV1505 // Namespace should match with assembly name

namespace BeaconLib;

internal static class UdpClientNatTraversalExtensions
{
    public static void EnableNatTraversal(this UdpClient client)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                client.AllowNatTraversal(true);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error switching on NAT traversal: " + ex.Message);
        }
    }
}
