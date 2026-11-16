using System;

namespace OpenMacroBoard.SDK;

/// <summary>
/// Is used for events that communicate connection changes.
/// </summary>
public class ConnectionEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionEventArgs"/> class.
    /// </summary>
    public ConnectionEventArgs(bool newConnectionState)
    {
        NewConnectionState = newConnectionState;
    }

    /// <summary>
    /// The new connection state.
    /// </summary>
    public bool NewConnectionState { get; }
}
