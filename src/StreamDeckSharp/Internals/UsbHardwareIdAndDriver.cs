using OpenMacroBoard.SDK;
using System.Collections.Generic;

#pragma warning disable AV1000 // Type name contains the word 'and', which suggests it has multiple purposes

namespace StreamDeckSharp.Internals
{
    internal sealed class UsbHardwareIdAndDriver : IUsbHidHardware
    {
        public UsbHardwareIdAndDriver(
            IReadOnlyList<UsbVendorProductPair> usbIds,
            string deviceName,
            GridKeyLayout keys,
            IStreamDeckHidComDriver driver
        )
        {
            UsbIds = usbIds;
            DeviceName = deviceName;
            Keys = keys;
            Driver = driver;
        }

        public IReadOnlyList<UsbVendorProductPair> UsbIds { get; }
        public string DeviceName { get; }
        public GridKeyLayout Keys { get; }
        public IStreamDeckHidComDriver Driver { get; }
    }
}
