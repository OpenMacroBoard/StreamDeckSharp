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
        IKeyPositionCollection Keys { get; }
    }
}
