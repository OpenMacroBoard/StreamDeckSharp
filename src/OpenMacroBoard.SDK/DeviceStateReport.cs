using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// A device state report.
/// </summary>
public class DeviceStateReport
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceStateReport"/> class.
    /// </summary>
    /// <param name="deviceReference">The device.</param>
    /// <param name="connected">The connection state.</param>
    /// <param name="newDevice">Info about if the device is new or not.</param>
    public DeviceStateReport(IDeviceReference deviceReference, bool connected, bool newDevice)
    {
        ArgumentNullException.ThrowIfNull(deviceReference);

        DeviceReference = deviceReference;
        Connected = connected;
        NewDevice = newDevice;
    }

    /// <summary>
    /// Gets the device reference.
    /// </summary>
    public IDeviceReference DeviceReference { get; }

    /// <summary>
    /// Gets the connection state.
    /// </summary>
    public bool Connected { get; }

    /// <summary>
    /// Gets the info if this device is new or not.
    /// </summary>
    public bool NewDevice { get; }
}
