using HidLibrary;
using OpenMacroBoard.SDK;

namespace StreamDeckSharp
{
    /// <summary>
    /// Device information about Stream Deck
    /// </summary>
    public class DeviceRefereceHandle : IStreamDeckRefHandle
    {
        internal DeviceRefereceHandle(string devicePath)
            : this(devicePath, null)
        {
        }

        internal DeviceRefereceHandle(string devicePath, string deviceName)
        {
            DevicePath = devicePath;
            DeviceName = deviceName;
        }

        /// <summary>
        /// Unique identifier for human interface device
        /// </summary>
        public string DevicePath { get; }
        public string DeviceName { get; }

        public override string ToString()
            => DeviceName;

        /// <summary>
        /// Opens the StreamDeck handle
        /// </summary>
        /// <returns>Returns an <see cref="IMacroBoard"/> reference</returns>
        public IStreamDeckBoard Open() => StreamDeck.OpenDevice(DevicePath);

        IMacroBoard IDeviceReferenceHandle.Open() => Open();
    }
}
