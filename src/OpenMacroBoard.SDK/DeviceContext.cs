using OpenMacroBoard.SDK.Internals;

namespace OpenMacroBoard.SDK;

/// <summary>
/// A collection of <see cref="IDeviceContext"/> released methods.
/// </summary>
public static class DeviceContext
{
    /// <summary>
    /// Creates a new device context (without any listeners).
    /// </summary>
    /// <returns>A new device context.</returns>
    public static IDeviceContext Create()
    {
        return new DeviceContextInternal();
    }
}
