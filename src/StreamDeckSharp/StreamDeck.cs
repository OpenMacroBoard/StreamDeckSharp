using HidLibrary;
using StreamDeckSharp.Exceptions;
using System.Linq;

namespace StreamDeckSharp
{
    /// <summary>
    /// This is a factorioy class to create IStreamDeck References
    /// </summary>
    public static class StreamDeck
    {
        /// <summary>
        /// Enumerates connected Stream Decks and returns the first one.
        /// </summary>
        /// <returns>The default <see cref="IStreamDeck"/> HID</returns>
        /// <exception cref="StreamDeckNotFoundException">Thrown when no Stream Deck is found</exception>
        public static IStreamDeck OpenDevice()
        {
            var dev = HidDevices.Enumerate(
                HidCommunicationHelper.VendorId,
                HidCommunicationHelper.ProductId
            ).FirstOrDefault();

            if (dev == null)
                throw new StreamDeckNotFoundException();

            return new HidClient(dev);
        }

        /// <summary>
        /// Get <see cref="IStreamDeck"/> with given <paramref name="devicePath"/>
        /// </summary>
        /// <param name="devicePath"></param>
        /// <returns><see cref="IStreamDeck"/> specified by <paramref name="devicePath"/></returns>
        public static IStreamDeck OpenDevice(string devicePath)
        {
            var dev = HidDevices.GetDevice(devicePath);

            if (dev == null)
                throw new StreamDeckNotFoundException();

            return new HidClient(dev);
        }
    }
}
