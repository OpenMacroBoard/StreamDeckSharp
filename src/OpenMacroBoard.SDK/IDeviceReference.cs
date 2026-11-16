namespace OpenMacroBoard.SDK;

/// <summary>
/// A handle that can be used to open an <see cref="IMacroBoard"/> instance.
/// </summary>
public interface IDeviceReference
{
    /// <summary>
    /// A user friendly display name.
    /// </summary>
    /// <remarks>
    /// <para>The device name is not part of the equality for a device reference handle. This means, that
    /// there can be two <see cref="IDeviceReference"/>s with different names which are still
    /// considered to be equal, because they refer to the same device.</para>
    /// </remarks>
    string DeviceName { get; set; }

    /// <summary>
    /// Gets the key layout for the referenced device.
    /// </summary>
    IKeyLayout Keys { get; }

    /// <summary>
    /// Connects to a macro board and returns an <see cref="IMacroBoard"/> instance
    /// which can be used to interact with the board.
    /// </summary>
    IMacroBoard Open();
}
