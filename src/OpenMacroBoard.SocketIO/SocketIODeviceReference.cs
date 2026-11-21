using OpenMacroBoard.SDK;
using OpenMacroBoard.SocketIO.Internals;
using System;
using System.Net;

namespace OpenMacroBoard.SocketIO;

/// <summary>
/// An <see cref="IDeviceReference"/> for SocketIO devices.
/// </summary>
public sealed class SocketIODeviceReference : IDeviceReference, IEquatable<SocketIODeviceReference>
{
    private readonly GridKeyLayout keyLayout;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketIODeviceReference"/> class.
    /// </summary>
    /// <remarks>
    /// <para>Even though you can create an instance of this class yourself there are probably not a lot
    /// of use cases where you have to. Instead use <see cref="DeviceContext"/> and the
    /// <see cref="SocketIOBoardListener"/> to listen for SocketIO devices.</para>
    /// </remarks>
    /// <param name="deviceName">The user friendly name for the referenced device.</param>
    /// <param name="ipEndPoint">The IP endpoint where the referenced device is listening.</param>
    /// <param name="keyLayout">The keyboard layout of the device.</param>
    public SocketIODeviceReference(
        string deviceName,
        IPEndPoint ipEndPoint,
        GridKeyLayout keyLayout
    )
    {
        ArgumentNullException.ThrowIfNull(deviceName);
        ArgumentNullException.ThrowIfNull(ipEndPoint);
        ArgumentNullException.ThrowIfNull(keyLayout);

        DeviceName = deviceName;
        IPEndPoint = ipEndPoint;
        this.keyLayout = keyLayout;
    }

    /// <inheritdoc />
    public string DeviceName { get; set; }

    /// <summary>
    /// Gets the IP endpoint for the references SocketIO device.
    /// </summary>
    public IPEndPoint IPEndPoint { get; }

    /// <inheritdoc />
    public IKeyLayout Keys => keyLayout;

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return Equals(obj as SocketIODeviceReference);
    }

    /// <inheritdoc />
    public bool Equals(SocketIODeviceReference? other)
    {
        if (other is null)
        {
            return false;
        }

        return IPEndPoint.Equals(other.IPEndPoint);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return IPEndPoint.GetHashCode();
    }

    /// <inheritdoc />
    public IMacroBoard Open()
    {
        return new SocketIOMacroBoardClient(IPEndPoint, keyLayout);
    }
}
