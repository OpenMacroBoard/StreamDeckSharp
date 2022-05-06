using OpenMacroBoard.SDK;

namespace StreamDeckSharp
{
    /// <summary>
    /// A device reference pointing to a stream deck.
    /// </summary>
    public sealed class StreamDeckDeviceReference : IDeviceReference
    {
        internal StreamDeckDeviceReference(
            string devicePath,
            string deviceName,
            GridKeyLayout keyLayout
        )
        {
            DevicePath = devicePath;
            DeviceName = deviceName;
            Keys = keyLayout;
        }

        /// <summary>
        /// Gets the OSes unique identifier for human interface device.
        /// </summary>
        public string DevicePath { get; }

        /// <inheritdoc/>
        public string DeviceName { get; set; }

        /// <inheritdoc/>
        public IKeyLayout Keys { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return DeviceName;
        }

        /// <inheritdoc/>
        public IMacroBoard Open(bool useWriteCache)
        {
            return StreamDeck.OpenDevice(DevicePath, useWriteCache);
        }

        /// <inheritdoc/>
        public IMacroBoard Open()
        {
            return StreamDeck.OpenDevice(DevicePath);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return DevicePath.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj is not StreamDeckDeviceReference other)
            {
                return false;
            }

            if (other.DevicePath != DevicePath)
            {
                return false;
            }

            return true;
        }
    }
}
