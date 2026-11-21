using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace OpenMacroBoard.SocketIO.Internals;

internal sealed class ListenerSubscriptionHandler : IDisposable
{
    private readonly Lock sync = new();

    private readonly IObserver<DeviceStateReport> observer;
    private readonly Dictionary<IPEndPoint, CurrentDeviceState> knownDevices = [];

    public ListenerSubscriptionHandler(IObserver<DeviceStateReport> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        this.observer = observer;
    }

    public bool Disposed { get; private set; }

    public void UpdateWithBeaconInfo(IReadOnlyCollection<DecodedBeaconLocation> beaconInfos)
    {
        lock (sync)
        {
            if (Disposed)
            {
                return;
            }

            var now = DateTimeOffset.UtcNow;

            foreach (var beaconInfo in beaconInfos)
            {
                if (knownDevices.TryGetValue(beaconInfo.BeaconLocation.Address, out var deviceInfos))
                {
                    var wasAlreadyConnected = deviceInfos.Connected;
                    deviceInfos.Connected = true;
                    deviceInfos.LastSeen = now;

                    if (!wasAlreadyConnected)
                    {
                        var report = new DeviceStateReport(deviceInfos.DeviceReference, true, false);
                        observer.OnNext(report);
                    }
                }
                else
                {
                    // not found -> new device
                    var deviceName = beaconInfo.MetaData.DeviceName;

                    var keys = beaconInfo.MetaData.Keys;
                    var keyLayout = new GridKeyLayout(keys.CountX, keys.CountY, keys.KeySize, keys.GapSize);

                    var devRef = new SocketIODeviceReference(
                        deviceName,
                        beaconInfo.BeaconLocation.Address,
                        keyLayout
                    );

                    var currentStatus = new CurrentDeviceState(devRef)
                    {
                        Connected = true,
                        LastSeen = now,
                    };

                    knownDevices.Add(beaconInfo.BeaconLocation.Address, currentStatus);

                    var report = new DeviceStateReport(devRef, true, true);
                    observer.OnNext(report);
                }
            }

            // "disconnect" devices we haven't seen for a while
            foreach (var device in knownDevices.Values)
            {
                if (device.Connected && (now - device.LastSeen).TotalSeconds > 5.0)
                {
                    device.Connected = false;
                    var report = new DeviceStateReport(device.DeviceReference, false, false);
                    observer.OnNext(report);
                }
            }
        }
    }

    public void Dispose()
    {
        lock (sync)
        {
            if (Disposed)
            {
                return;
            }

            observer.OnCompleted();
            Disposed = true;
        }
    }

    private sealed class CurrentDeviceState
    {
        public CurrentDeviceState(SocketIODeviceReference deviceReference)
        {
            ArgumentNullException.ThrowIfNull(deviceReference);
            DeviceReference = deviceReference;
        }

        public SocketIODeviceReference DeviceReference { get; }
        public DateTimeOffset LastSeen { get; set; }
        public bool Connected { get; set; }
    }
}
