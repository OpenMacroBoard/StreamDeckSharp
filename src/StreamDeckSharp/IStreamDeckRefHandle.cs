using OpenMacroBoard.SDK;

namespace StreamDeckSharp
{
    /// <inheritdoc />
    public interface IStreamDeckRefHandle
        : IDeviceReferenceHandle
    {
        /// <summary>
        /// The device path of the HID
        /// </summary>
        string DevicePath { get; }

        /// <summary>
        /// A friendly display name
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Determines if display write caching should be applied
        /// (true is default and recommended)
        /// </summary>
        bool UseWriteCache { get; set; }

        /// <inheritdoc />
        new IStreamDeckBoard Open();
    }
}
