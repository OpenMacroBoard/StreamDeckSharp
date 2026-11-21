using System;

namespace OpenMacroBoard.SDK.Internals;

internal sealed class MergedDeviceListener :
    DeviceListenerBase,
    IObserver<DeviceStateReport>
{
    public void OnCompleted()
    {
        // Intentionally empty.
        // When a device listener completes (which it shouldn't), we don't care.
    }

    public void OnError(Exception error)
    {
        // Intentionally empty.
        // When a device listener reports an error (which it shouldn't), we don't care.
    }

    public void OnNext(DeviceStateReport value)
    {
        Update(value.DeviceReference, value.Connected);
    }
}
