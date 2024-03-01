using HidSharp;
using OpenMacroBoard.SDK;
using StreamDeckSharp.Exceptions;
using StreamDeckSharp.Internals;
using System.Collections.Generic;
using System.Linq;

namespace StreamDeckSharp
{
    /// <summary>
    /// This is a factory class to create IStreamDeck References
    /// </summary>
    public static class StreamDeck
    {
        /// <summary>
        /// Enumerates connected Stream Decks and returns the first one.
        /// </summary>
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IMacroBoard OpenDevice(params IUsbHidHardware[] hardware)
        {
            return OpenDevice(true, hardware);
        }

        /// <summary>
        /// Enumerates connected Stream Decks and returns the first one.
        /// </summary>
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IMacroBoard OpenDevice(bool useWriteCache, params IUsbHidHardware[] hardware)
        {
            var dev = EnumerateDevices(hardware).FirstOrDefault() ?? throw new StreamDeckNotFoundException();
            return dev.Open();
        }

        /// <summary>
        /// Enumerates connected Stream Decks and returns the first one.
        /// </summary>
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IMacroBoard OpenDevice(string devicePath)
        {
            return OpenDevice(devicePath, true);
        }

        /// <summary>
        /// Get the Stream Deck with a given <paramref name="devicePath"/>.
        /// </summary>
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IMacroBoard OpenDevice(string devicePath, bool useWriteCache)
        {
            var dev = DeviceList.Local.GetHidDevices().First(d => d.DevicePath == devicePath);
            return FromHid(dev ?? throw new StreamDeckNotFoundException(), useWriteCache);
        }

        /// <summary>
        /// Enumerate Elgato Stream Deck Devices that match a given type.
        /// </summary>
        /// <param name="hardware">If no types or null is passed as argument, all known types are found</param>
        public static IEnumerable<StreamDeckDeviceReference> EnumerateDevices(params IUsbHidHardware[] hardware)
        {
            return DeviceList.Local.GetStreamDecks(hardware);
        }

        internal static IMacroBoard FromHid(HidDevice device, bool cached)
        {
            var hwInfo = device.GetHardwareInformation();
            var hidWrapper = new StreamDeckHidWrapper(device, hwInfo.Driver);

            if (cached)
            {
                return new CachedHidClient(hidWrapper, hwInfo.Keys, hwInfo.Driver);
            }

            return new BasicHidClient(hidWrapper, hwInfo.Keys, hwInfo.Driver);
        }
    }
}
