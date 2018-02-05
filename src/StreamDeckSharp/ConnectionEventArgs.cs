using System;

namespace StreamDeckSharp
{
    public class ConnectionEventArgs : EventArgs
    {
        public bool NewConnectionState { get; }

        public ConnectionEventArgs(bool newConnectionState)
        {
            NewConnectionState = newConnectionState;
        }
    }
}
