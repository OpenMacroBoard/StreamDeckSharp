using System;

namespace OpenMacroBoard.SDK
{
    /// <summary>
    /// Is used for events that communicate connection changes.
    /// </summary>
    public class ConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// The new connection state.
        /// </summary>
        public bool NewConnectionState { get; }

        /// <summary>
        /// Instantiates a new <see cref="ConnectionEventArgs"/> object.
        /// </summary>
        /// <param name="newConnectionState"></param>
        public ConnectionEventArgs(bool newConnectionState)
        {
            NewConnectionState = newConnectionState;
        }
    }
}
