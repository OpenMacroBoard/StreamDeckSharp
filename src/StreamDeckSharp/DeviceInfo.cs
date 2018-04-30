using HidLibrary;

namespace StreamDeckSharp
{
    /// <summary>
    /// Device information about Stream Deck
    /// </summary>
    public class DeviceInfo
    {
        internal DeviceInfo(string devicePath)
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
        /// <returns>Returns an <see cref="IStreamDeck"/> reference</returns>
        public IStreamDeck Open() => StreamDeck.OpenDevice(DevicePath);
    }
}
