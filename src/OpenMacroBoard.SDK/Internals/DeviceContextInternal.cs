using System;
using System.Collections.Generic;

namespace OpenMacroBoard.SDK.Internals;

internal sealed class DeviceContextInternal : IDeviceContext
{
    private readonly MergedDeviceListener mergedDeviceListener = new();
    private readonly List<IDisposable> disposeWithContext = [];
    private readonly List<KnownDeviceInternal> knownDevices = [];

    public DeviceContextInternal()
    {
        KnownDevices = knownDevices.AsReadOnly();

        DeviceStateReports = mergedDeviceListener;
        KnownDevices = mergedDeviceListener.KnownDevices;
    }

    public IReadOnlyList<IKnownDevice> KnownDevices { get; }
    public IObservable<DeviceStateReport> DeviceStateReports { get; }

    public void Dispose()
    {
        foreach (var disposable in disposeWithContext)
        {
            disposable.Dispose();
        }
    }

    public IDeviceContext AddListener(IObservable<DeviceStateReport> deviceListener)
    {
        return AddListener(deviceListener, true);
    }

    public IDeviceContext AddListener(IObservable<DeviceStateReport> deviceListener, bool disposeWithContext)
    {
        var subscription = deviceListener.Subscribe(mergedDeviceListener);
        this.disposeWithContext.Add(subscription);

        if (disposeWithContext && deviceListener is IDisposable disposableListener)
        {
            this.disposeWithContext.Add(disposableListener);
        }

        return this;
    }

    public IDeviceContext AddListener<TListener>()
        where TListener : IObservable<DeviceStateReport>, new()
    {
        var listener = new TListener();
        return AddListener(listener, true);
    }
}
