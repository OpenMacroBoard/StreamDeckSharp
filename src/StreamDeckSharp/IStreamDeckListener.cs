using System;
using System.Collections.Generic;

namespace StreamDeckSharp
{
    /// <summary>
    /// A listener and cache for known devices.
    /// </summary>
    public interface IStreamDeckListener : IDisposable
    {
        /// <summary>
        /// Fires when a recently connected device is seen for the first time.
        /// </summary>
        /// <remarks>
        /// This event is never raised for disconnects because a detected disconnet
        /// implies that the device was already known and only unknown devices are reported.
        /// </remarks>
        event EventHandler<StreamDeckConnectionChangedEventArgs> NewDeviceConnected;

        /// <summary>
        /// Fires when a stream deck connects or disconnects regardless if known or not.
        /// </summary>
        event EventHandler<StreamDeckConnectionChangedEventArgs> ConnectionChanged;

        /// <summary>
        /// A list of known devices.
        /// </summary>
        /// <remarks>
        /// The order of the list is consistant and new devices are always added at the end.
        /// </remarks>
        IReadOnlyList<IStreamDeckRefHandle> KnownStreamDecks { get; }
    }
}
