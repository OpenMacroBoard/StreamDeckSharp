using HidLibrary;
using StreamDeckSharp.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// <exception cref="StreamDeckSharp.Exceptions.StreamDeckNotFoundException">Thrown when no Stream Deck is found</exception>
        public static IStreamDeck FromHID()
        {
            var dev = HidDevices.Enumerate(
                StreamDeckCom.VendorId,
                StreamDeckCom.ProductId
            ).FirstOrDefault();

            if (dev == null)
                throw new StreamDeckNotFoundException();

            return new StreamDeckHID(dev);
        }

        /// <summary>
        /// Get <see cref="IStreamDeck"/> with given <paramref name="devicePath"/>
        /// </summary>
        /// <param name="devicePath"></param>
        /// <returns><see cref="IStreamDeck"/> specified by <paramref name="devicePath"/></returns>
        public static IStreamDeck FromHID(string devicePath)
        {
            var dev = HidDevices.GetDevice(devicePath);

            if (dev == null)
                throw new StreamDeckNotFoundException();

            return new StreamDeckHID(dev);
        }
    }
}
