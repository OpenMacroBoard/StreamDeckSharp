using OpenMacroBoard.SDK;

namespace StreamDeckSharp
{
    /// <inheritdoc />
    public interface IStreamDeckBoard
        : IMacroBoard
    {
        /// <inheritdoc />
        new GridKeyPositionCollection Keys { get; }

        /// <summary>
        /// Gets the firmware version
        /// </summary>
        /// <returns>Returns the firmware version as string</returns>
        string GetFirmwareVersion();

        /// <summary>
        /// Gets the serial number
        /// </summary>
        /// <returns>Returns the serial number as string</returns>
        string GetSerialNumber();
    }
}
