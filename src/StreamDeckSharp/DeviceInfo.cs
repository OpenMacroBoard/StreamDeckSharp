using HidLibrary;
using OpenMacroBoard.SDK;

namespace StreamDeckSharp
{
    /// <summary>
    /// Device information about Stream Deck
    /// </summary>
    public class DeviceRefereceHandle : IDeviceReferenceHandle
    {
        internal DeviceRefereceHandle(string devicePath)
        {
            DevicePath = devicePath;
        }

        /// <summary>
        /// Unique identifier for human interface device
        /// </summary>
        public string DevicePath { get; }

        /// <summary>
        /// Opens the StreamDeck handle
        /// </summary>
        /// <returns>Returns an <see cref="IMacroBoard"/> reference</returns>
        public IMacroBoard Open() => StreamDeck.OpenDevice(DevicePath);
    }
}
