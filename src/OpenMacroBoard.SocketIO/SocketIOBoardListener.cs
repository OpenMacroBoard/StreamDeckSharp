using BeaconLib;
using Newtonsoft.Json;
using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO.Internals;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace OpenMacroBoard.SocketIO;

/// <summary>
/// A listener that listens for Socket IO devices.
/// </summary>
public sealed class SocketIOBoardListener :
    IDisposable,
    IObservable<DeviceStateReport>
{
    private readonly Lock subscriptionSync = new();

    private readonly Probe probe;
    private readonly List<ListenerSubscriptionHandler> subscriptions = [];

    private bool disposed = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketIOBoardListener"/> class.
    /// </summary>
    public SocketIOBoardListener()
        : this(IPAddress.Loopback)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketIOBoardListener"/> class.
    /// </summary>
    /// <param name="bindIpAddress">The binding address the UDP beacon probe should bind to.</param>
    public SocketIOBoardListener(IPAddress bindIpAddress)
    {
        probe = new Probe(SocketIOConstants.BeaconIdentifier, bindIpAddress);
        probe.BeaconsUpdated += Probe_BeaconsUpdated;
        probe.Start();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (subscriptionSync)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            foreach (var sub in subscriptions)
            {
                sub.Dispose();
            }

            probe.Stop();
            probe.Dispose();
        }
    }

    /// <inheritdoc />
    public IDisposable Subscribe(IObserver<DeviceStateReport> observer)
    {
        lock (subscriptionSync)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(SocketIOBoardListener));
            }

            var subscription = new ListenerSubscriptionHandler(observer);
            AddToEmptySlot(subscription);
            return subscription;
        }
    }

    private void Probe_BeaconsUpdated(IEnumerable<BeaconLocation> locations)
    {
        var preparedList = new List<DecodedBeaconLocation>();

        foreach (var location in locations)
        {
            if (string.IsNullOrEmpty(location.Data))
            {
                continue;
            }

            SocketIODeviceMetaData? metaInfo;

            try
            {
                metaInfo = JsonConvert.DeserializeObject<SocketIODeviceMetaData>(location.Data);
            }
            catch
            {
                // We don't care if we can't deserialize it, just ignore.
                continue;
            }

            if (metaInfo is null)
            {
                continue;
            }

            preparedList.Add(new DecodedBeaconLocation(location, metaInfo));
        }

        lock (subscriptionSync)
        {
            foreach (var sub in subscriptions)
            {
                sub.UpdateWithBeaconInfo(preparedList);
            }
        }
    }

    private void AddToEmptySlot(ListenerSubscriptionHandler handler)
    {
        for (int i = 0; i < subscriptions.Count; i++)
        {
            if (subscriptions[i].Disposed)
            {
                subscriptions[i] = handler;
                return;
            }
        }

        subscriptions.Add(handler);
    }
}
