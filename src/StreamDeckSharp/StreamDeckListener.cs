using HidSharp;
using OpenMacroBoard.SDK;
using StreamDeckSharp.Internals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StreamDeckSharp
{
    /// <summary>
    /// A listener for stream deck devices.
    /// </summary>
    public sealed class StreamDeckListener :
        IDisposable,
        IObservable<DeviceStateReport>
    {
        private readonly object sync = new();
        private readonly List<DeviceState> knownDevices = new();
        private readonly List<Subscription> subscriptions = new();
        private readonly Dictionary<string, StreamDeckDeviceReference> knownDeviceLookup = new();

        private bool disposed = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamDeckListener"/> class.
        /// </summary>
        public StreamDeckListener()
        {
            // register event handler before we load the entire list
            // so we don't miss stream decks connecting between the calls.
            DeviceList.Local.Changed += DeviceListChanged;

            // initial force load
            ProcessDelta();
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<DeviceStateReport> observer)
        {
            var subscription = new Subscription(this, observer);
            subscriptions.Add(subscription);
            subscription.SendUpdates();
            return subscription;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            DeviceList.Local.Changed -= DeviceListChanged;
        }

        private void ProcessDelta()
        {
            lock (sync)
            {
                // because the HidDevice event doesn't tell us what changed
                // we calculate the difference ourselves.
                var currentDevices = DeviceList.Local
                    .GetStreamDecks()
                    .ToDictionary(s => s.DevicePath, s => s);

                // update connection states of known devices
                foreach (var knownDevice in knownDevices)
                {
                    knownDevice.Connected = currentDevices.ContainsKey(knownDevice.DeviceReference.DevicePath);
                }

                // add new devices
                foreach (var currentDevice in currentDevices)
                {
                    if (knownDeviceLookup.ContainsKey(currentDevice.Key))
                    {
                        // skip because this one is already known
                        continue;
                    }

                    knownDeviceLookup.Add(currentDevice.Key, currentDevice.Value);
                    knownDevices.Add(new DeviceState(currentDevice.Value, true));
                }

                // send updates to all subscribers
                foreach (var subscription in subscriptions)
                {
                    subscription.SendUpdates();
                }
            }
        }

        private void DeviceListChanged(object sender, DeviceListChangedEventArgs e)
        {
            ProcessDelta();
        }

        private sealed class DeviceState
        {
            public DeviceState(StreamDeckDeviceReference deviceReference, bool connected)
            {
                DeviceReference = deviceReference ?? throw new ArgumentNullException(nameof(deviceReference));
                Connected = connected;
            }

            public StreamDeckDeviceReference DeviceReference { get; }
            public bool Connected { get; set; }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly StreamDeckListener parent;
            private readonly IObserver<DeviceStateReport> observer;

            /// <summary>
            /// Contains the state the subscriber knows about.
            /// This is used to calculate new updates.
            /// </summary>
            private readonly List<bool> subscriberState = new();

            public Subscription(StreamDeckListener parent, IObserver<DeviceStateReport> observer)
            {
                this.parent = parent ?? throw new ArgumentNullException(nameof(parent));
                this.observer = observer ?? throw new ArgumentNullException(nameof(observer));
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
}
