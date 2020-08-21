using System;

namespace StreamDeckSharp
{
    /// <summary>
    /// An event argument that reports a connection status change for a particular device.
    /// </summary>
    public class StreamDeckConnectionChangedEventArgs : EventArgs
    {
        public StreamDeckConnectionChangedEventArgs(IStreamDeckRefHandle deckHandle, bool connected)
        {
            DeckHandle = deckHandle ?? throw new ArgumentNullException(nameof(deckHandle));
            Connected = connected;
        }

        /// <summary>
        /// Gets a handle to the device that changed.
        /// </summary>
        public IStreamDeckRefHandle DeckHandle { get; }

        /// <summary>
        /// Gets a value that indicates the connection state change. True if the device got connected,
        /// false if the device got disconnected.
        /// </summary>
        public bool Connected { get; }
    }
}
