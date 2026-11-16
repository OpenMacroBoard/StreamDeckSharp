using HidSharp;
using OpenMacroBoard.SDK.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StreamDeckSharp.Internals;

internal static class DeviceListExtensions
{
    public static IEnumerable<StreamDeckDeviceReference> GetStreamDecks(
        this DeviceList deviceList,
        StreamDeckOptions options,
        params IUsbHidHardware[]? hardware
    )
    {
        ArgumentNullException.ThrowIfNull(deviceList);

        (bool Success, UsbHardwareIdAndDriver Hardware) MatchingHardware(HidDevice d)
        {
            var hwDetails = d.GetHardwareInformation();

            if (hwDetails is null)
            {
                return (false, default!);
            }

            if (hardware is null || hardware.Length < 1)
            {
                return (true, hwDetails);
            }

            var deviceUsbKey = new UsbVendorProductPair(d.VendorID, d.ProductID);
            var hardwareMatches = hardware.Any(h => h.UsbIds.Contains(deviceUsbKey));

            return (
                hardwareMatches,
                hardwareMatches ? hwDetails : default!
            );
        }

        return deviceList
            .GetHidDevices()
            .SelectWhere(device =>
            {
                var (success, hardware) = MatchingHardware(device);
                var value = success ? new { HardwareInfo = hardware, Device = device } : default!;

                return (success, value);
            })
            .Select(i => new StreamDeckDeviceReference(
                i.Device.DevicePath,
                i.HardwareInfo.DeviceName,
                i.HardwareInfo.Keys,
                options
            ));
    }
}
