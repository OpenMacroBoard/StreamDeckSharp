using OpenMacroBoard.SDK;

namespace StreamDeckSharp
{
    /// <inheritdoc />
    public interface IStreamDeckRefHandle : IDeviceReferenceHandle
    {
        /// <inheritdoc />
        new IStreamDeckBoard Open();
        string DevicePath { get; }
        string DeviceName { get; }
        bool UseWriteCache { get; set; }
    }
}
