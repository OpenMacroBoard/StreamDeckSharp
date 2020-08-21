using HidSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StreamDeckSharp.Internals
{
    internal static class DeviceListExtensions
    {
        public static IEnumerable<DeviceReferenceHandle> GetStreamDecks(this DeviceList deviceList, params IUsbHidHardware[] hardware)
        {
            if (deviceList is null)
            {
                throw new ArgumentNullException(nameof(deviceList));
            }

            var matchAllKnowDevices = hardware is null || hardware.Length < 1;

            IHardwareInternalInfos MatchingHardware(HidDevice d)
            {
                var hwDetails = d.GetHardwareInformation();

                if (hwDetails is null)
                {
                    return null;
                }

                if (matchAllKnowDevices)
                {
                    return hwDetails;
                }

                foreach (var h in hardware)
                {
                    if (d.VendorID == h.UsbVendorId &&
                        d.ProductID == h.UsbProductId)
                    {
                        return hwDetails;
                    }
                }

                return null;
            }

            return deviceList
                    .GetHidDevices()
                    .Select(device => new { HardwareInfo = MatchingHardware(device), Device = device })
                    .Where(i => i.HardwareInfo != null)
                    .Select(i => new DeviceReferenceHandle(i.Device.DevicePath, i.HardwareInfo.DeviceName));
        }
    }
}
