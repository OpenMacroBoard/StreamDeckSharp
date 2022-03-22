using OpenMacroBoard.SDK;

namespace StreamDeckSharp
{
    /// <summary>
    /// A compact collection of hardware specific information about a device.
    /// </summary>
    public interface IHardware
    {
        /// <summary>
        /// Key layout information
        /// </summary>
        GridKeyLayout Keys { get; }

        /// <summary>
        /// Name of the device
        /// </summary>
        string DeviceName { get; }
    }
}
