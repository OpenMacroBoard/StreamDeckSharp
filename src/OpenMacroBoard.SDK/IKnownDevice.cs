namespace OpenMacroBoard.SDK;

/// <summary>
/// A <see cref="IDeviceReference"/> managed by a <see cref="IDeviceContext"/>
/// with additional meta data (like the connected state).
/// </summary>
public interface IKnownDevice : IDeviceReference
{
    /// <summary>
    /// Gets a value that indicates the current connection state.
    /// True if the device is currently connected, false if the device is disconnected.
    /// </summary>
    bool Connected { get; }
}
