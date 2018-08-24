using System.Collections.Generic;
using System.Linq;
using HidLibrary;
using OpenMacroBoard.SDK;
using StreamDeckSharp.Exceptions;
using StreamDeckSharp.Internals;

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
        /// <returns>The default <see cref="IMacroBoard"/> HID</returns>
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IMacroBoard OpenDevice()
        {
            var dev = EnumerateDevices().FirstOrDefault();
            return dev?.Open() ?? throw new StreamDeckNotFoundException();
        }

        /// <summary>
        /// Get <see cref="IMacroBoard"/> with given <paramref name="devicePath"/>
        /// </summary>
        /// <param name="devicePath"></param>
        /// <returns><see cref="IMacroBoard"/> specified by <paramref name="devicePath"/></returns>
        /// <exception cref="StreamDeckNotFoundException">Thrown if no Stream Deck is found</exception>
        public static IMacroBoard OpenDevice(string devicePath)
        {
            var dev = HidDevices.GetDevice(devicePath);
            return CachedHidClient.FromHid(dev ?? throw new StreamDeckNotFoundException());
        }

        /// <summary>
        /// Enumerate Elgato Stream Deck Devices that match a given type.
        /// </summary>
        /// <param name="hardware">If no types or null is passed passed as argument, all known types are found</param>
        /// <returns></returns>
        public static IEnumerable<IDeviceReferenceHandle> EnumerateDevices(params IUsbHidHardware[] hardware)
        {
            var matchAllKnowDevices = hardware is null || hardware.Length < 1;

            bool DoesMatchHardware(HidDevice d)
            {
                if (!Hardware.IsKnownDevice(d.Attributes.VendorId, d.Attributes.ProductId))
                    return false;

                if (matchAllKnowDevices)
                    return true;

                foreach (var h in hardware)
                {
                    if (d.Attributes.VendorId == h.UsbVendorId &&
                        d.Attributes.ProductId == h.UsbProductId)
                        return true;
                }
                return false;
            }

            return HidDevices.Enumerate().Where(DoesMatchHardware).Select(d => new DeviceRefereceHandle(d.DevicePath));
        }
    }
}
