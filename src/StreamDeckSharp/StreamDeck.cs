using HidSharp;
using StreamDeckSharp.Exceptions;
using StreamDeckSharp.Internals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace StreamDeckSharp
{
    /// <summary>
    /// This is a factorioy class to create IStreamDeck References
    /// </summary>
    public static class StreamDeck
    {
        /// <summary>
        /// Creates a listener and cache of devices that updates while devices connect or disconnet.
        /// </summary>
        /// <param name="hardwareFilter">A set of devices the listener uses as a whitelist filter.</param>
        /// <remarks>
        /// Make sure to properly dispose the listener. Not disposing can lead to memory leaks.
        /// </remarks>
        /// <returns>Returns the listener.</returns>
        public static IStreamDeckListener CreateDeviceListener(params IUsbHidHardware[] hardwareFilter)
        {
            return new StreamDeckListener(hardwareFilter);
        }

        /// <summary>
        /// Enumerates connected Stream Decks and returns the first one.
        /// </summary>
        /// <param name="hardware"></param>
        /// <returns>The default <see cref="IStreamDeckBoard"/> HID</returns>
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IStreamDeckBoard OpenDevice(params IUsbHidHardware[] hardware)
        {
            var dev = EnumerateDevices(hardware).FirstOrDefault();

            if (dev is null)
            {
                throw new StreamDeckNotFoundException();
            }

            return dev.Open();
        }

        /// <summary>
        /// Get <see cref="IStreamDeckBoard"/> with given <paramref name="devicePath"/>
        /// </summary>
        /// <param name="devicePath"></param>
        /// <param name="useWriteCache"></param>
        /// <returns><see cref="IStreamDeckBoard"/> specified by <paramref name="devicePath"/></returns>
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IStreamDeckBoard OpenDevice(string devicePath, bool useWriteCache = true)
        {
            var dev = DeviceList.Local.GetHidDevices().Where(d => d.DevicePath == devicePath).First();
            return FromHid(dev ?? throw new StreamDeckNotFoundException(), useWriteCache);
        }

        /// <summary>
        /// Enumerate Elgato Stream Deck Devices that match a given type.
        /// </summary>
        /// <param name="hardware">If no types or null is passed passed as argument, all known types are found</param>
        /// <returns></returns>
        public static IEnumerable<IStreamDeckRefHandle> EnumerateDevices(params IUsbHidHardware[] hardware)
        {
            return DeviceList.Local.GetStreamDecks(hardware);
        }

        internal static IStreamDeckBoard FromHid(HidDevice device, bool cached)
        {
            var hidWrapper = new StreamDeckHidWrapper(device);
            var hwInfo = device.GetHardwareInformation();

            if (cached)
            {
                return new CachedHidClient(hidWrapper, hwInfo);
            }
            else
            {
                return new BasicHidClient(hidWrapper, hwInfo);
            }
        }
    }
}
