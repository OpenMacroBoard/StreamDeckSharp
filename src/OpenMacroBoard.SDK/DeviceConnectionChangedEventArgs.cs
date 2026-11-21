using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// An event argument that reports a connection status change for a particular device.
/// </summary>
public class DeviceConnectionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceConnectionChangedEventArgs"/> class.
    /// </summary>
    /// <param name="deviceReference">A device reference.</param>
    /// <param name="connected">The current connection state.</param>
    public DeviceConnectionChangedEventArgs(IDeviceReference deviceReference, bool connected)
    {
        ArgumentNullException.ThrowIfNull(deviceReference);

        DeviceReference = deviceReference;
        Connected = connected;
    }

    /// <summary>
    /// Gets a handle to the device that changed.
    /// </summary>
    public IDeviceReference DeviceReference { get; }

    /// <summary>
    /// Gets a value that indicates the connection state change. True if the device got connected,
    /// false if the device got disconnected.
    /// </summary>
    public bool Connected { get; }
}
