using OpenMacroBoard.SDK;

namespace StreamDeckSharp
{
    /// <summary>
    /// Device information about Stream Deck
    /// </summary>
    internal class DeviceReferenceHandle : IStreamDeckRefHandle
    {
        internal DeviceReferenceHandle(string devicePath, string deviceName)
        {
            DevicePath = devicePath;
            DeviceName = deviceName;
        }

        /// <summary>
        /// Unique identifier for human interface device
        /// </summary>
        public string DevicePath { get; }
        public string DeviceName { get; }
        public bool UseWriteCache { get; set; } = true;

        public override string ToString()
            => DeviceName;

        /// <summary>
        /// Opens the StreamDeck handle
        /// </summary>
        /// <returns>Returns an <see cref="IMacroBoard"/> reference</returns>
        public IStreamDeckBoard Open()
            => StreamDeck.OpenDevice(DevicePath, UseWriteCache);

        IMacroBoard IDeviceReferenceHandle.Open()
            => Open();
    }
}
