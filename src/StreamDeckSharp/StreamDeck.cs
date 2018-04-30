using HidLibrary;
using StreamDeckSharp.Exceptions;
using System.Collections.Generic;
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
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IStreamDeck OpenDevice()
        {
            var dev = EnumerateDevices().FirstOrDefault();
            return dev?.Open() ?? throw new StreamDeckNotFoundException();
        }

        /// <summary>
        /// Get <see cref="IStreamDeck"/> with given <paramref name="devicePath"/>
        /// </summary>
        /// <param name="devicePath"></param>
        /// <returns><see cref="IStreamDeck"/> specified by <paramref name="devicePath"/></returns>
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IStreamDeck OpenDevice(string devicePath)
        {
            var dev = HidDevices.GetDevice(devicePath);
            return new HidClient(dev ?? throw new StreamDeckNotFoundException());
        }

        /// <summary>
        /// Enumerates all available StreamDeck devices
        /// </summary>
        /// <returns>Returns <see cref="DeviceInfo"/> for every StreamDeck device found</returns>
        public static IEnumerable<DeviceInfo> EnumerateDevices()
        {
            var hidDevices = HidDevices.Enumerate(
                HidCommunicationHelper.VendorId,
                HidCommunicationHelper.ProductId
            );

            foreach (var d in hidDevices)
            {
                using (d)
                {
                    yield return new DeviceInfo(d.DevicePath);
                }
            }
        }
    }
}
