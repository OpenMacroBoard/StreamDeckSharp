using OpenMacroBoard.SDK;

#pragma warning disable AV1000 // Type name contains the word 'and', which suggests it has multiple purposes

namespace StreamDeckSharp.Internals
{
    internal sealed class UsbHardwareIdAndDriver : IUsbHidHardware
    {
        public UsbHardwareIdAndDriver(
            UsbVendorProductPair usbId,
            string deviceName,
            GridKeyLayout keys,
            IStreamDeckHidComDriver driver
        )
        {
            UsbId = usbId;
            DeviceName = deviceName;
            Keys = keys;
            Driver = driver;
        }

        public UsbVendorProductPair UsbId { get; }
        public string DeviceName { get; }
        public GridKeyLayout Keys { get; }
        public IStreamDeckHidComDriver Driver { get; }
    }
}
