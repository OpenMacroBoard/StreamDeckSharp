using System;

namespace StreamDeckSharp
{
    public class StreamDeckConnectionEventArgs : EventArgs
    {
        public bool NewConnectionState { get; }

        public StreamDeckConnectionEventArgs(bool newConnectionState)
        {
            NewConnectionState = newConnectionState;
        }
    }
}
