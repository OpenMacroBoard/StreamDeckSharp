using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

#pragma warning disable AV1505 // Namespace should match with assembly name
#pragma warning disable AV1710 // Member name includes the name of its containing type

namespace BeaconLib;

/// <summary>
/// Counterpart of the beacon, searches for beacons
/// </summary>
/// <remarks>
/// <para>The beacon list event will not be raised on your main thread!</para>
/// </remarks>
internal sealed class Probe : IDisposable
{
    /// <summary>
    /// Remove beacons older than this
    /// </summary>
    private static readonly TimeSpan BeaconTimeout = TimeSpan.FromSeconds(5);

    private readonly Thread thread;
    private readonly EventWaitHandle waitHandle = new(false, EventResetMode.AutoReset);
    private readonly UdpClient udp = new();

    private IEnumerable<BeaconLocation> currentBeacons = [];

    private bool running = true;

    public Probe(string beaconType, IPAddress bindIpAddress)
    {
        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

        BeaconType = beaconType;
        thread = new Thread(BackgroundLoop) { IsBackground = true };

        udp.Client.Bind(new IPEndPoint(bindIpAddress, 0));
        udp.EnableNatTraversal();
        udp.BeginReceive(ResponseReceived, null);
    }

    public event Action<IEnumerable<BeaconLocation>>? BeaconsUpdated;

    public string BeaconType { get; }

    public void Start()
    {
        thread.Start();
    }

    public void Stop()
    {
        running = false;
        waitHandle.Set();
        thread.Join();
    }

    public void Dispose()
    {
        try
        {
            Stop();
            udp.Dispose();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void ResponseReceived(IAsyncResult ar)
    {
        var remote = new IPEndPoint(IPAddress.Any, 0);
        byte[] bytes;

        try
        {
            bytes = udp.EndReceive(ar, ref remote);
        }
        catch (ObjectDisposedException)
        {
            // ignore
            return;
        }

        var typeBytes = Beacon.Encode(BeaconType).ToList();
        Debug.WriteLine(string.Join(", ", typeBytes.Select(x => (char)x)));

        if (Beacon.HasPrefix(bytes, typeBytes))
        {
            try
            {
                var portBytes = bytes.Skip(typeBytes.Count).Take(2).ToArray();
                var port = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(portBytes, 0));
                var payload = Beacon.Decode(bytes.Skip(typeBytes.Count + 2));

                NewBeacon(
                    new BeaconLocation(
                        new IPEndPoint(remote!.Address, port),
                        payload,
                        DateTimeOffset.UtcNow
                    )
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        udp.BeginReceive(ResponseReceived, null);
    }

    private void BackgroundLoop()
    {
        while (running)
        {
            try
            {
                BroadcastProbe();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            waitHandle.WaitOne(2000);
            PruneBeacons();
        }
    }

    private void BroadcastProbe()
    {
        var probe = Beacon.Encode(BeaconType).ToArray();
        udp.Send(probe, probe.Length, new IPEndPoint(IPAddress.Broadcast, Beacon.DiscoveryPort));
    }

    private void PruneBeacons()
    {
        var cutOff = DateTimeOffset.UtcNow - BeaconTimeout;
        var oldBeacons = currentBeacons.ToList();

        var newBeacons = oldBeacons
            .Where(x => x.LastAdvertised >= cutOff)
            .ToList();

        if (oldBeacons.SequenceEqual(newBeacons))
        {
            return;
        }

        BeaconsUpdated?.Invoke(newBeacons);
        currentBeacons = newBeacons;
    }

    private void NewBeacon(BeaconLocation newBeacon)
    {
        var newBeacons = currentBeacons
            .Where(x => !x.Equals(newBeacon))
            .Concat([newBeacon])
            .OrderBy(x => x.Data)
            .ThenBy(x => x.Address, IPEndPointComparer.Instance)
            .ToList();

        BeaconsUpdated?.Invoke(newBeacons);
        currentBeacons = newBeacons;
    }
}
