using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

#pragma warning disable AV1505 // Namespace should match with assembly name
#pragma warning disable AV1710 // Member name includes the name of its containing type

namespace BeaconLib;

/// <summary>
/// Instances of this class can be auto-discovered on the local network through UDP broadcasts
/// </summary>
/// <remarks>
/// <para>
/// The advertisement consists of the beacon's application type and a short beacon-specific string.
/// </para>
/// </remarks>
internal sealed class Beacon : IDisposable
{
    internal const int DiscoveryPort = 35891;
    private readonly UdpClient udp;

    public Beacon(string beaconType, ushort advertisedPort, IPAddress bindIpAddress)
    {
        BeaconType = beaconType;
        AdvertisedPort = advertisedPort;
        BeaconData = string.Empty;

        udp = new UdpClient();
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udp.Client.Bind(new IPEndPoint(bindIpAddress, DiscoveryPort));
        udp.EnableNatTraversal();
    }

    public string BeaconType { get; }
    public ushort AdvertisedPort { get; }
    public bool Stopped { get; private set; }
    public string BeaconData { get; set; }

    public void Start()
    {
        Stopped = false;
        udp.BeginReceive(ProbeReceived, null);
    }

    public void Stop()
    {
        Stopped = true;
    }

    public void Dispose()
    {
        Stop();
        udp.Dispose();
    }

    /// <summary>
    /// Convert a string to network bytes
    /// </summary>
    internal static IEnumerable<byte> Encode(string data)
    {
        var bytes = Encoding.UTF8.GetBytes(data);
        var len = IPAddress.HostToNetworkOrder((short)bytes.Length);

        return BitConverter.GetBytes(len).Concat(bytes);
    }

    /// <summary>
    /// Convert network bytes to a string
    /// </summary>
    /// <exception cref="ArgumentException">Is thrown for invalid data.</exception>
    internal static string Decode(IEnumerable<byte> data)
    {
        var listData = data as IList<byte> ?? data.ToList();

        var len = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(listData.Take(2).ToArray(), 0));

        if (listData.Count < 2 + len)
        {
            throw new ArgumentException("Too few bytes in packet");
        }

        return Encoding.UTF8.GetString(listData.Skip(2).Take(len).ToArray());
    }

    internal static bool HasPrefix<T>(IEnumerable<T> haystack, IEnumerable<T> prefix)
    {
        return haystack.Count() >= prefix.Count() &&
            haystack.Zip(prefix, (a, b) => Equals(a, b)).All(_ => _);
    }

    private void ProbeReceived(IAsyncResult ar)
    {
        var remote = new IPEndPoint(IPAddress.Any, 0);
        byte[] bytes;

        try
        {
            bytes = udp.EndReceive(ar, ref remote);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        // Compare beacon type to probe type
        var typeBytes = Encode(BeaconType);

        if (HasPrefix(bytes, typeBytes))
        {
            // If true, respond again with our type, port and payload
            var responseData = Encode(BeaconType)
                .Concat(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)AdvertisedPort)))
                .Concat(Encode(BeaconData))
                .ToArray();

            udp.Send(responseData, responseData.Length, remote);
        }

        if (!Stopped)
        {
            udp.BeginReceive(ProbeReceived, null);
        }
    }
}
