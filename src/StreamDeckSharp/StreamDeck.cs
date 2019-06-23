using HidSharp;
using StreamDeckSharp.Exceptions;
using StreamDeckSharp.Internals;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("StreamDeckSharp.Tests")]

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
        /// <returns>The default <see cref="IStreamDeckBoard"/> HID</returns>
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IStreamDeckBoard OpenDevice(params IUsbHidHardware[] hardware)
        {
            var dev = EnumerateDevices(hardware).FirstOrDefault();

            if (dev is null)
                throw new StreamDeckNotFoundException();

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
            var matchAllKnowDevices = hardware is null || hardware.Length < 1;

            IHardwareInternalInfos MatchingHardware(HidDevice d)
            {
                var hwDetails = d.GetHardwareInformation();

                if (hwDetails is null)
                    return null;

                if (matchAllKnowDevices)
                    return hwDetails;

                foreach (var h in hardware)
                {
                    if (d.VendorID == h.UsbVendorId &&
                        d.ProductID == h.UsbProductId)
                        return hwDetails;
                }

                return null;
            }

            

            return DeviceList.Local
                    .GetHidDevices()
                    .Select(device => new { HardwareInfo = MatchingHardware(device), Device = device })
                    .Where(i => i.HardwareInfo != null)
                    .Select(i => new DeviceReferenceHandle(i.Device.DevicePath, i.HardwareInfo.DeviceName));
        }

        internal static IStreamDeckBoard FromHid(HidDevice device, bool cached)
        {
            var hidWrapper = new StreamDeckHidWrapper(device);
            var hwInfo = device.GetHardwareInformation();

            if (cached)
                return new CachedHidClient(hidWrapper, hwInfo);
            else
                return new BasicHidClient(hidWrapper, hwInfo);
        }
    }
}
