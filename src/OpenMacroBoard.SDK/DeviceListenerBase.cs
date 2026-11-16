using OpenMacroBoard.SDK.Internals;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OpenMacroBoard.SDK;

/// <summary>
/// A device listener base to simplify device listener implementations.
/// </summary>
public abstract class DeviceListenerBase : IObservable<DeviceStateReport>
{
    private readonly Lock sync = new();

    private readonly List<Subscription> subscriptions = [];
    private readonly List<KnownDeviceInternal> knownDevices;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceListenerBase"/> class.
    /// </summary>
    protected DeviceListenerBase()
    {
        knownDevices = [];
        KnownDevices = knownDevices.AsReadOnly();
    }

    /// <summary>
    /// Gets a list of the currently known (at least seen once) devices.
    /// </summary>
    public IReadOnlyList<IKnownDevice> KnownDevices { get; }

    /// <summary>
    /// Subscribes an observer that will be notified when a device state changes.
    /// </summary>
    /// <returns>Returns a disposable subscription.</returns>
    public IDisposable Subscribe(IObserver<DeviceStateReport> observer)
    {
        lock (sync)
        {
            // send currently known values
            foreach (var device in knownDevices)
            {
                observer.OnNext(new DeviceStateReport(device.DeviceReference, device.Connected, true));
            }

            // setup subscription for updates
            var sub = new Subscription(this, observer);
            subscriptions.Add(sub);
            return sub;
        }
    }

    /// <summary>
    /// Updates a device state. If the state has changed compared to the previous state all subscribed
    /// observers will be notified.
    /// </summary>
    /// <param name="deviceReference">A referenced device which has changed.</param>
    /// <param name="connected">The current connection state of the device.</param>
    protected void Update(IDeviceReference deviceReference, bool connected)
    {
        lock (sync)
        {
            var foundIndex = knownDevices.FindIndex(d => d.DeviceReference.Equals(deviceReference));
            var isNew = foundIndex < 0;

            if (isNew)
            {
                knownDevices.Add(new KnownDeviceInternal(deviceReference, connected));
                foundIndex = knownDevices.Count - 1;
            }

            var knownDevice = knownDevices[foundIndex];
            knownDevice.Connected = connected;

            foreach (var subscription in subscriptions)
            {
                subscription.SendUpdates();
            }
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly DeviceListenerBase parent;
        private readonly IObserver<DeviceStateReport> observer;

        /// <summary>
        /// Contains the state the subscriber knows about.
        /// This is used to calculate new updates.
        /// </summary>
        private readonly List<bool> subscriberState = [];

        public Subscription(DeviceListenerBase parent, IObserver<DeviceStateReport> observer)
        {
            ArgumentNullException.ThrowIfNull(parent);
            ArgumentNullException.ThrowIfNull(observer);

            this.parent = parent;
            this.observer = observer;
        }

        public void SendUpdates()
        {
            // send updates for existing devices
            for (int i = 0; i < subscriberState.Count; i++)
            {
                var device = parent.knownDevices[i];

                if (device.Connected != subscriberState[i])
                {
                    // report new connection state
                    observer.OnNext(new DeviceStateReport(device.DeviceReference, device.Connected, false));
                    subscriberState[i] = device.Connected;
                }
            }

            // add and send updates for new (to this subscriber) devices.
            for (int i = subscriberState.Count; i < parent.knownDevices.Count; i++)
            {
                var device = parent.knownDevices[i];
                subscriberState.Add(device.Connected);
                observer.OnNext(new DeviceStateReport(device.DeviceReference, device.Connected, true));
            }
        }

        public void Dispose()
        {
            lock (parent.sync)
            {
                parent.subscriptions.Remove(this);
            }
        }
    }
}
