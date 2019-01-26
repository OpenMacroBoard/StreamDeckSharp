using OpenMacroBoard.SDK;

namespace StreamDeckSharp
{
    /// <inheritdoc />
    public interface IStreamDeckBoard : IMacroBoard
    {
        /// <inheritdoc />
        new GridKeyPositionCollection Keys { get; }
    }
}
