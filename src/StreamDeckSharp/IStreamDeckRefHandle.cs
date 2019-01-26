using OpenMacroBoard.SDK;

namespace StreamDeckSharp
{
    /// <inheritdoc />
    public interface IStreamDeckRefHandle : IDeviceReferenceHandle
    {
        /// <inheritdoc />
        new IStreamDeckBoard Open();
    }
}
